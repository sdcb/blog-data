<Query Kind="Program">
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 示例中，我们将展示如何使用JsonSerializerOptions.Web进行序列化和反序列化。

        // 定义一个匿名对象，包含属性SomeValue，值为42。
        var myObject = new { SomeValue = 42 };

        // 使用JsonSerializerOptions.Web进行序列化。
        // 该选项不仅使用camelCase命名规则，还有其他特性。
        string webJson = JsonSerializer.Serialize(
            myObject,
            JsonSerializerOptions.Web
        );

        Console.WriteLine(webJson);
        // 输出结果：{"someValue":42}
        // 属性名SomeValue被自动转换为camelCase格式。

        // JsonSerializerOptions.Web还允许从字符串中读取数值。
        string jsonString = "{\"someValue\":\"42\"}";

		// 在反序列化时，_propertyNameCaseInsensitive设置为true，允许属性名不区分大小写。
		// 同时，AllowReadingFromString允许我们从字符串读取数值。
		MyObject deserializedObject = JsonSerializer.Deserialize<MyObject>(
			jsonString,
			JsonSerializerOptions.Web
		)!;

		Console.WriteLine(deserializedObject.SomeValue);
		// 输出结果：42
		// 即使在JSON字符串中，someValue是字符串"42"，也成功转换为了整数。
		// 这显示了AllowReadingFromString功能，并且someValue大小写不敏感。

		// 最后，定义一个简单的类用于反序列化。
	}

	class MyObject
	{
		public int SomeValue { get; set; }
	}
}