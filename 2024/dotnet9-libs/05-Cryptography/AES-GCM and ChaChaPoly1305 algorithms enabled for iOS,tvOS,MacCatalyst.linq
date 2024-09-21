<Query Kind="Program">
  <Namespace>System.Security.Cryptography</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
	{
		// 检查操作系统是否支持AES-GCM和ChaChaPoly1305算法
		if (AesGcm.IsSupported || ChaCha20Poly1305.IsSupported)
		{
			Console.WriteLine("AES-GCM或ChaCha20Poly1305算法在当前系统上被支持。");
		}
		else
		{
			Console.WriteLine("AES-GCM和ChaCha20Poly1305算法在当前系统上不被支持。");
			return;
		}

		// 定义密钥、nonce和数据
		// 密钥必须为256位
		byte[] key = new byte[32];
		byte[] nonce = new byte[12];
		byte[] dataToEncrypt = System.Text.Encoding.UTF8.GetBytes("Here is the data to be encrypted.");
		byte[] tag = new byte[16]; // 16字节的标签（128位），Apple操作系统限制

		// 填充密钥和nonce（通常你需要使用安全的方式生成这些）
		RandomNumberGenerator.Fill(key);
		RandomNumberGenerator.Fill(nonce);

		// 创建一个加密的数据缓冲区
		byte[] encryptedData = new byte[dataToEncrypt.Length];

		try
		{
			// 使用AES-GCM进行加密
			using (AesGcm aesGcm = new AesGcm(key, 16))
			{
				aesGcm.Encrypt(nonce, dataToEncrypt, encryptedData, tag);
			}

			Console.WriteLine("数据加密成功。");
			// 在演讲中可以展示加密后的数据
			Console.WriteLine("加密后的数据:" + Convert.ToBase64String(encryptedData));
		}
		catch (Exception ex)
		{
			Console.WriteLine("加密失败：" + ex.Message);
		}

		// 这里我们可以继续演示解密过程
		byte[] decryptedData = new byte[encryptedData.Length];

		try
		{
			// 使用AES-GCM进行解密
			using (AesGcm aesGcm = new AesGcm(key, 16))
			{
				aesGcm.Decrypt(nonce, encryptedData, tag, decryptedData);
			}

			Console.WriteLine("数据解密成功。");
			// 展示解密后的明文数据
			Console.WriteLine("解密后的数据: " + System.Text.Encoding.UTF8.GetString(decryptedData));
		}
		catch (Exception ex)
		{
			Console.WriteLine("解密失败：" + ex.Message);
		}
	}
}