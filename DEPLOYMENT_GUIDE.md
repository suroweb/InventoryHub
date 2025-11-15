# InventoryHub - Production Deployment Guide

**Version:** 1.0
**Last Updated:** 2025-11-13
**Deployment Complexity:** Medium-High
**Estimated Setup Time:** 8-24 hours

---

## Table of Contents

1. [Infrastructure Overview](#infrastructure-overview)
2. [Prerequisites](#prerequisites)
3. [Local Development Deployment](#local-development-deployment)
4. [Docker Deployment](#docker-deployment)
5. [Kubernetes Deployment](#kubernetes-deployment)
6. [Cloud Platform Deployment](#cloud-platform-deployment)
7. [Database Setup](#database-setup)
8. [AI Provider Configuration](#ai-provider-configuration)
9. [Monitoring & Logging](#monitoring--logging)
10. [Security Hardening](#security-hardening)
11. [Scaling Strategy](#scaling-strategy)
12. [Backup & Disaster Recovery](#backup--disaster-recovery)
13. [Troubleshooting](#troubleshooting)

---

## Infrastructure Overview

### Architecture Diagram

```
                           ┌─────────────────┐
                           │   Load Balancer │
                           │   (nginx/HAProxy)│
                           └────────┬────────┘
                                    │
                  ┌─────────────────┼─────────────────┐
                  │                 │                 │
           ┌──────▼──────┐   ┌─────▼──────┐   ┌─────▼──────┐
           │  API Server │   │ API Server │   │ API Server │
           │   Instance 1│   │ Instance 2 │   │ Instance 3 │
           └──────┬──────┘   └─────┬──────┘   └─────┬──────┘
                  │                 │                 │
                  └─────────────────┼─────────────────┘
                                    │
                  ┌─────────────────┼─────────────────┐
                  │                 │                 │
           ┌──────▼──────┐   ┌─────▼──────┐   ┌─────▼──────┐
           │  PostgreSQL │   │    Redis   │   │   Ollama   │
           │  (Master DB)│   │ (Caching)  │   │  (AI/ML)   │
           └──────┬──────┘   └────────────┘   └────────────┘
                  │
           ┌──────▼──────┐
           │  PostgreSQL │
           │  Tenant DBs │
           │(per-tenant) │
           └─────────────┘
```

### Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Application Server** | ASP.NET Core 10 | API endpoints |
| **Web Server** | Kestrel | HTTP server |
| **Reverse Proxy** | nginx | Load balancing, SSL |
| **Master Database** | PostgreSQL 14+ | Tenants & authentication |
| **Tenant Databases** | PostgreSQL 14+ | Per-tenant data |
| **Cache** | Redis 7+ | Distributed caching, rate limiting |
| **AI Engine** | Ollama/DeepSeek | Forecasting & insights |
| **Logging** | Serilog | Structured logging |
| **Monitoring** | Prometheus + Grafana | Metrics & dashboards |
| **Container Runtime** | Docker 24+ | Containerization |
| **Orchestration** | Kubernetes 1.28+ | Container orchestration |

---

## Prerequisites

### Software Requirements

**Development Environment:**
- .NET 10 SDK
- PostgreSQL 14+
- Redis 7+
- Docker 24+
- kubectl (for Kubernetes)

**Production Environment:**
- Linux server (Ubuntu 22.04 LTS recommended)
- 16GB+ RAM
- 4+ CPU cores
- 100GB+ SSD storage
- HTTPS/TLS certificates

### Cloud Provider Accounts (Optional)

- **AWS:** EC2, RDS, ElastiCache, ECS/EKS
- **Azure:** VM, Azure Database for PostgreSQL, Azure Cache for Redis, AKS
- **GCP:** Compute Engine, Cloud SQL, Memorystore, GKE

---

## Local Development Deployment

### Step 1: Setup Environment

```bash
# Clone repository
git clone https://github.com/yourusername/InventoryHub.git
cd InventoryHub

# Install dependencies
cd FullStackApp/ServerApp
dotnet restore
```

### Step 2: Configure appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "MasterDatabase": "Host=localhost;Database=InventoryHub_Master;Username=postgres;Password=dev_password",
    "TenantTemplate": "Host=localhost;Database=InventoryHub_{TenantId};Username=postgres;Password=dev_password"
  },
  "Jwt": {
    "Secret": "LOCAL_DEV_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG!",
    "Issuer": "InventoryHub",
    "Audience": "InventoryHub",
    "ExpirationDays": 7
  },
  "AI": {
    "Provider": "Ollama",
    "BaseUrl": "http://localhost:11434",
    "Model": "llama2"
  }
}
```

### Step 3: Setup Local Databases

```bash
# Start PostgreSQL
# Create databases
psql -U postgres -c "CREATE DATABASE InventoryHub_Master;"
psql -U postgres -c "CREATE DATABASE InventoryHub_Demo;"

# Run migrations
dotnet ef database update --context MasterDbContext
dotnet ef database update --context TenantDbContext
```

### Step 4: Start Ollama (Local AI)

```bash
# Pull and run Ollama
docker run -d -p 11434:11434 -v ollama-data:/root/.ollama ollama/ollama

# Pull Llama 2 model
docker exec -it <container_id> ollama pull llama2
```

### Step 5: Run Application

```bash
dotnet run
# API available at: https://localhost:5001
# Swagger UI: https://localhost:5001/swagger
```

---

## Docker Deployment

### Step 1: Create Dockerfile

**FullStackApp/ServerApp/Dockerfile:**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["FullStackApp/ServerApp/ServerApp.csproj", "ServerApp/"]
COPY ["FullStackApp/Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "ServerApp/ServerApp.csproj"

# Copy remaining files and build
COPY FullStackApp/ .
WORKDIR "/src/ServerApp"
RUN dotnet build "ServerApp.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ServerApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install PostgreSQL client tools (for health checks)
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chmod 777 /app/logs

# Expose ports
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "ServerApp.dll"]
```

### Step 2: Create docker-compose.yml

**docker-compose.yml:**

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: FullStackApp/ServerApp/Dockerfile
    container_name: inventoryhub-api
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - ConnectionStrings__MasterDatabase=Host=postgres;Port=5432;Database=InventoryHub_Master;Username=postgres;Password=${POSTGRES_PASSWORD}
      - Jwt__Secret=${JWT_SECRET}
      - AI__Provider=Ollama
      - AI__BaseUrl=http://ollama:11434
      - AI__Model=llama2
    volumes:
      - ./logs:/app/logs
    depends_on:
      - postgres
      - redis
      - ollama
    restart: unless-stopped
    networks:
      - inventoryhub-network

  postgres:
    image: postgres:14-alpine
    container_name: inventoryhub-postgres
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_DB=InventoryHub_Master
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql
    restart: unless-stopped
    networks:
      - inventoryhub-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: inventoryhub-redis
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis-data:/data
    restart: unless-stopped
    networks:
      - inventoryhub-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5

  ollama:
    image: ollama/ollama:latest
    container_name: inventoryhub-ollama
    ports:
      - "11434:11434"
    volumes:
      - ollama-data:/root/.ollama
    restart: unless-stopped
    networks:
      - inventoryhub-network
    # GPU support (optional, requires nvidia-docker)
    # deploy:
    #   resources:
    #     reservations:
    #       devices:
    #         - driver: nvidia
    #           count: 1
    #           capabilities: [gpu]

  nginx:
    image: nginx:alpine
    container_name: inventoryhub-nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
    depends_on:
      - api
    restart: unless-stopped
    networks:
      - inventoryhub-network

volumes:
  postgres-data:
  redis-data:
  ollama-data:

networks:
  inventoryhub-network:
    driver: bridge
```

### Step 3: Create .env File

**.env:**

```bash
# Database
POSTGRES_PASSWORD=CHANGE_ME_STRONG_PASSWORD_123!

# JWT
JWT_SECRET=CHANGE_ME_JWT_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG!

# AI (Optional - for DeepSeek)
DEEPSEEK_API_KEY=your_deepseek_api_key_here
```

### Step 4: Create nginx Configuration

**nginx/nginx.conf:**

```nginx
events {
    worker_connections 1024;
}

http {
    upstream api_servers {
        least_conn;
        server api:80 max_fails=3 fail_timeout=30s;
        # Add more API servers here for horizontal scaling
        # server api2:80 max_fails=3 fail_timeout=30s;
        # server api3:80 max_fails=3 fail_timeout=30s;
    }

    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=100r/s;

    server {
        listen 80;
        server_name _;

        # Redirect HTTP to HTTPS
        return 301 https://$host$request_uri;
    }

    server {
        listen 443 ssl http2;
        server_name api.inventoryhub.com;

        # SSL configuration
        ssl_certificate /etc/nginx/ssl/fullchain.pem;
        ssl_certificate_key /etc/nginx/ssl/privkey.pem;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;
        ssl_prefer_server_ciphers on;

        # Security headers
        add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
        add_header X-Frame-Options "SAMEORIGIN" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header X-XSS-Protection "1; mode=block" always;

        # Gzip compression
        gzip on;
        gzip_types text/plain text/css application/json application/javascript text/xml application/xml;

        # Proxy settings
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # API routes
        location / {
            limit_req zone=api_limit burst=20 nodelay;
            proxy_pass http://api_servers;
            proxy_read_timeout 300s;
            proxy_connect_timeout 75s;
        }

        # Health check endpoint (no rate limit)
        location /health {
            proxy_pass http://api_servers;
        }

        # WebSocket support (for SignalR)
        location /hub {
            proxy_pass http://api_servers;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
        }
    }
}
```

### Step 5: Deploy with Docker Compose

```bash
# Build and start all services
docker-compose up -d --build

# View logs
docker-compose logs -f api

# Check service status
docker-compose ps

# Run database migrations
docker-compose exec api dotnet ef database update --context MasterDbContext
docker-compose exec api dotnet ef database update --context TenantDbContext

# Pull Ollama model
docker-compose exec ollama ollama pull llama2

# Stop services
docker-compose down

# Stop and remove volumes (WARNING: deletes data)
docker-compose down -v
```

---

## Kubernetes Deployment

### Step 1: Create Kubernetes Manifests

**k8s/namespace.yaml:**

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: inventoryhub
```

**k8s/configmap.yaml:**

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: inventoryhub-config
  namespace: inventoryhub
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  AI__Provider: "Ollama"
  AI__BaseUrl: "http://ollama-service:11434"
  AI__Model: "llama2"
```

**k8s/secret.yaml:**

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: inventoryhub-secrets
  namespace: inventoryhub
type: Opaque
stringData:
  POSTGRES_PASSWORD: "CHANGE_ME_STRONG_PASSWORD"
  JWT_SECRET: "CHANGE_ME_JWT_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG!"
  ConnectionStrings__MasterDatabase: "Host=postgres-service;Port=5432;Database=InventoryHub_Master;Username=postgres;Password=CHANGE_ME"
```

**k8s/deployment.yaml:**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: inventoryhub-api
  namespace: inventoryhub
spec:
  replicas: 3
  selector:
    matchLabels:
      app: inventoryhub-api
  template:
    metadata:
      labels:
        app: inventoryhub-api
    spec:
      containers:
      - name: api
        image: inventoryhub/api:latest
        ports:
        - containerPort: 80
          name: http
        envFrom:
        - configMapRef:
            name: inventoryhub-config
        - secretRef:
            name: inventoryhub-secrets
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
        volumeMounts:
        - name: logs
          mountPath: /app/logs
      volumes:
      - name: logs
        emptyDir: {}
```

**k8s/service.yaml:**

```yaml
apiVersion: v1
kind: Service
metadata:
  name: inventoryhub-api-service
  namespace: inventoryhub
spec:
  selector:
    app: inventoryhub-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 80
  type: ClusterIP
```

**k8s/ingress.yaml:**

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: inventoryhub-ingress
  namespace: inventoryhub
  annotations:
    kubernetes.io/ingress.class: "nginx"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/rate-limit: "100"
spec:
  tls:
  - hosts:
    - api.inventoryhub.com
    secretName: inventoryhub-tls
  rules:
  - host: api.inventoryhub.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: inventoryhub-api-service
            port:
              number: 80
```

**k8s/postgres-statefulset.yaml:**

```yaml
apiVersion: v1
kind: Service
metadata:
  name: postgres-service
  namespace: inventoryhub
spec:
  selector:
    app: postgres
  ports:
  - port: 5432
    targetPort: 5432
  clusterIP: None
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
  namespace: inventoryhub
spec:
  serviceName: postgres-service
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:14-alpine
        ports:
        - containerPort: 5432
        env:
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: inventoryhub-secrets
              key: POSTGRES_PASSWORD
        - name: POSTGRES_DB
          value: InventoryHub_Master
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
  volumeClaimTemplates:
  - metadata:
      name: postgres-storage
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 50Gi
```

### Step 2: Deploy to Kubernetes

```bash
# Apply namespace
kubectl apply -f k8s/namespace.yaml

# Create secrets (encode base64 first)
kubectl create secret generic inventoryhub-secrets \
  --from-literal=POSTGRES_PASSWORD='YOUR_PASSWORD' \
  --from-literal=JWT_SECRET='YOUR_JWT_SECRET' \
  -n inventoryhub

# Apply configurations
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/postgres-statefulset.yaml
kubectl apply -f k8s/redis-deployment.yaml
kubectl apply -f k8s/ollama-deployment.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/ingress.yaml

# Check deployment status
kubectl get pods -n inventoryhub
kubectl get svc -n inventoryhub
kubectl logs -f deployment/inventoryhub-api -n inventoryhub

# Scale deployment
kubectl scale deployment/inventoryhub-api --replicas=5 -n inventoryhub
```

---

## Cloud Platform Deployment

### AWS Deployment

**Architecture:**
- **EC2:** API servers (t3.medium or larger)
- **RDS PostgreSQL:** Master and tenant databases
- **ElastiCache Redis:** Distributed caching
- **ECS/EKS:** Container orchestration
- **ALB:** Application load balancer
- **Route 53:** DNS management
- **CloudWatch:** Logging and monitoring

**Terraform Configuration (example):**

```hcl
# main.tf
provider "aws" {
  region = "us-east-1"
}

# VPC and networking
module "vpc" {
  source = "terraform-aws-modules/vpc/aws"
  version = "3.19.0"

  name = "inventoryhub-vpc"
  cidr = "10.0.0.0/16"

  azs             = ["us-east-1a", "us-east-1b", "us-east-1c"]
  private_subnets = ["10.0.1.0/24", "10.0.2.0/24", "10.0.3.0/24"]
  public_subnets  = ["10.0.101.0/24", "10.0.102.0/24", "10.0.103.0/24"]

  enable_nat_gateway = true
  enable_vpn_gateway = false
}

# RDS PostgreSQL
resource "aws_db_instance" "master" {
  identifier        = "inventoryhub-master"
  engine            = "postgres"
  engine_version    = "14.10"
  instance_class    = "db.t3.medium"
  allocated_storage = 100

  db_name  = "InventoryHub_Master"
  username = "postgres"
  password = var.db_password

  vpc_security_group_ids = [aws_security_group.rds.id]
  db_subnet_group_name   = aws_db_subnet_group.main.name

  backup_retention_period = 7
  skip_final_snapshot     = false
  final_snapshot_identifier = "inventoryhub-master-final"
}

# ElastiCache Redis
resource "aws_elasticache_cluster" "redis" {
  cluster_id           = "inventoryhub-redis"
  engine               = "redis"
  node_type            = "cache.t3.medium"
  num_cache_nodes      = 1
  parameter_group_name = "default.redis7"
  engine_version       = "7.0"
  port                 = 6379
  security_group_ids   = [aws_security_group.redis.id]
  subnet_group_name    = aws_elasticache_subnet_group.main.name
}

# ECS Cluster
resource "aws_ecs_cluster" "main" {
  name = "inventoryhub-cluster"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }
}

# Application Load Balancer
resource "aws_lb" "main" {
  name               = "inventoryhub-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = module.vpc.public_subnets
}
```

### Azure Deployment

**Architecture:**
- **Azure App Service:** API hosting
- **Azure Database for PostgreSQL:** Master and tenant databases
- **Azure Cache for Redis:** Distributed caching
- **AKS:** Kubernetes (alternative)
- **Azure Front Door:** Global load balancing
- **Application Insights:** Monitoring

### GCP Deployment

**Architecture:**
- **Cloud Run / GKE:** Container hosting
- **Cloud SQL PostgreSQL:** Databases
- **Memorystore Redis:** Caching
- **Cloud Load Balancing:** Load distribution
- **Cloud Monitoring:** Observability

---

## Database Setup

### Master Database Schema

```sql
-- Master database: InventoryHub_Master

-- Tenants table
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    subdomain VARCHAR(100) UNIQUE NOT NULL,
    connection_string TEXT NOT NULL,
    subscription_tier VARCHAR(50) NOT NULL,
    subscription_expires_at TIMESTAMP NOT NULL,
    is_active BOOLEAN DEFAULT true,
    max_users INT NOT NULL,
    max_products INT NOT NULL,
    api_rate_limit INT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    is_deleted BOOLEAN DEFAULT false
);

CREATE INDEX idx_tenants_subdomain ON tenants(subdomain);
CREATE INDEX idx_tenants_is_active ON tenants(is_active);

-- ASP.NET Identity tables (created by migrations)
-- AspNetUsers, AspNetRoles, etc.
```

### Tenant Database Template

```sql
-- Per-tenant database: InventoryHub_{TenantId}

-- Products table
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    sku VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    price DECIMAL(18,2) NOT NULL,
    cost_price DECIMAL(18,2),
    category_id UUID,
    supplier_id UUID,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP,
    created_by VARCHAR(256),
    updated_by VARCHAR(256),
    is_deleted BOOLEAN DEFAULT false
);

CREATE INDEX idx_products_tenant_id ON products(tenant_id);
CREATE INDEX idx_products_sku ON products(sku);
CREATE UNIQUE INDEX idx_products_sku_tenant ON products(sku, tenant_id) WHERE is_deleted = false;

-- Add all other tables from TenantDbContext...
-- (Categories, Suppliers, Orders, etc.)
```

### Database Backup Strategy

```bash
# Automated daily backups
#!/bin/bash
# backup-databases.sh

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups/postgres"

# Backup master database
pg_dump -h localhost -U postgres InventoryHub_Master | gzip > "$BACKUP_DIR/master_$DATE.sql.gz"

# Backup all tenant databases
psql -h localhost -U postgres -t -c "SELECT connection_string FROM tenants WHERE is_active = true" | while read connstr; do
  DBNAME=$(echo $connstr | grep -oP 'Database=\K[^;]+')
  pg_dump -h localhost -U postgres $DBNAME | gzip > "$BACKUP_DIR/tenant_${DBNAME}_$DATE.sql.gz"
done

# Delete backups older than 30 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete
```

---

## AI Provider Configuration

### Option 1: Ollama (Local - FREE)

```bash
# Docker deployment (included in docker-compose.yml)
docker run -d -p 11434:11434 -v ollama-data:/root/.ollama ollama/ollama

# Pull models
docker exec -it ollama ollama pull llama2     # 7B parameters
docker exec -it ollama ollama pull mistral    # 7B parameters
docker exec -it ollama ollama pull llama3     # 8B parameters (best quality)

# Configure in appsettings.json
"AI": {
  "Provider": "Ollama",
  "BaseUrl": "http://ollama:11434",
  "Model": "llama3"
}
```

**Pros:**
- 100% free
- Full privacy (no data leaves your servers)
- No API rate limits
- GDPR compliant

**Cons:**
- Requires GPU for best performance (optional)
- Self-hosted maintenance

### Option 2: DeepSeek (Cloud - Low Cost)

```bash
# Sign up at: https://platform.deepseek.com
# Get API key

# Configure in appsettings.json
"AI": {
  "Provider": "DeepSeek",
  "ApiKey": "YOUR_DEEPSEEK_API_KEY",
  "BaseUrl": "https://api.deepseek.com/v1",
  "Model": "deepseek-chat"
}
```

**Pricing:** $0.14 per 1M tokens (~$50/month for 1000 tenants)

**Pros:**
- Very cost-effective (200x cheaper than GPT-4)
- No infrastructure management
- High quality responses

**Cons:**
- Requires internet connection
- Data sent to third party (encryption in transit)

### Option 3: OpenAI (Premium)

```bash
# Get API key from: https://platform.openai.com

# Configure in appsettings.json
"AI": {
  "Provider": "OpenAI",
  "ApiKey": "YOUR_OPENAI_API_KEY",
  "BaseUrl": "https://api.openai.com/v1",
  "Model": "gpt-4"
}
```

**Pricing:** $20+ per 1M tokens (~$10,000/month for 1000 tenants)

---

## Monitoring & Logging

### Serilog Configuration

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/inventoryhub-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### Prometheus Metrics

**Install Prometheus exporter:**

```bash
dotnet add package prometheus-net.AspNetCore
```

**Configure in Program.cs:**

```csharp
using Prometheus;

var app = builder.Build();

// Add metrics endpoint
app.UseMetricServer();
app.UseHttpMetrics();
```

**Metrics exposed at:** `http://localhost:5000/metrics`

### Grafana Dashboards

**Key Metrics to Monitor:**
- API request rate (req/s)
- API response time (p50, p95, p99)
- Error rate (%)
- Active database connections
- Cache hit rate
- AI request latency
- Tenant count
- User count

---

## Security Hardening

### 1. HTTPS/TLS

```bash
# Generate self-signed certificate (dev only)
openssl req -x509 -newkey rsa:4096 -nodes \
  -keyout key.pem -out cert.pem -days 365 \
  -subj "/CN=localhost"

# Production: Use Let's Encrypt with cert-manager (Kubernetes)
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml
```

### 2. Environment Variables

```bash
# NEVER commit secrets to git
# Use environment variables or secret management

# AWS Secrets Manager
aws secretsmanager create-secret \
  --name inventoryhub/jwt-secret \
  --secret-string "YOUR_JWT_SECRET"

# Azure Key Vault
az keyvault secret set \
  --vault-name inventoryhub-vault \
  --name jwt-secret \
  --value "YOUR_JWT_SECRET"

# Kubernetes Secrets
kubectl create secret generic inventoryhub-secrets \
  --from-literal=jwt-secret='YOUR_SECRET' \
  -n inventoryhub
```

### 3. Firewall Rules

```bash
# Allow only necessary ports
ufw allow 80/tcp    # HTTP
ufw allow 443/tcp   # HTTPS
ufw allow 22/tcp    # SSH (restrict to specific IPs)
ufw deny 5432/tcp   # PostgreSQL (internal only)
ufw deny 6379/tcp   # Redis (internal only)
ufw enable
```

### 4. Database Security

```sql
-- Create read-only user for reporting
CREATE USER readonly_user WITH PASSWORD 'strong_password';
GRANT CONNECT ON DATABASE "InventoryHub_Master" TO readonly_user;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO readonly_user;

-- Revoke public schema permissions
REVOKE ALL ON DATABASE "InventoryHub_Master" FROM PUBLIC;
```

---

## Scaling Strategy

### Horizontal Scaling

**Auto-scaling with Kubernetes:**

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: inventoryhub-api-hpa
  namespace: inventoryhub
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: inventoryhub-api
  minReplicas: 3
  maxReplicas: 20
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### Database Scaling

**Read Replicas:**

```sql
-- PostgreSQL streaming replication
-- On master:
CREATE ROLE replication_user WITH REPLICATION LOGIN PASSWORD 'replica_password';

-- On replica:
pg_basebackup -h master-ip -D /var/lib/postgresql/data -U replication_user -P
```

**Connection Pooling:**

```bash
# Use PgBouncer for connection pooling
docker run -d --name pgbouncer \
  -e DB_HOST=postgres \
  -e DB_PORT=5432 \
  -e DB_USER=postgres \
  -e DB_PASSWORD=password \
  -p 6432:6432 \
  edoburu/pgbouncer
```

---

## Backup & Disaster Recovery

### Backup Schedule

| Data | Frequency | Retention | Method |
|------|-----------|-----------|--------|
| Master DB | Hourly | 7 days | pg_dump |
| Tenant DBs | Daily | 30 days | pg_dump |
| Redis | Daily | 7 days | RDB snapshot |
| Application Logs | Daily | 90 days | Archive to S3 |
| Configuration | On change | Infinite | Git |

### Disaster Recovery Plan

**RTO (Recovery Time Objective):** 1 hour
**RPO (Recovery Point Objective):** 1 hour (hourly backups)

**Recovery Steps:**
1. Deploy infrastructure (Terraform/Kubernetes)
2. Restore master database from latest backup
3. Restore tenant databases
4. Verify application health
5. Update DNS records
6. Monitor for issues

---

## Troubleshooting

### Common Issues

**Issue:** "Tenant not found or subscription expired"
```bash
# Check tenant exists and is active
psql -h postgres -U postgres InventoryHub_Master \
  -c "SELECT * FROM tenants WHERE subdomain = 'acme';"

# Check subscription expiration
psql -h postgres -U postgres InventoryHub_Master \
  -c "SELECT id, name, subscription_expires_at FROM tenants WHERE subscription_expires_at < NOW();"
```

**Issue:** "Database connection failed"
```bash
# Test connection
psql -h postgres -U postgres -d InventoryHub_Master

# Check connection string
docker exec -it api printenv | grep ConnectionStrings

# Check PostgreSQL logs
docker logs postgres
```

**Issue:** "AI forecasting fails"
```bash
# Test Ollama connection
curl http://ollama:11434/api/generate -d '{
  "model": "llama2",
  "prompt": "Hello",
  "stream": false
}'

# Check AI configuration
docker exec -it api printenv | grep AI
```

**Issue:** "High memory usage"
```bash
# Check container stats
docker stats

# Restart API with memory limit
docker run -m 2g inventoryhub/api:latest

# Analyze .NET heap
dotnet-dump collect -p $(pgrep dotnet)
dotnet-dump analyze core_dump
```

---

## Performance Tuning

### ASP.NET Core Settings

```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100,
      "MaxRequestBodySize": 10485760,
      "KeepAliveTimeout": "00:02:00"
    }
  }
}
```

### PostgreSQL Tuning

```sql
-- postgresql.conf
shared_buffers = 256MB
effective_cache_size = 1GB
maintenance_work_mem = 64MB
checkpoint_completion_target = 0.9
wal_buffers = 16MB
default_statistics_target = 100
random_page_cost = 1.1
effective_io_concurrency = 200
work_mem = 4MB
min_wal_size = 1GB
max_wal_size = 4GB
max_worker_processes = 4
max_parallel_workers_per_gather = 2
max_parallel_workers = 4
```

### Redis Tuning

```bash
# redis.conf
maxmemory 2gb
maxmemory-policy allkeys-lru
save 900 1
save 300 10
save 60 10000
```

---

**Document Version:** 1.0
**Last Updated:** 2025-11-13
**Maintained By:** DevOps Team
**Support:** devops@inventoryhub.com

---

**Production Checklist:**

- [ ] SSL/TLS certificates configured
- [ ] Environment variables set
- [ ] Database backups automated
- [ ] Monitoring dashboards configured
- [ ] Logging centralized
- [ ] Secrets management configured
- [ ] Firewall rules applied
- [ ] Load balancer configured
- [ ] Auto-scaling enabled
- [ ] Disaster recovery tested
- [ ] Performance benchmarks met
- [ ] Security audit passed

**Ready for Production! 🚀**
