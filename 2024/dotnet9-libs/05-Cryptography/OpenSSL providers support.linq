<Query Kind="Program">
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 示例数据，我们将使用这个数据进行签名
        byte[] data = new byte[] { /* 数据内容 */ };
        
        // 使用SafeEvpPKeyHandle类打开来自OpenSSL provider的密钥
        // 这里使用的是tpm2 provider，并指定密钥句柄
        using (SafeEvpPKeyHandle priKeyHandle = SafeEvpPKeyHandle.OpenKeyFromProvider("tpm2", "handle:0x81000007"))
        {
            // 使用ECDsaOpenSsl类与打开的密钥交互
            using (ECDsa ecdsaPri = new ECDsaOpenSsl(priKeyHandle))
            {
                // 使用SHA256哈希算法对数据进行签名
                byte[] signature = ecdsaPri.SignData(data, HashAlgorithmName.SHA256);
                
                // 将生成的签名进行处理，比如存储、验证等
                Console.WriteLine("签名创建成功。");
            }
        }

		// 使用新的API，增加了对OpenSSL providers的支持
		// 原支持的ENGINE组件有些发行版已不推荐使用，.NET 9 提供更好的解决方案
	}
}