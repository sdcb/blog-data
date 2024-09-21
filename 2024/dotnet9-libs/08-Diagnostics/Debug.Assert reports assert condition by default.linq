<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// 要演示效果，不能在LINQPad中运行，请使用.NET SDK命令行工具或Visual Studio
class Program
{
	static void Main(string[] args)
	{
		// 假设我们有两个参数 a 和 b
		int a = -1;
		int b = 5;

		// 在.NET 9 之前，Debug.Assert 只是简单地报告断言失败，而不会说明具体的条件。
		// 在.NET 9 中，默认情况下会报告具体的条件 "a > 0 && b > 0"。
		Debug.Assert(a > 0 && b > 0);

		Console.WriteLine("程序继续运行...");
	}
}