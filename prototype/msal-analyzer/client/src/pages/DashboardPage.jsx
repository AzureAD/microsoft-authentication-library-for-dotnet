import React, { useState } from 'react';
import axios from 'axios';
import MetricsCards from '../components/MetricsCards';
import MermaidDiagram from '../components/MermaidDiagram';
import ModulesTable from '../components/ModulesTable';
import FindingsPanel from '../components/FindingsPanel';

const TABS = [
  { id: 'overview', label: 'Overview', icon: '📊' },
  { id: 'flow', label: 'Flow Diagram', icon: '🔄' },
  { id: 'modules', label: 'Modules', icon: '🧩' },
  { id: 'findings', label: 'Findings', icon: '💡' },
];

/**
 * Dashboard page showing the complete analysis results.
 * Includes tabbed navigation for different views.
 */
function DashboardPage({ result, onNewAnalysis }) {
  const [activeTab, setActiveTab] = useState('overview');
  const [isExporting, setIsExporting] = useState(false);
  const [exportError, setExportError] = useState(null);
  const [darkMode] = useState(() => document.documentElement.classList.contains('dark'));

  const { report, fileName } = result || {};

  const handleExport = async () => {
    setIsExporting(true);
    setExportError(null);

    try {
      const response = await axios.post('/api/export', {
        report,
        fileName,
      }, {
        responseType: 'blob',
        timeout: 30000,
      });

      // Trigger browser download
      const url = URL.createObjectURL(response.data);
      const link = document.createElement('a');
      link.href = url;
      link.download = `log-report-${Date.now()}.html`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (err) {
      setExportError('Failed to export report. Please try again.');
    } finally {
      setIsExporting(false);
    }
  };

  if (!report) {
    return (
      <div className="text-center py-20">
        <p className="text-gray-500 dark:text-gray-400">No analysis data available.</p>
        <button onClick={onNewAnalysis}
          className="mt-4 text-blue-600 dark:text-blue-400 hover:underline">
          Start a new analysis
        </button>
      </div>
    );
  }

  const findingCount = report.findings?.length || 0;
  const moduleCount = report.modules?.length || 0;

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            Analysis Report
          </h2>
          <div className="flex flex-wrap items-center gap-2 mt-1">
            <span className="text-sm text-gray-500 dark:text-gray-400">
              {fileName && (
                <span className="font-mono text-blue-600 dark:text-blue-400">{fileName}</span>
              )}
            </span>
            {report.analyzedAt && (
              <span className="text-xs text-gray-400 dark:text-gray-600">
                • {new Date(report.analyzedAt).toLocaleString()}
              </span>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-3">
          <button
            onClick={handleExport}
            disabled={isExporting}
            className={`
              flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors
              ${isExporting
                ? 'bg-gray-100 dark:bg-gray-700 text-gray-400 cursor-wait'
                : 'bg-green-600 hover:bg-green-700 text-white shadow-sm'
              }
            `}
          >
            {isExporting ? (
              <>
                <svg className="w-4 h-4 animate-spin" viewBox="0 0 24 24" fill="none">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Exporting...
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                    d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                Export HTML
              </>
            )}
          </button>
        </div>
      </div>

      {/* Export error */}
      {exportError && (
        <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800
          rounded-lg text-red-700 dark:text-red-400 text-sm">
          {exportError}
        </div>
      )}

      {/* Metrics cards */}
      <MetricsCards summary={report.summary} stats={report.stats} />

      {/* Overview text */}
      {report.summary?.overview && (
        <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <h3 className="font-semibold text-gray-900 dark:text-white mb-2 flex items-center gap-2">
            <span>📋</span> Analysis Overview
          </h3>
          <p className="text-gray-600 dark:text-gray-300 leading-relaxed">
            {report.summary.overview}
          </p>

          {/* Quick stats */}
          {report.stats && (
            <div className="mt-4 pt-4 border-t border-gray-100 dark:border-gray-700
              flex flex-wrap gap-4 text-xs text-gray-500 dark:text-gray-400">
              <span>📄 {report.stats.rawLineCount?.toLocaleString()} log lines</span>
              {report.stats.cacheMissCount > 0 && (
                <span>🔄 {report.stats.cacheMissCount} cache misses</span>
              )}
              <span>⏱️ Analyzed {new Date(report.analyzedAt).toLocaleTimeString()}</span>
            </div>
          )}
        </div>
      )}

      {/* Tabbed content */}
      <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
        {/* Tab bar */}
        <div className="border-b border-gray-200 dark:border-gray-700 px-1">
          <nav className="flex overflow-x-auto" aria-label="Tabs">
            {TABS.map(tab => {
              const isActive = activeTab === tab.id;
              let badge = null;
              if (tab.id === 'modules') badge = moduleCount;
              if (tab.id === 'findings') badge = findingCount;

              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`
                    flex items-center gap-2 px-5 py-4 text-sm font-medium whitespace-nowrap
                    border-b-2 transition-colors
                    ${isActive
                      ? 'border-blue-600 text-blue-600 dark:text-blue-400 dark:border-blue-400'
                      : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:border-gray-300 dark:hover:border-gray-600'
                    }
                  `}
                >
                  <span>{tab.icon}</span>
                  <span>{tab.label}</span>
                  {badge != null && badge > 0 && (
                    <span className={`
                      text-xs px-1.5 py-0.5 rounded-full font-medium
                      ${isActive
                        ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-400'
                        : 'bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400'
                      }
                    `}>
                      {badge}
                    </span>
                  )}
                </button>
              );
            })}
          </nav>
        </div>

        {/* Tab content */}
        <div className="p-6">
          {activeTab === 'overview' && (
            <OverviewTab report={report} />
          )}
          {activeTab === 'flow' && (
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
                Interactive sequence diagram showing module interactions.
              </p>
              <MermaidDiagram diagram={report.mermaidDiagram} darkMode={darkMode} />
            </div>
          )}
          {activeTab === 'modules' && (
            <ModulesTable modules={report.modules} />
          )}
          {activeTab === 'findings' && (
            <FindingsPanel findings={report.findings} />
          )}
        </div>
      </div>
    </div>
  );
}

