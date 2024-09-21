<Query Kind="Program">
  <Namespace>System.Numerics</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class PerformanceDemo
{
	// 分别在.NET 8和.NET 9中运行
	// .NET 8参考结果：
	// Matrix4x4 Multiply: 62 ms
	// Quaternion Concatenate: 370 ms
	// SinCos: 822 ms

	// .NET 9参考结果：
	// Matrix4x4 Multiply: 26 ms
	// Quaternion Concatenate: 20 ms
	// SinCos: 20 ms
	static void Main()
    {
        int N = 1_0000_0000;
        
        // 创建向量和矩阵用于演示
        Vector3 v1 = new Vector3(1, 2, 3);
        Vector3 v2 = new Vector3(4, 5, 6);
        Matrix4x4 matrix = Matrix4x4.Identity;
        
        Stopwatch stopwatch = new Stopwatch();

        // 1. 矩阵乘法性能测试
        stopwatch.Start();
        for (int i = 0; i < N; i++)
        {
            _ = Matrix4x4.Multiply(matrix, matrix);
        }
        stopwatch.Stop();
        Console.WriteLine($"Matrix4x4 Multiply: {stopwatch.ElapsedMilliseconds} ms");

        // 2. 四元数连接性能测试
        Quaternion q1 = Quaternion.Identity;
        Quaternion q2 = Quaternion.Identity;
        stopwatch.Restart();
        for (int i = 0; i < N; i++)
        {
            _ = Quaternion.Concatenate(q1, q2);
        }
        stopwatch.Stop();
        Console.WriteLine($"Quaternion Concatenate: {stopwatch.ElapsedMilliseconds} ms");

		// 3. SinCos 性能测试
		double angle = Math.PI / 4;
		stopwatch.Restart();
		for (int i = 0; i < N; i++)
		{
			_ = SinCos(angle);
		}
		stopwatch.Stop();
		Console.WriteLine($"SinCos: {stopwatch.ElapsedMilliseconds} ms");
	}

	// SinCos 方法
	static (double, double) SinCos(double x)
	{
		return (Math.Sin(x), Math.Cos(x));
	}
}