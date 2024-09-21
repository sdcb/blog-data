<Query Kind="Program">
  <Namespace>System.Formats.Tar</Namespace>
  <Namespace>System.IO.Compression</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static async Task Main()
	{
		Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");

		// 创建一个example.tar文件用于演示
		CreateTarFile();

		// 打开example.tar文件进行读取
		using FileStream fileStream = new FileStream("example.tar", FileMode.Open, FileAccess.Read);

		// 使用TarReader从文件流中解析Tar文件
		TarReader tarReader = new TarReader(fileStream);

		// 获取第一个TarEntry
		TarEntry? tarEntry = await tarReader.GetNextEntryAsync();

		// 判断tarEntry是否为空
		if (tarEntry is null)
		{
			Console.WriteLine("No entries found in TAR file.");
			return;
		}

		// 使用TarEntry.DataOffset属性获取数据在文件流中的实际偏移位置
		long entryOffsetInFileStream = tarEntry.DataOffset;
		long entryLength = tarEntry.Length;

		// 输出TarEntry的信息
		Console.WriteLine($"Entry Name: {tarEntry.Name}");
		Console.WriteLine($"Data Offset: {entryOffsetInFileStream}");
		Console.WriteLine($"Entry Length: {entryLength}");

		// 定位文件流到TarEntry数据的起始位置
		fileStream.Seek(entryOffsetInFileStream, SeekOrigin.Begin);

		// 读取TarEntry的数据
		byte[] data = new byte[entryLength];
		await fileStream.ReadAsync(data, 0, (int)entryLength);
		Console.WriteLine($"Data: {Encoding.UTF8.GetString(data)}");

		// 在此输出或处理数据
		Console.WriteLine($"Read {data.Length} bytes from TAR entry.");
	}

	static void CreateTarFile()
	{
		// 用于演示的样本数据
		byte[] sampleData = Encoding.UTF8.GetBytes("Hello, this is a sample data for the TAR file.");

		// 创建example.tar文件
		using FileStream tarFileStream = new FileStream("example.tar", FileMode.Create, FileAccess.Write);
		using TarWriter tarWriter = new TarWriter(tarFileStream, leaveOpen: true);

		// 使用PaxTarEntry创建一个具体的TarEntry
		PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, "sample.txt");
		entry.DataStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello World"));

		tarWriter.WriteEntry(entry);

		// 写入实际的数据
		tarFileStream.Write(sampleData, 0, sampleData.Length);

		Console.WriteLine("example.tar created with sample.txt entry.");
	}
}