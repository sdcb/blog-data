<Query Kind="Program">
  <Namespace>System.IO.Compression</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	// 参考数据：
	// 输入大小：144595
	// .NET 9（使用zlib-ng） 压缩1664ms，解压332ms，压缩后大小：34829
	// .NET 8                压缩2542ms，解压454ms，压缩后大小：34957
	static async Task Main()
	{
		// 定义循环次数变量
		int loopCount = 1000;

		// 原始数据字符串
		string originalData = await new HttpClient().GetStringAsync("https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/libraries");

		// 将字符串转换为字节数组
		byte[] dataToCompress = System.Text.Encoding.UTF8.GetBytes(originalData);
		Console.WriteLine("压缩前的数据长度：" + dataToCompress.Length);

		// 用于多次压缩的流对象
		using (var compressedStream = new MemoryStream())
		{
			// 压缩计时
			Stopwatch compressionTimer = Stopwatch.StartNew();

			for (int i = 0; i < loopCount; i++)
			{
				compressedStream.Position = 0;

				// 创建一个使用GZip算法的压缩流
				using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
				{
					// 写入要压缩的数据
					gzipStream.Write(dataToCompress, 0, dataToCompress.Length);
				}
			}

			compressionTimer.Stop();
			Console.WriteLine($"压缩{loopCount}次耗时: " + compressionTimer.ElapsedMilliseconds + " 毫秒");

			byte[] compressedData = compressedStream.ToArray();
			Console.WriteLine("压缩后的数据长度：" + compressedData.Length);

			// 解压缩计时
			Stopwatch decompressionTimer = Stopwatch.StartNew();

			for (int i = 0; i < loopCount; i++)
			{
				compressedStream.Seek(0, SeekOrigin.Begin);

				using (var decompressedStream = new MemoryStream())
				{
					// 创建一个使用GZip算法的解压缩流
					using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveOpen: true))
					{
						// 将解压缩的数据写入MemoryStream
						decompressionStream.CopyTo(decompressedStream);
					}

					// 验证解压缩后的数据正确性
					string decompressedData = System.Text.Encoding.UTF8.GetString(decompressedStream.ToArray());
				}
			}

			decompressionTimer.Stop();
			Console.WriteLine($"解压缩{loopCount}次耗时: " + decompressionTimer.ElapsedMilliseconds + " 毫秒");
		}
	}
}