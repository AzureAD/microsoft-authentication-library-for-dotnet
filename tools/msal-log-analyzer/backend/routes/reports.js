'use strict';

const express = require('express');
const { getDb } = require('../db/database');
const { generateHtmlReport } = require('../services/reportGenerator');

const router = express.Router();

/**
 * GET /api/reports
 * List all past analyses with optional search and filter.
 */
router.get('/', (req, res) => {
  try {
    const db = getDb();
    const { search, favorite, limit = 50, offset = 0 } = req.query;

    let sql = `SELECT id, filename, file_size, created_at, duration_ms, cache_hits, cache_misses,
               http_calls, error_count, modules_count, is_favorite, tags, summary
               FROM analyses`;
    const params = [];
    const conditions = [];

    if (search) {
      conditions.push(`(filename LIKE ? OR summary LIKE ?)`);
      params.push(`%${search}%`, `%${search}%`);
    }
    if (favorite === 'true') {
      conditions.push(`is_favorite = 1`);
    }

    if (conditions.length) sql += ' WHERE ' + conditions.join(' AND ');
    sql += ` ORDER BY created_at DESC LIMIT ? OFFSET ?`;
    params.push(parseInt(limit, 10) || 50, parseInt(offset, 10) || 0);

    const rows = db.prepare(sql).all(...params);
    const total = db.prepare(`SELECT COUNT(*) as count FROM analyses`).get().count;

    res.json({ reports: rows.map(normalizeReport), total, limit: parseInt(limit, 10), offset: parseInt(offset, 10) });
  } catch (err) {
    console.error('List reports error:', err);
    res.status(500).json({ error: err.message });
  }
});

/**
 * GET /api/reports/:id
 * Get a specific analysis by ID.
 */
router.get('/:id', (req, res) => {
  try {
    const db = getDb();
    const row = db.prepare(`SELECT * FROM analyses WHERE id = ?`).get(req.params.id);
    if (!row) return res.status(404).json({ error: 'Analysis not found' });

    const result = normalizeReport(row);
    if (row.parsed_data) {
      try { result.analysis = JSON.parse(row.parsed_data); } catch {}
    }
    res.json(result);
  } catch (err) {
    console.error('Get report error:', err);
    res.status(500).json({ error: err.message });
  }
});

/**
 * GET /api/reports/:id/html
 * Export analysis as HTML report.
 */
router.get('/:id/html', (req, res) => {
  try {
    const db = getDb();
    const row = db.prepare(`SELECT * FROM analyses WHERE id = ?`).get(req.params.id);
    if (!row) return res.status(404).json({ error: 'Analysis not found' });

    const html = generateHtmlReport(row);
    const safeName = row.filename.replace(/[^a-zA-Z0-9._-]/g, '_');
    res.setHeader('Content-Type', 'text/html; charset=utf-8');
    res.setHeader('Content-Disposition', `attachment; filename="msal-report-${safeName}.html"`);
    res.send(html);
  } catch (err) {
    console.error('HTML report error:', err);
    res.status(500).json({ error: err.message });
  }
});

/**
 * PATCH /api/reports/:id/favorite
 * Toggle favorite status.
 */
router.patch('/:id/favorite', (req, res) => {
  try {
    const db = getDb();
    const row = db.prepare(`SELECT is_favorite FROM analyses WHERE id = ?`).get(req.params.id);
    if (!row) return res.status(404).json({ error: 'Analysis not found' });

    const newVal = row.is_favorite ? 0 : 1;
    db.prepare(`UPDATE analyses SET is_favorite = ? WHERE id = ?`).run(newVal, req.params.id);
    res.json({ id: req.params.id, is_favorite: newVal === 1 });
  } catch (err) {
    console.error('Toggle favorite error:', err);
    res.status(500).json({ error: err.message });
  }
});

/**
 * DELETE /api/reports/:id
 * Delete an analysis.
 */
router.delete('/:id', (req, res) => {
  try {
    const db = getDb();
    const info = db.prepare(`DELETE FROM analyses WHERE id = ?`).run(req.params.id);
    if (info.changes === 0) return res.status(404).json({ error: 'Analysis not found' });
    res.json({ deleted: true });
  } catch (err) {
    console.error('Delete report error:', err);
    res.status(500).json({ error: err.message });
  }
});

function normalizeReport(row) {
  return {
    ...row,
    is_favorite: row.is_favorite === 1,
    tags: tryParseJSON(row.tags, []),
    summary: tryParseJSON(row.summary, {}),
  };
}

function tryParseJSON(str, fallback) {
  if (!str) return fallback;
  try { return JSON.parse(str); } catch { return fallback; }
}

module.exports = router;
