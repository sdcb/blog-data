<Query Kind="Program">
  <Namespace>System.Text.Json</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// 定义一个示例的 Book 类，其中包含一个 Title 属性
public class Book
{
	// 通过 C# 8.0 及更高版本的可空性注解，指示此属性不能为 null
	public required string Title { get; set; } = null!;
}

class Program
{
	static void Main()
	{
		
		// 创建 JsonSerializerOptions 并启用 RespectNullableAnnotations 选项
		JsonSerializerOptions options = new() { RespectNullableAnnotations = true };

		try
		{
			// 尝试序列化一个 Title 为 null 的书
			// 演示：由于启用了 RespectNullableAnnotations，因此无法接受 Title 为 null
			string json = JsonSerializer.Serialize(new Book { Title = null! }, options);
			Console.WriteLine(json);
		}
		catch (Exception e)
		{
			Console.WriteLine("序列化抛出异常: " + e.Message);
		}

		try
		{
			// 尝试反序列化一个包含 null Title 的 JSON 字符串
			// 演示：同样的限制也适用于反序列化
			Book book = JsonSerializer.Deserialize<Book>("""{ "Title" : null }""", options)!;
			Console.WriteLine(book);
		}
		catch (Exception e)
		{
			Console.WriteLine("反序列化抛出异常: " + e.Message);
		}
	}
}
