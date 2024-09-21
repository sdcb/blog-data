<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static void Main()
	{
		Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
		// 演示如何用新的 File 类助手方法写入 ReadOnlySpan<char>

		// 定义一个 ReadOnlySpan<char>，它是不可修改的字符数据视图
		ReadOnlySpan<char> text = "这是一些需要写入文件的文本";

		// 使用新方法 File.WriteAllText 直接将 text 写入到指定文件路径
		// 这种方式避免了将字符数组转换为字符串，大大提高了效率
		string filePath = "example.txt";
		File.WriteAllText(filePath, text);

		// 演示新的扩展方法 StartsWith<T> 和 EndsWith<T>

		// 定义另一个 ReadOnlySpan<char>，用于检测文本是否以指定字符开头或结尾
		ReadOnlySpan<char> sampleText = "某个任意的文本";

		// 使用 StartsWith 判断 sampleText 是否以 '"' 开头
		bool startsWithQuote = sampleText.StartsWith('"');

		// 使用 EndsWith 判断 sampleText 是否以 '"' 结尾
		bool endsWithQuote = sampleText.EndsWith('"');

		// 输出结果，告诉我们 text 既不以 '"' 开头也不以 '"' 结尾
		Console.WriteLine($"Starts with '\"'?: {startsWithQuote}, Ends with '\"'?: {endsWithQuote}");
		// 输出: Starts with '"': False, Ends with '"': False
	}
}