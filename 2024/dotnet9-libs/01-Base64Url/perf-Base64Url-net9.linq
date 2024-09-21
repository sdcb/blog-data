<Query Kind="Statements">
  <Namespace>System.Buffers.Text</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

#LINQPad Optimize+

// .NET 9 的 Base64Url 类无需额外引用库，它集成在 System.Private.CoreLib 中，
// 并且经过了 SIMD 指令集 (如 AVX512/AVX2/SSE) 的优化，能有效提高性能。
// 下面通过性能测试比较 .NET 9 与 .NET 8 中编码性能的差距。

const int iterations = 100_0000;
byte[] data = new byte[1000];
new Random().NextBytes(data);

// 使用 .NET 9 的 Base64Url 编码进行测试
for (int N = 0; N < 5; ++N)
{
	Stopwatch stopwatch = new Stopwatch();
	stopwatch.Start();
	for (int i = 0; i < iterations; i++)
	{
		_ = Base64Url.EncodeToUtf8(data);
	}
	stopwatch.Stop();
	Console.WriteLine($".NET 9 Base64Url 编码时间: {stopwatch.ElapsedMilliseconds} ms");
}
