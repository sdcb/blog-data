<Query Kind="Program">
  <Namespace>System.Diagnostics.Metrics</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        // 创建一个 Meter 实例，用于度量相关测量
        // 此处创建了名为 "MeasurementLibrary.Sound" 的 Meter
        Meter soundMeter = new("MeasurementLibrary.Sound");

        // 创建一个 Gauge<int> 实例来测量背景噪声的水平
        // Gauge 是一个适合记录非加性值的工具类，比如噪声水平
        Gauge<int> gauge = soundMeter.CreateGauge<int>(
            name: "NoiseLevel",  // 度量的名称
            unit: "dB",          // 单位为分贝（Decibels）
            description: "Background Noise Level"  // 对度量的描述
        );

		// 注册回调以处理每个记录的值
		using MeterListener listener = new();
		listener.SetMeasurementEventCallback<int>((Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
		{
			// 记录我们进行的操作
			Console.Write($"Instrument: {instrument.Name}, Measurement: {measurement} dB, Tags: ");
			foreach (var tag in tags)
			{
				Console.Write($"({tag.Key}: {tag.Value}),");
			}
			Console.WriteLine();
		});
		listener.InstrumentPublished = (instrument, listener) =>
		{
			if (instrument.Name == "NoiseLevel")
			{
				listener.EnableMeasurementEvents(instrument);
			}
		};
		listener.Start();

		// 使用 Gauge 记录一个新的值
		// 这里记录了一个噪声值为 10 的数据点
		// 通过 TagList 可以指定一些附加信息，比如房间编号
		gauge.Record(10, new TagList() { { "Room1", "dB" } });
		gauge.Record(7, new TagList() { { "Room2", "dB" } });
	}
}