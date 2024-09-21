<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// 暂时不能在LINQPad 8中运行这个示例，需要使用.NET SDK或者Visual Studio

partial class Program
{
	static void Main()
	{
		// 示例字符串
		string text = "Hello world, this is a test with words of varying lengths.";

		// 使用 FiveCharWordProperty 来匹配正则表达式
		foreach (Match match in FiveCharWordProperty.Matches(text))
		{
			// 输出匹配的五个字符的单词
			Console.WriteLine(match.Value);
		}
	}

	// 在 .NET 7 中引入了 Regex 源生成器和对应的 GeneratedRegexAttribute 属性。
	// .NET 9 允许在属性上使用 [GeneratedRegex(...)] 进行类似的操作。
	// 这里定义一个部分属性，它将在编译时自动生成匹配代码。
	[GeneratedRegex(@"\b\w{5}\b")]
	private static partial Regex FiveCharWordProperty { get; }  // Partial property example in .NET 9
}