<Query Kind="Program">
  <Namespace>Microsoft.Extensions.DependencyInjection</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static void Main()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient("ExampleClient", client =>
        {
            client.BaseAddress = new Uri("https://example.com");
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ExampleClient");

        // 获取 HttpMessageInvoker 中的 _handler 字段
        var handlerField = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);
        var currentHandler = handlerField?.GetValue(httpClient) as HttpMessageHandler;

        // 遍历查找 InnerHandler 为 null 的处理程序
        while (currentHandler != null)
        {
            var innerHandlerProperty = currentHandler.GetType().GetProperty("InnerHandler", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            var innerHandler = innerHandlerProperty?.GetValue(currentHandler) as HttpMessageHandler;

            if (innerHandler == null)
            {
                break;
            }

            currentHandler = innerHandler;
        }

        // 打印当前处理程序的类型
        if (currentHandler != null)
        {
            Console.WriteLine($"The handler at the end of the chain is: {currentHandler.GetType().Name}");
        }
        else
        {
            Console.WriteLine("No handler found.");
        }

		// 我们通过遍历 InnerHandler 链，找到了处理程序链的终点。
		// 这里展示了 HttpClient 是如何组织其内部结构的。
		// 打印出的类型告诉我们，结束点的处理程序类型。
	}
}