<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static void Main()
	{
		// 定义一个字符串
		string original = "HELLO WORLD";

		// 使用新的 ToLowerInvariant 重载将字符串转为小写
		string lowerInvariant = ToLowerInvariant(original.AsSpan());

		// 输出结果
		Console.WriteLine(lowerInvariant);
	}

	// 这是 C# 13 新增的 String.ToLowerInvariant 方法重载，接收 ReadOnlySpan<char> 参数
	public static string ToLowerInvariant(ReadOnlySpan<char> input)
	{
		// 使用 string.Create 方法创建一个新的字符串
		return string.Create(input.Length, input, static (stringBuffer, spanInput) =>
		{
			// 直接在内存块中写入，将 span 转为小写
			spanInput.ToLowerInvariant(stringBuffer);
		});
	}
}