using Grpc.Core;
using GrpcStock;
using System.Threading.Channels;

namespace GrpcStockServer.Services;

public class CommentServiceImpl : CommentService.CommentServiceBase
{
    private readonly ILogger<CommentServiceImpl> _logger;

    public CommentServiceImpl(ILogger<CommentServiceImpl> logger)
    {
        _logger = logger;
    }

    public override async Task ChatStream(
        IAsyncStreamReader<CommentMessage> requestStream,
        IServerStreamWriter<CommentMessage> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("コメントストリーム接続: {Peer}", context.Peer);

        // このクライアント専用の受信キュー
        var localChannel = Channel.CreateUnbounded<CommentMessage>();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

        ClientManager.Add(localChannel.Writer);

        // 送信タスク：ブロードキャストキューからクライアントへ
        var sendTask = Task.Run(async () =>
        {
            await foreach (var msg in localChannel.Reader.ReadAllAsync(cts.Token))
            {
                try { await responseStream.WriteAsync(msg); }
                catch { break; }
            }
        }, cts.Token);

        // 受信：クライアントからブロードキャストへ
        try
        {
            while (await requestStream.MoveNext(context.CancellationToken))
            {
                var msg = requestStream.Current;
                _logger.LogInformation("[{Symbol}] {User}: {Text}", msg.Symbol, msg.UserName, msg.Text);
                await ClientManager.BroadcastAsync(msg);
            }
        }
        catch (RpcException) { /* クライアント切断 */ }
        finally
        {
            ClientManager.Remove(localChannel.Writer);
            cts.Cancel();
            await sendTask;
            _logger.LogInformation("コメントストリーム切断: {Peer}", context.Peer);
        }
    }
}

// 接続中の全クライアントを管理するクラス
public static class ClientManager
{
    private static readonly List<ChannelWriter<CommentMessage>> _writers = new();
    private static readonly object _lock = new();

    public static void Add(ChannelWriter<CommentMessage> writer)
    {
        lock (_lock) { _writers.Add(writer); }
    }

    public static void Remove(ChannelWriter<CommentMessage> writer)
    {
        lock (_lock) { _writers.Remove(writer); writer.TryComplete(); }
    }

    public static async Task BroadcastAsync(CommentMessage msg)
    {
        List<ChannelWriter<CommentMessage>> snapshot;
        lock (_lock) { snapshot = _writers.ToList(); }
        foreach (var w in snapshot)
        {
            await w.WriteAsync(msg);
        }
    }
}
