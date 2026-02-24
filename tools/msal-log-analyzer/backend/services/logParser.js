'use strict';

/**
 * MSAL Log Parser Service
 * Extracts structured information from MSAL .NET debug log output.
 */

const MODULE_PATTERNS = [
  { name: 'TokenCache', pattern: /token.?cache/i },
  { name: 'AcquireTokenSilent', pattern: /acquiretokensilent|silent.*token/i },
  { name: 'AcquireTokenInteractive', pattern: /acquiretokeninteractive|interactive.*token/i },
  { name: 'AcquireTokenForClient', pattern: /acquiretokenforclient|client.*credentials/i },
  { name: 'AcquireTokenOnBehalfOf', pattern: /obo|onbehalfof|on.*behalf/i },
  { name: 'HttpManager', pattern: /httpmanager|http.*request|http.*response/i },
  { name: 'OAuth2Client', pattern: /oauth2client|oauth.*client/i },
  { name: 'TokenRequestHandler', pattern: /tokenrequest|token.*request/i },
  { name: 'AuthorizationCode', pattern: /authorization.*code|auth.*code/i },
  { name: 'ManagedIdentity', pattern: /managedidentity|managed.*identity|imds/i },
  { name: 'BrokerPlugin', pattern: /broker|wam|web.*account/i },
  { name: 'CertificateCredential', pattern: /certificate|x509|cert/i },
  { name: 'TokenBroker', pattern: /tokenbroker/i },
  { name: 'InstanceDiscovery', pattern: /instancediscovery|instance.*discovery/i },
  { name: 'RegionDiscovery', pattern: /regiondiscovery|region.*discovery/i },
  { name: 'ThrottlingCache', pattern: /throttl/i },
  { name: 'CacheManager', pattern: /cachemanager/i },
  { name: 'ServiceBundle', pattern: /servicebundle/i },
];

const LOG_LEVELS = {
  INFO: /\[Info\]|\bINFO\b/i,
  WARNING: /\[Warning\]|\bWARN(ING)?\b/i,
  ERROR: /\[Error\]|\bERROR\b/i,
  VERBOSE: /\[Verbose\]|\bVERBOSE\b/i,
};

const TIMESTAMP_PATTERN = /(\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?)/;
const DURATION_PATTERN = /(\d+(?:\.\d+)?)\s*ms/gi;
const CORRELATION_ID_PATTERN = /correlation.?id[:\s]+([a-f0-9-]{36})/i;
const HTTP_STATUS_PATTERN = /HTTP\s+(\d{3})/gi;
const CACHE_HIT_PATTERN = /cache.*hit|hit.*cache/gi;
const CACHE_MISS_PATTERN = /cache.*miss|miss.*cache/gi;
const TOKEN_ENDPOINT_PATTERN = /token.*endpoint[:\s]+(https?:\/\/[^\s]+)/gi;
const AUTHORITY_PATTERN = /authority[:\s]+(https?:\/\/[^\s]+)/gi;
const CLIENT_ID_PATTERN = /client.?id[:\s]+([a-f0-9-]{36})/i;
const TENANT_PATTERN = /tenant(?:.?id)?[:\s]+([a-f0-9-]{36}|[a-zA-Z0-9.-]+\.onmicrosoft\.com)/i;
const SCOPE_PATTERN = /scope[s]?[:\s]+([^\n\r]+)/gi;
const EXCEPTION_PATTERN = /(MsalException|MsalClientException|MsalServiceException|MsalUiRequiredException|Exception)[:\s]+([^\n\r]+)/g;

/**
 * Parse a timestamp from a log line.
 * @param {string} line
 * @returns {Date|null}
 */
function parseTimestamp(line) {
  const m = TIMESTAMP_PATTERN.exec(line);
  if (m) {
    try { return new Date(m[1]); } catch { return null; }
  }
  return null;
}

/**
 * Detect the log level of a line.
 * @param {string} line
 * @returns {string}
 */
