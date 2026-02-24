'use strict';

require('dotenv').config({ path: require('path').join(__dirname, '..', '.env') });

const express = require('express');
const cors = require('cors');
const morgan = require('morgan');
const path = require('path');
const cookieParser = require('cookie-parser');

const analyzeRouter = require('./routes/analyze');
const reportsRouter = require('./routes/reports');
const sessionsRouter = require('./routes/sessions');

const app = express();
const PORT = process.env.PORT || 3001;
const FRONTEND_DIR = path.join(__dirname, '..', 'frontend');

// ─── Middleware ───────────────────────────────────────────────────────────────
app.use(morgan('combined'));
app.use(cors({
  origin: process.env.CORS_ORIGIN || '*',
  credentials: true,
}));
app.use(express.json({ limit: '10mb' }));
app.use(express.urlencoded({ extended: true }));
app.use(cookieParser());

// Serve static frontend
app.use(express.static(FRONTEND_DIR));

// ─── API Routes ───────────────────────────────────────────────────────────────
app.use('/api/analyze', analyzeRouter);
app.use('/api/reports', reportsRouter);
app.use('/api/sessions', sessionsRouter);

/**
 * GET /api/health
 * Health check endpoint.
 */
app.get('/api/health', (_req, res) => {
  res.json({
    status: 'ok',
    version: require('./package.json').version,
    timestamp: new Date().toISOString(),
  });
});

/**
 * GET /api/docs
 * Simple API documentation.
 */
app.get('/api/docs', (_req, res) => {
  res.json({
    name: 'MSAL Log Analyzer API',
    version: require('./package.json').version,
    endpoints: {
      'POST /api/analyze': 'Upload and analyze a single MSAL log file (field: logFile)',
      'POST /api/analyze/batch': 'Upload and analyze multiple files (field: logFiles)',
      'GET /api/reports': 'List analyses (query: search, favorite, limit, offset)',
      'GET /api/reports/:id': 'Get analysis by ID',
      'GET /api/reports/:id/html': 'Export analysis as HTML report',
      'PATCH /api/reports/:id/favorite': 'Toggle favorite status',
      'DELETE /api/reports/:id': 'Delete analysis',
      'GET /api/sessions/preferences': 'Get session preferences',
      'PUT /api/sessions/preferences': 'Update session preferences',
      'GET /api/health': 'Health check',
    },
  });
});

// ─── SPA Fallback ─────────────────────────────────────────────────────────────
app.get('/{*splat}', (_req, res) => {
  res.sendFile(path.join(FRONTEND_DIR, 'index.html'));
});

// ─── Error Handler ────────────────────────────────────────────────────────────
// eslint-disable-next-line no-unused-vars
app.use((err, req, res, _next) => {
  console.error('Unhandled error:', err);
  const status = err.status || err.statusCode || 500;
  res.status(status).json({
    error: err.message || 'Internal server error',
    ...(process.env.NODE_ENV !== 'production' && { stack: err.stack }),
  });
});

// ─── Start ────────────────────────────────────────────────────────────────────
app.listen(PORT, () => {
  console.log(`MSAL Log Analyzer running at http://localhost:${PORT}`);
  console.log(`API docs: http://localhost:${PORT}/api/docs`);
});

module.exports = app;
