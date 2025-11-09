#!/bin/bash
# Clean development environment (remove containers, volumes, images)

echo "🧹 Cleaning InventoryHub Development Environment..."

read -p "This will remove all containers, volumes, and images. Continue? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    docker-compose -f docker-compose.dev.yml down -v --rmi local
    rm -rf logs/*
    echo "✅ Development environment cleaned"
else
    echo "❌ Cancelled"
fi
