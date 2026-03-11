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
        command = "dotnet"
        args    = ["GrpcStockServer.dll"]
        work_dir = "C:/Users/keiju.fukushima/CascadeProjects/learn/grpc-stock-dashboard/GrpcStockServer/GrpcStockServer/bin/Release/net9.0/publish"
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
      port "web" { static = 8080 }
    }

    task "web-server" {
      driver = "raw_exec"

      config {
        command = "dotnet"
        args    = ["GrpcStockWeb.dll"]
        work_dir = "C:/Users/keiju.fukushima/CascadeProjects/learn/grpc-stock-dashboard/GrpcStockWeb/GrpcStockWeb/bin/Release/net9.0/publish"
      }

      env {
        NOMAD_PORT_web       = "${NOMAD_PORT_web}"
        GRPC_SERVER_ADDRESS  = "http://127.0.0.1:50051"
      }

      resources {
        cpu    = 512
        memory = 512
      }
    }
  }
}
