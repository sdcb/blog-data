<Query Kind="Statements">
  <Namespace>System.Runtime.Serialization.Formatters.Binary</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
</Query>

// 初始化一个 Person 对象
var person = new Person { Name = "Alice", Age = 30 };

// 尝试使用BinaryFormatter进行序列化
IFormatter formatter = new BinaryFormatter();
using (var stream = new MemoryStream())
{
	try
	{
		// 序列化对象到内存流
		formatter.Serialize(stream, person);

		// 输出流的大小
		Console.WriteLine("Serialized data length: " + stream.Length);

		// 重置流的位置到起点，为即将开始的反序列化准备
		stream.Seek(0, SeekOrigin.Begin);

		// 尝试反序列化内存流中的对象
		var deserializedPerson = (Person)formatter.Deserialize(stream);

		// 输出反序列化后的对象信息
		Console.WriteLine($"Deserialized Person: Name = {deserializedPerson.Name}, Age = {deserializedPerson.Age}");
	}
	catch (Exception ex)
	{
		// 捕获异常，因为在.NET 9中会抛出异常
		Console.WriteLine("Serialization/Deserialization failed with exception: " + ex.Message);
	}
}
[Serializable]
public class Person
{
	public required string Name { get; set; }
	public required int Age { get; set; }
}