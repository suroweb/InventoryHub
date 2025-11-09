#!/bin/bash
# Development environment setup script

set -e

echo "🚀 Setting up InventoryHub Development Environment..."

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

echo -e "${BLUE}📦 Creating necessary directories...${NC}"
mkdir -p logs
mkdir -p dev-certs

# Generate development HTTPS certificate if it doesn't exist
if [ ! -f "dev-certs/aspnetapp.pfx" ]; then
    echo -e "${BLUE}🔐 Generating development HTTPS certificate...${NC}"

    # Check if .NET SDK is available locally
    if command -v dotnet &> /dev/null; then
        dotnet dev-certs https -ep dev-certs/aspnetapp.pfx -p DevPassword123 --trust
        echo -e "${GREEN}✅ Development certificate generated and trusted${NC}"
    else
        echo -e "${YELLOW}⚠️  .NET SDK not found locally. Skipping HTTPS certificate generation.${NC}"
        echo -e "${YELLOW}   You can generate it later by running: dotnet dev-certs https -ep dev-certs/aspnetapp.pfx -p DevPassword123${NC}"
    fi
fi

# Create .env file for development if it doesn't exist
if [ ! -f ".env.dev" ]; then
    echo -e "${BLUE}📝 Creating .env.dev file...${NC}"
    cat > .env.dev << 'EOF'
# Development Environment Configuration
ASPNETCORE_ENVIRONMENT=Development
COMPOSE_PROJECT_NAME=inventoryhub-dev

# API Configuration
API_PORT=5000
API_HTTPS_PORT=5001
API_DEBUG_PORT=5002

# Web Configuration
WEB_PORT=5173

# Database (when enabled)
POSTGRES_USER=inventoryhub
POSTGRES_PASSWORD=DevPassword123
POSTGRES_DB=inventoryhub_dev
DB_PORT=5432

# Redis (when enabled)
REDIS_PORT=6379

# Mailhog (when enabled)
MAILHOG_SMTP_PORT=1025
MAILHOG_WEB_PORT=8025
EOF
    echo -e "${GREEN}✅ .env.dev created${NC}"
fi

echo -e "${BLUE}🔨 Building development Docker images...${NC}"
docker-compose -f docker-compose.dev.yml build

echo ""
echo -e "${GREEN}✅ Development environment setup complete!${NC}"
echo ""
echo -e "${BLUE}To start development:${NC}"
echo -e "  ${YELLOW}./scripts/dev-start.sh${NC}  - Start all services"
echo -e "  ${YELLOW}./scripts/dev-stop.sh${NC}   - Stop all services"
echo -e "  ${YELLOW}./scripts/dev-logs.sh${NC}   - View logs"
echo ""
echo -e "${BLUE}Services will be available at:${NC}"
echo -e "  API:         http://localhost:5000"
echo -e "  API (HTTPS): https://localhost:5001"
echo -e "  Web:         http://localhost:5173"
echo -e "  Health:      http://localhost:5000/health"
echo ""
