<Query Kind="Program">
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
	static async Task Main(string[] args)
	{
		using HttpClient http1 = new();
		using HttpClient http2 = new();
		Stopwatch stopwatch = new();

		// 记录并行处理的时间
		stopwatch.Start();

		// 使用两个 HttpClient 实例创建并发起多个异步请求
		var dotnetTask = FetchWebsiteContentAsync(http1, "http://dot.net");
		var bingTask = FetchWebsiteContentAsync(http1, "http://www.bing.com");
		var msTask = FetchWebsiteContentAsync(http1, "http://microsoft.com");

		// 使用 Task.WhenEach 处理每个任务的完成结果
		await foreach (var websiteContentTask in Task.WhenEach(dotnetTask, bingTask, msTask))
		{
			var websiteContent = websiteContentTask.Result;
			Console.WriteLine($"Website: {websiteContent.WebsiteUrl}, Content Length: {websiteContent.Content.Length}");
		}

		stopwatch.Stop();
		Console.WriteLine($"并行处理耗时: {stopwatch.ElapsedMilliseconds} ms");

		// 记录顺序处理的时间
		stopwatch.Restart();

		// 使用单个 HttpClient 按顺序处理每个任务
		var dotnetContent = await FetchWebsiteContentAsync(http2, "http://dot.net");
		Console.WriteLine($"Website: {dotnetContent.WebsiteUrl}, Content Length: {dotnetContent.Content.Length}");

		var bingContent = await FetchWebsiteContentAsync(http2, "http://www.bing.com");
		Console.WriteLine($"Website: {bingContent.WebsiteUrl}, Content Length: {bingContent.Content.Length}");

		var msContent = await FetchWebsiteContentAsync(http2, "http://microsoft.com");
		Console.WriteLine($"Website: {msContent.WebsiteUrl}, Content Length: {msContent.Content.Length}");

		stopwatch.Stop();
		Console.WriteLine($"顺序处理耗时: {stopwatch.ElapsedMilliseconds} ms");
	}

	static async Task<WebsiteContent> FetchWebsiteContentAsync(HttpClient httpClient, string url)
	{
		string content = await httpClient.GetStringAsync(url);
		return new WebsiteContent
		{
			WebsiteUrl = url,
			Content = content
		};
	}
}

// 定义一个类来存储网站的信息，包括URL和内容
record WebsiteContent
{
	public required string WebsiteUrl { get; init; }
	public required string Content { get; init; }
}