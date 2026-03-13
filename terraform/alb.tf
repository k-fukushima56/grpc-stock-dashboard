# Application Load Balancer
resource "aws_lb" "demo" {
  name               = "${var.project_name}-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = data.aws_subnets.default.ids

  enable_deletion_protection = false

  tags = {
    Name = "${var.project_name}-alb"
  }
}

# Target Group for Frontend (HTTP)
resource "aws_lb_target_group" "frontend" {
  name     = "${var.project_name}-frontend-tg"
  port     = 3000
  protocol = "HTTP"
  vpc_id   = data.aws_vpc.default.id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = "/"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 2
  }

  tags = {
    Name = "${var.project_name}-frontend-tg"
  }
}

# Target Group for gRPC Server
resource "aws_lb_target_group" "grpc" {
  name             = "${var.project_name}-grpc-tg"
  port             = 50051
  protocol         = "HTTP"
  protocol_version = "GRPC"
  vpc_id           = data.aws_vpc.default.id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "0"
    path                = "/grpc.health.v1.Health/Check"
    port                = "traffic-port"
    protocol            = "GRPC"
    timeout             = 5
    unhealthy_threshold = 2
  }

  tags = {
    Name = "${var.project_name}-grpc-tg"
  }
}

# Attach EC2 to Frontend Target Group
resource "aws_lb_target_group_attachment" "frontend" {
  target_group_arn = aws_lb_target_group.frontend.arn
  target_id        = aws_instance.demo.id
  port             = 3000
}

# Attach EC2 to gRPC Target Group
resource "aws_lb_target_group_attachment" "grpc" {
  target_group_arn = aws_lb_target_group.grpc.arn
  target_id        = aws_instance.demo.id
  port             = 50051
}

# HTTPS Listener (443)
resource "aws_lb_listener" "https" {
  load_balancer_arn = aws_lb.demo.arn
  port              = "443"
  protocol          = "HTTPS"
  ssl_policy        = "ELBSecurityPolicy-2016-08"
  certificate_arn   = aws_acm_certificate.cert.arn

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.frontend.arn
  }
}

# HTTP Listener (80) - Redirect to HTTPS
resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.demo.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type = "redirect"

    redirect {
      port        = "443"
      protocol    = "HTTPS"
      status_code = "HTTP_301"
    }
  }
}

# Listener Rule for gRPC
resource "aws_lb_listener_rule" "grpc" {
  listener_arn = aws_lb_listener.https.arn
  priority     = 100

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.grpc.arn
  }

  condition {
    path_pattern {
      values = ["/stock.StockService/*"]
    }
  }
}
