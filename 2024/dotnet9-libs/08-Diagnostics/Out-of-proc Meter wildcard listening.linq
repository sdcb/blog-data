<Query Kind="Program">
  <Namespace>System.Diagnostics.Metrics</Namespace>
  <Namespace>System.Diagnostics.Tracing</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// 演示如何使用 .NET 9 中的通配符功能来监听所有 Meter
class Program
{
    static void Main()
    {
        // 创建一个 Meter，名称为 "MyCompany.MyMeter"
        var meter = new Meter("MyCompany.MyMeter");

		// 创建一个可观察的计数器，提供一个值用于发布
		meter.CreateObservableCounter("MyCounter", () => 1);
		meter.CreateObservableCounter("MyCounter2", () => 2);

		// 创建自定义的事件监听器
        var listener = new MyEventListener();
        
        // 在 .NET 9 中，可以使用通配符 * 来监听所有 Meter
        // 在此示例中，监听所有名称以 "MyCompany" 开头的 Meter
    }
}

// 自定义事件监听器类，继承自 EventListener
internal class MyEventListener : EventListener
{
    // 当创建新的事件源时调用此方法
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // 打印事件源的名称
        Console.WriteLine(eventSource.Name);
        if (eventSource.Name == "System.Diagnostics.Metrics")
        {
            // 通过事件源启用事件，设置过滤器以便监听 Meter 名称以 "MyCompany" 开头的
            EnableEvents(
                eventSource,
				EventLevel.Informational,
				(EventKeywords)0x3,
				new Dictionary<string, string?>() { { "Metrics", "MyCompany*" } }
			);
		}
	}

	// 当事件源发布事件时调用此方法
	protected override void OnEventWritten(EventWrittenEventArgs eventData)
	{
		// 过滤掉不感兴趣的事件
		if (eventData.EventSource.Name != "System.Diagnostics.Metrics" ||
			eventData.EventName == "CollectionStart" ||
			eventData.EventName == "CollectionStop" ||
			eventData.EventName == "InstrumentPublished")
			return;

		// 打印事件名称
		Console.WriteLine(eventData.EventName);

		// 如果有有效载荷（就是事件所携带的数据）
		if (eventData.Payload is not null)
		{
			for (int i = 0; i < eventData.Payload.Count; i++)
				// 格式化打印每一个有效载荷的名称和值
				Console.WriteLine($"\t{eventData.PayloadNames![i]}: {eventData.Payload[i]}");
		}
	}
}