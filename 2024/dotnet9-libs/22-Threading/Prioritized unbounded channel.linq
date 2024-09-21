<Query Kind="Program">
  <Namespace>System.Threading.Channels</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static async Task Main(string[] args)
    {
        // 自定义比较器，比较爬虫请求的优先级
        IComparer<CrawlerRequest> comparer = Comparer<CrawlerRequest>.Create((x, y) => x.Priority.CompareTo(y.Priority));

        // 使用自定义比较器创建无界限优先级Channel
        var options = new UnboundedPrioritizedChannelOptions<CrawlerRequest>()
        {
            Comparer = comparer
        };

        Channel<CrawlerRequest> channel = Channel.CreateUnboundedPrioritized(options);

		// 创建一些爬虫请求并写入Channel
		await channel.Writer.WriteAsync(new CrawlerRequest("example.com", 3));
		await channel.Writer.WriteAsync(new CrawlerRequest("microsoft.com", 5));
		await channel.Writer.WriteAsync(new CrawlerRequest("starworks.cc", 1));
		await channel.Writer.WriteAsync(new CrawlerRequest("sdcb.cc", 2));
		await channel.Writer.WriteAsync(new CrawlerRequest("bing.com", 4));

		// 完成写入操作
		channel.Writer.Complete();

		// 读取并输出爬虫请求，以优先级顺序
		while (await channel.Reader.WaitToReadAsync())
		{
			while (channel.Reader.TryRead(out CrawlerRequest? request))
			{
				Console.WriteLine($"Website: {request.Website}, Priority: {request.Priority}");
			}
		}
	}
}

// 爬虫请求类，包含网站和优先级
class CrawlerRequest
{
	public string Website { get; }
	public int Priority { get; }

	public CrawlerRequest(string website, int priority)
	{
		Website = website;
		Priority = priority;
	}
}
