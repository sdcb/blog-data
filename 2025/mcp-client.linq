<Query Kind="Statements">
  <NuGetReference Prerelease="true">ModelContextProtocol</NuGetReference>
  <Namespace>ModelContextProtocol</Namespace>
  <Namespace>ModelContextProtocol.Client</Namespace>
  <Namespace>System.Text.Json.Nodes</Namespace>
</Query>

var clientTransport = new SseClientTransport(new SseClientTransportOptions()
{
	Name = "MyServer",
	Endpoint = new Uri("http://localhost:5000"),
});

var client = await McpClientFactory.CreateAsync(clientTransport);

// Print the list of tools available from the server.
(await client.ListToolsAsync()).Select(x => new { x.Name, Desc = JsonObject.Parse(x.JsonSchema.ToString()) }).Dump();

// Execute a tool (this would normally be driven by LLM tool invocations).
(await client.CallToolAsync(
	"echo",
	new Dictionary<string, object?>() { ["message"] = ".NET is awesome!" },
	cancellationToken: CancellationToken.None)).Dump();

(await client.CallToolAsync(
	"count",
	new Dictionary<string, object?>() { ["n"] = 5 },
	new Reporter(),
	cancellationToken: CancellationToken.None)).Dump();
	
(await client.CallToolAsync("test_throw", cancellationToken: CancellationToken.None)).Dump();

(await client.CallToolAsync("not-existing-tool", cancellationToken: CancellationToken.None)).Dump();

public class Reporter : IProgress<ProgressNotificationValue>
{
	public void Report(ProgressNotificationValue value)
	{
		value.Dump();
	}
}