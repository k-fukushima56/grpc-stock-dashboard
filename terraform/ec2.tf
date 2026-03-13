# Key Pair for SSH access
resource "aws_key_pair" "demo_key" {
  key_name   = "${var.project_name}-key"
  public_key = file("${path.module}/grpc-demo-key.pub")

  tags = {
    Name = "${var.project_name}-key"
  }
}

# Get latest Amazon Linux 2023 AMI
data "aws_ami" "amazon_linux_2023" {
  most_recent = true
  owners      = ["amazon"]

  filter {
    name   = "name"
    values = ["al2023-ami-*-x86_64"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# EC2 Instance
resource "aws_instance" "demo" {
  ami                    = data.aws_ami.amazon_linux_2023.id
  instance_type          = "t3.small"
  key_name               = aws_key_pair.demo_key.key_name
  iam_instance_profile   = aws_iam_instance_profile.ec2_profile.name
  vpc_security_group_ids = [aws_security_group.ec2.id]
  subnet_id              = data.aws_subnets.default.ids[0]

  # User data for basic setup
  user_data = base64encode(templatefile("${path.module}/user_data.sh", {
    ecr_server_url    = aws_ecr_repository.server.repository_url
    ecr_frontend_url = aws_ecr_repository.frontend.repository_url
    aws_region       = var.aws_region
  }))

  tags = {
    Name = "${var.project_name}-ec2"
  }
}
