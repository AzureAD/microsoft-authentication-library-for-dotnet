'use strict';

/**
 * Mermaid Diagram Generator Service
 * Generates Mermaid flowchart definitions from parsed MSAL log data.
 */

/**
 * Generate a Mermaid flowchart from module interaction data.
 * @param {object} parsedData  Parsed log data from logParser
 * @returns {string}           Mermaid diagram definition string
 */
function generateFlowDiagram(parsedData) {
  const { modules, moduleInteractions, summary } = parsedData;

  if (!modules || modules.length === 0) {
    return `flowchart TD
    A[No module data detected in log]`;
  }

  const lines = ['flowchart TD'];

  // Define node styles based on module type
  const nodeStyles = new Map();
  const topModules = modules.slice(0, 15);

  topModules.forEach((mod) => {
    const nodeId = sanitizeId(mod.name);
    const label = `${mod.name} (${mod.count})`;
    // Use simple bracket nodes only - avoid special shapes that may cause parsing errors
    lines.push(`    ${nodeId}["${label}"]`);
    nodeStyles.set(nodeId, isCacheModule(mod.name) ? 'cache' : isNetworkModule(mod.name) ? 'network' : 'default');
  });

  // Add interactions (edges)
  const topModuleNames = new Set(topModules.map(m => m.name));
  const interactions = (moduleInteractions || [])
    .filter(i => topModuleNames.has(i.from) && topModuleNames.has(i.to))
    .slice(0, 20);

  interactions.forEach(interaction => {
    const from = sanitizeId(interaction.from);
    const to = sanitizeId(interaction.to);
    if (interaction.count > 1) {
      lines.push(`    ${from} -->|${interaction.count}x| ${to}`);
    } else {
      lines.push(`    ${from} --> ${to}`);
    }
  });

  // Add style classes
  lines.push('');
  lines.push('    classDef cache fill:#e8f5e9,stroke:#388e3c,color:#1b5e20');
  lines.push('    classDef network fill:#e3f2fd,stroke:#1565c0,color:#0d47a1');
  lines.push('    classDef error fill:#ffebee,stroke:#c62828,color:#b71c1c');
  lines.push('    classDef default fill:#f3e5f5,stroke:#6a1b9a,color:#4a148c');

  // Apply styles
  nodeStyles.forEach((style, nodeId) => {
    if (style !== 'default') {
      lines.push(`    class ${nodeId} ${style}`);
    }
  });

  return lines.join('\n');
}

/**
 * Generate a sequence diagram showing the main MSAL authentication flow.
 * @param {object} parsedData
 * @returns {string}
 */
function generateSequenceDiagram(parsedData) {
  const { events, summary } = parsedData;
  const timeline = parsedData.timeline || [];

  const lines = ['sequenceDiagram'];
  lines.push('    participant App as Application');
  lines.push('    participant MSAL as MSAL.NET');
  lines.push('    participant Cache as Token Cache');
  lines.push('    participant AAD as Azure AD');

  // Detect key flow events from timeline
  let hasAcquireToken = false;
  let hasCacheHit = false;
  let hasCacheMiss = false;
  let hasHttpCall = false;
  let hasTokenResponse = false;
  let hasError = false;

  for (const event of timeline) {
    const text = event.text.toLowerCase();
    if (!hasAcquireToken && /acquiretoken|acquire.*token/i.test(event.text)) {
      lines.push('    App->>MSAL: AcquireToken()');
      hasAcquireToken = true;
    }
    if (!hasCacheHit && /cache.*hit/i.test(event.text)) {
      lines.push('    MSAL->>Cache: Lookup token');
      lines.push('    Cache-->>MSAL: Token found (cache hit)');
      hasCacheHit = true;
    }
    if (!hasCacheMiss && /cache.*miss/i.test(event.text)) {
      lines.push('    MSAL->>Cache: Lookup token');
      lines.push('    Cache-->>MSAL: Token not found (cache miss)');
      hasCacheMiss = true;
    }
    if (!hasHttpCall && /http.*request|sending.*request/i.test(event.text)) {
      lines.push('    MSAL->>AAD: HTTP POST /token');
      hasHttpCall = true;
    }
    if (!hasTokenResponse && /token.*response|response.*token|200/i.test(event.text)) {
      lines.push('    AAD-->>MSAL: Token response (200 OK)');
      hasTokenResponse = true;
    }
    if (!hasError && event.level === 'ERROR') {
      lines.push(`    Note over MSAL: Error: ${truncate(event.text, 50)}`);
      hasError = true;
    }
  }

  // Default flow if nothing detected
  if (!hasAcquireToken) {
    lines.push('    App->>MSAL: AcquireToken()');
    lines.push('    MSAL->>Cache: Lookup token');
    if (summary && summary.cacheHits > 0) {
      lines.push('    Cache-->>MSAL: Token found');
    } else {
      lines.push('    Cache-->>MSAL: Token not found');
      lines.push('    MSAL->>AAD: Token request');
      lines.push('    AAD-->>MSAL: Token response');
    }
  }

  lines.push('    MSAL-->>App: AuthenticationResult');

  return lines.join('\n');
}

/**
 * Sanitize a string to a valid Mermaid node ID.
 */
function sanitizeId(name) {
  return name.replace(/[^a-zA-Z0-9]/g, '_');
}

function isCacheModule(name) {
  return /cache/i.test(name);
}

function isNetworkModule(name) {
  return /http|oauth|network|broker/i.test(name);
}

function truncate(str, maxLen) {
  if (str.length <= maxLen) return str;
  return str.slice(0, maxLen - 3) + '...';
}

module.exports = { generateFlowDiagram, generateSequenceDiagram };
