<Query Kind="Program">
  <Namespace>System.Runtime.CompilerServices</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        Guid version7Guid = Guid.CreateVersion7();
        Console.WriteLine("Version 7 Guid: " + version7Guid);
        Console.WriteLine("Guid Version: " + (version7Guid.ToByteArray()[7] >> 4));

        DateTimeOffset dateTime = DateTimeOffset.UtcNow;
        Guid version7GuidWithTimestamp = Guid.CreateVersion7(dateTime);
        Console.WriteLine("Version 7 Guid with Timestamp: " + version7GuidWithTimestamp);

        // 使用自定义函数从 Guid 中提取生成时间。
        DateTimeOffset extractedTime = ExtractTimestampFromVersion7Guid(version7GuidWithTimestamp);
        Console.WriteLine("Extracted Timestamp: " + extractedTime.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));
    }

	// 从 Version 7 Guid 中提取时间戳的函数。
	static DateTimeOffset ExtractTimestampFromVersion7Guid(Guid version7Guid)
	{
		// 将 Guid 转换为字节数组以提取时间戳信息。
		byte[] guidBytes = version7Guid.ToByteArray();

		// 根据 UUID Version 7 的结构，解析时间戳信息。
		long timestamp = (
			((long)Unsafe.As<byte, int>(ref guidBytes[0]) << 16) |
			((long)Unsafe.As<byte, short>(ref guidBytes[4]) & 0xFFFF)
		);

		// 将时间戳转换回 DateTimeOffset。
		return DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
	}
}