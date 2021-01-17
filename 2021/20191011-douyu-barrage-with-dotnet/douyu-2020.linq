<Query Kind="Statements">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>System.Net.WebSockets</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

using var ws = new ClientWebSocket();
ws.Options.AddSubProtocol("-");
await ws.ConnectAsync(new Uri("wss://danmuproxy.douyu.com:8506"), QueryCancelToken);
await ws.SendAsync(SerializeDouyu($"type@=loginreq/roomid@=74751/ver@=20190610/"), WebSocketMessageType.Binary, false, QueryCancelToken);
await ws.SendAsync(SerializeDouyu($"type@=joingroup/rid@=74751/gid@=-9999/"), WebSocketMessageType.Binary, false, QueryCancelToken);
_ = Task.Run(async () =>
{
	while (!QueryCancelToken.IsCancellationRequested)
	{
		await Task.Delay(45000, QueryCancelToken);
		await ws.SendAsync(SerializeDouyu($"type@=mrkl/"), WebSocketMessageType.Binary, false, QueryCancelToken);
	}
});

while (!QueryCancelToken.IsCancellationRequested)
{
	var buffer = new byte[4096];
	WebSocketReceiveResult r = await ws.ReceiveAsync(buffer, QueryCancelToken);
	string result = DeserializeDouyu(new Memory<byte>(buffer, 0, r.Count), QueryCancelToken);
	DecodeStringToJObject(result).Dump();
}

byte[] SerializeDouyu(string body)
{
	const short ClientSendToServer = 689;
	const byte Encrypted = 0;
	const byte Reserved = 0;

	byte[] bodyBuffer = Encoding.UTF8.GetBytes(body);
	using var ms = new MemoryStream(bodyBuffer.Length + 13);
	using var writer = new BinaryWriter(ms);
	writer.Write(4 + 4 + body.Length + 1);
	writer.Write(4 + 4 + body.Length + 1);
	writer.Write(ClientSendToServer);
	writer.Write(Encrypted);
	writer.Write(Reserved);
	writer.Write(bodyBuffer);
	writer.Write((byte)0);
	writer.Flush();
	
	return ms.ToArray();
}

string DeserializeDouyu(Memory<byte> buffer, CancellationToken cancellationToken)
{
	const short ServerSendToClient = 690;
	const byte Encrypted = 0;
	const byte Reserved = 0;

	using var ms = new MemoryStream(buffer.ToArray(), 0, buffer.Length, writable: false);
	using var reader = new BinaryReader(ms);

	int fullMsgLength = reader.ReadInt32();
	int fullMsgLength2 = reader.ReadInt32();
	Debug.Assert(fullMsgLength == fullMsgLength2);

	int bodyLength = fullMsgLength - 1 - 4 - 4;
	short packType = reader.ReadInt16();
	Debug.Assert(packType == ServerSendToClient);
	short encrypted = reader.ReadByte();
	Debug.Assert(encrypted == Encrypted);
	short reserved = reader.ReadByte();
	Debug.Assert(reserved == Reserved);

	Memory<byte> bytes = reader.ReadBytes(bodyLength);
	byte zero = reader.ReadByte();
	Debug.Assert(zero == 0);

	return Encoding.UTF8.GetString(bytes.Span);
}

JToken DecodeStringToJObject(string str)
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

	static string UnscapeSlashAt(string str)
	{
		return str
			.Replace("@S", "/")
			.Replace("@A", "@");
	}
}

