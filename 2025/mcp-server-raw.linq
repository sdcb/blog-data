<Query Kind="Statements">
  <Namespace>Microsoft.AspNetCore.Builder</Namespace>
  <Namespace>Microsoft.AspNetCore.Http</Namespace>
  <Namespace>Microsoft.AspNetCore.Mvc</Namespace>
  <Namespace>Microsoft.AspNetCore.Routing</Namespace>
  <Namespace>Microsoft.AspNetCore.WebUtilities</Namespace>
  <Namespace>Microsoft.Extensions.DependencyInjection</Namespace>
  <Namespace>Microsoft.Extensions.Hosting</Namespace>
  <Namespace>Microsoft.Extensions.Logging</Namespace>
  <Namespace>Microsoft.Extensions.Primitives</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>System.ComponentModel</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Nodes</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
</Query>

WebApplicationBuilder builder = WebApplication.CreateBuilder();

// 1. 注册原生服务和我们的工具类
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<Tools>();

WebApplication app = builder.Build();

// 2. 映射 MCP 端点，自动发现并使用 Tools 类
app.MapMcpEndpoint<Tools>("/");

// 3. 启动应用
await app.RunAsync(QueryCancelToken);

public class Tools(IHttpContextAccessor http)
{
	[Description("Echoes the message back to the client.")]
	public string Echo(string message) => $"hello {message}";

	[Description("Returns the IP address of the client.")]
	public string EchoIP() => http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

    [Description("Counts from 0 to n, reporting progress at each step.")]
    public async Task<int> Count(int n, IProgress<ProgressNotificationValue> progress)
    {
        for (int i = 0; i < n; ++i)
        {
            progress.Report(new ProgressNotificationValue()
            {
                Progress = i,
                Total = n,
                Message = $"Step {i} of {n}",
            });
            await Task.Delay(100);
        }
        return n;
    }

    [Description("Throws an exception for testing purposes.")]
    public string TestThrow()
    {
        throw new Exception("This is a test exception");
	}
}

// --- JSON-RPC Base Structures ---
public record JsonRpcRequest(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] object? Params,
    [property: JsonPropertyName("id")] int? Id
);

public record JsonRpcResponse(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("result")] object? Result,
    [property: JsonPropertyName("error")] object? Error,
    [property: JsonPropertyName("id")] int? Id
);

public record JsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message
);

// --- MCP Specific Payloads ---

// For initialize method
public record InitializeParams(
    [property: JsonPropertyName("protocolVersion")] string ProtocolVersion,
    [property: JsonPropertyName("clientInfo")] ClientInfo ClientInfo
);
public record ClientInfo([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("version")] string Version);

public record InitializeResult(
    [property: JsonPropertyName("protocolVersion")] string ProtocolVersion,
    [property: JsonPropertyName("capabilities")] ServerCapabilities Capabilities,
    [property: JsonPropertyName("serverInfo")] ClientInfo ServerInfo
);
public record ServerCapabilities([property: JsonPropertyName("tools")] object Tools);


// For tools/call method
public record ToolCallParams(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] Dictionary<string, object?> Arguments,
    [property: JsonPropertyName("_meta")] ToolCallMeta? Meta
);
public record ToolCallMeta([property: JsonPropertyName("progressToken")] string ProgressToken);

// For tool call results
public record ToolCallResult(
    [property: JsonPropertyName("content")] List<ContentItem> Content,
    [property: JsonPropertyName("isError")] bool IsError = false
);
public record ContentItem([property: JsonPropertyName("type")] string Type, [property: JsonPropertyName("text")] string Text);

// For tools/list results
public record ToolListResult(
    [property: JsonPropertyName("tools")] List<ToolDefinition> Tools
);

public record ToolDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("inputSchema")] object InputSchema
);

// For progress notifications
public record ProgressNotification(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] ProgressParams Params
);
public record ProgressParams(
    [property: JsonPropertyName("progressToken")] string ProgressToken,
    [property: JsonPropertyName("progress")] int Progress,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("message")] string Message
);

// This class is for the IProgress<T> interface in our Tools methods
public class ProgressNotificationValue
{
    public int Progress { get; set; }
    public int Total { get; set; }
	public string Message { get; set; } = string.Empty;
}


