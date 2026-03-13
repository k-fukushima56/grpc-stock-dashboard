#!/bin/bash
# EC2初期設定スクリプト

# ECRログイン用のAWS CLI設定
yum update -y
yum install -y docker

# Dockerサービス起動
systemctl start docker
systemctl enable docker

# ECRログイン
aws ecr get-login-password --region ${aws_region} | docker login --username AWS --password-stdin ${ecr_server_url%/*}

# Docker Composeインストール
curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

# アプリケーション用ディレクトリ作成
mkdir -p /opt/grpc-demo
cd /opt/grpc-demo

# docker-compose.yml作成（テンプレート）
cat > docker-compose.yml << 'EOF'
version: '3.8'
services:
  grpc-stock-server:
    image: ${ecr_server_url}:latest
    ports:
      - "50051:50051"
    environment:
      - ASPNETCORE_URLS=http://0.0.0.0:50051
    restart: unless-stopped

  grpc-stock-frontend:
    image: ${ecr_frontend_url}:latest
    ports:
      - "3000:8080"
    environment:
      - GRPC_SERVER_ADDRESS=http://grpc-stock-server:50051
    depends_on:
      - grpc-stock-server
    restart: unless-stopped
EOF

echo "EC2 setup completed!"
