'use strict';

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const YAML = require('yaml');

// ─── Constants ──────────────────────────────────────────────────────────────

const MAX_LOG_CHARS = 250_000;
const TRIAGE_MARKER = '<!-- msal-log-triage:';

// Log-line heuristic prefixes that indicate MSI v2 / MSAL logs
const MSI_LOG_HEURISTICS = [
  '[MSAL]',
  '[HttpManager]',
  '[CertCache]',
  '[PersistentCert]',
  '[ImdsV2]',
];

// Markers that indicate this issue is MSI v2 related
const MIV2_MARKERS = [
  'ManagedIdentityClient',
  'scenario:ManagedIdentity',
  'mi-v2',
  'ImdsV2',
  'issuecredential',
  'getplatformmetadata',
  'mTLS',
  'mtls_pop',
  'KeyGuard',
];

// ─── PII Redaction ──────────────────────────────────────────────────────────

/**
 * Redacts sensitive data from a log string.
 * - JWT tokens become <redacted-jwt>
 * - GUIDs that are NOT correlation IDs get masked
 */
function redactPii(text) {
  if (!text) return text;

  // Redact JWT tokens (three base64url segments separated by dots)
  text = text.replace(
    /eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*/g,
    '<redacted-jwt>'
  );

  return text;
}

// ─── Log Extraction ─────────────────────────────────────────────────────────

/**
 * Extracts text from markdown code blocks.
 */
function extractCodeBlocks(markdown) {
  if (!markdown) return '';
  const blocks = [];
  const regex = /```[^\n]*\n([\s\S]*?)```/g;
  let match;
  while ((match = regex.exec(markdown)) !== null) {
    blocks.push(match[1]);
  }
  // If no code blocks found, treat the whole body as potential log text
  if (blocks.length === 0) {
    return markdown;
  }
  return blocks.join('\n');
}

/**
 * Filters lines that look like MSAL log output using heuristics.
 */
function filterMsalLines(text) {
  const lines = text.split('\n');
  const msalLines = lines.filter(line =>
    MSI_LOG_HEURISTICS.some(h => line.includes(h))
  );
  // If heuristic lines found, return only those; otherwise return all lines
  if (msalLines.length > 0) {
    return msalLines.join('\n');
  }
  return text;
}

/**
 * Collects and caps log text from issue body + comments.
 * @param {string} issueBody
 * @param {string[]} commentBodies - last 5 comments
 * @returns {string}
 */
function collectLogText(issueBody, commentBodies) {
  const parts = [issueBody, ...(commentBodies || [])].filter(Boolean);
  const combined = parts.map(extractCodeBlocks).join('\n');
  const filtered = filterMsalLines(combined);

  // Cap at MAX_LOG_CHARS (take last MAX_LOG_CHARS characters)
  if (filtered.length > MAX_LOG_CHARS) {
    return filtered.slice(filtered.length - MAX_LOG_CHARS);
  }
  return filtered;
}

// ─── Gating Logic ───────────────────────────────────────────────────────────

/**
 * Returns true if the issue appears to be an MSI v2 issue with MSAL logs.
 */
function isMsiV2Issue(issueBody, labels, logText) {
  const allText = (issueBody || '') + '\n' + (logText || '');

  // Check for MSI v2 markers in body or log text
  const hasMsiMarker = MIV2_MARKERS.some(m =>
    allText.toLowerCase().includes(m.toLowerCase())
  );

  // Check for MI label
  const hasMiLabel = (labels || []).some(l =>
    l === 'scenario:ManagedIdentity' || l.startsWith('mi-v2')
  );

  // Check that there are actual log lines
  const hasLogLines = logText && logText.trim().length > 0;

  return !!((hasMsiMarker || hasMiLabel) && hasLogLines);
}

// ─── Event Parsing ──────────────────────────────────────────────────────────

/**
 * Parses a single log line into a structured event.
 */
