<Query Kind="Program">
  <Namespace>System.Numerics</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static void Main()
	{
		// 创建一个Vector4，其中x, y, z, w分量初始化为1
		Vector4 vector4 = new Vector4(1, 2, 3, 4);

		// 1. 相同大小的零成本转换
		// 将Vector4转换为Quaternion（四元数）
		Quaternion quaternion = vector4.AsQuaternion();
		// 将Vector4转换为Plane（平面）
		Plane plane = vector4.AsPlane();
		Console.WriteLine($"Quaternion: {quaternion}");
		Console.WriteLine($"Plane: {plane}");

		// 2. 缩小转换（从Vector4到Vector2/Vector3）
		// 将Vector4转换为Vector2，自动丢弃z, w分量
		Vector2 vector2 = vector4.AsVector2();
		Console.WriteLine($"Vector2: {vector2}");

		// 将Vector4转换为Vector3，自动丢弃w分量
		Vector3 vector3 = vector4.AsVector3();
		Console.WriteLine($"Vector3: {vector3}");

		// 3. 扩大转换（从Vector2/Vector3到Vector4）
		// 使用正常API方法，将新元素初始化为0
		Vector4 expandedFromVector2 = vector2.AsVector4();
		Vector4 expandedFromVector3 = vector3.AsVector4();
		Console.WriteLine($"Expanded from Vector2: {expandedFromVector2}");
		Console.WriteLine($"Expanded from Vector3: {expandedFromVector3}");

		// 使用不安全（Unsafe）方法，不定义新元素，可能是零成本
		Vector4 expandedUnsafeFromVector2 = vector2.AsVector4Unsafe();
		Vector4 expandedUnsafeFromVector3 = vector3.AsVector4Unsafe();
		Console.WriteLine($"Expanded Unsafe from Vector2: {expandedUnsafeFromVector2}");
		Console.WriteLine($"Expanded Unsafe from Vector3: {expandedUnsafeFromVector3}");
	}
}