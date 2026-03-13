# ECR Repository for grpc-stock-server
resource "aws_ecr_repository" "server" {
  name                 = "${var.project_name}-server"
  image_tag_mutability = "MUTABLE"
  
  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Name = "${var.project_name}-server"
  }
}

# ECR Repository for grpc-stock-frontend
resource "aws_ecr_repository" "frontend" {
  name                 = "${var.project_name}-frontend"
  image_tag_mutability = "MUTABLE"
  
  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Name = "${var.project_name}-frontend"
  }
}
