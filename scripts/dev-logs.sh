#!/bin/bash
# View development logs

SERVICE=$1

if [ -z "$SERVICE" ]; then
    echo "📋 Viewing all logs (Ctrl+C to exit)..."
    docker-compose -f docker-compose.dev.yml logs -f
else
    echo "📋 Viewing logs for $SERVICE (Ctrl+C to exit)..."
    docker-compose -f docker-compose.dev.yml logs -f $SERVICE
fi
