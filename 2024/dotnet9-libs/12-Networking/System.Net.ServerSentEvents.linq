<Query Kind="Program">
  <NuGetReference Prerelease="true">System.Net.ServerSentEvents</NuGetReference>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Net.ServerSentEvents</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

class Program
{
    static async Task Main()
    {
		// 初始化HttpClient对象，用于发送HTTP请求
		using HttpClient client = new HttpClient();
        
        // 设置请求的URL为OpenAI兼容的聊天接口
        var requestUri = "https://chats-api.starworks.cc:88/api/openai-compatible/chat/completions";

		// 准备请求内容，包含聊天信息和配置
		var jsonContent = new StringContent(@"
        {
            ""messages"":[
                {""role"":""system"",""content"":""看看这张图里面有什么？""},
                {""role"":""user"",""content"":[{""type"":""image_url"",""image_url"":{""url"":""https://io.starworks.cc:88/paddlesharp/ocr/samples/xdr5450.webp""}}]}
            ],
            ""model"":""通义千问-vl-max-0809"",
            ""stream"":true,
            ""stream_options"":{""include_usage"":true},
            ""temperature"":1
        }", System.Text.Encoding.UTF8, "application/json");

		// 设置Authorization头，包含Bearer token（替换为你的token）
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "sk-******");

		// 发送POST请求，获取响应流
		HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = jsonContent };
		using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

		// 确保请求成功
		response.EnsureSuccessStatusCode();

		// 从响应中获取流式数据
		var responseStream = await response.Content.ReadAsStreamAsync();

		// 使用SseParser解析Server-Sent Events响应
		await foreach (SseItem<string> e in SseParser.Create(responseStream).EnumerateAsync())
		{
			// 逐个输出解析到的事件数据到控制台
			Console.WriteLine(e.Data);
		}
	}
}