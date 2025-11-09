#!/bin/bash
# Restart development environment

echo "🔄 Restarting InventoryHub Development Environment..."

docker-compose -f docker-compose.dev.yml restart

echo "✅ Development environment restarted"
echo "View logs with: ./scripts/dev-logs.sh"
