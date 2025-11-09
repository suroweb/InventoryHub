# InventoryHub - Development Guide

This guide helps you set up a local development environment that mirrors production.

## Table of Contents

- [Quick Start](#quick-start)
- [Development Environment Architecture](#development-environment-architecture)
- [Prerequisites](#prerequisites)
- [Setup Instructions](#setup-instructions)
- [Development Workflow](#development-workflow)
- [Available Scripts](#available-scripts)
- [Hot Reload](#hot-reload)
- [Debugging](#debugging)
- [Database Development](#database-development)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

---

## Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/yourusername/InventoryHub.git
cd InventoryHub

# 2. Run setup script
./scripts/dev-setup.sh

# 3. Start development environment
./scripts/dev-start.sh

# 4. Open your browser
# Web UI: http://localhost:5173
# API: http://localhost:5000
```

That's it! You're now running a production-like environment locally with hot reload.

---

## Development Environment Architecture

### Production Parity

Your local development environment mirrors production with:

- ✅ **Same containerization** (Docker)
- ✅ **Same service architecture** (API + Web)
- ✅ **Same network configuration**
- ✅ **Same security headers**
- ✅ **Same health checks**
- ✅ **Same logging infrastructure**

### Development-Specific Features

Enhanced with developer-friendly features:

- 🔥 **Hot Reload** - Code changes auto-reload without restart
- 🐛 **Debugging Support** - Attach debugger on port 5002
- 📝 **Verbose Logging** - Debug-level logs for troubleshooting
- 🔓 **Relaxed Rate Limiting** - 1000 req/min instead of 100
- ⚡ **Fast Cache** - 1-minute cache instead of 5 minutes
- 📂 **Volume Mounts** - Edit code directly on your machine

---

## Prerequisites

### Required

- **Docker** 20.10 or higher
- **Docker Compose** 2.0 or higher
- **Git**

### Optional (for native development without Docker)

- **.NET 10 SDK** (RC or later)
- **Visual Studio 2022**, **VS Code**, or **JetBrains Rider**

### Verify Installation

```bash
docker --version          # Should be 20.10+
docker-compose --version  # Should be 2.0+
git --version
```

---

## Setup Instructions

### Option 1: Automated Setup (Recommended)

```bash
# Run the setup script
./scripts/dev-setup.sh
```

This script will:
1. Create necessary directories
2. Generate HTTPS development certificate
3. Create `.env.dev` configuration file
4. Build development Docker images

### Option 2: Manual Setup

```bash
# 1. Create directories
mkdir -p logs dev-certs

# 2. Generate development certificate (requires .NET SDK)
dotnet dev-certs https -ep dev-certs/aspnetapp.pfx -p DevPassword123 --trust

# 3. Create environment file
cp .env.example .env.dev

# 4. Build images
docker-compose -f docker-compose.dev.yml build
```

---

## Development Workflow

### Starting Your Day

```bash
# Start all services
./scripts/dev-start.sh

# View logs (optional)
./scripts/dev-logs.sh
```

### Making Code Changes

1. **Edit code** in your IDE (VS Code, Visual Studio, Rider)
2. **Save the file** - Changes are automatically detected
3. **Wait for reload** - Service restarts automatically (5-10 seconds)
4. **Refresh browser** - See your changes

### Example Workflow

```bash
# Terminal 1: Start services and watch logs
./scripts/dev-start.sh
./scripts/dev-logs.sh

# Terminal 2: Your IDE or editor
code .

# Make changes to FullStackApp/ServerApp/Program.cs
# Save the file
# Watch Terminal 1 for "Hot reload succeeded" message
# Refresh http://localhost:5000/api/productlist
```

### Running Tests

```bash
# Run all tests
./scripts/dev-test.sh

# Or use dotnet directly if installed locally
cd FullStackApp/ServerApp.Tests
dotnet test
```

### Ending Your Day

```bash
# Stop all services
./scripts/dev-stop.sh
```

---

## Available Scripts

All scripts are located in the `scripts/` directory:

### Core Scripts

| Script | Description |
|--------|-------------|
| `dev-setup.sh` | Initial setup (run once) |
| `dev-start.sh` | Start all services |
| `dev-stop.sh` | Stop all services |
| `dev-restart.sh` | Restart all services |
| `dev-logs.sh [service]` | View logs (all or specific service) |

### Utility Scripts

| Script | Description |
|--------|-------------|
| `dev-test.sh` | Run unit tests |
| `dev-shell.sh [service]` | Open shell in container |
| `dev-clean.sh` | Clean up everything (destructive) |

### Usage Examples

```bash
# View API logs only
./scripts/dev-logs.sh api-dev

# View Web logs only
./scripts/dev-logs.sh web-dev

# Open shell in API container
./scripts/dev-shell.sh api-dev

# Run tests
./scripts/dev-test.sh

# Complete cleanup (removes volumes, images)
./scripts/dev-clean.sh
```

---

## Hot Reload

### How It Works

The development environment uses `dotnet watch` to monitor file changes:

1. **File Watcher** monitors your source code directories
2. **Change Detection** detects when you save a file
3. **Auto Rebuild** recompiles affected code
4. **Auto Restart** restarts the service with new code
5. **Notification** logs "Hot reload succeeded" or error message

### What Triggers Hot Reload

- ✅ `.cs` file changes (C# code)
- ✅ `.razor` file changes (Blazor components)
- ✅ `.json` file changes (configuration files)
- ✅ `.csproj` file changes (project files)

### What Requires Manual Restart

- ❌ Dockerfile changes - Run `docker-compose -f docker-compose.dev.yml build`
- ❌ docker-compose.yml changes - Run `./scripts/dev-restart.sh`
- ❌ New NuGet packages - Rebuild container

### Hot Reload Troubleshooting

**Issue**: Hot reload not working

```bash
# Check if file watcher is enabled
docker exec inventoryhub-api-dev env | grep DOTNET_USE_POLLING_FILE_WATCHER
# Should output: DOTNET_USE_POLLING_FILE_WATCHER=true

# Check logs for errors
./scripts/dev-logs.sh api-dev
```

**Issue**: Changes detected but not applied

```bash
# Force restart
./scripts/dev-restart.sh
```

---

## Debugging

### Attach Debugger (Visual Studio / VS Code)

The API service exposes port **5002** for remote debugging.

#### VS Code Configuration

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Attach to API (Docker)",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:pickRemoteProcess}",
      "pipeTransport": {
        "pipeCwd": "${workspaceRoot}",
        "pipeProgram": "docker",
        "pipeArgs": ["exec", "-i", "inventoryhub-api-dev"],
        "debuggerPath": "/vsdbg/vsdbg",
        "quoteArgs": false
      },
      "sourceFileMap": {
        "/app": "${workspaceRoot}/FullStackApp"
      }
    }
  ]
}
```

#### Visual Studio 2022

1. **Debug** → **Attach to Process**
2. **Connection Type**: Docker (Linux Container)
3. **Connection Target**: inventoryhub-api-dev
4. Select the `ServerApp` process
5. Click **Attach**

### Browser DevTools (Blazor)

Blazor WebAssembly has built-in debugging:

1. Open **http://localhost:5173** in Chrome/Edge
2. Press **Shift + Alt + D**
3. Follow instructions to enable debugging
4. Set breakpoints in browser DevTools

---

## Database Development

### Enable PostgreSQL (Optional)

Uncomment the `db-dev` service in `docker-compose.dev.yml`:

```yaml
# Uncomment these lines:
db-dev:
  image: postgres:16-alpine
  # ... rest of configuration
```

Then restart:

```bash
./scripts/dev-restart.sh
```

**Connection String**:
```
Server=localhost;Port=5432;Database=inventoryhub_dev;User Id=inventoryhub;Password=DevPassword123;
```

### Enable Redis Caching (Optional)

Uncomment the `redis-dev` service in `docker-compose.dev.yml`:

```yaml
# Uncomment these lines:
redis-dev:
  image: redis:7-alpine
  # ... rest of configuration
```

**Connection String**:
```
localhost:6379
```

### Database Migrations (Future)

When you add Entity Framework Core:

```bash
# Create migration
docker exec inventoryhub-api-dev dotnet ef migrations add InitialCreate

# Apply migration
docker exec inventoryhub-api-dev dotnet ef database update

# Or use script
./scripts/dev-shell.sh api-dev
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Testing

### Run Tests in Development

```bash
# Quick test run
./scripts/dev-test.sh

# Watch mode (tests run on file changes)
docker exec inventoryhub-api-dev dotnet watch test /app/ServerApp.Tests/ServerApp.Tests.csproj
```

### Integration Testing

The development environment is perfect for integration testing:

```bash
# API is running at http://localhost:5000
curl http://localhost:5000/api/productlist
curl http://localhost:5000/health

# Or use the ServerApp.http file in VS Code with REST Client extension
```

### Test Coverage

```bash
# Generate coverage report
docker exec inventoryhub-api-dev dotnet test /app/ServerApp.Tests/ServerApp.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory /app/coverage
```

---

## Environment Variables

### Development Defaults

The `.env.dev` file contains development-specific configuration:

```bash
ASPNETCORE_ENVIRONMENT=Development
API_PORT=5000
API_HTTPS_PORT=5001
API_DEBUG_PORT=5002
WEB_PORT=5173
```

### Override Configuration

You can override settings in `appsettings.Development.json`:

- **Logging**: More verbose (Debug level)
- **CORS**: Allows localhost origins
- **Caching**: Shorter duration (1 minute vs 5 minutes)
- **Rate Limiting**: Higher limits (1000/min vs 100/min)

---

## Service URLs

### Local Development

| Service | URL | Description |
|---------|-----|-------------|
| Web UI | http://localhost:5173 | Blazor WebAssembly app |
| API | http://localhost:5000 | REST API (HTTP) |
| API (HTTPS) | https://localhost:5001 | REST API (HTTPS) |
| Health Check | http://localhost:5000/health | Health endpoint |
| OpenAPI | http://localhost:5000/openapi/v1.json | API documentation |
| Debugger | localhost:5002 | Remote debugging port |

### Optional Services (when enabled)

| Service | URL | Description |
|---------|-----|-------------|
| PostgreSQL | localhost:5432 | Database |
| Redis | localhost:6379 | Cache |
| Mailhog | http://localhost:8025 | Email testing UI |

---

## Development vs Production Differences

| Feature | Development | Production |
|---------|-------------|------------|
| **Log Level** | Debug | Information/Warning |
| **Cache Duration** | 1 minute | 5 minutes |
| **Rate Limiting** | 1000 req/min | 100 req/min |
| **HTTPS** | Optional | Required |
| **Hot Reload** | Enabled | Disabled |
| **Volume Mounts** | Source code | Compiled binaries |
| **Image Size** | Large (SDK) | Small (runtime) |
| **Error Details** | Verbose | Generic |

---

## Troubleshooting

### Port Already in Use

```bash
# Check what's using the port
lsof -i :5000  # or netstat -ano | findstr :5000 on Windows

# Kill the process or change port in .env.dev
```

### Container Won't Start

```bash
# Check logs
./scripts/dev-logs.sh

# Rebuild containers
docker-compose -f docker-compose.dev.yml build --no-cache
./scripts/dev-start.sh
```

### Hot Reload Not Working

```bash
# Ensure file watcher is enabled
docker exec inventoryhub-api-dev env | grep DOTNET_USE_POLLING_FILE_WATCHER

# Force restart
./scripts/dev-restart.sh
```

### Changes Not Appearing

```bash
# 1. Check if file is mounted correctly
docker exec inventoryhub-api-dev ls -la /app/ServerApp

# 2. Check logs for rebuild messages
./scripts/dev-logs.sh api-dev

# 3. Try manual restart
./scripts/dev-restart.sh
```

### Volume Permission Issues (Linux)

```bash
# Fix permissions
sudo chown -R $USER:$USER FullStackApp/
chmod -R 755 FullStackApp/
```

### HTTPS Certificate Issues

```bash
# Regenerate certificate
rm -rf dev-certs/*
dotnet dev-certs https -ep dev-certs/aspnetapp.pfx -p DevPassword123 --trust

# Restart services
./scripts/dev-restart.sh
```

### Database Connection Issues

```bash
# Check if database is running
docker ps | grep postgres

# Check logs
docker logs inventoryhub-db-dev

# Try connecting manually
docker exec -it inventoryhub-db-dev psql -U inventoryhub -d inventoryhub_dev
```

---

## IDE Setup

### Visual Studio Code

**Recommended Extensions**:

- C# Dev Kit
- Docker
- REST Client (for testing APIs)
- GitLens

**Settings** (`.vscode/settings.json`):

```json
{
  "dotnet.defaultSolution": "FullStackApp/FullStackSolution.sln",
  "files.watcherExclude": {
    "**/bin/**": true,
    "**/obj/**": true
  }
}
```

### Visual Studio 2022

1. Open `FullStackApp/FullStackSolution.sln`
2. Right-click solution → **Properties** → **Startup Project**
3. Select **Multiple startup projects**
4. Set both ServerApp and ClientApp to **Start**

### JetBrains Rider

1. Open `FullStackApp/FullStackSolution.sln`
2. **Run** → **Edit Configurations**
3. Add **Compound** configuration
4. Add both ServerApp and ClientApp

---

## Best Practices

### 1. Always Use Docker for Development

- Ensures consistency across team
- Mirrors production environment
- Avoids "works on my machine" issues

### 2. Check Health Before Testing

```bash
curl http://localhost:5000/health
```

### 3. Watch Logs While Developing

```bash
./scripts/dev-logs.sh
```

### 4. Run Tests Before Committing

```bash
./scripts/dev-test.sh
```

### 5. Keep Docker Images Updated

```bash
docker-compose -f docker-compose.dev.yml pull
docker-compose -f docker-compose.dev.yml build
```

---

## Performance Tips

### Faster Builds

```bash
# Use BuildKit
export DOCKER_BUILDKIT=1
docker-compose -f docker-compose.dev.yml build
```

### Volume Caching

The docker-compose.dev.yml uses `:cached` mounts for better performance on macOS/Windows.

### Reduce Log Noise

Edit `appsettings.Development.json` to reduce verbosity:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## Next Steps

1. **Read the main [README.md](README.md)** for project overview
2. **Check [DEPLOYMENT.md](DEPLOYMENT.md)** for production deployment
3. **Review [PRODUCTION_IMPROVEMENTS.md](PRODUCTION_IMPROVEMENTS.md)** for features

---

## Getting Help

- **Check logs**: `./scripts/dev-logs.sh`
- **View container status**: `docker ps`
- **Inspect container**: `./scripts/dev-shell.sh api-dev`
- **GitHub Issues**: Report problems at [GitHub Issues](https://github.com/yourusername/InventoryHub/issues)

---

**Happy Coding! 🚀**

Your local environment now mirrors production, giving you confidence that your code will work when deployed.
