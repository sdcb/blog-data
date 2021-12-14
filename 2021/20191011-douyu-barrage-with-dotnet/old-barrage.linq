<Query Kind="Program">
  <Output>DataGrids</Output>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
</Query>

public static void Main(string[] args)
{
	var roomId = 71415;
	var messageContainer = new DumpContainer().Dump("最新消息");
	var container = new Container();	
	using (var client = new TcpClient())
	{
		client.ConnectAsync("danmu.douyutv.com", 8601).Wait();
		var stream = client.GetStream();
		MsgTool.Login(stream, roomId);
		MsgTool.JoinGroup(stream, roomId);

		var task = Task.Run(async () =>
		{
			while (true)
			{
				MsgTool.SendTick(stream);
				await Task.Delay(15000);
			}
		});

		while (stream.CanRead)
		{
			var obj = MsgTool.Recieve(stream);
			messageContainer.Content = new Dictionary<string, string>
			{
				["Type"] = obj["type"].ToString(), 
				["Time"] = DateTime.Now.ToString()
			};
			if (obj["type"].ToString() == "chatmsg")
			{
				var name = obj["nn"];
				var txt = obj["txt"];
				container.Add(name.ToString(), txt.ToString());
			}
		}

		GC.KeepAlive(task);
		MsgTool.Logout(stream);
	}
}

public class Container
{
	public LinkedList<object> queue = new LinkedList<object>();
	
	private int _total = 0;
	private readonly DumpContainer _totalContainer, _listContainer;
	
	public Container()
	{
		_totalContainer = new DumpContainer(queue).Dump("总数");
		_listContainer = new DumpContainer(queue).Dump("最新弹幕");
	}
	
	public void Add(string name, string text)
	{
		queue.AddFirst(new
		{
			Name = name, 
			Text = text, 
			Time = DateTime.Now
		});
		if (queue.Count > 10)
		{
			queue.RemoveLast();
		}
		
		_totalContainer.Content = (_total += 1);
		_listContainer.Refresh();
	}
}

public class MsgTool
{
	public static void Login(Stream stream, int roomId)
	{
		Send(stream, $"type@=loginreq/roomid@={roomId}/");
	}

	public static void SendTick(Stream stream)
	{
		Send(stream, $"type@=keeplive/tick@={Environment.TickCount}/");
	}

	public static void JoinGroup(Stream stream, int roomId, int groupId = -9999)
	{
		Send(stream, $"type@=joingroup/rid@={roomId}/gid@={groupId}/");
	}

	public static void Logout(Stream stream)
	{
		Send(stream, $"type@=logout/");
	}

	const byte Encrypted = 0;
	const byte Reserved = 0;
	const short ClientSendToServer = 689;
	const short ServerSendToClient = 690;
	const byte ByteZero = 0;

	private static void Send(Stream stream, string msg)
	{
		Send(stream, Encoding.UTF8.GetBytes(msg));
	}

	private static void Send(Stream stream, byte[] body)
	{
		using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
		{
			writer.Write(4 + 4 + body.Length + 1);
			writer.Write(4 + 4 + body.Length + 1);

			writer.Write(ClientSendToServer);
			writer.Write(Encrypted);
			writer.Write(Reserved);

			writer.Write(body);
			writer.Write(ByteZero);
		}
	}

	public static JObject Recieve(Stream stream)
	{
		using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
		{
			var fullMsgLength = reader.ReadInt32();
			var fullMsgLength2 = reader.ReadInt32();
			Debug.Assert(fullMsgLength == fullMsgLength2);

			var length = fullMsgLength - 1 - 4 - 4;
			var packType = reader.ReadInt16();
			Debug.Assert(packType == ServerSendToClient);
			var encrypted = reader.ReadByte();
			Debug.Assert(encrypted == Encrypted);
			var reserved = reader.ReadByte();
			Debug.Assert(reserved == Reserved);

			var bytes = reader.ReadBytes(length);
			var zero = reader.ReadByte();
			Debug.Assert(zero == ByteZero);

			var str = Encoding.UTF8.GetString(bytes);
			return (JObject)DecodeStringToJObject(str);
		}
	}

	private static JToken DecodeStringToJObject(string str)
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

	private static string EscapeSlashAt(string str)
	{
		return str
			.Replace("/", "@S")
			.Replace("@", "@A");
	}

	private static string UnscapeSlashAt(string str)
	{
		return str
			.Replace("@S", "/")
			.Replace("@A", "@");
	}
}