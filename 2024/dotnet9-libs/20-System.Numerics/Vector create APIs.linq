<Query Kind="Program">
  <Namespace>System.Numerics</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 示例 1: 使用 Vector.Create 创建一个全值向量
        Vector<int> vector = Vector.Create(5);
        Console.WriteLine("Vector (全值5):");
        for (int i = 0; i < Vector<int>.Count; i++)
        {
            Console.Write($"{vector[i]} ");
        }
        Console.WriteLine();

        // 示例 2: 使用 Vector2.Create 创建一个 Vector2
        Vector2 vector2 = Vector2.Create(1.0f, 2.0f);
        Console.WriteLine($"Vector2: {vector2}");

		// 示例 3: 使用 Vector3.Create 创建一个 Vector3
		Vector3 vector3 = Vector3.Create(1.0f, 2.0f, 3.0f);
		Console.WriteLine($"Vector3: {vector3}");

		// 示例 4: 使用 Vector4.Create 创建一个 Vector4
		Vector4 vector4 = Vector4.Create(1.0f, 2.0f, 3.0f, 4.0f);
		Console.WriteLine($"Vector4: {vector4}");

		// 示例 5: 不同类型之间的向量转换
		// 将 Vector4 转换为 Quaternion 和 Plane（零成本）
		Quaternion quaternion = vector4.AsQuaternion();
		Plane plane = vector4.AsPlane();
		Console.WriteLine($"Quaternion from Vector4: {quaternion}");
		Console.WriteLine($"Plane from Vector4: {plane}");

		// 缩小转换
		Vector2 shrunkVector2 = vector4.AsVector2();
		Vector3 shrunkVector3 = vector4.AsVector3();
		Console.WriteLine($"Shrunk Vector2 from Vector4: {shrunkVector2}");
		Console.WriteLine($"Shrunk Vector3 from Vector4: {shrunkVector3}");

		// 扩大转换
		Vector4 expandedFromVector2 = shrunkVector2.AsVector4();
		Vector4 expandedFromVector3 = shrunkVector3.AsVector4();
		Console.WriteLine($"Expanded from Vector2: {expandedFromVector2}");
		Console.WriteLine($"Expanded from Vector3: {expandedFromVector3}");

		// 使用不安全（Unsafe）方法进行零成本扩大转换
		Vector4 expandedUnsafeFromVector2 = shrunkVector2.AsVector4Unsafe();
		Vector4 expandedUnsafeFromVector3 = shrunkVector3.AsVector4Unsafe();
		Console.WriteLine($"Expanded Unsafe from Vector2: {expandedUnsafeFromVector2}");
		Console.WriteLine($"Expanded Unsafe from Vector3: {expandedUnsafeFromVector3}");
	}
}