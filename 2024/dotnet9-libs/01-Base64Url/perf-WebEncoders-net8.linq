<Query Kind="Statements">
  <Namespace>System.Buffers.Text</Namespace>
  <Namespace>Microsoft.AspNetCore.WebUtilities</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
  <RuntimeVersion>8.0</RuntimeVersion>
</Query>

#LINQPad Optimize+
const int iterations = 100_0000;
byte[] data = new byte[1000];
new Random().NextBytes(data);

// 使用 WebEncoders 的 Base64UrlEncode 方法进行测试
for (int N = 0; N < 5; ++N)
{
	Stopwatch stopwatch = new Stopwatch();
	stopwatch.Start();
	for (int i = 0; i < iterations; i++)
	{
		_ = WebEncoders.Base64UrlEncode(data);
	}
	stopwatch.Stop();
	Console.WriteLine($"WebEncoders Base64UrlEncode 编码时间: {stopwatch.ElapsedMilliseconds} ms");
}
