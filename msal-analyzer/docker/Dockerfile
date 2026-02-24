# ─── Stage 1: Build React Frontend ───────────────────────────────────────────
FROM node:18-alpine AS client-builder

WORKDIR /app/client

# Install dependencies
COPY client/package.json ./
RUN npm install

# Copy source and build
COPY client/ ./
RUN npm run build

# ─── Stage 2: Production Server ───────────────────────────────────────────────
FROM node:18-alpine AS production

# Add security: run as non-root user
RUN addgroup -g 1001 -S nodejs && \
    adduser -S msalapp -u 1001

WORKDIR /app

# Install server dependencies
COPY server/package.json ./server/
RUN cd server && npm install --omit=dev

# Copy server source
COPY server/ ./server/

# Copy built frontend from builder stage
COPY --from=client-builder /app/client/build ./client/build

# Set ownership
RUN chown -R msalapp:nodejs /app

USER msalapp

# Environment
ENV NODE_ENV=production
ENV PORT=3001

EXPOSE 3001

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:3001/api/health || exit 1

CMD ["node", "server/server.js"]
