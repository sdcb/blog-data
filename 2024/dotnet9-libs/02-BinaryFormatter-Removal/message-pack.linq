<Query Kind="Statements">
  <NuGetReference>MessagePack</NuGetReference>
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>System.Runtime.Serialization.Formatters.Binary</Namespace>
  <Namespace>MessagePack</Namespace>
</Query>

// 初始化一个 Person 对象
var person = new Person { Name = "Alice", Age = 30 };

// 使用MessagePack进行序列化
byte[] serializedData = MessagePackSerializer.Serialize(person);

// 输出序列化后的数据长度
Console.WriteLine("Serialized data length (MessagePack): " + serializedData.Length);

// 反序列化回 Person 对象
var deserializedPerson = MessagePackSerializer.Deserialize<Person>(serializedData);

// 输出还原后的对象信息
Console.WriteLine($"Deserialized Person: Name = {deserializedPerson.Name}, Age = {deserializedPerson.Age}");


// 使用 MessagePack 进行序列化（推荐方式）
[MessagePackObject]
public class Person
{
	[Key(0)]
	public required string Name { get; set; }

	[Key(1)]
	public required int Age { get; set; }
}