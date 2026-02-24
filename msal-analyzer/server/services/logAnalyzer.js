/**
 * MSAL Log Analyzer - Core Log Analysis Service
 * Parses MSAL log files and uses Azure OpenAI for intelligent insights.
 *
 * Azure OpenAI is a Microsoft-approved AI service and is safe to use with
 * Microsoft business data such as MSAL logs. Third-party AI services (e.g.
 * Anthropic Claude, OpenAI.com) must NOT be used because MSAL logs contain
 * Microsoft business-sensitive data including tenant IDs, client IDs, account
 * identifiers, and token acquisition metadata.
 */

const { AzureOpenAI } = require('@azure/openai');

// Initialize Azure OpenAI client (lazy - only when credentials are present)
let azureOpenAIClient = null;

function getAzureOpenAIClient() {
  if (!azureOpenAIClient && process.env.AZURE_OPENAI_ENDPOINT && process.env.AZURE_OPENAI_API_KEY) {
    azureOpenAIClient = new AzureOpenAI({
      endpoint: process.env.AZURE_OPENAI_ENDPOINT,
      apiKey: process.env.AZURE_OPENAI_API_KEY,
      apiVersion: process.env.AZURE_OPENAI_API_VERSION || '2024-10-21',
    });
  }
  return azureOpenAIClient;
}

/**
 * Main analysis entry point.
 * Combines regex-based parsing with AI-powered insights.
 *
 * @param {string} logContent - Raw log file content
 * @param {string} fileName - Original file name
 * @returns {Promise<Object>} Structured analysis report
 */
async function analyzeLogContent(logContent, fileName) {
  // Phase 1: Rule-based parsing (always runs)
  const parsed = parseLogStructure(logContent);

  // Phase 2: AI-powered insights (runs if Azure OpenAI credentials are configured)
  let aiInsights = null;
  const client = getAzureOpenAIClient();
  if (client) {
    try {
      aiInsights = await getAiInsights(client, logContent, parsed);
    } catch (aiError) {
      console.warn('AI analysis unavailable, falling back to rule-based analysis:', aiError.message);
    }
  }

  // Phase 3: Build the final report
  return buildReport(parsed, aiInsights, fileName);
}

// ─── Rule-Based Log Parsing ───────────────────────────────────────────────────

/**
 * Parses MSAL log content using regex patterns to extract structured data.
 */
function parseLogStructure(content) {
  const lines = content.split(/\r?\n/);

  return {
    modules: extractModules(lines),
    httpCalls: extractHttpCalls(lines),
    cacheEvents: extractCacheEvents(lines),
    errors: extractErrors(lines),
    tokenInfo: extractTokenInfo(lines),
    timings: extractTimings(lines, content),
    rawLineCount: lines.length,
  };
}

// Log level keywords to skip when extracting module names
const LOG_LEVEL_KEYWORDS = new Set(['INFO', 'DEBUG', 'WARN', 'WARNING', 'ERROR', 'FATAL', 'CRITICAL', 'TRACE', 'VERBOSE']);

/**
 * Extracts module entries from log lines.
 * MSAL logs use the format: [timestamp] [LEVEL] [ModuleName] message
 * We skip log-level bracket entries and capture actual module names.
 */
function extractModules(lines) {
  // Match all bracket-enclosed identifiers on a line; we pick the first non-level one
  const bracketPattern = /\[([A-Z][A-Za-z0-9]+(?:[A-Za-z0-9]*)?)\]/g;
  const moduleMap = new Map();

  lines.forEach((line, index) => {
    let match;
    let name = null;

    // Find all bracketed words; pick first that's not a log level keyword
    while ((match = bracketPattern.exec(line)) !== null) {
      const candidate = match[1];
      if (!LOG_LEVEL_KEYWORDS.has(candidate)) {
        name = candidate;
        break;
      }
    }
    // Reset lastIndex for the global regex
    bracketPattern.lastIndex = 0;

    if (!name) return;
    const timestamp = extractTimestamp(line);
    const level = extractLogLevel(line);

    if (!moduleMap.has(name)) {
      moduleMap.set(name, {
        name,
        firstSeen: timestamp,
        lastSeen: timestamp,
        firstLine: index + 1,
        lastLine: index + 1,
        logCount: 0,
        errorCount: 0,
        warnCount: 0,
        level: 'INFO',
        messages: [],
      });
    }

    const mod = moduleMap.get(name);
    mod.lastSeen = timestamp || mod.lastSeen;
    mod.lastLine = index + 1;
    mod.logCount++;

    if (level === 'ERROR') {
      mod.errorCount++;
      mod.level = 'ERROR';
    } else if (level === 'WARN' && mod.level !== 'ERROR') {
      mod.warnCount++;
      mod.level = 'WARN';
    }

    // Keep first 3 messages per module for context
    if (mod.messages.length < 3) {
      const message = line.replace(/^\[.*?\]\s*/, '').trim();
      if (message) mod.messages.push(message.substring(0, 200));
    }
  });

  return Array.from(moduleMap.values());
}

