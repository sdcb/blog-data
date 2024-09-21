<Query Kind="Statements">
  <Namespace>System.Text.Json</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// 创建 JsonSerializerOptions 对象
var options = new JsonSerializerOptions
{
	// 设置为 true 来启用 JSON 的缩进格式（美化 JSON 输出）
	WriteIndented = true,

	// 指定缩进所用的字符，这里使用 '\t' 制表符
	IndentCharacter = '\t',

	// 设置缩进的大小，为每个级别的缩进指定字符数
	IndentSize = 2,
};

// 使用 JsonSerializer 序列化一个匿名对象为 JSON 字符串
string json = JsonSerializer.Serialize(
	new { Value = 1 }, // 这里是要序列化的对象
	options            // 传入之前定义的缩进选项
);

// 输出序列化后的 JSON 字符串
Console.WriteLine(JsonSerializer.Serialize(json));

// 注意输出结果被缩进了，查看每一行开始的制表符数量与 IndentSize 设置的关系
// "{\r\n\t\t\u0022Value\u0022: 1\r\n}"