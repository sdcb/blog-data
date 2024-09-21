<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static void Main()
	{
		// 初始化一个byte类型的变量
		byte originalValue = 10;
		byte newValue = 20;
		byte comparand = 10;

		// 使用Interlocked.CompareExchange进行原子操作
		// 比较originalValue和comparand，如果相等则将原子更新为newValue
		byte result = Interlocked.CompareExchange(ref originalValue, newValue, comparand);

		// 输出结果
		// 当originalValue和comparand相等时，originalValue将被更新为newValue
		Console.WriteLine($"Result: {result}, Updated Value: {originalValue}");

		// 由于Interlocked.CompareExchange现在支持更多的原始类型
		// 我们可以用它来处理sbyte, short, ushort等
		short originalShort = 100;
		short newShort = 200;
		short comparandShort = 100;

		// 对short类型变量进行同样的原子操作
		short resultShort = Interlocked.CompareExchange(ref originalShort, newShort, comparandShort);

		// 输出短整型操作的结果
		Console.WriteLine($"Short Result: {resultShort}, Updated Short Value: {originalShort}");

		// 使用Interlocked.Exchange进行原子操作
		// 交换现有的值为一个新值
		int exchangingValue = 42;
		int newExchangeValue = 84;

		// 输出之前交换的值
		int exchangedResult = Interlocked.Exchange(ref exchangingValue, newExchangeValue);
		Console.WriteLine($"Exchanged Result: {exchangedResult}, New Value: {exchangingValue}");

		// 演示泛型版本的Interlocked.Exchange<T>
		// 在.NET 9中，可以适用于任何原始类型，例如bool、char和enum类型
		bool originalBool = false;
		bool newBool = true;

		// 对bool类型变量进行交换操作
		bool exchangedBool = Interlocked.Exchange(ref originalBool, newBool);
		Console.WriteLine($"Boolean Exchanged: {exchangedBool}, New Boolean Value: {originalBool}");
	}
}