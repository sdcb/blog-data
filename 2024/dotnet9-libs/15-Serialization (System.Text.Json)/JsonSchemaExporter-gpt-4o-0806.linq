<Query Kind="Statements">
  <NuGetReference Prerelease="true">Azure.AI.OpenAI</NuGetReference>
  <Namespace>Azure.AI.OpenAI</Namespace>
  <Namespace>Azure</Namespace>
  <Namespace>OpenAI.Chat</Namespace>
  <Namespace>OpenAI</Namespace>
  <Namespace>System.ClientModel</Namespace>
  <Namespace>System.Text.Json.Schema</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization.Metadata</Namespace>
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

OpenAIClient api = new AzureOpenAIClient(new Uri($"https://{Util.GetPassword("azure-ai-resource")}.openai.azure.com/"), new AzureKeyCredential(Util.GetPassword("azure-ai-key")));
ChatClient cc = api.GetChatClient("gpt-4o");
// https://github.com/openai/openai-dotnet?tab=readme-ov-file#how-to-use-structured-outputs

ClientResult<ChatCompletion> result = await cc.CompleteChatAsync(
[
	new SystemChatMessage("你是人工智能助理"),
	new UserChatMessage("""
	有个空篮子，放里面放1个红色球，再往里面放1个蓝色球，再把红色球和黄色球拿出来，再放2个绿色球，请问这个篮子里面有几个球？分别是什么颜色？
	"""),
], new ChatCompletionOptions()
{
	Temperature = 0,
	ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(nameof(BallCounts), BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(JsonSchemaExporter.GetJsonSchemaAsNode(JsonSerializerOptions.Default, typeof(BallCounts), new JsonSchemaExporterOptions()
	{
		TreatNullObliviousAsNonNullable = true
	}))))
}, cancellationToken: QueryCancelToken);

JsonSerializer.Deserialize<BallCounts>(result.Value.Content[0].Text.Dump(), JsonSerializerOptions.Default).Dump();

public class BallCounts
{
	public string? Think { get; init; }
	public int Red { get; init; }
	public int Blue { get; init; }
	public int Yellow { get; init; }
	public int Green { get; init; }
	public int Confidence { get; init; }
}