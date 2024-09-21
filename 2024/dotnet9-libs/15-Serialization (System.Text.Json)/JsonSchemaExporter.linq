<Query Kind="Program">
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Schema</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>


// 定义一个名为 Book 的类
public class Book
{
    // 必须的字符串属性 Title
    public required string Title { get; set; }

	// 可选的字符串属性 Author，允许为 null
	public string? Author { get; set; }

    // 整数属性 PublishYear
    public int PublishYear { get; set; }
}

class Program
{
    static void Main()
    {
        // 使用 JsonSchemaExporter 生成 Book 类型的 JSON Schema
        // JsonSchemaExporter 是一个新的工具，可以为 .NET 类型生成 JSON schema
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(JsonSerializerOptions.Default, typeof(Book));

        // 输出生成的 JSON schema
        Console.WriteLine(schema);
    }
}

// 在这段代码中，我们首先定义了一个名为 Book 的类。
// 这个类包含了三个属性：Title、Author 和 PublishYear。
// Title 是一个必需的字符串属性，这意味着在实例化 Book 时必须为它指定一个值。
// Author 是一个可空字符串属性，因此我们在定义时允许它为 null。
// PublishYear 是一个整数属性，用来存储书籍的出版年份。

// 接下来，在 Main 方法中，我们使用 System.Text.Json 中的新工具 JsonSchemaExporter。
// JsonSchemaExporter 可以用来生成一个 JSON schema，该 schema 描述了 .NET 类型的结构。

// 我们调用 JsonSchemaExporter.GetJsonSchemaAsNode 方法，
// 传入 JsonSerializerOptions.Default 和 typeof(Book)，以生成 Book 类型的 schema。
// 最后，我们输出生成的 JSON schema 到控制台。

// 这使得我们能够以 JSON schema 的形式表示一个 .NET 类型，
// 对于远程过程调用或与 OpenAI 等 AI 服务的集成非常有用。