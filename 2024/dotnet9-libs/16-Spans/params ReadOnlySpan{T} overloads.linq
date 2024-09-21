<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 在C#中，使用params可以简化数组参数的调用
        string result1 = string.Join(", ", new string[3] { "a", "b", "c" });
        string result2 = string.Join(", ", "a", "b", "c");

        Console.WriteLine(result1); // 输出: a, b, c
        Console.WriteLine(result2); // 输出: a, b, c

        string concatResult1 = string.Concat(new string[] { "Hello", " ", "World", "!" });
        string concatResult2 = string.Concat("Hello", " ", "World", "!");

        Console.WriteLine(concatResult1); // 输出: Hello World!
        Console.WriteLine(concatResult2); // 输出: Hello World!

        // 性能测试部分
        const int iterations = 100_0000;

        // 测试使用数组的String.Concat
        Stopwatch stopwatch1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            string.Concat(new string[] { "Hello", " ", "World", "!" });
        }
		stopwatch1.Stop();
		Console.WriteLine($"Array-based Concat: {stopwatch1.ElapsedMilliseconds} ms");

		// 测试使用ReadOnlySpan<T>的String.Concat
		Stopwatch stopwatch2 = Stopwatch.StartNew();
		for (int i = 0; i < iterations; i++)
		{
			string.Concat("Hello", " ", "World", "!");
		}
		stopwatch2.Stop();
		Console.WriteLine($"Span-based Concat: {stopwatch2.ElapsedMilliseconds} ms");

		// 输出性能差异
		Console.WriteLine($"性能差异: {1.0 * stopwatch1.ElapsedMilliseconds / stopwatch2.ElapsedMilliseconds:F2}倍");
	}
}