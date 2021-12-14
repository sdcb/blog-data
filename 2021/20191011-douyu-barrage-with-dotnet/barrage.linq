<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Buffers</Namespace>
</Query>

void Main(string[] args)
{
	DouyuBarrage.RawFromUrl("https://www.douyu.com/topic/lscs?rid=128489").Dump();
}

public class DouyuBarrage
{
    static HttpClient http = new HttpClient();

    public static IObservable<string> RawFromUrl(string url)
    {
        return Observable.Create<string>(async (ob, cancellationToken) =>
        {
            HttpResponseMessage html = await http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var roomId = Regex.Match(await html.Content.ReadAsStringAsync().ConfigureAwait(false), @"\$ROOM.room_id[ ]?=[ ]?(\d+);").Groups[1].Value;

            using var client = new TcpClient();
            await client.ConnectAsync("openbarrage.douyutv.com", 8601).ConfigureAwait(false);
            await using NetworkStream stream = client.GetStream();
            await MsgTool.LoginAsync(stream, roomId, cancellationToken).ConfigureAwait(false);
            await MsgTool.JoinGroupAsync(stream, roomId, cancellationToken).ConfigureAwait(false);

            var task = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await MsgTool.SendTick(stream, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(45000, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);

            while (stream.CanRead && !cancellationToken.IsCancellationRequested)
            {
                ob.OnNext(await MsgTool.RecieveAsync(stream, cancellationToken).ConfigureAwait(false));
            }

            GC.KeepAlive(task);
            await MsgTool.Logout(stream, cancellationToken).ConfigureAwait(false);
            ob.OnCompleted();
        });
    }
	
	public static IObservable<JToken> JObjectFromUrl(string url) => RawFromUrl(url).Select(MsgTool.DecodeStringToJObject);
	
	public static IObservable<Barrage> ChatMessageFromUrl(string url) => JObjectFromUrl(url)
		.Where(x => x["type"].Value<string>() == "chatmsg")
		.Select(Barrage.FromJToken);
}

public class Barrage
{
    public string UserName { get; set; }
	public string Message { get; set; }
	public int Color { get; set; }

	public static Barrage FromJToken(JToken x) => new Barrage
	{
		UserName = x["nn"].Value<string>(),
		Message = x["txt"].Value<string>(),
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
    public static Task LoginAsync(Stream stream, string roomId, CancellationToken cancellationToken)
    {
        return SendAsync(stream, $"type@=loginreq/roomid@={roomId}/", cancellationToken);
    }

    public static Task SendTick(Stream stream, CancellationToken cancellationToken)
    {
        return SendAsync(stream, $"type@=keeplive/tick@={Environment.TickCount}/", cancellationToken);
    }

    public static Task JoinGroupAsync(Stream stream, string roomId, CancellationToken cancellationToken, int groupId = -9999)
    {
        return SendAsync(stream, $"type@=joingroup/rid@={roomId}/gid@={groupId}/", cancellationToken);
    }

    public static Task Logout(Stream stream, CancellationToken cancellationToken)
    {
        return SendAsync(stream, $"type@=logout/", cancellationToken);
    }

    const byte Encrypted = 0;
    const byte Reserved = 0;
    const short ClientSendToServer = 689;
    const short ServerSendToClient = 690;
    const byte ByteZero = 0;

    static Task SendAsync(Stream stream, string msg, CancellationToken cancellationToken)
    {
        return SendAsync(stream, Encoding.UTF8.GetBytes(msg), cancellationToken);
    }

    static async Task SendAsync(Stream stream, byte[] body, CancellationToken cancellationToken)
    {
		var buffer = new byte[4];
		
        await stream.WriteAsync(GetBytesI32(4 + 4 + body.Length + 1), cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(GetBytesI32(4 + 4 + body.Length + 1), cancellationToken).ConfigureAwait(false);

        await stream.WriteAsync(GetBytesI16(ClientSendToServer), cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { Encrypted}, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { Reserved}, cancellationToken).ConfigureAwait(false);

        await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { ByteZero}, cancellationToken).ConfigureAwait(false);
		
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

	public static async Task<string> RecieveAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[65536];
		var intBuffer = new byte[4];
        var int32Buffer = new Memory<byte>(intBuffer, 0, 4);
        var int16Buffer = int32Buffer.Slice(0, 2);
        var int8Buffer = int32Buffer.Slice(0, 1);
        
        int fullMsgLength = await ReadInt32().ConfigureAwait(false);
        int fullMsgLength2 = await ReadInt32().ConfigureAwait(false);
        Debug.Assert(fullMsgLength == fullMsgLength2);

        int length = fullMsgLength - 1 - 4 - 4;
        short packType = await ReadInt16().ConfigureAwait(false);
        Debug.Assert(packType == ServerSendToClient);
        short encrypted = await ReadByte().ConfigureAwait(false);
        Debug.Assert(encrypted == Encrypted);
        short reserved = await ReadByte().ConfigureAwait(false);
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
				read += await stream.ReadAsync(memory.Slice(read), cancellationToken).ConfigureAwait(false);
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
            var c = await stream.ReadAsync(int16Buffer, cancellationToken).ConfigureAwait(false);
            Debug.Assert(c == int16Buffer.Length);
            return (short)((intBuffer[0] << 0) + (intBuffer[1] << 8));
        }

        async ValueTask<byte> ReadByte()
        {
            var c = await stream.ReadAsync(int8Buffer, cancellationToken).ConfigureAwait(false);
            Debug.Assert(c == int8Buffer.Length);
            return int8Buffer.Span[0];
        }
        
        async ValueTask<Memory<byte>> ReadBytes(int length)
        {
            var memory = new Memory<byte>(buffer, 0, length);
			int read = 0;
			while (read < length)
			{
				read += await stream.ReadAsync(memory.Slice(read), cancellationToken).ConfigureAwait(false);
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