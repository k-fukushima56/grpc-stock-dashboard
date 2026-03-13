terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    tls = {
      source  = "hashicorp/tls"
      version = "~> 4.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

# デフォルトVPCとサブネットをdataソースで取得
data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

# 現在のIPアドレスを取得（SSHアクセス用）
data "http" "my_ip" {
  url = "https://checkip.amazonaws.com"
}

locals {
  my_ip = "${data.http.my_ip.response_body}/32"
}
