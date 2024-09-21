<Query Kind="Program">
  <Namespace>System.IO.Compression</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class CompressionExample
{
	static async Task Main()
	{
		// 创建一个示例未压缩的内存流
		MemoryStream uncompressedStream = new MemoryStream(await new HttpClient().GetByteArrayAsync("https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/libraries"));
		Console.WriteLine($"压缩前数据大小为：{uncompressedStream.Length}");

		// 重置流的位置以便从头开始读取
		uncompressedStream.Position = 0;

		// 调用压缩方法
		MemoryStream compressedStream = CompressStream(uncompressedStream);

		Console.WriteLine("压缩完成，压缩后的数据大小为: " + compressedStream.Length);
	}

	/// <summary>
	/// 使用 ZLib 压缩给定的未压缩流
	/// </summary>
	/// <param name="uncompressedStream">未压缩的输入流</param>
	/// <returns>压缩后的内存流</returns>
	private static MemoryStream CompressStream(Stream uncompressedStream)
	{
		// 创建一个新的内存流来存储压缩后的数据
		MemoryStream compressorOutput = new();

		// 创建 ZLibStream 使用新的 ZLibCompressionOptions 进行压缩
		using (ZLibStream compressionStream = new ZLibStream(
			compressorOutput,
			new ZLibCompressionOptions()
			{
				// 设置压缩级别（0-9），6 为一个合适的中间值
				CompressionLevel = 6,

				// 设置压缩策略为 HuffmanOnly, 优化仅使用霍夫曼编码
				CompressionStrategy = ZLibCompressionStrategy.HuffmanOnly
			}, 
			leaveOpen: true
		))
		{
			// 将未压缩数据复制到压缩流中进行压缩
			uncompressedStream.CopyTo(compressionStream);

			// 确保所有数据都被写入
			compressionStream.Flush();
		}

		// 返回存储压缩数据的内存流
		return compressorOutput;
	}
}