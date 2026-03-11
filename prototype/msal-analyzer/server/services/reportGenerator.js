/**
 * MSAL Log Analyzer - HTML Report Generator
 * Generates self-contained HTML reports from analysis results
 */

/**
 * Generates a complete, self-contained HTML report.
 *
 * @param {Object} report - Analysis report object
 * @param {string} fileName - Original log file name
 * @returns {string} Complete HTML document
 */
function generateHtmlReport(report, fileName) {
  const { summary, modules, mermaidDiagram, findings, analyzedAt, stats } = report;

  const statusColor = {
    success: '#10b981',
    partial: '#f59e0b',
    failure: '#ef4444',
    unknown: '#6b7280',
  };

  const findingIcons = {
    info: '💡',
    warning: '⚠️',
    error: '🔴',
    performance: '⚡',
    security: '🔒',
  };

  const modulesTableRows = (modules || []).map(mod => `
    <tr>
      <td class="td">${escapeHtml(mod.name)}</td>
      <td class="td">${escapeHtml(mod.role || '—')}</td>
      <td class="td">
        <span class="badge badge-${(mod.aiStatus || mod.level || 'info').toLowerCase()}">
          ${escapeHtml(mod.aiStatus || mod.level || 'INFO')}
        </span>
      </td>
      <td class="td">${mod.durationMs != null ? `${mod.durationMs}ms` : '—'}</td>
      <td class="td">${mod.logCount || 0}</td>
      <td class="td">${mod.errorCount || 0}</td>
    </tr>`).join('');

  const findingsHtml = (findings || []).map(f => `
    <div class="finding finding-${f.type || 'info'}">
      <div class="finding-header">
        <span class="finding-icon">${findingIcons[f.type] || '💡'}</span>
        <strong>${escapeHtml(f.title || 'Finding')}</strong>
      </div>
      <p class="finding-desc">${escapeHtml(f.description || '')}</p>
      ${f.recommendation ? `<p class="finding-rec"><strong>Recommendation:</strong> ${escapeHtml(f.recommendation)}</p>` : ''}
    </div>`).join('');

  const html = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>MSAL Log Analysis Report - ${escapeHtml(fileName || 'Unknown')}</title>
  <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
  <style>
    *, *::before, *::after { box-sizing: border-box; }
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 24px; background: #f8fafc; color: #1e293b; }
    .container { max-width: 1200px; margin: 0 auto; }
    .header { background: linear-gradient(135deg, #0f172a 0%, #1e3a5f 100%); color: white; padding: 32px; border-radius: 12px; margin-bottom: 24px; }
    .header h1 { margin: 0 0 8px; font-size: 24px; }
    .header .meta { opacity: 0.7; font-size: 14px; }
    .status-badge { display: inline-block; padding: 4px 12px; border-radius: 20px; font-size: 13px; font-weight: 600; color: white; background: ${statusColor[summary?.status] || statusColor.unknown}; }
    .metrics-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 24px; }
    .metric-card { background: white; padding: 20px; border-radius: 10px; border: 1px solid #e2e8f0; }
    .metric-card .label { font-size: 12px; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em; }
    .metric-card .value { font-size: 28px; font-weight: 700; color: #0f172a; margin: 4px 0; }
    .metric-card .sub { font-size: 12px; color: #94a3b8; }
    .section { background: white; border-radius: 10px; border: 1px solid #e2e8f0; padding: 24px; margin-bottom: 24px; }
    .section h2 { margin: 0 0 16px; font-size: 18px; color: #0f172a; }
    .overview { background: #f8fafc; padding: 16px; border-radius: 8px; border-left: 4px solid #3b82f6; margin-bottom: 16px; }
    table { width: 100%; border-collapse: collapse; }
    .th { text-align: left; padding: 10px 12px; background: #f1f5f9; font-size: 12px; font-weight: 600; text-transform: uppercase; color: #64748b; }
    .td { padding: 10px 12px; border-bottom: 1px solid #f1f5f9; font-size: 14px; }
    .badge { padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 600; }
    .badge-success, .badge-info { background: #d1fae5; color: #065f46; }
    .badge-warning, .badge-warn { background: #fef3c7; color: #92400e; }
    .badge-error { background: #fee2e2; color: #991b1b; }
    .badge-debug { background: #ede9fe; color: #5b21b6; }
    .finding { padding: 16px; border-radius: 8px; margin-bottom: 12px; border-left: 4px solid; }
    .finding-info { background: #eff6ff; border-color: #3b82f6; }
    .finding-warning { background: #fffbeb; border-color: #f59e0b; }
    .finding-error { background: #fef2f2; border-color: #ef4444; }
    .finding-performance { background: #f0fdf4; border-color: #10b981; }
    .finding-security { background: #fdf4ff; border-color: #a855f7; }
    .finding-header { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
    .finding-icon { font-size: 18px; }
    .finding-desc { margin: 0 0 8px; color: #374151; }
    .finding-rec { margin: 0; color: #4b5563; font-size: 14px; }
    .mermaid { text-align: center; overflow-x: auto; padding: 16px; background: #f8fafc; border-radius: 8px; }
    .footer { text-align: center; color: #94a3b8; font-size: 12px; margin-top: 24px; }
    @media print { body { background: white; padding: 0; } .section { break-inside: avoid; } }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h1>🔍 MSAL Log Analysis Report</h1>
      <div class="meta">
        File: <strong>${escapeHtml(fileName || 'Unknown')}</strong> &nbsp;|&nbsp;
        Analyzed: <strong>${new Date(analyzedAt || Date.now()).toLocaleString()}</strong> &nbsp;|&nbsp;
        <span class="status-badge">${escapeHtml((summary?.status || 'unknown').toUpperCase())}</span>
      </div>
    </div>

    <div class="metrics-grid">
      <div class="metric-card">
        <div class="label">Total Duration</div>
        <div class="value">${summary?.totalDurationMs != null ? `${summary.totalDurationMs}ms` : '—'}</div>
        <div class="sub">End-to-end</div>
      </div>
      <div class="metric-card">
        <div class="label">HTTP Calls</div>
        <div class="value">${summary?.httpCallCount ?? stats?.httpCallCount ?? 0}</div>
        <div class="sub">Network requests</div>
      </div>
      <div class="metric-card">
        <div class="label">Cache Hits</div>
        <div class="value">${summary?.cacheHitCount ?? stats?.cacheHitCount ?? 0}</div>
        <div class="sub">Token cache</div>
      </div>
      <div class="metric-card">
        <div class="label">Token Source</div>
        <div class="value" style="font-size:18px">${escapeHtml(summary?.tokenSource || '—')}</div>
        <div class="sub">Acquisition method</div>
      </div>
      <div class="metric-card">
        <div class="label">Errors</div>
        <div class="value" style="color: ${(summary?.errorCount || 0) > 0 ? '#ef4444' : '#10b981'}">${summary?.errorCount ?? stats?.errorCount ?? 0}</div>
        <div class="sub">In log</div>
      </div>
      <div class="metric-card">
        <div class="label">Modules</div>
        <div class="value">${(modules || []).length}</div>
        <div class="sub">Components found</div>
      </div>
    </div>

    <div class="section">
      <h2>📋 Overview</h2>
      <div class="overview">${escapeHtml(summary?.overview || 'No overview available.')}</div>
    </div>

    ${mermaidDiagram ? `
    <div class="section">
      <h2>🔄 Module Flow Diagram</h2>
      <div class="mermaid">
${escapeHtml(mermaidDiagram)}
      </div>
    </div>` : ''}

    <div class="section">
      <h2>🧩 Module Details (${(modules || []).length})</h2>
      <table>
        <thead>
          <tr>
            <th class="th">Module</th>
            <th class="th">Role</th>
            <th class="th">Status</th>
            <th class="th">Duration</th>
            <th class="th">Log Entries</th>
            <th class="th">Errors</th>
          </tr>
        </thead>
        <tbody>
          ${modulesTableRows || '<tr><td class="td" colspan="6">No modules found</td></tr>'}
        </tbody>
      </table>
    </div>

    ${findings && findings.length > 0 ? `
    <div class="section">
      <h2>💡 Key Findings (${findings.length})</h2>
      ${findingsHtml}
    </div>` : ''}

    <div class="footer">
      Generated by MSAL Log Analyzer &nbsp;|&nbsp;
      ${stats ? `${stats.rawLineCount || 0} log lines processed` : ''}
    </div>
  </div>

  <script>
    mermaid.initialize({ startOnLoad: true, theme: 'default', securityLevel: 'loose' });
  </script>
</body>
</html>`;

  return html;
}

/**
 * Escapes HTML special characters to prevent XSS.
 */
function escapeHtml(str) {
  if (str == null) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

module.exports = { generateHtmlReport };
