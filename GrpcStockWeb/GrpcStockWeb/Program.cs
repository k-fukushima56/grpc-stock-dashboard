using Grpc.Net.Client;
using GrpcStock;

var builder = WebApplication.CreateBuilder(args);

// gRPCサーバのアドレス（環境変数から取得 or デフォルト）
var grpcServerAddress = Environment.GetEnvironmentVariable("GRPC_SERVER_ADDRESS")
    ?? "http://localhost:50051";

var webPort = Environment.GetEnvironmentVariable("NOMAD_PORT_web") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{webPort}");

var app = builder.Build();

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

app.MapFallbackToFile("index.html");
app.Run();
