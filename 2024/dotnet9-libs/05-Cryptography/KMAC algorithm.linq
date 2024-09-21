<Query Kind="Program">
  <Namespace>System.Security.Cryptography</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static void Main()
	{
		// 判断 KMAC128 算法是否支持
		if (Kmac128.IsSupported)
		{
			// 获取用于 KMAC 的密钥
			byte[] key = GetKmacKey();

			// 获取需要进行 MAC 的输入数据
			byte[] input = GetInputToMac();

			// 通过 KMAC128 的 HashData 方法计算消息认证码
			byte[] mac = Kmac128.HashData(key, input, outputLength: 32);

			// 输出结果
			Console.WriteLine("MAC 计算成功: " + BitConverter.ToString(mac));
		}
		else
		{
			// KMAC 不可用时的处理方案
			Console.WriteLine("KMAC128 算法在此平台上不支持。");
		}
	}

	// 模拟获取 KMAC 密钥的方法
	static byte[] GetKmacKey()
	{
		// 在实际应用中，这个密钥应安全存储并管理
		return new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };
	}

	// 模拟获取输入数据的方法
	static byte[] GetInputToMac()
	{
		// 输入数据可以是任何需要进行认证的字节数组
		return new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 };
	}
}