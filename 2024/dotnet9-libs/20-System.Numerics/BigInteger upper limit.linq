<Query Kind="Program">
  <Namespace>System.Numerics</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 演示 .NET 9 中 BigInteger 的位数限制更新
        // 初始化字节数组以表示接近最大长度的 BigInteger
        
        // 数组填充为 0xff 并将最后一个字节设定为 0x7f
        // 这使得数字成为可表示的最大值
        // 例如：2147483647 为 0xff, 0xff, 0xff, 0x7f（小端）
        byte[] data = new byte[int.MaxValue / 8 - 7];
        Array.Fill(data, (byte)0xff);
        data[data.Length - 1] = 0x7f;

		// 创建 BigInteger 实例
		BigInteger bigNumber = new BigInteger(data);
		bigNumber = bigNumber * 2 + 1;

		// 输出 BigInteger 的位数
        // Log10 方法用于估算位数，与观众共同探讨其意义
        Console.WriteLine($"BigInteger 位数: {BigInteger.Log10(bigNumber):N0}");

		// 执行加法操作触发处理边界情况
		// 在 .NET 9 中，监测限制带来的可能异常
		try
		{
			bigNumber--;
			bigNumber++; // 这两行代码是安全的，因为没有到达最小限制
			bigNumber++; // 这行代码会在.NET 9中触发异常
		}
		catch (OverflowException ex)
		{
			// 捕获可能的溢出异常以展示限制影响
			// 将.NET 9改为.NET 8，然后重新运行一遍
			Console.WriteLine("运算导致溢出: " + ex.Message);
		}
	}
}