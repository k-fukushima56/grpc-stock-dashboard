# Terraform AWSインフラ構築ガイド

## 🚀 概要
gRPC × Nomad デモアプリ用AWSインフラ構築

## 📋 前提条件
- AWS CLI設定済み
- Terraformインストール済み
- 適切なIAM権限

## 🔧 実行手順

### 1. SSHキーペア生成
```bash
# PowerShellで実行
ssh-keygen -t rsa -b 2048 -f terraform/grpc-demo-key
```

### 2. Terraform実行
```bash
cd terraform

# 変数設定
$aws_account_id = "YOUR_AWS_ACCOUNT_ID"

# 初期化
terraform init

# 実行計画確認
terraform plan -var="aws_account_id=$aws_account_id"

# 適用
terraform apply -var="aws_account_id=$aws_account_id"
```

### 3. アプリケーションデプロイ
Terraform実行後、出力されたECRリポジトリにDockerイメージをプッシュ：

```bash
# ECRログイン
aws ecr get-login-password --region ap-northeast-1 | docker login --username AWS --password-stdin [ACCOUNT_ID].dkr.ecr.ap-northeast-1.amazonaws.com

# サーバーイメージビルド＆プッシュ
cd ../GrpcStockServer
docker build -t [ACCOUNT_ID].dkr.ecr.ap-northeast-1.amazonaws.com/grpc-demo-server:latest .
docker push [ACCOUNT_ID].dkr.ecr.ap-northeast-1.amazonaws.com/grpc-demo-server:latest

# フロントエンドイメージビルド＆プッシュ
cd ../GrpcStockWeb
docker build -t [ACCOUNT_ID].dkr.ecr.ap-northeast-1.amazonaws.com/grpc-demo-frontend:latest .
docker push [ACCOUNT_ID].dkr.ecr.ap-northeast-1.amazonaws.com/grpc-demo-frontend:latest
```

### 4. EC2でコンテナ起動
```bash
# SSHでEC2に接続
ssh -i terraform/grpc-demo-key ec2-user@[EC2_PUBLIC_IP]

# コンテナ起動
cd /opt/grpc-demo
docker-compose up -d
```

## 📊 アクセス先
- **フロントエンド**: `https://[ALB_DNS_NAME]`
- **gRPCサーバー**: `https://[ALB_DNS_NAME]/stock.StockService/*`

## 🗑️ クリーンアップ
```bash
terraform destroy -var="aws_account_id=$aws_account_id"
```

## ⚠️ 注意事項
- 自己署名証明書を使用（ブラウザで警告表示）
- SSHアクセスは実行元IPのみ許可
- 1週間の一時環境用設計
