FROM node:22-alpine

WORKDIR /app

# Copy shared code and install its dependencies (+ pg driver for Postgres)
COPY serverless-v2/shared ./shared
RUN cd shared && npm install --production && npm install pg pg-hstore

# Copy holdem-processor service
COPY serverless-v2/services/holdem-processor ./service

# Copy Postgres init script
COPY infrastructure/postgres-init.sql ./init.sql

# Replace the symlink with a proper link to /app/shared
WORKDIR /app/service
RUN rm -rf shared && ln -s /app/shared shared

# Install service dependencies + express for the standalone server
RUN npm install --production && npm install express pg pg-hstore

# Copy startup script
COPY deploy/start.sh /app/start.sh
RUN chmod +x /app/start.sh

EXPOSE 3030

CMD ["/app/start.sh"]
