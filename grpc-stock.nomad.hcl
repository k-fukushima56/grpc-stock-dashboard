job "grpc-stock" {
  datacenters = ["dc1"]
  type        = "service"

  # ── gRPCサーバ ──────────────────────────────────────────
  group "stock-server" {
    count = 1

    network {
      port "grpc" {}  # Nomadが自動割り当て → NOMAD_PORT_grpc
    }

    service {
      name = "grpc-stock-server"
      port = "grpc"
      check {
        type     = "tcp"
        interval = "10s"
        timeout  = "2s"
      }
    }

    task "grpc-server" {
      driver = "docker"

      config {
        image = "grpc-stock-server:latest"
        ports = ["grpc"]
      }

      env {
        NOMAD_PORT_grpc = "${NOMAD_PORT_grpc}"
      }

      resources {
        cpu    = 256
        memory = 256
      }
    }
  }

  # ── Webサーバ（gRPCクライアント＋SSE中継） ───────────────
  group "stock-web" {
    count = 1

    network {
      port "web" { static = 8080 }  # ブラウザからアクセスするポート
    }

    service {
      name = "grpc-stock-web"
      port = "web"
      check {
        type     = "http"
        path     = "/"
        interval = "10s"
        timeout  = "2s"
      }
    }

    task "web-server" {
      driver = "docker"

      config {
        image = "grpc-stock-web:latest"
        ports = ["web"]
      }

      env {
        NOMAD_PORT_web       = "${NOMAD_PORT_web}"
        # gRPCサーバのアドレスをConsulで解決（Consul未使用の場合は直接指定）
        GRPC_SERVER_ADDRESS  = "http://${NOMAD_ADDR_grpc}"
      }

      resources {
        cpu    = 256
        memory = 256
      }
    }
  }
}
