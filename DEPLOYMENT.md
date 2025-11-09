# InventoryHub - Production Deployment Guide

This guide covers deploying InventoryHub to production environments.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Environment Configuration](#environment-configuration)
- [Docker Deployment](#docker-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Azure App Service Deployment](#azure-app-service-deployment)
- [Monitoring and Logging](#monitoring-and-logging)
- [Security Considerations](#security-considerations)

---

## Prerequisites

- Docker and Docker Compose (for containerized deployment)
- .NET 10 SDK (for manual deployment)
- SSL/TLS certificate (for HTTPS)
- Domain name (optional, for production)

---

## Environment Configuration

### 1. Create Environment File

Copy the example environment file:

```bash
cp .env.example .env
```

### 2. Configure Settings

Edit `.env` with your production values:

```bash
# Required settings
ASPNETCORE_ENVIRONMENT=Production
CORS_ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com

# Optional settings
CACHE_DURATION_MINUTES=10
RATE_LIMIT_PERMITS=200
RATE_LIMIT_WINDOW_SECONDS=60
```

### 3. Update appsettings.Production.json

Create `FullStackApp/ServerApp/appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com"
    ]
  },
  "Caching": {
    "OutputCacheDurationMinutes": 10
  },
  "RateLimiting": {
    "PermitLimit": 200,
    "WindowSeconds": 60
  }
}
```

---

## Docker Deployment

### Option 1: Docker Compose (Recommended for simple deployments)

```bash
# Build and start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

Services will be available at:
- **API**: http://localhost:5000
- **Web**: http://localhost:80

### Option 2: Individual Docker Containers

#### Build Images

```bash
# Build API image
docker build --target runtime-server -t inventoryhub-api:1.0 .

# Build Web image
docker build --target runtime-client -t inventoryhub-web:1.0 .
```

#### Run Containers

```bash
# Create network
docker network create inventoryhub-network

# Run API
docker run -d \
  --name inventoryhub-api \
  --network inventoryhub-network \
  -p 5000:8080 \
  -v $(pwd)/logs:/app/logs \
  -e ASPNETCORE_ENVIRONMENT=Production \
  inventoryhub-api:1.0

# Run Web
docker run -d \
  --name inventoryhub-web \
  --network inventoryhub-network \
  -p 80:80 \
  inventoryhub-web:1.0
```

### Health Checks

Monitor container health:

```bash
# Check API health
curl http://localhost:5000/health

# Check detailed health
curl http://localhost:5000/health/ready
curl http://localhost:5000/health/live

# Check API version
curl http://localhost:5000/api/version
```

---

## Kubernetes Deployment

### 1. Create Kubernetes Manifests

**api-deployment.yaml**:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: inventoryhub-api
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
        image: inventoryhub-api:1.0
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: inventoryhub-api-service
spec:
  selector:
    app: inventoryhub-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: LoadBalancer
```

### 2. Deploy to Kubernetes

```bash
# Apply configurations
kubectl apply -f k8s/

# Check deployments
kubectl get deployments
kubectl get pods
kubectl get services

# View logs
kubectl logs -f deployment/inventoryhub-api
```

---

## Azure App Service Deployment

### 1. Using Azure CLI

```bash
# Login to Azure
az login

# Create resource group
az group create --name inventoryhub-rg --location eastus

# Create App Service plan
az appservice plan create \
  --name inventoryhub-plan \
  --resource-group inventoryhub-rg \
  --sku B1 \
  --is-linux

# Create web app for API
az webapp create \
  --name inventoryhub-api \
  --resource-group inventoryhub-rg \
  --plan inventoryhub-plan \
  --runtime "DOTNET:10.0"

# Deploy API
cd FullStackApp/ServerApp
dotnet publish -c Release
cd bin/Release/net10.0/publish
zip -r deploy.zip .
az webapp deployment source config-zip \
  --resource-group inventoryhub-rg \
  --name inventoryhub-api \
  --src deploy.zip
```

### 2. Configure App Settings

```bash
az webapp config appsettings set \
  --resource-group inventoryhub-rg \
  --name inventoryhub-api \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    Cors__AllowedOrigins__0=https://yourdomain.com
```

---

## Monitoring and Logging

### Application Logs

Logs are written to `/app/logs` directory with daily rotation:

```bash
# View logs in Docker
docker exec -it inventoryhub-api cat /app/logs/inventoryhub-20250109.log

# Tail logs
docker exec -it inventoryhub-api tail -f /app/logs/inventoryhub-20250109.log
```

### Structured Logging with Serilog

The application uses Serilog for structured logging:

- **Console sink**: Development debugging
- **File sink**: Production log persistence
- **Log levels**: Information, Warning, Error, Fatal

### Health Monitoring

Set up automated health checks:

```bash
# Create a monitoring script
cat > monitor.sh << 'EOF'
#!/bin/bash
HEALTH_URL="http://localhost:5000/health"
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ $RESPONSE -eq 200 ]; then
  echo "✓ Service is healthy"
else
  echo "✗ Service is unhealthy (HTTP $RESPONSE)"
  # Add alerting logic here
fi
EOF

chmod +x monitor.sh

# Run with cron (every 5 minutes)
# */5 * * * * /path/to/monitor.sh
```

### Application Insights (Optional)

To enable Azure Application Insights:

1. Update `appsettings.Production.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key-here"
  }
}
```

2. Add package to ServerApp.csproj:

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

---

## Security Considerations

### 1. HTTPS Configuration

For production, always use HTTPS:

```bash
# Generate self-signed certificate (development only)
dotnet dev-certs https -ep ./cert.pfx -p YourPassword

# For production, use Let's Encrypt or purchased certificate
```

Update docker-compose.yml for HTTPS:

```yaml
api:
  environment:
    - ASPNETCORE_URLS=https://+:8081;http://+:8080
    - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pfx
    - ASPNETCORE_Kestrel__Certificates__Default__Password=YourPassword
  volumes:
    - ./cert.pfx:/app/cert.pfx:ro
```

### 2. Security Headers

The application includes security headers by default:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: no-referrer`

### 3. Rate Limiting

Configured to prevent abuse:

- **Default**: 100 requests per 60 seconds per client
- Adjust in `appsettings.json` based on your needs

### 4. CORS Configuration

Only allow trusted origins:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://www.yourdomain.com"
    ]
  }
}
```

### 5. Secrets Management

**Never commit secrets to source control!**

Use environment variables or Azure Key Vault:

```bash
# Example with Azure Key Vault
az keyvault secret set \
  --vault-name your-vault \
  --name ConnectionString \
  --value "your-connection-string"
```

---

## Performance Tuning

### 1. Output Caching

Adjust cache duration based on data volatility:

```json
{
  "Caching": {
    "OutputCacheDurationMinutes": 10  // Increase for stable data
  }
}
```

### 2. Resource Limits

For Docker, set appropriate resource limits:

```yaml
api:
  deploy:
    resources:
      limits:
        cpus: '1.0'
        memory: 512M
      reservations:
        cpus: '0.5'
        memory: 256M
```

---

## Backup and Recovery

### Database Backups (Future Enhancement)

When database is integrated:

```bash
# Automated daily backups
0 2 * * * docker exec inventoryhub-db pg_dump -U postgres inventoryhub > /backups/db-$(date +\%Y\%m\%d).sql
```

### Log Retention

Configure log rotation in Serilog:

```csharp
.WriteTo.File("logs/inventoryhub-.log",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 30)  // Keep 30 days of logs
```

---

## Troubleshooting

### Common Issues

**1. CORS errors**

- Verify `Cors:AllowedOrigins` includes the client origin
- Check middleware order in Program.cs
- Ensure cache varies by Origin header

**2. Container won't start**

```bash
# Check logs
docker logs inventoryhub-api

# Verify configuration
docker exec -it inventoryhub-api env | grep ASPNETCORE
```

**3. Health check fails**

```bash
# Test from inside container
docker exec -it inventoryhub-api curl http://localhost:8080/health

# Check application logs
docker exec -it inventoryhub-api cat /app/logs/inventoryhub-*.log
```

**4. Rate limiting too aggressive**

Adjust settings in `appsettings.json`:

```json
{
  "RateLimiting": {
    "PermitLimit": 500,
    "WindowSeconds": 60
  }
}
```

---

## Support

For issues and questions:

- GitHub Issues: https://github.com/yourusername/InventoryHub/issues
- Documentation: See README.md

---

**Last Updated**: January 2025
