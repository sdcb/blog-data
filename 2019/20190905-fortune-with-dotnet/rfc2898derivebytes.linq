<Query Kind="Statements">
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

GetForturn("张三", new DateTime(1994, 10, 24), 0).Dump("张三");
GetForturn("李四", new DateTime(1996, 10, 24), 0).Dump("李四");
GetForturn("王五", new DateTime(1996, 10, 24), 0).Dump("王五");

int GetForturn(string name, DateTime birthDay, int faithCount)
{
	using (var h = new Rfc2898DeriveBytes(name + birthDay + faithCount,
		salt: new byte[8] { 44, 2, 3, 4, 5, 6, 7, 8 },
		iterations: 10086))
	{
		return (int)(BitConverter.ToUInt64(h.GetBytes(8), 0) % 100) + 1;
	};
}