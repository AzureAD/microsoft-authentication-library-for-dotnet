/**
 * MSAL Log Analyzer - Request Validation Middleware
 * Validates incoming requests before processing
 */

/**
 * Validates that a log file was uploaded and has content.
 */
function validateLogFile(req, res, next) {
  if (!req.file) {
    return res.status(400).json({
      error: 'No log file uploaded. Please include a file with field name "logFile".',
      code: 'NO_FILE',
    });
  }

  if (!req.file.buffer || req.file.buffer.length === 0) {
    return res.status(400).json({
      error: 'Uploaded file is empty.',
      code: 'EMPTY_FILE',
    });
  }

  // Validate it's readable text (not binary)
  const sample = req.file.buffer.slice(0, 512);
  const nullBytes = [...sample].filter(b => b === 0).length;
  if (nullBytes > sample.length * 0.1) {
    return res.status(400).json({
      error: 'File appears to be binary. Please upload a text log file.',
      code: 'BINARY_FILE',
    });
  }

  next();
}

module.exports = { validateLogFile };