function detectLogLevel(line) {
  if (LOG_LEVELS.ERROR.test(line)) return 'ERROR';
  if (LOG_LEVELS.WARNING.test(line)) return 'WARNING';
  if (LOG_LEVELS.VERBOSE.test(line)) return 'VERBOSE';
  return 'INFO';
}

/**
 * Detect which MSAL module a log line belongs to.
 * @param {string} line
 * @returns {string[]}
 */
function detectModules(line) {
  return MODULE_PATTERNS
    .filter(m => m.pattern.test(line))
    .map(m => m.name);
}

/**
 * Extract all durations (in ms) from a string.
 * @param {string} text
 * @returns {number[]}
 */
function extractDurations(text) {
  const result = [];
  let m;
  const re = /(\d+(?:\.\d+)?)\s*ms/gi;
  while ((m = re.exec(text)) !== null) {
    result.push(parseFloat(m[1]));
  }
  return result;
}

/**
 * Main parse function.
 * @param {string} logText  Raw log text content
 * @returns {object}        Structured analysis result
 */
function parseMsalLog(logText) {
  const lines = logText.split(/\r?\n/);
  const events = [];
  const modules = new Map();
  const errors = [];
  const warnings = [];
  const httpCalls = [];
  const scopes = new Set();
  let cacheHits = 0;
  let cacheMisses = 0;
  let totalDuration = 0;
  let correlationId = null;
  let clientId = null;
  let tenant = null;
  let authority = null;
  let firstTimestamp = null;
  let lastTimestamp = null;
  const allDurations = [];
  const exceptions = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (!line.trim()) continue;

    const level = detectLogLevel(line);
    const timestamp = parseTimestamp(line);
    if (timestamp) {
      if (!firstTimestamp) firstTimestamp = timestamp;
      lastTimestamp = timestamp;
    }

    const lineModules = detectModules(line);
    lineModules.forEach(mod => {
      if (!modules.has(mod)) modules.set(mod, { name: mod, count: 0, lines: [] });
      const m = modules.get(mod);
      m.count++;
      if (m.lines.length < 20) m.lines.push(i + 1);
    });

    const durations = extractDurations(line);
    allDurations.push(...durations);

    if (CACHE_HIT_PATTERN.test(line)) { cacheHits++; CACHE_HIT_PATTERN.lastIndex = 0; }
    if (CACHE_MISS_PATTERN.test(line)) { cacheMisses++; CACHE_MISS_PATTERN.lastIndex = 0; }

    let httpMatch;
    const httpRe = /HTTP\s+(\d{3})/gi;
    while ((httpMatch = httpRe.exec(line)) !== null) {
      httpCalls.push({ status: parseInt(httpMatch[1], 10), line: i + 1, text: line.trim() });
    }

    if (!correlationId) {
      const cid = CORRELATION_ID_PATTERN.exec(line);
      if (cid) { correlationId = cid[1]; CORRELATION_ID_PATTERN.lastIndex = 0; }
    }
    if (!clientId) {
      const ci = CLIENT_ID_PATTERN.exec(line);
      if (ci) { clientId = ci[1]; CLIENT_ID_PATTERN.lastIndex = 0; }
    }
    if (!tenant) {
      const te = TENANT_PATTERN.exec(line);
      if (te) { tenant = te[1]; TENANT_PATTERN.lastIndex = 0; }
    }
    if (!authority) {
      const au = AUTHORITY_PATTERN.exec(line);
      if (au) { authority = au[1]; AUTHORITY_PATTERN.lastIndex = 0; }
    }

    let scopeMatch;
    const scopeRe = /scope[s]?[:\s]+([^\n\r]+)/gi;
    while ((scopeMatch = scopeRe.exec(line)) !== null) {
      scopeMatch[1].trim().split(/\s+/).forEach(s => { if (s) scopes.add(s); });
    }

    let exMatch;
    const exRe = /(MsalException|MsalClientException|MsalServiceException|MsalUiRequiredException|Exception)[:\s]+([^\n\r]+)/g;
    while ((exMatch = exRe.exec(line)) !== null) {
      exceptions.push({ type: exMatch[1], message: exMatch[2].trim(), line: i + 1 });
    }

    if (level === 'ERROR') errors.push({ line: i + 1, text: line.trim() });
    if (level === 'WARNING') warnings.push({ line: i + 1, text: line.trim() });

    events.push({
      lineNumber: i + 1,
      level,
      modules: lineModules,
      timestamp: timestamp ? timestamp.toISOString() : null,
      text: line.trim(),
    });
  }

  // Calculate total duration
  if (firstTimestamp && lastTimestamp) {
    totalDuration = lastTimestamp - firstTimestamp;
  } else if (allDurations.length > 0) {
    totalDuration = Math.max(...allDurations);
  }

  // Build module interactions for flow diagram
  const moduleList = Array.from(modules.values()).sort((a, b) => b.count - a.count);
  const moduleInteractions = buildModuleInteractions(events);

  // Build timeline events (sample up to 100 events for display)
  const timelineEvents = events.filter(e => e.timestamp || e.modules.length > 0);
  const sampledTimeline = sampleArray(timelineEvents, 100);

  return {
    summary: {
      totalLines: lines.length,
      totalEvents: events.length,
      totalDurationMs: totalDuration,
      cacheHits,
      cacheMisses,
      httpCalls: httpCalls.length,
      errorCount: errors.length,
      warningCount: warnings.length,
      modulesCount: modules.size,
      firstTimestamp: firstTimestamp ? firstTimestamp.toISOString() : null,
      lastTimestamp: lastTimestamp ? lastTimestamp.toISOString() : null,
      correlationId,
      clientId,
      tenant,
      authority,
      scopes: Array.from(scopes),
    },
    modules: moduleList,
    moduleInteractions,
    errors,
    warnings,
    exceptions,
    httpCalls,
    timeline: sampledTimeline,
    performanceData: buildPerformanceData(allDurations, events),
  };
}

