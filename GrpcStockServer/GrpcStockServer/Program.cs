using GrpcStockServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();  // 追加

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<StockServiceImpl>();
app.MapGrpcService<CommentServiceImpl>();
app.MapGrpcReflectionService();         // 追加
app.MapGet("/", () => "gRPC Stock Server is running...");

// ポート50051で起動
app.Urls.Add("http://0.0.0.0:50051");

app.Run();
