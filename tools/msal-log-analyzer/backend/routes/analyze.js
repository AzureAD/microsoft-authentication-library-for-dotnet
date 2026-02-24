'use strict';

const express = require('express');
const multer = require('multer');
const { v4: uuidv4 } = require('uuid');
const { getDb } = require('../db/database');
const { parseMsalLog } = require('../services/logParser');
const { generateFlowDiagram, generateSequenceDiagram } = require('../services/diagramGenerator');

const router = express.Router();

// Configure multer for memory storage (no file system writes needed for analysis)
const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: 50 * 1024 * 1024 }, // 50MB max
  fileFilter: (_req, file, cb) => {
    // Accept .log, .txt, and plain text
    const allowed = ['.log', '.txt', '.text'];
    const ext = '.' + file.originalname.split('.').pop().toLowerCase();
    if (allowed.includes(ext) || file.mimetype.startsWith('text/')) {
      cb(null, true);
    } else {
      cb(new Error('Only .log and .txt files are accepted'), false);
    }
  },
});

/**
 * POST /api/analyze
 * Upload and analyze a single MSAL log file.
 */
router.post('/', upload.single('logFile'), async (req, res) => {
  try {
    if (!req.file) {
      return res.status(400).json({ error: 'No file uploaded. Use field name "logFile".' });
    }

    const logText = req.file.buffer.toString('utf8');
    const filename = req.file.originalname;
    const fileSize = req.file.size;
    const id = uuidv4();

    // Parse the log
    const parsedData = parseMsalLog(logText);

    // Generate diagrams
    const flowDiagram = generateFlowDiagram(parsedData);
    const sequenceDiagram = generateSequenceDiagram(parsedData);
    parsedData.diagrams = { flow: flowDiagram, sequence: sequenceDiagram };

    // Store in DB
    const db = getDb();
    const stmt = db.prepare(`
      INSERT INTO analyses (id, filename, file_size, duration_ms, cache_hits, cache_misses,
        http_calls, error_count, modules_count, summary, raw_log, parsed_data)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    `);
    stmt.run(
      id,
      filename,
      fileSize,
      parsedData.summary.totalDurationMs,
      parsedData.summary.cacheHits,
      parsedData.summary.cacheMisses,
      parsedData.summary.httpCalls,
      parsedData.summary.errorCount,
      parsedData.summary.modulesCount,
      JSON.stringify(parsedData.summary),
      logText.length > 500000 ? logText.slice(0, 500000) : logText, // limit stored raw log
      JSON.stringify(parsedData),
    );

    res.status(201).json({
      id,
      filename,
      fileSize,
      analysis: parsedData,
    });
  } catch (err) {
    console.error('Analysis error:', err);
    res.status(500).json({ error: err.message || 'Failed to analyze log file' });
  }
});

/**
 * POST /api/analyze/batch
 * Upload and analyze multiple MSAL log files.
 */
router.post('/batch', upload.array('logFiles', 10), async (req, res) => {
  try {
    if (!req.files || req.files.length === 0) {
      return res.status(400).json({ error: 'No files uploaded. Use field name "logFiles".' });
    }

    const results = [];
    const db = getDb();

    for (const file of req.files) {
      try {
        const logText = file.buffer.toString('utf8');
        const id = uuidv4();
        const parsedData = parseMsalLog(logText);
        const flowDiagram = generateFlowDiagram(parsedData);
        const sequenceDiagram = generateSequenceDiagram(parsedData);
        parsedData.diagrams = { flow: flowDiagram, sequence: sequenceDiagram };

        const stmt = db.prepare(`
          INSERT INTO analyses (id, filename, file_size, duration_ms, cache_hits, cache_misses,
            http_calls, error_count, modules_count, summary, raw_log, parsed_data)
          VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        `);
        stmt.run(
          id, file.originalname, file.size,
          parsedData.summary.totalDurationMs,
          parsedData.summary.cacheHits, parsedData.summary.cacheMisses,
          parsedData.summary.httpCalls, parsedData.summary.errorCount,
          parsedData.summary.modulesCount,
          JSON.stringify(parsedData.summary),
          logText.length > 500000 ? logText.slice(0, 500000) : logText,
          JSON.stringify(parsedData),
        );

        results.push({ id, filename: file.originalname, status: 'success', summary: parsedData.summary });
      } catch (fileErr) {
        results.push({ filename: file.originalname, status: 'error', error: fileErr.message });
      }
    }

    res.status(201).json({ results });
  } catch (err) {
    console.error('Batch analysis error:', err);
    res.status(500).json({ error: err.message || 'Batch analysis failed' });
  }
});

module.exports = router;
