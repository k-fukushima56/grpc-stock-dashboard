job "grpc-stock" {
  datacenters = ["dc1"]
  type        = "service"

  # ── gRPCサーバ ──────────────────────────────────────────
  group "stock-server" {
    count = 1

    network {
      port "grpc" { static = 50051 }
    }

    task "grpc-server" {
      driver = "raw_exec"

      config {
        command = "docker"
        args    = ["run", "--rm", "--name", "grpc-stock-server-nomad", "-p", "50051:50051", "grpc-stock-server:latest"]
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
      port "web" { static = 8080 }
    }

    task "web-server" {
      driver = "raw_exec"

      config {
        command = "docker"
        args    = ["run", "--rm", "--name", "grpc-stock-web-nomad", "-p", "8080:8080", "--link", "grpc-stock-server-nomad:grpc-stock-server", "-e", "GRPC_SERVER_ADDRESS=http://grpc-stock-server:50051", "grpc-stock-web:latest"]
      }

      resources {
        cpu    = 256
        memory = 256
      }
    }
  }
}