/**
 * Build module-to-module interaction pairs for flow diagram.
 */
function buildModuleInteractions(events) {
  const interactions = new Map();
  let prevModules = [];

  for (const event of events) {
    if (event.modules.length === 0) continue;
    for (const prev of prevModules) {
      for (const curr of event.modules) {
        if (prev !== curr) {
          const key = `${prev}-->${curr}`;
          interactions.set(key, (interactions.get(key) || 0) + 1);
        }
      }
    }
    prevModules = event.modules;
  }

  return Array.from(interactions.entries())
    .map(([key, count]) => {
      const [from, to] = key.split('-->');
      return { from, to, count };
    })
    .sort((a, b) => b.count - a.count)
    .slice(0, 30);
}

/**
 * Build performance data buckets for charting.
 */
function buildPerformanceData(allDurations, events) {
  const buckets = { '<10ms': 0, '10-50ms': 0, '50-200ms': 0, '200-500ms': 0, '>500ms': 0 };
  for (const d of allDurations) {
    if (d < 10) buckets['<10ms']++;
    else if (d < 50) buckets['10-50ms']++;
    else if (d < 200) buckets['50-200ms']++;
    else if (d < 500) buckets['200-500ms']++;
    else buckets['>500ms']++;
  }

  const levelCounts = { INFO: 0, WARNING: 0, ERROR: 0, VERBOSE: 0 };
  for (const e of events) levelCounts[e.level] = (levelCounts[e.level] || 0) + 1;

  return { durationBuckets: buckets, levelCounts };
}

/**
 * Sample an array to at most maxSize elements evenly.
 */
function sampleArray(arr, maxSize) {
  if (arr.length <= maxSize) return arr;
  const step = Math.ceil(arr.length / maxSize);
  return arr.filter((_, i) => i % step === 0);
}

module.exports = { parseMsalLog };
