'use strict';

const Database = require('better-sqlite3');
const path = require('path');
const fs = require('fs');

const DB_DIR = process.env.DB_DIR || path.join(__dirname, '..', 'data');
const DB_PATH = path.join(DB_DIR, 'msal-analyzer.db');

let db;

function getDb() {
  if (!db) {
    fs.mkdirSync(DB_DIR, { recursive: true });
    db = new Database(DB_PATH);
    db.pragma('journal_mode = WAL');
    db.pragma('foreign_keys = ON');
    initSchema();
  }
  return db;
}

function initSchema() {
  db.exec(`
    CREATE TABLE IF NOT EXISTS analyses (
      id TEXT PRIMARY KEY,
      filename TEXT NOT NULL,
      file_size INTEGER,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      duration_ms INTEGER,
      cache_hits INTEGER DEFAULT 0,
      cache_misses INTEGER DEFAULT 0,
      http_calls INTEGER DEFAULT 0,
      error_count INTEGER DEFAULT 0,
      modules_count INTEGER DEFAULT 0,
      is_favorite INTEGER DEFAULT 0,
      tags TEXT DEFAULT '[]',
      summary TEXT,
      raw_log TEXT,
      parsed_data TEXT
    );

    CREATE TABLE IF NOT EXISTS sessions (
      id TEXT PRIMARY KEY,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      last_active TEXT NOT NULL DEFAULT (datetime('now')),
      preferences TEXT DEFAULT '{}'
    );

    CREATE INDEX IF NOT EXISTS idx_analyses_created_at ON analyses(created_at DESC);
    CREATE INDEX IF NOT EXISTS idx_analyses_is_favorite ON analyses(is_favorite);
  `);
}

module.exports = { getDb };
