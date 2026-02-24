/**
 * MSAL Log Analyzer - Analysis Routes
 * Handles log file upload and analysis endpoints
 */

const express = require('express');
const multer = require('multer');
const rateLimit = require('express-rate-limit');
const { v4: uuidv4 } = require('uuid');

const { analyzeLogContent } = require('../services/logAnalyzer');
const { generateHtmlReport } = require('../services/reportGenerator');
const { validateLogFile } = require('../middleware/validateRequest');

const router = express.Router();

// ─── Rate Limiting ────────────────────────────────────────────────────────────
const analyzeLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 20,
  message: { error: 'Too many analysis requests. Please try again later.' },
  standardHeaders: true,
  legacyHeaders: false,
});

// ─── Multer Configuration ─────────────────────────────────────────────────────
const maxFileSize = parseInt(process.env.MAX_FILE_SIZE, 10) || 10 * 1024 * 1024; // 10MB default

const storage = multer.memoryStorage();
const upload = multer({
  storage,
  limits: { fileSize: maxFileSize },
  fileFilter: (req, file, cb) => {
    // Accept text files, log files, and files without extension
    const allowedMimes = ['text/plain', 'application/octet-stream', 'text/log'];
    const allowedExtensions = /\.(log|txt|text)$/i;

    if (allowedMimes.includes(file.mimetype) || allowedExtensions.test(file.originalname) || !file.originalname.includes('.')) {
      cb(null, true);
    } else {
      cb(new Error('Invalid file type. Please upload a text or log file.'));
    }
  },
});

// ─── POST /api/analyze ────────────────────────────────────────────────────────
/**
 * Upload and analyze an MSAL log file.
 * Returns parsed modules, metrics, Mermaid diagram, and findings.
 */
router.post('/analyze', analyzeLimiter, upload.single('logFile'), validateLogFile, async (req, res, next) => {
  const requestId = uuidv4();

  try {
    const logContent = req.file.buffer.toString('utf-8');
    const fileName = req.file.originalname;

    console.log(`[${requestId}] Analyzing log file: ${fileName} (${logContent.length} chars)`);

    // Perform analysis
    const report = await analyzeLogContent(logContent, fileName);

    console.log(`[${requestId}] Analysis complete. Found ${report.modules?.length || 0} modules.`);

    res.json({
      success: true,
      requestId,
      fileName,
      report,
    });
  } catch (error) {
    console.error(`[${requestId}] Analysis error:`, error.message);
    next(error);
  }
});

// ─── POST /api/export ─────────────────────────────────────────────────────────
/**
 * Generate an HTML report from analysis results.
 * Returns the HTML content as a downloadable file.
 */
router.post('/export', express.json({ limit: '5mb' }), async (req, res, next) => {
  try {
    const { report, fileName } = req.body;

    if (!report) {
      return res.status(400).json({ error: 'Report data is required for export.' });
    }

    const html = generateHtmlReport(report, fileName || 'msal-analysis');
    const exportFileName = `msal-report-${Date.now()}.html`;

    res.setHeader('Content-Type', 'text/html');
    res.setHeader('Content-Disposition', `attachment; filename="${exportFileName}"`);
    res.send(html);
  } catch (error) {
    next(error);
  }
});

module.exports = router;
