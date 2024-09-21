<Query Kind="Program">
  <Namespace>System.Reflection.Emit</Namespace>
  <Namespace>System.Reflection.Metadata</Namespace>
  <Namespace>System.Reflection.Metadata.Ecma335</Namespace>
  <Namespace>System.Reflection.PortableExecutable</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	public static void Main()
	{
		Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
		string assemblyPath = "Calculator.dll";
		CreateAndSaveAssembly(assemblyPath);
		UseAssembly(assemblyPath);
	}

	public static void CreateAndSaveAssembly(string assemblyPath)
	{
		// 创建PersistedAssemblyBuilder 实例，传入程序集名称
		PersistedAssemblyBuilder ab = new PersistedAssemblyBuilder(
			new AssemblyName("Calculator"),
			typeof(object).Assembly  //引用核心程序集System.Private.CoreLib
		);

		// 定义动态模块和静态类Calculator
		TypeBuilder tb = ab.DefineDynamicModule("CalcModule")
			.DefineType("Calculator", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed);

		// 定义Sum方法，接受两个int参数并返回它们的和
		MethodBuilder sumMethod = tb.DefineMethod(
			"Sum",
			MethodAttributes.Public | MethodAttributes.Static,
			typeof(int),
			new Type[] { typeof(int), typeof(int) }
		);

		ILGenerator il = sumMethod.GetILGenerator();
		// 加载方法参数到计算堆栈并相加
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Add);
		il.Emit(OpCodes.Ret);

		// 创建类型
		tb.CreateType();

		// 生成元数据
		MetadataBuilder metadataBuilder = ab.GenerateMetadata(
			out BlobBuilder ilStream,
			out BlobBuilder fieldData
		);

		// 创建PE头部信息
		PEHeaderBuilder peHeaderBuilder = new PEHeaderBuilder(
			imageCharacteristics: Characteristics.ExecutableImage);

		// 使用ManagedPEBuilder创建PE
		ManagedPEBuilder peBuilder = new ManagedPEBuilder(
			header: peHeaderBuilder,
			metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
			ilStream: ilStream,
			mappedFieldData: fieldData
		);

		// 序列化PE，写入文件
		BlobBuilder peBlob = new BlobBuilder();
		peBuilder.Serialize(peBlob);

		using var fileStream = new FileStream(assemblyPath, FileMode.Create, FileAccess.Write);
		peBlob.WriteContentTo(fileStream);
	}

	public static void UseAssembly(string assemblyPath)
	{
		// 从文件加载程序集
		Assembly assembly = Assembly.LoadFrom(assemblyPath);
		// 获取Calculator类型和Sum方法
		Type? type = assembly.GetType("Calculator");
		MethodInfo? sumMethod = type?.GetMethod("Sum");

		// 调用Sum方法并输出结果
		if (sumMethod != null)
		{
			int result = (int)sumMethod!.Invoke(null, new object[] { 5, 10 });
			Console.WriteLine($"5 + 10 = {result}");
		}
	}
}