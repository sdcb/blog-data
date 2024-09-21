<Query Kind="Program">
  <NuGetReference Prerelease="true">System.Numerics.Tensors</NuGetReference>
  <Namespace>System.Numerics.Tensors</Namespace>
</Query>

using System;

class Program
{
    static void Main()
    {
        // 创建两个ReadOnlySpan<float>类型的向量
        ReadOnlySpan<float> vector1 = new float[] { 1, 2, 3 };
        ReadOnlySpan<float> vector2 = new float[] { 4, 5, 6 };

        // 计算并打印float向量的余弦相似度
        // 使用TensorPrimitives类的CosineSimilarity方法，该方法适用于各种类型
        // 这是float类型计算的结果
        Console.WriteLine(TensorPrimitives.CosineSimilarity(vector1, vector2)); // 输出: 0.9746318

        // 创建两个ReadOnlySpan<double>类型的向量
        ReadOnlySpan<double> vector3 = new double[] { 1, 2, 3 };
        ReadOnlySpan<double> vector4 = new double[] { 4, 5, 6 };

        // 计算并打印double向量的余弦相似度
        // 这是double类型计算的结果，可以看到精度的区别
		// 注：之前System.Numerics.Tensor 8.0.0不提供此API
        Console.WriteLine(TensorPrimitives.CosineSimilarity(vector3, vector4)); // 输出: 0.9746318461970762
		

		// 通过比较两种不同精度的浮点数可以看出，double类型提供了更高的精度
		// 这在某些需要高精度计算的场景中非常重要
	}
}