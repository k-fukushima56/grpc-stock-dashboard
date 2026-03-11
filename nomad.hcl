client {
  enabled = true
  options {
    "docker.privileged.enabled" = "true"
  }
}

server {
  enabled = true
  bootstrap_expect = 1
}

ui {
  enabled = true
}

data_dir = "C:/Users/keiju.fukushima/CascadeProjects/dev/grpc-stock-dashboard/nomad-data"