public static class McpEndpointExtensions
{
    // JSON-RPC Error Codes from your article's findings
    private const int InvalidParamsErrorCode = -32602; // Invalid params
    private const int MethodNotFoundErrorCode = -32601; // Method not found

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Maps an endpoint that speaks the Model Context Protocol.
    /// </summary>
    public static IEndpointRouteBuilder MapMcpEndpoint<TTools>(this IEndpointRouteBuilder app, string pattern) where TTools : class
    {
        // 预先通过反射发现所有工具方法，并转换为snake_case以匹配MCP命名习惯
        Dictionary<string, MethodInfo> methods = typeof(TTools).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToDictionary(k => ToSnakeCase(k.Name), v => v);

        app.MapPost(pattern, async (HttpContext context, [FromServices] IServiceProvider sp) =>
        {
            JsonRpcRequest? request = await JsonSerializer.DeserializeAsync<JsonRpcRequest>(context.Request.Body, s_jsonOptions);
            if (request == null)
            {
                context.Response.StatusCode = 400; // Bad Request
                return;
            }

            // 核心：处理不同的MCP方法
            switch (request.Method)
            {
                case "initialize":
                    await HandleInitialize(context, request);
                    break;
                case "notifications/initialized":
                    // 在无状态模式下，这个请求只是一个确认，我们返回与initialize类似的信息
                    await HandleInitialize(context, request);
                    break;
                case "tools/list":
                    await HandleToolList<TTools>(context, request);
                    break;
                case "tools/call":
                    await HandleToolCall<TTools>(context, request, sp, methods);
                    break;
                default:
                    JsonRpcResponse errorResponse = new("2.0", null, new JsonRpcError(MethodNotFoundErrorCode, "Method not found"), request.Id);
                    await WriteSseMessageAsync(context.Response, errorResponse);
                    break;
            }
        });

        // 旧版SDK会发送GET请求，我们明确返回405
        app.MapGet(pattern, context =>
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.Headers.Allow = "POST";
            return Task.CompletedTask;
        });

        return app;
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                if (sb.Length > 0 && i > 0 && !char.IsUpper(name[i-1])) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static async Task HandleInitialize(HttpContext context, JsonRpcRequest request)
    {
        // 复用或创建 Session ID
        string sessionId = context.Request.Headers.TryGetValue("Mcp-Session-Id", out StringValues existingSessionId)
            ? existingSessionId.ToString()
            : WebEncoders.Base64UrlEncode(Guid.NewGuid().ToByteArray());

        context.Response.Headers["Mcp-Session-Id"] = sessionId;

        // 构建与抓包一致的响应
        InitializeResult result = new(
            "2025-06-18", // Echo the protocol version
            new ServerCapabilities(new { listChanged = true }), // Mimic the capabilities
            new ClientInfo("PureAspNetCoreMcpServer", "1.0.0")
        );
        JsonRpcResponse response = new("2.0", result, null, request.Id);
        await WriteSseMessageAsync(context.Response, response);
    }

    private static async Task HandleToolList<TTools>(HttpContext context, JsonRpcRequest request) where TTools : class
    {
        EchoSessionId(context);

        List<ToolDefinition> toolDefs = [];
        MethodInfo[] toolMethods = typeof(TTools).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (MethodInfo method in toolMethods)
        {
            string description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "No description.";

            // 简化的动态Schema生成
            Dictionary<string, object> properties = [];
            List<string> required = [];
            foreach (ParameterInfo param in method.GetParameters())
            {
                if (param.ParameterType == typeof(IProgress<ProgressNotificationValue>)) continue; // 忽略进度报告参数
                properties[param.Name!] = new { type = GetJsonType(param.ParameterType) };
                if (!param.IsOptional)
                {
                    required.Add(param.Name!);
                }
            }
            var schema = new { type = "object", properties, required };
            toolDefs.Add(new ToolDefinition(ToSnakeCase(method.Name), description, schema));
        }

        ToolListResult result = new(toolDefs);
        JsonRpcResponse response = new("2.0", result, null, request.Id);
        await WriteSseMessageAsync(context.Response, response);
    }