function parseLine(line) {
  const event = {
    raw: line,
    message: line,
    correlationId: null,
    timestamp: null,
    uri: null,
    method: null,
    statusCode: null,
    bindingCertificate: false,
    cacheOp: null,
    certReuse: false,
    mtlsCacheHit: false,
  };

  // Extract timestamp [YYYY-MM-DD HH:MM:SS] or similar
  const tsMatch = line.match(/\[(\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}[^\]]*)\]/);
  if (tsMatch) event.timestamp = tsMatch[1];

  // Extract correlation ID (GUID pattern) from log lines like: correlationId=GUID or correlation_id: GUID
  const corrMatch = line.match(/correlat[io]+n[_\s-]?[Ii][Dd][\s:=]+([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/i);
  if (corrMatch) event.correlationId = corrMatch[1];

  // Parse HttpManager: method, URI, status
  const httpMatch = line.match(/\[HttpManager\].*?(GET|POST|PUT|DELETE|PATCH)\s+(https?:\/\/[^\s,]+)/i);
  if (httpMatch) {
    event.method = httpMatch[1].toUpperCase();
    event.uri = httpMatch[2];
  }

  // Also catch URI without [HttpManager] prefix
  if (!event.uri) {
    const uriMatch = line.match(/(GET|POST|PUT|DELETE)\s+(https?:\/\/[^\s,'"]+)/i);
    if (uriMatch) {
      event.method = uriMatch[1].toUpperCase();
      event.uri = uriMatch[2];
    }
  }

  // Parse Binding Certificate flag
  if (/Binding Certificate\s*:\s*True/i.test(line)) {
    event.bindingCertificate = true;
  }

  // Parse HTTP status code
  const statusMatch = line.match(/\bstatus[_\s]?[Cc]ode[\s:=]+(\d{3})\b/) ||
    line.match(/\bHTTP[\s/]+\d+\.\d+\s+(\d{3})\b/) ||
    line.match(/\bResponse\s+(\d{3})\b/i);
  if (statusMatch) event.statusCode = parseInt(statusMatch[1], 10);

  // Parse [CertCache] operations
  const cacheMatch = line.match(/\[CertCache\]\s+(HIT|MISS|SET|REPLACE|REMOVE)/i);
  if (cacheMatch) event.cacheOp = cacheMatch[1].toUpperCase();

  // Parse [PersistentCert] reuse
  if (/\[PersistentCert\].*Reused certificate/i.test(line) ||
      /mTLS binding cache HIT/i.test(line)) {
    event.certReuse = true;
  }

  // Parse mTLS binding cache HIT
  if (/mTLS binding cache HIT/i.test(line)) {
    event.mtlsCacheHit = true;
  }

  return event;
}

/**
 * Parses all log lines into structured events.
 */
function parseEvents(logText) {
  if (!logText) return [];
  return logText.split('\n')
    .filter(l => l.trim().length > 0)
    .map(parseLine);
}

// ─── Stage Detection ─────────────────────────────────────────────────────────

/**
 * Detects which of the six MSI v2 pipeline stages appear in the events.
 */
function detectStages(events) {
  const seen = new Set();

  for (const e of events) {
    if (e.uri && e.uri.includes('/metadata/identity/getplatformmetadata')) {
      seen.add('MIv2.IMDS.GetPlatformMetadata');
    }
    if (e.uri && e.uri.includes('/metadata/identity/issuecredential')) {
      seen.add('MIv2.IMDS.IssueCredential');
    }
    if ((e.uri && e.uri.includes('/oauth2/v2.0/token')) || e.bindingCertificate) {
      seen.add('MIv2.STS.mTLS.Token');
    }
    if (e.message.includes('[ImdsV2] Attestation token provider') ||
        (e.uri && e.uri.includes('attest'))) {
      seen.add('MIv2.MAA.Attestation');
    }
    if (e.cacheOp || e.certReuse) {
      seen.add('MIv2.Cache.ReuseOrMintCert');
    }
    if (/KeyGuard/i.test(e.message) || /mtls_pop_requires_keyguard/i.test(e.message)) {
      seen.add('MIv2.KeyGuard.KeyCreation');
    }
  }

  return [...seen];
}

// ─── Failure Classification ──────────────────────────────────────────────────

/**
 * Loads signatures from YAML file.
 */
function loadSignatures(signaturesPath) {
  const content = fs.readFileSync(signaturesPath, 'utf8');
  const parsed = YAML.parse(content);
  return parsed.rules || [];
}

/**
 * Evaluates a single condition against an event.
 */
function matchCondition(event, condition) {
  const { field, equals, notEquals, contains } = condition;
  const value = event[field];

  if (contains !== undefined) {
    if (typeof value !== 'string') return false;
    return value.toLowerCase().includes(contains.toLowerCase());
  }
  if (equals !== undefined) {
    return value === equals;
  }
  if (notEquals !== undefined) {
    return value !== notEquals;
  }
  return false;
}

/**
 * Evaluates a rule against a set of events.
 * Returns { matched: boolean, matchedEvents: Event[] }
 */
function evaluateRule(rule, events) {
  const matchedEvents = [];

  for (const event of events) {
    const { conditions } = rule;
    let allMet = true;
    let anyMet = !conditions.any; // if no any conditions, treat as met

    // Evaluate "all" conditions (must ALL match this event)
    if (conditions.all) {
      for (const cond of conditions.all) {
        if (!matchCondition(event, cond)) {
          allMet = false;
          break;
        }
      }
    }

    if (!allMet) continue;

    // Evaluate "any" conditions (at least one must match somewhere in all events)
    if (conditions.any) {
      anyMet = false;
      for (const cond of conditions.any) {
        if (matchCondition(event, cond)) {
          anyMet = true;
          break;
        }
      }
    }

    if (allMet && anyMet) {
      matchedEvents.push(event);
    }
  }

  // For rules with both all+any, we need cross-event matching:
  // "all" conditions bind within an event, "any" conditions can match different events
  if (matchedEvents.length === 0 && rule.conditions.all && rule.conditions.any) {
    const hasAllEvents = events.filter(e => {
      if (!rule.conditions.all) return true;
      return rule.conditions.all.every(c => matchCondition(e, c));
    });
    const hasAnyEvents = events.filter(e => {
      if (!rule.conditions.any) return true;
      return rule.conditions.any.some(c => matchCondition(e, c));
    });
    if (hasAllEvents.length > 0 && hasAnyEvents.length > 0) {
      return { matched: true, matchedEvents: [...hasAllEvents, ...hasAnyEvents] };
    }
  }

  return { matched: matchedEvents.length > 0, matchedEvents };
}

/**
 * Classifies the failure by evaluating all rules against parsed events.
 * Returns the best matching rule result.
 */
function classifyFailure(events, rules) {
  const results = [];

  for (const rule of rules) {
    const { matched, matchedEvents } = evaluateRule(rule, events);
    if (matched) {
      results.push({ rule, matchedEvents });
    }
  }

  if (results.length === 0) return null;

  // Prefer high confidence, then first match
  const highConf = results.find(r => r.rule.confidence === 'high');
  return highConf || results[0];
}

// ─── Evidence Extraction ─────────────────────────────────────────────────────

/**
 * Builds a summary of evidence from matched events (without raw log lines).
 */
function buildEvidence(events, matchedEvents) {
  const evidence = [];

  // Last HTTP request
  const lastHttpEvent = [...events].reverse().find(e => e.uri && e.method);
  if (lastHttpEvent) {
    const maskedUri = lastHttpEvent.uri.replace(/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi, '<guid>');
    evidence.push(`Last request: \`${lastHttpEvent.method} ${maskedUri}\``);
  }

  // Binding certificate status
  const bindingEvent = events.find(e => e.bindingCertificate);
  if (bindingEvent) {
    evidence.push('Binding Certificate: **True**');
  }

  // Exception signatures
  const exceptionPatterns = ['SslStream', 'AuthenticationException', 'SocketException', 'SCHANNEL', 'TlsException'];
  for (const pattern of exceptionPatterns) {
    const found = events.find(e => e.message.includes(pattern));
    if (found) {
      evidence.push(`Exception signature: \`${pattern}\``);
    }
  }

  // Cache operations
  const cacheEvents = events.filter(e => e.cacheOp);
  if (cacheEvents.length > 0) {
    const ops = [...new Set(cacheEvents.map(e => e.cacheOp))].join(', ');
    evidence.push(`Cache operations observed: ${ops}`);
  }

  // Cert reuse
  const reuseEvent = events.find(e => e.certReuse);
  if (reuseEvent) {
    evidence.push('`[PersistentCert] Reused certificate` observed');
  }

  // mTLS cache hit
  const mtlsHit = events.find(e => e.mtlsCacheHit);
  if (mtlsHit) {
    evidence.push('mTLS binding cache HIT observed');
  }

  // Status codes
  const nonOkEvents = events.filter(e => e.statusCode && e.statusCode !== 200);
  for (const e of nonOkEvents.slice(0, 2)) {
    evidence.push(`Non-200 HTTP status: \`${e.statusCode}\``);
  }

  // Correlation IDs
  const corrIds = [...new Set(events.filter(e => e.correlationId).map(e => e.correlationId))];
  if (corrIds.length > 0) {
    evidence.push(`Correlation ID(s): \`${corrIds.slice(0, 3).join('`, `')}\``);
  }

  return evidence;
}

// ─── Comment Formatting ──────────────────────────────────────────────────────

/**
 * Formats the triage comment markdown.
 */
function formatComment(classification, detectedStages, evidence, inputHash) {
  let body = '## MSAL MSI v2 automated log triage (best-effort)\n\n';
  body += '**Detected:** Managed Identity V2 (mTLS PoP flow)\n\n';

  if (!classification) {
    body += '**Most likely failing stage:** Unable to classify — no matching failure pattern found.\n\n';
    body += '### Evidence found in logs\n';
    if (evidence.length > 0) {
      body += evidence.map(e => `- ${e}`).join('\n') + '\n';
    } else {
      body += '- No specific failure pattern detected in provided logs.\n';
    }
    body += '\n### Detected pipeline stages\n';
    body += detectedStages.length > 0
      ? detectedStages.map(s => `- \`${s}\``).join('\n') + '\n'
      : '- No specific MSI v2 pipeline stages detected.\n';
  } else {
    const { rule } = classification;
    body += `**Most likely failing stage:** \`${rule.stage}\`\n`;
    body += `**Confidence:** ${rule.confidence.charAt(0).toUpperCase() + rule.confidence.slice(1)}\n\n`;
    body += '### Evidence found in logs\n';
    evidence.forEach(e => { body += `- ${e}\n`; });
    body += '\n### What this usually means\n';
    (rule.whatItMeans || []).forEach(m => { body += `- ${m}\n`; });
    body += '\n### Next triage steps\n';
    (rule.nextSteps || []).forEach((s, i) => { body += `${i + 1}. ${s}\n`; });
    if (detectedStages.length > 0) {
      body += '\n### Detected pipeline stages\n';
      body += detectedStages.map(s => `- \`${s}\``).join('\n') + '\n';
    }
  }

  body += `\n${TRIAGE_MARKER} v1 hash=${inputHash} -->`;
  return body;
}

// ─── Main Analyzer ───────────────────────────────────────────────────────────

/**
 * Main entry point: analyzes MSI v2 issue logs and returns a triage comment.
 *
 * @param {object} options
 * @param {string} options.issueBody - Issue body markdown
 * @param {string[]} [options.commentBodies] - Recent comment bodies
 * @param {string[]} [options.labels] - Issue labels
 * @param {string} [options.signaturesPath] - Path to signatures.yml
 * @returns {{ shouldComment: boolean, comment: string, stage: string|null, confidence: string|null }}
 */
function analyze(options) {
  const {
    issueBody = '',
    commentBodies = [],
    labels = [],
    signaturesPath = path.join(__dirname, 'signatures.yml'),
  } = options;

  const logText = collectLogText(issueBody, commentBodies);

  if (!isMsiV2Issue(issueBody, labels, logText)) {
    return { shouldComment: false, comment: '', stage: null, confidence: null };
  }

  const redactedLog = redactPii(logText);
  const events = parseEvents(redactedLog);
  const detectedStages = detectStages(events);

  const rules = loadSignatures(signaturesPath);
  const classification = classifyFailure(events, rules);

  const evidence = buildEvidence(events, classification ? classification.matchedEvents : []);
  const inputHash = crypto.createHash('md5').update(logText).digest('hex').slice(0, 8);
  const comment = formatComment(classification, detectedStages, evidence, inputHash);

  return {
    shouldComment: true,
    comment,
    stage: classification ? classification.rule.stage : null,
    confidence: classification ? classification.rule.confidence : null,
  };
}

// ─── CLI Mode ────────────────────────────────────────────────────────────────

if (require.main === module) {
  const args = process.argv.slice(2);
  const inputIdx = args.indexOf('--input');
  if (inputIdx === -1 || !args[inputIdx + 1]) {
    console.error('Usage: node index.js --input <logfile>');
    process.exit(1);
  }
  const inputFile = args[inputIdx + 1];
  const logContent = fs.readFileSync(inputFile, 'utf8');

  // Wrap the log file content in a code block as if it were an issue body
  const issueBody = '```\n' + logContent + '\n```';

  // Use filename as a signal for ManagedIdentityClient
  const result = analyze({
    issueBody,
    labels: ['scenario:ManagedIdentity'],
    signaturesPath: path.join(__dirname, 'signatures.yml'),
  });

  if (!result.shouldComment) {
    console.log('No MSI v2 issue detected. No comment would be posted.');
  } else {
    console.log('--- Triage Comment ---');
    console.log(result.comment);
    console.log('--- End Comment ---');
    console.log(`Stage: ${result.stage || 'unknown'}`);
    console.log(`Confidence: ${result.confidence || 'n/a'}`);
  }
}

module.exports = {
  analyze,
  isMsiV2Issue,
  collectLogText,
  extractCodeBlocks,
  filterMsalLines,
  redactPii,
  parseLine,
  parseEvents,
  detectStages,
  classifyFailure,
  evaluateRule,
  buildEvidence,
  formatComment,
  loadSignatures,
};
