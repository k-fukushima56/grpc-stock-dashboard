# gRPC × Nomad リアルタイム株価ダッシュボード

Dockerコンテナで動作するgRPC Server StreamingとASP.NET Core Webアプリケーションのデモンストレーション。

## 🚀 クイックスタート

### 前提条件
- Docker Desktop（Linuxコンテナモード）
- Git

### 1. プロジェクトの取得

```bash
git clone <repository-url>
cd grpc-stock-dashboard
```

### 2. Dockerコンテナモード確認

```bash
docker version
# "Context: desktop-linux" となっていることを確認
```

Linuxコンテナモードでない場合：
- Docker Desktopを右クリック
- "Switch to Linux containers" を選択

### 3. 起動

```bash
# 全サービスを起動
docker-compose up --build

# バックグラウンドで起動
docker-compose up --build -d
```

### 4. 確認

ブラウザで `http://localhost:8080` にアクセスし、「配信開始」ボタンをクリック。

---

## 📋 アーキテクチャ

```
ブラウザ ←→ Webサーバ (SSE) ←→ gRPCサーバ (Server Streaming)
   ↓           ↓                    ↓
ポート8080   ポート8080          ポート50051
```

### 技術スタック
- **gRPC Server Streaming**: リアルタイム株価配信
- **ASP.NET Core**: WebサーバとSSE中継
- **Docker**: コンテナ化
- **Protocol Buffers**: データシリアライズ

---

## 🛠️ 開発

### ローカル開発

```bash
# gRPCサーバ
cd GrpcStockServer/GrpcStockServer
dotnet run

# Webサーバ（別ターミナル）
cd GrpcStockWeb/GrpcStockWeb
dotnet run
```

### Docker開発

```bash
# ビルド
docker-compose build

# 再ビルド
docker-compose up --build

# ログ確認
docker-compose logs -f
```

---

## 📚 詳細ドキュメント

- [Dockerクイックスタート](grpc-stock-dashboard-docker-quickstart.md)
- [開発環境セットアップ](grpc-stock-dashboard-quickstart.md)
- [ハンズオン実施レポート](grpc-nomad-hands-on-report.md)
│   │   └── StockServiceImpl.cs           # Server Streamingの実装（モック株価）
│   └── Dockerfile
├── GrpcStockWeb/
│   ├── GrpcStockWeb.csproj
│   ├── Program.cs                        # SSE中継エンドポイント
│   ├── Dockerfile
│   └── wwwroot/
│       └── index.html                    # ダッシュボード画面
└── nomad/
    └── grpc-stock.nomad.hcl              # Nomadジョブ定義（2サービス）
```

---

## 前提条件

| ツール | 確認コマンド |
|---|---|
| .NET SDK 8.0以上 | `dotnet --version` |
| Docker | `docker --version` |
| Nomad | `nomad --version` |

---

## 起動手順

### Step 1: Dockerイメージをビルドする

```bash
# grpc-stock/ ディレクトリで実行

# gRPCサーバのイメージ
docker build -f GrpcStockServer/Dockerfile -t grpc-stock-server:latest .

# Webサーバのイメージ
docker build -f GrpcStockWeb/Dockerfile -t grpc-stock-web:latest .
```

### Step 2: Nomadを開発モードで起動する

```bash
# 別ターミナルで実行
sudo nomad agent -dev -bind=0.0.0.0
```

### Step 3: Nomadジョブをデプロイする

```bash
nomad job run nomad/grpc-stock.nomad.hcl
```

### Step 4: デプロイ状況を確認する

```bash
nomad job status grpc-stock
```

```
# 両グループが running になればOK
Task Groups
Name          Queued  Starting  Running
stock-server  0       0         1
stock-web     0       0         1
```

### Step 5: ブラウザでアクセスする

```
http://localhost:8080
```

「▶ 配信開始」ボタンを押すと、株価がリアルタイムで更新されます。

---

## ローカル動作確認（Nomadなし）

Nomadを使わず手元で素早く確認したい場合は以下で動かせます。

```bash
# ターミナル1：gRPCサーバを起動
cd GrpcStockServer
dotnet run

# ターミナル2：Webサーバを起動
cd GrpcStockWeb
dotnet run
```

```
# ブラウザでアクセス
http://localhost:8080
```

---

## 動作確認のポイント

ブラウザの開発者ツール（F12）→ Network タブで確認できます。

```
/api/stocks/stream  EventStream  ← SSEの通信
  data: {"symbol":"AAPL","price":189.72,...}   ← 1秒ごとに届く
  data: {"symbol":"GOOGL","price":141.53,...}
  ...
```

---

## Nomadのポイント

### 2サービスを1つのジョブに定義

```hcl
job "grpc-stock" {
  group "stock-server" { ... }  # gRPCサーバ
  group "stock-web"    { ... }  # Webサーバ（中継）
}
```

Nomadでは1つのジョブファイルに複数のサービスグループを定義できます。

### サービス間のアドレス解決

```hcl
env {
  GRPC_SERVER_ADDRESS = "http://${NOMAD_ADDR_grpc}"
}
```

Nomadの環境変数 `NOMAD_ADDR_<ポートラベル>` を使うと、同じジョブ内の別サービスのアドレスを動的に取得できます。

---

## トラブルシューティング

### ブラウザが `http://localhost:8080` に繋がらない

```bash
# Webサーバの起動を確認
nomad job status grpc-stock
nomad alloc logs <Web側のAllocation-ID>
```

### 株価が更新されない（画面が止まる）

```bash
# gRPCサーバのログを確認
nomad alloc logs <Server側のAllocation-ID>
```

### `GRPC_SERVER_ADDRESS` の接続エラー

Nomadなしのローカル確認では、`GrpcStockWeb/Program.cs` の
デフォルト値 `http://localhost:50051` が使われます。
gRPCサーバが50051で起動していることを確認してください。

---

## チャットアプリとの比較

| 観点 | チャット（Bidirectional） | 株価ダッシュボード（Server Streaming） |
|---|---|---|
| protoの`stream` | 送受信**両方**に付く | **returns側のみ**に付く |
| データの流れ | 双方向 | サーバ→クライアントのみ |
| 接続の性質 | 会話型 | 購読型 |
| 典型的な用途 | チャット・ゲーム | 通知・モニタリング・ログ配信 |