/**
 * Extracts HTTP call information from log lines.
 */
function extractHttpCalls(lines) {
  const httpPattern = /https?:\/\/[^\s"'<>]+/gi;
  const methodPattern = /\b(GET|POST|PUT|DELETE|PATCH)\b/i;
  const statusPattern = /\b(status|response)[:\s]+(\d{3})\b/i;
  const calls = [];

  lines.forEach((line, index) => {
    const urlMatch = line.match(httpPattern);
    if (!urlMatch) return;

    const methodMatch = line.match(methodPattern);
    const statusMatch = line.match(statusPattern);

    calls.push({
      line: index + 1,
      url: urlMatch[0].substring(0, 150),
      method: methodMatch ? methodMatch[1].toUpperCase() : 'GET',
      status: statusMatch ? parseInt(statusMatch[2]) : null,
      timestamp: extractTimestamp(line),
    });
  });

  return calls;
}

/**
 * Extracts cache hit/miss events.
 */
function extractCacheEvents(lines) {
  const cachePattern = /cache\s*(hit|miss|read|write|refresh|expired|lookup)/i;
  const events = [];

  lines.forEach((line, index) => {
    const match = line.match(cachePattern);
    if (match) {
      events.push({
        line: index + 1,
        type: match[1].toLowerCase(),
        isHit: /hit/i.test(match[0]),
        timestamp: extractTimestamp(line),
        context: line.trim().substring(0, 200),
      });
    }
  });

  return events;
}

/**
 * Extracts error entries from log lines.
 */
function extractErrors(lines) {
  const errorPattern = /\b(error|exception|failed|failure|critical)\b/i;
  const errors = [];

  lines.forEach((line, index) => {
    if (extractLogLevel(line) === 'ERROR' || errorPattern.test(line)) {
      errors.push({
        line: index + 1,
        message: line.trim().substring(0, 300),
        timestamp: extractTimestamp(line),
        level: extractLogLevel(line),
      });
    }
  });

  return errors.slice(0, 50); // Cap at 50 errors
}

/**
 * Extracts token-related information.
 */
function extractTokenInfo(lines) {
  const info = {
    source: 'unknown',
    scopes: [],
    tokenType: null,
    expiresIn: null,
  };

  lines.forEach(line => {
    const lower = line.toLowerCase();

    // Token source detection
    if (/token.*from.*cache|cache.*token.*hit/i.test(line)) info.source = 'cache';
    else if (/refresh.*token|token.*refreshed/i.test(line)) info.source = 'refresh';
    else if (/acquired.*new.*token|new.*token.*acquired|token.*network/i.test(line)) info.source = 'network';

    // Token type
    if (/access.?token/i.test(line)) info.tokenType = 'access_token';
    else if (/id.?token/i.test(line) && !info.tokenType) info.tokenType = 'id_token';

    // Scopes
    const scopeMatch = line.match(/scope[s]?[:\s=]+([^\n,;]+)/i);
    if (scopeMatch && info.scopes.length < 5) {
      const scope = scopeMatch[1].trim().replace(/['"]/g, '');
      if (scope && !info.scopes.includes(scope)) {
        info.scopes.push(scope.substring(0, 100));
      }
    }

    // Expiry
    const expiryMatch = line.match(/expires.?in[:\s=]+(\d+)/i);
    if (expiryMatch) info.expiresIn = parseInt(expiryMatch[1]);
  });

  return info;
}

/**
 * Extracts timing information from the log.
 */
function extractTimings(lines, content) {
  const timings = {
    startTime: null,
    endTime: null,
    totalDurationMs: null,
    durationPattern: null,
  };

  // Try to find explicit duration statements
  const durationMatch = content.match(/total.*?(\d+)\s*ms|duration[:\s]+(\d+)\s*ms|elapsed[:\s]+(\d+)\s*ms/i);
  if (durationMatch) {
    timings.totalDurationMs = parseInt(durationMatch[1] || durationMatch[2] || durationMatch[3]);
    timings.durationPattern = 'explicit';
  }

  // Try to calculate from timestamps
  const timestamps = [];
  lines.forEach(line => {
    const ts = extractTimestamp(line);
    if (ts) timestamps.push(ts);
  });

  if (timestamps.length >= 2) {
    timings.startTime = timestamps[0];
    timings.endTime = timestamps[timestamps.length - 1];

    if (!timings.totalDurationMs) {
      // Try to parse as ISO timestamps or epoch
      try {
        const start = new Date(timestamps[0]).getTime();
        const end = new Date(timestamps[timestamps.length - 1]).getTime();
        if (!isNaN(start) && !isNaN(end) && end > start) {
          timings.totalDurationMs = end - start;
          timings.durationPattern = 'calculated';
        }
      } catch (_) {
        // Ignore timestamp parsing errors
      }
    }
  }

  return timings;
}

// ─── Helper Functions ─────────────────────────────────────────────────────────

function extractTimestamp(line) {
  // ISO 8601: 2024-01-15T10:30:00.123Z
  const isoMatch = line.match(/\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[.,]\d+Z?/);
  if (isoMatch) return isoMatch[0];

  // Date + time: 2024-01-15 10:30:00
  const dateTimeMatch = line.match(/\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}/);
  if (dateTimeMatch) return dateTimeMatch[0];

  // Epoch ms
  const epochMatch = line.match(/\b(\d{13})\b/);
  if (epochMatch) return new Date(parseInt(epochMatch[1])).toISOString();

  return null;
}

function extractLogLevel(line) {
  if (/\b(ERROR|CRITICAL|FATAL)\b/i.test(line)) return 'ERROR';
  if (/\b(WARN|WARNING)\b/i.test(line)) return 'WARN';
  if (/\b(DEBUG|TRACE|VERBOSE)\b/i.test(line)) return 'DEBUG';
  return 'INFO';
}

// ─── AI-Powered Analysis ──────────────────────────────────────────────────────

/**
 * Calls Azure OpenAI to extract additional insights from the log.
 * Truncates the log to fit within token limits.
 *
 * Uses Azure OpenAI (Microsoft-approved) — do NOT replace with Anthropic,
 * OpenAI.com, or any other third-party AI service.
 */
async function getAiInsights(client, logContent, parsed) {
  // Prepare a compact summary for the AI to reduce token usage
  const truncatedLog = logContent.length > 8000
    ? logContent.substring(0, 4000) + '\n...[truncated]...\n' + logContent.substring(logContent.length - 4000)
    : logContent;

  const deploymentName = process.env.AZURE_OPENAI_DEPLOYMENT || 'gpt-4o';

  const systemPrompt = 'You are an expert in Microsoft Authentication Library (MSAL) for .NET. ' +
    'Analyze MSAL log files and return structured JSON insights.';

  const userPrompt = `Analyze this MSAL log and provide structured insights.

Log file content:
\`\`\`
${truncatedLog}
\`\`\`

Pre-parsed data summary:
- Modules found: ${parsed.modules.map(m => m.name).join(', ') || 'none'}
- HTTP calls: ${parsed.httpCalls.length}
- Cache events: ${parsed.cacheEvents.length} (${parsed.cacheEvents.filter(e => e.isHit).length} hits)
- Errors: ${parsed.errors.length}
- Token source: ${parsed.tokenInfo.source}

Provide a JSON response with exactly this structure:
{
  "summary": {
    "overview": "2-3 sentence description of what this authentication flow does",
    "tokenSource": "cache|refresh|network|unknown",
    "totalDurationMs": number_or_null,
    "httpCallCount": number,
    "cacheHitCount": number,
    "errorCount": number,
    "status": "success|partial|failure|unknown"
  },
  "modules": [
    {
      "name": "ModuleName",
      "role": "brief description of this module's role",
      "status": "success|warning|error",
      "durationMs": number_or_null,
      "callsTo": ["ModuleB", "ModuleC"]
    }
  ],
  "findings": [
    {
      "type": "info|warning|error|performance|security",
      "title": "Finding title",
      "description": "Detailed description with context",
      "recommendation": "What to do about it"
    }
  ],
  "mermaidFlow": "sequence diagram string showing module interactions (just the diagram body, no \`\`\`mermaid wrapper)"
}

Focus on:
1. Authentication flow type (interactive, silent, device code, client credentials, etc.)
2. Performance bottlenecks
3. Any errors or warnings
4. Token cache behavior
5. Security observations`;

  const response = await client.chat.completions.create({
    model: deploymentName,
    max_tokens: 2000,
    response_format: { type: 'json_object' },
    messages: [
      { role: 'system', content: systemPrompt },
      { role: 'user', content: userPrompt },
    ],
  });

  const responseText = response.choices[0].message.content;

  // Extract JSON from response (handle any markdown code block wrapping)
  const jsonMatch = responseText.match(/\{[\s\S]*\}/);
  if (!jsonMatch) {
    throw new Error('AI response did not contain valid JSON');
  }

  return JSON.parse(jsonMatch[0]);
}

// ─── Report Builder ───────────────────────────────────────────────────────────

/**
 * Combines parsed data and AI insights into a final structured report.
 */
function buildReport(parsed, aiInsights, fileName) {
  const modules = buildModules(parsed, aiInsights);
  const summary = buildSummary(parsed, aiInsights);
  const findings = buildFindings(parsed, aiInsights);
  const mermaidDiagram = buildMermaidDiagram(modules, aiInsights);

  return {
    fileName,
    analyzedAt: new Date().toISOString(),
    summary,
    modules,
    mermaidDiagram,
    findings,
    rawInsights: aiInsights ? aiInsights.summary?.overview : null,
    stats: {
      rawLineCount: parsed.rawLineCount,
      httpCallCount: parsed.httpCalls.length,
      cacheHitCount: parsed.cacheEvents.filter(e => e.isHit).length,
      cacheMissCount: parsed.cacheEvents.filter(e => !e.isHit).length,
      errorCount: parsed.errors.length,
    },
  };
}

function buildSummary(parsed, aiInsights) {
  // AI insights take precedence over rule-based
  if (aiInsights?.summary) {
    return {
      ...aiInsights.summary,
      // Fill in any missing values from rule-based parsing
      httpCallCount: aiInsights.summary.httpCallCount ?? parsed.httpCalls.length,
      cacheHitCount: aiInsights.summary.cacheHitCount ?? parsed.cacheEvents.filter(e => e.isHit).length,
      errorCount: aiInsights.summary.errorCount ?? parsed.errors.length,
      totalDurationMs: aiInsights.summary.totalDurationMs ?? parsed.timings.totalDurationMs,
      tokenSource: aiInsights.summary.tokenSource !== 'unknown'
        ? aiInsights.summary.tokenSource
        : parsed.tokenInfo.source,
    };
  }

  // Fallback to rule-based summary
  return {
    overview: `MSAL authentication log with ${parsed.modules.length} modules, ${parsed.httpCalls.length} HTTP calls, and ${parsed.errors.length} errors.`,
    tokenSource: parsed.tokenInfo.source,
    totalDurationMs: parsed.timings.totalDurationMs,
    httpCallCount: parsed.httpCalls.length,
    cacheHitCount: parsed.cacheEvents.filter(e => e.isHit).length,
    errorCount: parsed.errors.length,
    status: parsed.errors.length > 0 ? 'partial' : 'unknown',
  };
}

function buildModules(parsed, aiInsights) {
  // Start with rule-based modules
  const moduleMap = new Map(parsed.modules.map(m => [m.name, { ...m }]));

  // Enrich with AI data if available
  if (aiInsights?.modules) {
    aiInsights.modules.forEach(aiMod => {
      if (moduleMap.has(aiMod.name)) {
        const existing = moduleMap.get(aiMod.name);
        moduleMap.set(aiMod.name, {
          ...existing,
          role: aiMod.role,
          aiStatus: aiMod.status,
          durationMs: aiMod.durationMs,
          callsTo: aiMod.callsTo || [],
        });
      } else {
        // AI found a module that regex didn't catch
        moduleMap.set(aiMod.name, {
          name: aiMod.name,
          role: aiMod.role,
          aiStatus: aiMod.status,
          durationMs: aiMod.durationMs,
          callsTo: aiMod.callsTo || [],
          logCount: 0,
          errorCount: 0,
          source: 'ai',
        });
      }
    });
  }

  return Array.from(moduleMap.values());
}

function buildFindings(parsed, aiInsights) {
  const findings = [];

  // Add AI findings if available
  if (aiInsights?.findings) {
    findings.push(...aiInsights.findings);
  }

  // Add rule-based findings
  if (parsed.errors.length > 0) {
    findings.push({
      type: 'error',
      title: `${parsed.errors.length} Error(s) Detected`,
      description: `Found ${parsed.errors.length} error entries in the log. First error: ${parsed.errors[0]?.message?.substring(0, 150) || 'unknown'}`,
      recommendation: 'Review error messages and check for misconfiguration, expired credentials, or network issues.',
    });
  }

  if (parsed.httpCalls.length === 0 && parsed.tokenInfo.source !== 'cache') {
    findings.push({
      type: 'info',
      title: 'No HTTP Calls Detected',
      description: 'No outgoing HTTP requests were found in the log.',
      recommendation: 'This may indicate a silent token acquisition from cache or an incomplete log.',
    });
  }

  const cacheHitRate = parsed.cacheEvents.length > 0
    ? parsed.cacheEvents.filter(e => e.isHit).length / parsed.cacheEvents.length
    : null;

  if (cacheHitRate !== null && cacheHitRate < 0.5) {
    findings.push({
      type: 'performance',
      title: 'Low Cache Hit Rate',
      description: `Cache hit rate is ${Math.round(cacheHitRate * 100)}% (${parsed.cacheEvents.filter(e => e.isHit).length}/${parsed.cacheEvents.length}).`,
      recommendation: 'Consider reviewing token cache configuration to improve performance.',
    });
  }

  return findings.slice(0, 10); // Cap findings at 10
}

/**
 * Generates a Mermaid sequence diagram for module interactions.
 */
function buildMermaidDiagram(modules, aiInsights) {
  // Use AI-generated diagram if available
  if (aiInsights?.mermaidFlow && aiInsights.mermaidFlow.trim()) {
    return `sequenceDiagram\n${aiInsights.mermaidFlow}`;
  }

  // Generate from module data
  if (modules.length === 0) {
    return `sequenceDiagram
    participant App
    participant MSAL
    App->>MSAL: AcquireToken()
    Note over MSAL: Log data insufficient for detailed diagram`;
  }

  const lines = ['sequenceDiagram'];
  const shownModules = modules.slice(0, 8); // Limit to 8 for readability

  // Declare participants
  shownModules.forEach(mod => {
    lines.push(`    participant ${sanitizeMermaidId(mod.name)}`);
  });

  // Show interactions
  shownModules.forEach((mod, i) => {
    if (i < shownModules.length - 1) {
      const next = shownModules[i + 1];
      const arrow = mod.errorCount > 0 ? '-->>' : '->>';
      lines.push(`    ${sanitizeMermaidId(mod.name)}${arrow}${sanitizeMermaidId(next.name)}: ${mod.role || 'calls'}`);
    }
  });

  return lines.join('\n');
}

function sanitizeMermaidId(name) {
  // Remove dots and special chars for Mermaid participant IDs
  return name.replace(/\./g, '_').replace(/[^a-zA-Z0-9_]/g, '');
}

module.exports = { analyzeLogContent };
