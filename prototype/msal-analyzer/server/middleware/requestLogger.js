/**
 * MSAL Log Analyzer - Request Logger Middleware
 * Logs incoming API requests with timing information
 */

function requestLogger(req, res, next) {
  const start = Date.now();

  res.on('finish', () => {
    const duration = Date.now() - start;
    const level = res.statusCode >= 400 ? 'WARN' : 'INFO';
    console.log(`[${level}] ${req.method} ${req.path} → ${res.statusCode} (${duration}ms)`);
  });

  next();
}

module.exports = { requestLogger };
