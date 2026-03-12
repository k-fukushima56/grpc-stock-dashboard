# Postman で Unary RPC GetPrice を実行する手順

---

## 🚀 事前準備

### 1. Dockerコンテナ起動確認
```bash
docker-compose up --build
```

### 2. ポート確認
- **gRPCサーバー**: `localhost:50051` (Postman接続先)
- **Webアプリ**: `http://localhost:8080` (ブラウザ確認用)

---

## 📮 Postman 設定手順

### 1. 新規gRPCリクエスト作成
1. Postmanを起動
2. **New** → **gRPC Request** を選択
3. **URL**: `localhost:50051` を入力

### 2. サービス定義の自動取得（Server Reflection）
1. **Service definition** セクション
2. **Use Server Reflection** をクリック
3. 自動的にサービス一覧が表示される
4. **stock.StockService** を選択
5. **GetPrice** を選択

### 3. リクエストペイロード設定
**Request JSON** に以下を入力：

```json
{
  "symbol": "AAPL"
}
```

---

## ✅ 正常系テスト

### テストケース1: 有効な銘柄 (AAPL)

**リクエスト**:
```json
{
  "symbol": "AAPL"
}
```

**期待するレスポンス**:
```json
{
  "symbol": "AAPL",
  "price": 189.5,
  "change": 0,
  "changePct": 0,
  "updatedAt": "14:32:11"
}
```

**確認ポイント**:
- [ ] HTTP Status: 200 OK
- [ ] gRPC Status: OK (0)
- [ ] symbolがリクエストと一致
- [ ] priceが189.5を返す
- [ ] updatedAtが現在時刻形式

### テストケース2: 他の有効銘柄

**GOOGL**:
```json
{ "symbol": "GOOGL" }
```
→ `price: 141.8`

**MSFT**:
```json
{ "symbol": "MSFT" }
```
→ `price: 415.2`

---

## ❌ 異常系テスト

### テストケース3: 存在しない銘柄

**リクエスト**:
```json
{
  "symbol": "INVALID"
}
```

**期待するレスポンス**:
- **HTTP Status**: 404 Not Found
- **gRPC Status**: NOT_FOUND (5)
- **Error Message**: 
  ```
  銘柄 'INVALID' は存在しません。有効な銘柄: AAPL, GOOGL, MSFT, AMZN, TSLA, META
  ```

**確認ポイント**:
- [ ] 適切なgRPCステータスコードが返る
- [ ] エラーメッセージに有効な銘柄一覧が含まれる
- [ ] Postmanでエラーが適切に表示される

---

## 🧪 全テスト実行チェックリスト

### ✅ 正常系
- [ ] AAPLで正常レスポンスが返る
- [ ] GOOGLで正常レスポンスが返る
- [ ] MSFTで正常レスポンスが返る
- [ ] AMZNで正常レスポンスが返る
- [ ] TSLAで正常レスポンスが返る
- [ ] METAで正常レスポンスが返る

### ✅ 異常系
- [ ] INVALIDでNOT_FOUNDエラーが返る
- [ ] 空文字でNOT_FOUNDエラーが返る
- [ ] nullでエラーが返る

### ✅ 既存機能への影響確認
- [ ] Webアプリの株価配信が正常に動作
- [ ] コメント機能が正常に動作
- [ ] エビデンス機能が正常に動作

---

## 📊 学習ポイントの確認

### 1. Unary RPC の基本
- ✅ リクエスト1回 → レスポンス1回のシンプルな往復
- ✅ 同期処理（async不要）

### 2. proto の書き方の違い
- ✅ `returns (stream StockPrice)` → Server Streaming
- ✅ `returns (StockPrice)` → Unary

### 3. gRPC ステータスコード
- ✅ 正常: OK (0)
- ✅ 異常: NOT_FOUND (5)

### 4. Server Reflection
- ✅ Postmanが.protoなしでサービス定義を自動取得
- ✅ 手動での.protoインポートが不要

---

## 🔍 トラブルシューティング

### 接続エラーの場合
1. **ポート確認**: `docker ps` で50051ポートが開いているか確認
2. **URL確認**: `localhost:50051` を正確に入力
3. **ファイアウォール**: ローカルファイアウォールを確認

### サービス定義が取得できない場合
1. **Reflection有効化**: Program.csに`AddGrpcReflection()`があるか確認
2. **パッケージ確認**: Grpc.AspNetCore.Server.Reflectionがインストールされているか確認
3. **再ビルド**: `docker-compose up --build` を再実行

### レスポンスが期待通りでない場合
1. **proto定義確認**: GetPriceRequestとGetPriceが正しく定義されているか
2. **実装確認**: StockServiceImpl.csのGetPriceメソッドを確認
3. **ログ確認**: Dockerコンテナのログを確認

---

## 🎯 成功の目印

Postmanで以下が確認できれば成功です：

1. **Server Reflection動作**: サービス定義が自動取得
2. **正常系**: 6銘柄すべてで正しい価格が返る
3. **異常系**: 不正な銘柄で適切なエラーが返る
4. **既存機能**: Webアプリの他機能が影響を受けない

これでgRPC Unary RPCの実装が完成です！🎉
