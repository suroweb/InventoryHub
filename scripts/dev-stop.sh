#!/bin/bash
# Stop development environment

echo "🛑 Stopping InventoryHub Development Environment..."

docker-compose -f docker-compose.dev.yml down

echo "✅ Development environment stopped"
