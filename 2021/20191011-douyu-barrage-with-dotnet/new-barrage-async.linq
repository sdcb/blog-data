<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Linq.Async</NuGetReference>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System.Buffers</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Net.WebSockets</Namespace>
  <Namespace>System.Runtime.CompilerServices</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main(string[] args)
{
	var dc = new DumpContainer().Dump();
	await DouyuBarrage.ChatMessageFromUrl("https://www.douyu.com/74751")
		.ForEachAwaitWithCancellationAsync((x, c) =>
		{
			dc.Content = x;
			return Task.FromResult(0);
		}, QueryCancelToken);
}

public class DouyuBarrage
{
    static HttpClient http = new HttpClient();

    public static async IAsyncEnumerable<string> RawFromUrl(string url, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        HttpResponseMessage html = await http.GetAsync(url, cancellationToken);
        var roomId = Regex.Match(await html.Content.ReadAsStringAsync(), @"\$ROOM.room_id[ ]?=[ ]?(\d+);").Groups[1].Value;

        using var ws = new ClientWebSocket();
		ws.Options.AddSubProtocol("-");
        await ws.ConnectAsync(new Uri("wss://danmuproxy.douyu.com:8506/"), cancellationToken);
        await MsgTool.LoginAsync(ws, roomId, cancellationToken);
        await MsgTool.JoinGroupAsync(ws, roomId, cancellationToken);

        var task = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await MsgTool.SendTick(ws, cancellationToken);
                await Task.Delay(45000, cancellationToken);
            }
        }, cancellationToken);

        while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            yield return await MsgTool.RecieveAsync(ws, cancellationToken);
        }

        GC.KeepAlive(task);
        await MsgTool.Logout(ws, cancellationToken);
    }
	
	public static IAsyncEnumerable<JToken> JObjectFromUrl(string url) => RawFromUrl(url)
		.Select(MsgTool.DecodeStringToJObject);
	
	public static IAsyncEnumerable<Barrage> ChatMessageFromUrl(string url) => JObjectFromUrl(url)
		.Where(x => x["type"].Value<string>() == "chatmsg")
		.Select(Barrage.FromJToken);
}

public class Barrage
{
    public string UserName { get; set; }
	public string Message { get; set; }
	public string ColorName { get; set; }
	public int Color { get; set;}

	public static Barrage FromJToken(JToken x) => new Barrage
	{
		UserName = x["nn"].Value<string>(),
		Message = x["txt"].Value<string>(),
		ColorName = (x["col"] ?? new JValue(0)).Value<int>() switch
		{
			1 => "红",
			2 => "浅蓝",
			3 => "浅绿",
			4 => "橙色",
			5 => "紫色",
			6 => "洋红",
			0 => "默认，白色",
			_ => "未知",
		},
		Color = (x["col"] ?? new JValue(0)).Value<int>() switch
		{
			1 => 0xff0000, // 红
			2 => 0x1e87f0, // 浅蓝
			3 => 0x7ac84b, // 浅绿
			4 => 0xff7f00, // 橙色
			5 => 0x9b39f4, // 紫色
			6 => 0xff69b4, // 洋红
			_ => 0xffffff, // 默认，白色
		}
	};
}

private class MsgTool
{
    public static Task LoginAsync(ClientWebSocket stream, string roomId, CancellationToken cancellationToken)
    {
        return SendAsync(stream, $"type@=loginreq/roomid@={roomId}/", cancellationToken);
    }

    public static Task SendTick(ClientWebSocket stream, CancellationToken cancellationToken)
    {
        return SendAsync(stream, $"type@=keeplive/tick@={Environment.TickCount}/", cancellationToken);
    }

    public static Task JoinGroupAsync(ClientWebSocket stream, string roomId, CancellationToken cancellationToken, int groupId = -9999)
    {
        return SendAsync(stream, $"type@=joingroup/rid@={roomId}/gid@={groupId}/", cancellationToken);
    }

    public static Task Logout(ClientWebSocket stream, CancellationToken cancellationToken)
    {
        return SendAsync(stream, $"type@=logout/", cancellationToken);
    }

    const byte Encrypted = 0;
    const byte Reserved = 0;
    const short ClientSendToServer = 689;
    const short ServerSendToClient = 690;
    const byte ByteZero = 0;

    static Task SendAsync(ClientWebSocket stream, string msg, CancellationToken cancellationToken)
    {
        return SendAsync(stream, Encoding.UTF8.GetBytes(msg), cancellationToken);
    }

