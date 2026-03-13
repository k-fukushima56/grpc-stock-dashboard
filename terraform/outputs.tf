output "alb_dns_name" {
  description = "ALB DNS name with HTTPS scheme"
  value       = "https://${aws_lb.demo.dns_name}"
}

output "ec2_public_ip" {
  description = "EC2 public IP address"
  value       = aws_instance.demo.public_ip
}

output "ecr_server_url" {
  description = "ECR repository URL for server"
  value       = aws_ecr_repository.server.repository_url
}

output "ecr_frontend_url" {
  description = "ECR repository URL for frontend"
  value       = aws_ecr_repository.frontend.repository_url
}
