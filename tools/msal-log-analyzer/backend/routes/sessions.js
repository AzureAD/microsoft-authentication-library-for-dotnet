'use strict';

const express = require('express');
const { v4: uuidv4 } = require('uuid');
const { getDb } = require('../db/database');

const router = express.Router();

/**
 * GET /api/sessions/preferences
 * Get current session preferences.
 */
router.get('/preferences', (req, res) => {
  try {
    const sessionId = getOrCreateSessionId(req, res);
    const db = getDb();
    const row = db.prepare(`SELECT preferences FROM sessions WHERE id = ?`).get(sessionId);
    const preferences = row ? JSON.parse(row.preferences || '{}') : {};
    res.json({ sessionId, preferences });
  } catch (err) {
    console.error('Get preferences error:', err);
    res.status(500).json({ error: err.message });
  }
});

/**
 * PUT /api/sessions/preferences
 * Update session preferences.
 */
router.put('/preferences', (req, res) => {
  try {
    const sessionId = getOrCreateSessionId(req, res);
    const db = getDb();
    const preferences = JSON.stringify(req.body || {});

    const existing = db.prepare(`SELECT id FROM sessions WHERE id = ?`).get(sessionId);
    if (existing) {
      db.prepare(`UPDATE sessions SET preferences = ?, last_active = datetime('now') WHERE id = ?`)
        .run(preferences, sessionId);
    } else {
      db.prepare(`INSERT INTO sessions (id, preferences) VALUES (?, ?)`)
        .run(sessionId, preferences);
    }

    res.json({ sessionId, preferences: req.body });
  } catch (err) {
    console.error('Update preferences error:', err);
    res.status(500).json({ error: err.message });
  }
});

function getOrCreateSessionId(req, res) {
  let sessionId = req.cookies && req.cookies.sessionId;
  if (!sessionId) {
    sessionId = uuidv4();
    res.cookie('sessionId', sessionId, {
      httpOnly: true,
      maxAge: 30 * 24 * 60 * 60 * 1000,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'lax',
    });
  }
  return sessionId;
}

module.exports = router;
