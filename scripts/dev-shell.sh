#!/bin/bash
# Attach to a development container shell

SERVICE=${1:-api-dev}

echo "🔌 Attaching to $SERVICE container..."
docker exec -it inventoryhub-$SERVICE /bin/bash