    private static async Task HandleToolCall<TTools>(HttpContext context, JsonRpcRequest request, IServiceProvider sp, Dictionary<string, MethodInfo> methods) where TTools : class
    {
        EchoSessionId(context);

        ToolCallParams? toolCallParams = JsonSerializer.Deserialize<ToolCallParams>(JsonSerializer.Serialize(request.Params, s_jsonOptions), s_jsonOptions);
        if (toolCallParams == null) return;

        string toolName = toolCallParams.Name;
        methods.TryGetValue(toolName, out MethodInfo? method);

        // 场景1: 调用不存在的工具 -> 返回标准JSON-RPC错误
        if (method == null)
        {
            JsonRpcError error = new(InvalidParamsErrorCode, $"Unknown tool: '{toolName}'");
            JsonRpcResponse response = new("2.0", null, error, request.Id);
            await WriteSseMessageAsync(context.Response, response);
            return;
        }

        // 使用DI容器创建工具类的实例
        using IServiceScope scope = sp.CreateScope();
        TTools toolInstance = scope.ServiceProvider.GetRequiredService<TTools>();

        object? resultValue;
        bool isError = false;

        try
        {
            // 通过反射准备方法参数
            ParameterInfo[] methodParams = method.GetParameters();
            object?[] args = new object?[methodParams.Length];
            for (int i = 0; i < methodParams.Length; i++)
            {
                ParameterInfo p = methodParams[i];
                if (p.ParameterType == typeof(IProgress<ProgressNotificationValue>))
                {
                    // 创建一个IProgress<T>的实现，它会将进度作为SSE消息发回客户端
                    args[i] = new ProgressReporter(context.Response, toolCallParams.Meta!.ProgressToken);
                }
                else if (toolCallParams.Arguments.TryGetValue(p.Name!, out object? argValue) && argValue is JsonElement element)
                {
                    args[i] = element.Deserialize(p.ParameterType, s_jsonOptions);
                }
                else if (p.IsOptional)
                {
                    args[i] = p.DefaultValue;
                }
                else
                {
                     // 场景2a: 缺少必要参数 -> 抛出异常，进入catch块
                    throw new TargetParameterCountException($"Tool '{toolName}' requires parameter '{p.Name}' but it was not provided.");
                }
            }

            object? invokeResult = method.Invoke(toolInstance, args);

            // 处理异步方法
            if (invokeResult is Task task)
            {
                await task;
                resultValue = task.GetType().IsGenericType ? task.GetType().GetProperty("Result")?.GetValue(task) : null;
            }
            else
            {
                resultValue = invokeResult;
            }
        }
        // 场景2b: 工具执行时内部抛出异常 -> isError: true
        catch (Exception ex)
        {
            isError = true;
            // 将异常信息包装在result中，而不是顶层error
            resultValue = $"An error occurred invoking '{toolName}'. Details: {ex.InnerException?.Message ?? ex.Message}";
        }

        List<ContentItem> content = [new("text", resultValue?.ToString() ?? string.Empty)];
        ToolCallResult result = new(content, isError);
        JsonRpcResponse finalResponse = new("2.0", result, null, request.Id);
        await WriteSseMessageAsync(context.Response, finalResponse);
    }

    // 手动实现SSE消息写入，告别预览版包
    private static async Task WriteSseMessageAsync(HttpResponse response, object data)
    {
        if (!response.Headers.ContainsKey("Content-Type"))
        {
            response.ContentType = "text/event-stream";
            response.Headers.CacheControl = "no-cache,no-store";
            response.Headers.ContentEncoding = "identity";
            response.Headers.KeepAlive = "true";
        }

        string json = JsonSerializer.Serialize(data, s_jsonOptions);
        string message = $"event: message\ndata: {json}\n\n";
        await response.WriteAsync(message);
        await response.Body.FlushAsync();
    }

    private static void EchoSessionId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Mcp-Session-Id", out StringValues sessionId))
        {
            context.Response.Headers["Mcp-Session-Id"] = sessionId;
        }
    }

    private static string GetJsonType(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.String => "string",
        TypeCode.Int32 or TypeCode.Int64 or TypeCode.Int16 or TypeCode.UInt32 => "integer",
        TypeCode.Double or TypeCode.Single or TypeCode.Decimal => "number",
        TypeCode.Boolean => "boolean",
        _ => "object"
    };

    // 专门用于处理进度报告的辅助类
    private class ProgressReporter(HttpResponse response, string token) : IProgress<ProgressNotificationValue>
    {
        public void Report(ProgressNotificationValue value)
        {
            ProgressParams progressParams = new(token, value.Progress, value.Total, value.Message);
            ProgressNotification notification = new("2.0", "notifications/progress", progressParams);
            // 警告: 在同步方法中调用异步代码，在真实生产环境中需要更优雅的处理
            WriteSseMessageAsync(response, notification).GetAwaiter().GetResult();
        }
	}
}