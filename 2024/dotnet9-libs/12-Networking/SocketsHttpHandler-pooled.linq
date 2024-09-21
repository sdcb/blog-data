<Query Kind="Program">
  <Namespace>Microsoft.Extensions.DependencyInjection</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

static async Task Main()
{
	// 使用 SocketsHttpHandler
	using var socketsHandler = new SocketsHttpHandler
	{
		PooledConnectionLifetime = TimeSpan.FromMinutes(1)
	};
	using var socketsClient = new HttpClient(socketsHandler)
	{
		BaseAddress = new Uri("https://example.com")
	};

	// 使用 HttpClientHandler
	using var httpClientHandler = new HttpClientHandler();
	using var httpClient = new HttpClient(httpClientHandler)
	{
		BaseAddress = new Uri("https://example.com")
	};

	// 发起请求
	Console.WriteLine("Using SocketsHttpHandler:");
	await MakeRequest(socketsClient);

	Console.WriteLine("\nUsing HttpClientHandler:");
	await MakeRequest(httpClient);

	// 我们创建了两个 HttpClient 实例，一个使用 SocketsHttpHandler，另一个使用 HttpClientHandler。
	// SocketsHttpHandler 支持连接池中的连接生命周期管理，这可以通过设置 PooledConnectionLifetime 来实现。
	// 这对于长时间运行的应用程序尤为重要，因为它允许我们定期轮换连接，从而避免潜在的连接问题。
}

static async Task MakeRequest(HttpClient client)
{
	try
	{
		var response = await client.GetAsync("/");
		Console.WriteLine($"Status Code: {response.StatusCode}");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Exception: {ex.Message}");
	}
}