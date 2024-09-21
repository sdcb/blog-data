<Query Kind="Program">
  <Namespace>System.Text.Json.Nodes</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static void Main()
	{
		// 创建一个新的JsonObject实例
		// 其中包含两个键值对: "key1" 和 "key3"
		JsonObject jObj = new()
		{
			["key1"] = true,
			["key3"] = 3
		};

		// 检查JsonObject是否实现了IList接口
		// 这样我们就知道它可以按顺序操作
		Console.WriteLine(jObj is IList<KeyValuePair<string, JsonNode?>>); // 打印 True

		// 找到 "key3" 的位置以便于插入之前
		// 使用索引找到 "key3" 的位置
		// 如果找不到，默认位置为 0
		int key3Pos = jObj.IndexOf("key3") is int i and >= 0 ? i : 0;

		// 在 "key3" 前插入一个新键值对 "key2": "two"
		jObj.Insert(key3Pos, "key2", "two");

		// 遍历JsonObject并打印出所有的键值对
		// 以展示插入后的顺序
		foreach (KeyValuePair<string, JsonNode?> item in jObj)
		{
			Console.WriteLine($"{item.Key}: {item.Value}");
		}

		// 输出结果:
		// key1: true
		// key2: two
		// key3: 3
	}
}