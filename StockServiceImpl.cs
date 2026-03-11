using Grpc.Core;
using GrpcStock;

namespace GrpcStockServer.Services;

public class StockServiceImpl : StockService.StockServiceBase
{
    private readonly ILogger<StockServiceImpl> _logger;

    // モック用の初期株価
    private static readonly Dictionary<string, double> BasePrices = new()
    {
        ["AAPL"]  = 189.50,
        ["GOOGL"] = 141.80,
        ["MSFT"]  = 415.20,
        ["AMZN"]  = 178.90,
        ["TSLA"]  = 245.30,
        ["META"]  = 505.60,
    };

    private readonly Random _random = new();

    public StockServiceImpl(ILogger<StockServiceImpl> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Server Streaming RPC
    /// クライアントが銘柄リストを送ると、1秒ごとに株価データを流し続ける
    /// </summary>
    public override async Task StreamPrices(
        StockRequest request,
        IServerStreamWriter<StockPrice> responseStream,
        ServerCallContext context)
    {
        var symbols = request.Symbols.Count > 0
            ? request.Symbols.ToList()
            : BasePrices.Keys.ToList();

        _logger.LogInformation("株価配信開始: {Symbols}", string.Join(", ", symbols));

        // 前回の価格を保持（変動計算用）
        var previousPrices = symbols
            .Where(BasePrices.ContainsKey)
            .ToDictionary(s => s, s => BasePrices[s]);

        // クライアントが切断するまで配信し続ける
        while (!context.CancellationToken.IsCancellationRequested)
        {
            foreach (var symbol in symbols)
            {
                if (!previousPrices.TryGetValue(symbol, out var prevPrice))
                    continue;

                // ±1.5% のランダムな価格変動をシミュレート
                var changeRate = (_random.NextDouble() - 0.5) * 0.03;
                var newPrice   = Math.Round(prevPrice * (1 + changeRate), 2);
                var change     = Math.Round(newPrice - prevPrice, 2);
                var changePct  = Math.Round(changeRate * 100, 2);

                previousPrices[symbol] = newPrice;

                var stockPrice = new StockPrice
                {
                    Symbol    = symbol,
                    Price     = newPrice,
                    Change    = change,
                    ChangePct = changePct,
                    UpdatedAt = DateTime.Now.ToString("HH:mm:ss"),
                };

                try
                {
                    await responseStream.WriteAsync(stockPrice);
                }
                catch (Exception)
                {
                    _logger.LogInformation("クライアント切断: {Symbol} 配信停止", symbol);
                    return;
                }
            }

            // 1秒ごとに更新
            await Task.Delay(1000, context.CancellationToken);
        }

        _logger.LogInformation("株価配信終了");
    }
}
