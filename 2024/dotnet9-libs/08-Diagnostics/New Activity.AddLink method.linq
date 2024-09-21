<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 创建一个随机的 ActivityContext
        // 这个上下文会有随机的 TraceId 和 SpanId，并且不包含任何标志
        ActivityContext activityContext = new(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.None
        );

        // 基于上述上下文创建一个 ActivityLink
        ActivityLink activityLink = new(activityContext);

		// 创建一个新的 Activity 对象，并命名为 "LinkTest"
		Activity activity = new("LinkTest");

		// 演示 .NET 9 新增的 Activity.AddLink 方法
		// 这个 API 允许在 Activity 创建后，链接其他的 tracing 上下文
		activity.AddLink(activityLink);

		// 输出活动信息以验证链接
		// 这有助于在调试时检查 trace 数据
		Console.WriteLine($"Activity Name: {activity.DisplayName}");
		Console.WriteLine($"Links: {activity.Links.Count()}");
		
		activity.Dump();
	}
}