    static async Task SendAsync(ClientWebSocket stream, byte[] body, CancellationToken cancellationToken)
    {
		var buffer = new byte[4];
		
        await stream.SendAsync(GetBytesI32(4 + 4 + body.Length + 1), WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);
        await stream.SendAsync(GetBytesI32(4 + 4 + body.Length + 1), WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);

        await stream.SendAsync(GetBytesI16(ClientSendToServer), WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);
        await stream.SendAsync(new byte[] { Encrypted}, WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);
        await stream.SendAsync(new byte[] { Reserved}, WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);

        await stream.SendAsync(body, WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);
        await stream.SendAsync(new byte[] { ByteZero}, WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);
		
		Memory<byte> GetBytesI32(int v)
		{
			buffer[0] = (byte)v;
			buffer[1] = (byte)(v >> 8);
			buffer[2] = (byte)(v >> 16);
			buffer[3] = (byte)(v >> 24);
			return new Memory<byte>(buffer, 0, 4);
		}

		Memory<byte> GetBytesI16(short v)
		{
			buffer[0] = (byte)v;
			buffer[1] = (byte)(v >> 8);;
			return new Memory<byte>(buffer, 0, 2);
		}
	}

	public static async Task<string> RecieveAsync(ClientWebSocket stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[65536];
		var intBuffer = new byte[4];
        var int32Buffer = new Memory<byte>(intBuffer, 0, 4);
        var int16Buffer = int32Buffer.Slice(0, 2);
        var int8Buffer = int32Buffer.Slice(0, 1);
        
        int fullMsgLength = await ReadInt32();
        int fullMsgLength2 = await ReadInt32();
        Debug.Assert(fullMsgLength == fullMsgLength2);

        int length = fullMsgLength - 1 - 4 - 4;
        short packType = await ReadInt16();
        Debug.Assert(packType == ServerSendToClient);
        short encrypted = await ReadByte();
        Debug.Assert(encrypted == Encrypted);
        short reserved = await ReadByte();
        Debug.Assert(reserved == Reserved);
        Memory<byte> bytes = await ReadBytes(length).ConfigureAwait(false);
        byte zero = await ReadByte().ConfigureAwait(false);
        Debug.Assert(zero == ByteZero);

        return Encoding.UTF8.GetString(bytes.Span);
        
        async ValueTask<int> ReadInt32()
		{
			var memory = int32Buffer;
			int read = 0;
			while (read < 4)
			{
                ValueWebSocketReceiveResult result = await stream.ReceiveAsync(memory.Slice(read), cancellationToken);
				read += result.Count;
			}
			Debug.Assert(read == memory.Length);
			return 
				(intBuffer[0] << 0) + 
				(intBuffer[1] << 8) +
				(intBuffer[2] << 16) + 
				(intBuffer[3] << 24);
        }

        async ValueTask<short> ReadInt16()
        {
            ValueWebSocketReceiveResult result = await stream.ReceiveAsync(int16Buffer, cancellationToken);
            Debug.Assert(result.Count == int16Buffer.Length);
            return (short)((intBuffer[0] << 0) + (intBuffer[1] << 8));
        }

        async ValueTask<byte> ReadByte()
        {
            ValueWebSocketReceiveResult result = await stream.ReceiveAsync(int8Buffer, cancellationToken);
            Debug.Assert(result.Count == int8Buffer.Length);
            return int8Buffer.Span[0];
        }
        
        async ValueTask<Memory<byte>> ReadBytes(int length)
        {
            var memory = new Memory<byte>(buffer, 0, length);
			int read = 0;
			while (read < length)
			{
                ValueWebSocketReceiveResult result = await stream.ReceiveAsync(memory.Slice(read), cancellationToken);
                read += result.Count;
			}
            Debug.Assert(read == memory.Length);
            return memory;
        }
    }

    public static JToken DecodeStringToJObject(string str)
    {
        if (str.Contains("//"))
        {
            var result = new JArray();
            foreach (var field in str.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(DecodeStringToJObject(field));
            }
            return result;
        }
        if (str.Contains("@="))
        {
            var result = new JObject();
            foreach (var field in str.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var tokens = field.Split(new[] { "@=" }, StringSplitOptions.None);
                var k = tokens[0];
                var v = UnscapeSlashAt(tokens[1]);
                result[k] = DecodeStringToJObject(v);
            }
            return result;
        }
        else if (str.Contains("@A="))
        {
            return DecodeStringToJObject(UnscapeSlashAt(str));
        }
        else
		{
			return UnscapeSlashAt(str);
		}
	}

	static string EscapeSlashAt(string str)
	{
		return str
			.Replace("/", "@S")
			.Replace("@", "@A");
	}

	static string UnscapeSlashAt(string str)
	{
		return str
			.Replace("@S", "/")
			.Replace("@A", "@");
	}
}