#!/bin/bash
# Run tests in development environment

echo "🧪 Running tests..."

# Option 1: Run tests in existing dev container
if docker ps | grep -q inventoryhub-api-dev; then
    echo "Running tests in dev container..."
    docker exec inventoryhub-api-dev dotnet test /app/ServerApp.Tests/ServerApp.Tests.csproj --verbosity normal
else
    # Option 2: Run tests in a new container
    echo "Starting test container..."
    docker-compose -f docker-compose.dev.yml run --rm api-dev dotnet test /app/ServerApp.Tests/ServerApp.Tests.csproj --verbosity normal
fi
