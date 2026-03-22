#!/bin/sh
set -e

echo "Initializing database schema..."

# Run the Postgres init script if DATABASE_URL is set
if [ -n "$DATABASE_URL" ]; then
  # Install psql client if not present
  apk add --no-cache postgresql-client > /dev/null 2>&1 || true
  psql "$DATABASE_URL" -f /app/init.sql 2>&1 || echo "Schema may already exist (OK)"
fi

echo "Starting holdem-processor..."
cd /app/service
exec node server.js