/**
 * Overview tab with a summary of the analysis.
 */
function OverviewTab({ report }) {
  const httpCalls = report.stats?.httpCallCount || report.summary?.httpCallCount || 0;
  const cacheHits = report.stats?.cacheHitCount || report.summary?.cacheHitCount || 0;
  const cacheMisses = report.stats?.cacheMissCount || 0;
  const totalCache = cacheHits + cacheMisses;
  const cacheHitRate = totalCache > 0 ? Math.round((cacheHits / totalCache) * 100) : null;

  return (
    <div className="space-y-6">
      {/* Stats summary grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* HTTP calls breakdown */}
        {httpCalls > 0 && (
          <div className="bg-gray-50 dark:bg-gray-700/40 rounded-xl p-5">
            <h4 className="font-semibold text-gray-800 dark:text-gray-200 mb-3 flex items-center gap-2">
              <span>🌐</span> Network Activity
            </h4>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-400">Total HTTP requests</span>
                <span className="font-semibold text-gray-900 dark:text-white">{httpCalls}</span>
              </div>
            </div>
          </div>
        )}

        {/* Cache summary */}
        {totalCache > 0 && (
          <div className="bg-gray-50 dark:bg-gray-700/40 rounded-xl p-5">
            <h4 className="font-semibold text-gray-800 dark:text-gray-200 mb-3 flex items-center gap-2">
              <span>🗄️</span> Cache
            </h4>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-400">Cache hits</span>
                <span className="font-semibold text-green-600 dark:text-green-400">{cacheHits}</span>
              </div>
              {cacheMisses > 0 && (
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-400">Cache misses</span>
                  <span className="font-semibold text-yellow-600 dark:text-yellow-400">{cacheMisses}</span>
                </div>
              )}
              {cacheHitRate !== null && (
                <div className="mt-3">
                  <div className="flex justify-between text-xs mb-1">
                    <span className="text-gray-500 dark:text-gray-400">Hit rate</span>
                    <span className="font-medium">{cacheHitRate}%</span>
                  </div>
                  <div className="h-2 bg-gray-200 dark:bg-gray-600 rounded-full">
                    <div
                      className="h-full bg-green-500 rounded-full"
                      style={{ width: `${cacheHitRate}%` }}
                    />
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Top findings preview */}
      {report.findings && report.findings.length > 0 && (
        <div>
          <h4 className="font-semibold text-gray-800 dark:text-gray-200 mb-3 flex items-center gap-2">
            <span>💡</span> Top Findings
          </h4>
          <div className="space-y-2">
            {report.findings.slice(0, 3).map((finding, i) => {
              const typeColors = {
                error: 'text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20',
                warning: 'text-yellow-600 dark:text-yellow-400 bg-yellow-50 dark:bg-yellow-900/20',
                performance: 'text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20',
                security: 'text-purple-600 dark:text-purple-400 bg-purple-50 dark:bg-purple-900/20',
                info: 'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20',
              };
              const color = typeColors[finding.type] || typeColors.info;

              return (
                <div key={i} className={`rounded-lg p-3 ${color.split(' ').slice(1).join(' ')}`}>
                  <p className={`text-sm font-medium ${color.split(' ')[0]}`}>{finding.title}</p>
                  <p className="text-xs text-gray-600 dark:text-gray-400 mt-0.5 line-clamp-1">
                    {finding.description}
                  </p>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}

export default DashboardPage;
