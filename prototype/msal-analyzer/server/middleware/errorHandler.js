/**
 * MSAL Log Analyzer - Error Handler Middleware
 * Centralized error handling for the Express API
 */

/**
 * Express error handler middleware.
 * Formats errors into consistent JSON responses.
 */
function errorHandler(err, req, res, next) {
  console.error('Error:', err.message);

  // Multer file size error
  if (err.code === 'LIMIT_FILE_SIZE') {
    const maxMb = Math.round((parseInt(process.env.MAX_FILE_SIZE, 10) || 10485760) / (1024 * 1024));
    return res.status(413).json({
      error: `File too large. Maximum size is ${maxMb}MB.`,
      code: 'FILE_TOO_LARGE',
    });
  }

  // Multer unexpected field error
  if (err.code === 'LIMIT_UNEXPECTED_FILE') {
    return res.status(400).json({
      error: 'Unexpected file field. Use field name "logFile".',
      code: 'UNEXPECTED_FILE',
    });
  }

  // Multer file filter rejection
  if (err.message && err.message.includes('Invalid file type')) {
    return res.status(400).json({
      error: err.message,
      code: 'INVALID_FILE_TYPE',
    });
  }

  // JSON parse error
  if (err.type === 'entity.parse.failed') {
    return res.status(400).json({
      error: 'Invalid JSON in request body.',
      code: 'INVALID_JSON',
    });
  }

  // Azure OpenAI API errors
  if (err.name === 'APIError' || err.name === 'AuthenticationError' || err.code === 'DeploymentNotFound') {
    return res.status(502).json({
      error: 'Azure OpenAI service error. Log analysis completed without AI insights.',
      code: 'AI_SERVICE_ERROR',
    });
  }

  // Default 500 error
  const statusCode = err.status || err.statusCode || 500;
  res.status(statusCode).json({
    error: process.env.NODE_ENV === 'production'
      ? 'An internal error occurred.'
      : err.message || 'Unknown error',
    code: err.code || 'INTERNAL_ERROR',
  });
}

module.exports = { errorHandler };
