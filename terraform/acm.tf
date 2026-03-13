# Private Key for self-signed certificate
resource "tls_private_key" "cert_key" {
  algorithm = "RSA"
  rsa_bits  = 2048
}

# Self-signed certificate
resource "tls_self_signed_cert" "cert" {
  private_key_pem = tls_private_key.cert_key.private_key_pem

  subject {
    common_name  = "grpc-demo.local"
    organization = "Demo"
  }

  validity_period_hours = 720 # 30日間

  allowed_uses = [
    "key_encipherment",
    "digital_signature",
    "server_auth",
  ]

  dns_names = ["grpc-demo.local", "*.grpc-demo.local"]
}

# Import self-signed certificate to ACM
resource "aws_acm_certificate" "cert" {
  private_key       = tls_private_key.cert_key.private_key_pem
  certificate_body  = tls_self_signed_cert.cert.cert_pem
  
  tags = {
    Name = "${var.project_name}-cert"
  }
}
