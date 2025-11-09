#!/bin/bash
# Start development environment

echo "🚀 Starting InventoryHub Development Environment..."

# Load environment variables
if [ -f .env.dev ]; then
    export $(cat .env.dev | grep -v '^#' | xargs)
fi

# Start services
docker-compose -f docker-compose.dev.yml up -d

echo ""
echo "✅ Development environment started!"
echo ""
echo "Services:"
echo "  🌐 Web UI:        http://localhost:5173"
echo "  🔌 API:           http://localhost:5000"
echo "  🔒 API (HTTPS):   https://localhost:5001"
echo "  ❤️  Health Check: http://localhost:5000/health"
echo "  📊 Swagger:       http://localhost:5000/openapi/v1.json"
echo ""
echo "Logs:"
echo "  View logs:   docker-compose -f docker-compose.dev.yml logs -f"
echo "  API logs:    docker logs -f inventoryhub-api-dev"
echo "  Web logs:    docker logs -f inventoryhub-web-dev"
echo ""
echo "Commands:"
echo "  Stop:        ./scripts/dev-stop.sh"
echo "  Restart:     ./scripts/dev-restart.sh"
echo "  Logs:        ./scripts/dev-logs.sh"
echo ""
