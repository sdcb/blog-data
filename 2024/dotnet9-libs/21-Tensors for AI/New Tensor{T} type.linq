<Query Kind="Program">
  <NuGetReference Prerelease="true">System.Numerics.Tensors</NuGetReference>
  <Namespace>System.Numerics.Tensors</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

#pragma warning disable SYSLIB5001

class Program
{
	static void Main()
	{
		// 创建一个一维张量（1行3列）。
		Tensor<int> t0 = Tensor.Create(new int[] { 1, 2, 3 }, [ 1, 3 ]); // [[1, 2, 3]]
		Console.WriteLine("Original Tensor (t0):");
		PrintTensor(t0);

		// 调整张量形状为（3行1列）。
		Tensor<int> t1 = t0.Reshape(3, 1); // [[1], [2], [3]]
		Console.WriteLine("Reshaped Tensor (t1):");
		PrintTensor(t1);

		// 切片操作，获取从第二行开始的子张量。
		Tensor<int> t2 = t1.Slice(1.., ..); // [[2], [3]]
		Console.WriteLine("Sliced Tensor (t2):");
		PrintTensor(t2);

		// 广播张量，将其扩展到（3行3列）。
		// 广播后：
		// [
		//  [ 1, 1, 1],
		//  [ 2, 2, 2],
		//  [ 3, 3, 3]
		// ]
		Tensor<int> t3 = Tensor.Broadcast<int>(t1, [3, 3]);
		Console.WriteLine("Broadcasted Tensor (t3):");
		PrintTensor(t3);

		// 各种数学运算示例。

		// 为每个元素加1。
		Tensor<int> t4 = Tensor.Add(t0, 1); // [[2, 3, 4]]
		Console.WriteLine("Tensor Add 1 (t4):");
		PrintTensor(t4);

		// 将t0和t0相加。
		Tensor<int> t5 = Tensor.Add<int>(t0, t0); // [[2, 4, 6]]
		Console.WriteLine("Tensor Add t0+t0 (t5):");
		PrintTensor(t5);

		// 为每个元素减1。
		Tensor<int> t6 = Tensor.Subtract(t0, 1); // [[0, 1, 2]]
		Console.WriteLine("Tensor Subtract 1 (t6):");
		PrintTensor(t6);

		// 将t0和t0相减。
		Tensor<int> t7 = Tensor.Subtract<int>(t0, t0); // [[0, 0, 0]]
		Console.WriteLine("Tensor Subtract t0-t0 (t7):");
		PrintTensor(t7);

		// 每个元素乘以2。
		Tensor<int> t8 = Tensor.Multiply(t0, 2); // [[2, 4, 6]]
		Console.WriteLine("Tensor Multiply by 2 (t8):");
		PrintTensor(t8);

		// 将t0元素逐一平方。
		Tensor<int> t9 = Tensor.Multiply<int>(t0, t0); // [[1, 4, 9]]
		Console.WriteLine("Tensor Element-wise Multiply t0*t0 (t9):");
		PrintTensor(t9);

		// 每个元素除以2。
		Tensor<int> t10 = Tensor.Divide(t0, 2); // [[0, 1, 1]]
		Console.WriteLine("Tensor Divide by 2 (t10):");
		PrintTensor(t10);

		// 将t0元素逐一相除。
		Tensor<int> t11 = Tensor.Divide<int>(t0, t0); // [[1, 1, 1]]
		Console.WriteLine("Tensor Element-wise Divide t0/t0 (t11):");
		PrintTensor(t11);
	}

	// 辅助方法：输出张量的值
	static void PrintTensor<T>(Tensor<T> tensor)
	{
		// 假设Tensor有个方法ToString返回元内容，需要根据实际API调整
		Console.WriteLine(tensor.ToString());
	}
}

// 添加注释：
// 这是实验性的API，强调目前这是.NET 9中的一个测试特性，必须标注：#pragma warning disable SYSLIB5001