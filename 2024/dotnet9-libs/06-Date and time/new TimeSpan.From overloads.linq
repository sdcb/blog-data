<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static void Main()
	{
		// 使用原始的 TimeSpan.FromSeconds(double) 方法
		// double 类型无法精确表示小数，因此可能导致微小的误差
		TimeSpan timeSpan1 = TimeSpan.FromSeconds(value: 101.832);
		Console.WriteLine($"timeSpan1 = {timeSpan1}");
		// 输出的结果中，毫秒部分存在轻微的误差，预期结果是 101 秒和 832 毫秒
		// 输出: timeSpan1 = 00:01:41.8319999

		// 使用 .NET 9 中新增的 TimeSpan.FromSeconds(int, int) 重载方法
		// 通过整数来创建 TimeSpan 对象，避免了浮点数的误差问题
		TimeSpan timeSpan2 = TimeSpan.FromSeconds(seconds: 101, milliseconds: 832);
		Console.WriteLine($"timeSpan2 = {timeSpan2}");
		// 这次输出的结果更加精确，没有误差
		// 输出: timeSpan2 = 00:01:41.8320000

		// 这个例子展示了如何使用整数而非浮点数表示时间间隔，从而提高了精确度
		// 这种方法在处理时间敏感的数据时尤其有用
	}
}