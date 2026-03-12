using Grpc.Net.Client;
using GrpcStock;

var builder = WebApplication.CreateBuilder(args);

// gRPCサーバのアドレス（環境変数から取得 or デフォルト）
var grpcServerAddress = Environment.GetEnvironmentVariable("GRPC_SERVER_ADDRESS")
    ?? "http://localhost:50051";

var webPort = Environment.GetEnvironmentVariable("NOMAD_PORT_web") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{webPort}");

var app = builder.Build();

// WebSocketサポートを有効化
app.UseWebSockets();

// 静的ファイル（index.html）を配信
app.UseStaticFiles();

// SSEエンドポイント：ブラウザ → このサーバ → gRPCサーバ の中継
app.MapGet("/api/stocks/stream", async (HttpContext ctx) =>
{
    // SSEヘッダーの設定
    ctx.Response.Headers["Content-Type"]  = "text/event-stream";
    ctx.Response.Headers["Cache-Control"] = "no-cache";
    ctx.Response.Headers["Connection"]    = "keep-alive";

    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    
    using var channel = GrpcChannel.ForAddress(grpcServerAddress, new GrpcChannelOptions
    {
        HttpHandler = handler
    });
    var client  = new StockService.StockServiceClient(channel);
    var request = new StockRequest();
    request.Symbols.AddRange(new[] { "AAPL", "GOOGL", "MSFT", "AMZN", "TSLA", "META" });

    // gRPCサーバからServer Streamingで受け取り、SSEとしてブラウザに流す
    using var streamingCall = client.StreamPrices(request);

    while (await streamingCall.ResponseStream.MoveNext(ctx.RequestAborted))
    {
        var price = streamingCall.ResponseStream.Current;
        
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            symbol    = price.Symbol,
            price     = price.Price,
            change    = price.Change,
            changePct = price.ChangePct,
            updatedAt = price.UpdatedAt,
        });

        // SSE形式で送信
        await ctx.Response.WriteAsync($"data: {json}\n\n");
        await ctx.Response.Body.FlushAsync();
    }
});

// コメント用 WebSocket エンドポイント（Bidirectional gRPC の中継）
app.MapGet("/ws/comments", async (HttpContext ctx) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();

    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

    using var grpcChannel = GrpcChannel.ForAddress(grpcServerAddress, new GrpcChannelOptions
    {
        HttpHandler = handler
    });
    var client = new CommentService.CommentServiceClient(grpcChannel);

    // Bidirectional Streaming で gRPCサーバに接続
    using var grpcStream = client.ChatStream();
    var cts = new CancellationTokenSource();

    // gRPC → WebSocket（受信した全コメントをブラウザへ転送）
    var receiveFromGrpc = Task.Run(async () =>
    {
        try
        {
            while (await grpcStream.ResponseStream.MoveNext(cts.Token))
            {
                var msg = grpcStream.ResponseStream.Current;
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    userName = msg.UserName,
                    symbol   = msg.Symbol,
                    text     = msg.Text,
                    sentAt   = msg.SentAt,
                });
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                if (ws.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    await ws.SendAsync(bytes,
                        System.Net.WebSockets.WebSocketMessageType.Text,
                        true, cts.Token);
                }
            }
        }
        catch (OperationCanceledException) { }
    });

    // WebSocket → gRPC（ブラウザのコメントをサーバへ送信）
    var buffer = new byte[4096];
    try
    {
        while (ws.State == System.Net.WebSockets.WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, cts.Token);
            if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                break;

            var json = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
            var dto  = System.Text.Json.JsonSerializer.Deserialize<CommentDto>(json);
            if (dto is null) continue;

            await grpcStream.RequestStream.WriteAsync(new CommentMessage
            {
                UserName = dto.userName ?? "Anonymous",
                Symbol   = dto.symbol   ?? "",
                Text     = dto.text     ?? "",
                SentAt   = DateTime.Now.ToString("HH:mm:ss"),
            });
        }
    }
    finally
    {
        cts.Cancel();
        await grpcStream.RequestStream.CompleteAsync();
        await receiveFromGrpc;
    }
});

app.MapFallbackToFile("index.html");
app.Run();

record CommentDto(string? userName, string? symbol, string? text);
