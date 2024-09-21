<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static void Main()
	{
		// 示例字符串
		ReadOnlySpan<char> span = "apple,banana,cherry,dates";

		// 调用方法检查字符串是否包含特定项
		bool containsBanana = ListContainsItem(span, "banana");
		bool containsMango = ListContainsItem(span, "mango");

		Console.WriteLine($"Contains 'banana': {containsBanana}"); // 输出: true
		Console.WriteLine($"Contains 'mango': {containsMango}");   // 输出: false
	}

	// 在 .NET 9 中的 ReadOnlySpan<char>.Split() 可枚举的用法
	public static bool ListContainsItem(ReadOnlySpan<char> span, string item)
	{
		// 遍历分割后的每个段落，由逗号分隔
		foreach (Range segment in span.Split(','))
		{
			// 截取当前段落并与目标项进行比较
			if (span[segment].SequenceEqual(item))
			{
				return true;  // 如果相等，返回true
			}
		}

		return false;  // 如果没有找到匹配项，返回false
	}
}