<Query Kind="Statements" />

GetForturn("周杰").Dump("周杰");
GetForturn("张三").Dump("张三");
GetForturn("李四").Dump("李四");
GetForturn("王五").Dump("王五");

int GetForturn(string name)
{
	return Math.Abs(name.GetHashCode() % 100) + 1;
}