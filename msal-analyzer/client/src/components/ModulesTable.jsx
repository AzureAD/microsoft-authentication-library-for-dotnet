import React, { useState } from 'react';

const STATUS_CONFIG = {
  success: { bg: 'bg-green-100 dark:bg-green-900/30', text: 'text-green-700 dark:text-green-400', label: 'Success' },
  warning: { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-700 dark:text-yellow-400', label: 'Warning' },
  warn: { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-700 dark:text-yellow-400', label: 'Warning' },
  error: { bg: 'bg-red-100 dark:bg-red-900/30', text: 'text-red-700 dark:text-red-400', label: 'Error' },
  info: { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-700 dark:text-blue-400', label: 'Info' },
  debug: { bg: 'bg-purple-100 dark:bg-purple-900/30', text: 'text-purple-700 dark:text-purple-400', label: 'Debug' },
};

const getStatus = (mod) => {
  const key = (mod.aiStatus || mod.level || 'info').toLowerCase();
  return STATUS_CONFIG[key] || STATUS_CONFIG.info;
};

/**
 * Sortable, filterable table of log modules with details.
 */
function ModulesTable({ modules }) {
  const [sortField, setSortField] = useState('name');
  const [sortDir, setSortDir] = useState('asc');
  const [filter, setFilter] = useState('');
  const [expandedRow, setExpandedRow] = useState(null);

  if (!modules || modules.length === 0) {
    return (
      <div className="text-center py-12 text-gray-400 dark:text-gray-600">
        <svg className="w-12 h-12 mx-auto mb-3 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1}
            d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
        </svg>
        <p>No modules detected in this log file.</p>
      </div>
    );
  }

  // Filter
  const filtered = modules.filter(mod =>
    !filter ||
    mod.name?.toLowerCase().includes(filter.toLowerCase()) ||
    mod.role?.toLowerCase().includes(filter.toLowerCase())
  );

  // Sort
  const sorted = [...filtered].sort((a, b) => {
    let aVal = a[sortField] ?? '';
    let bVal = b[sortField] ?? '';
    if (typeof aVal === 'string') aVal = aVal.toLowerCase();
    if (typeof bVal === 'string') bVal = bVal.toLowerCase();
    if (aVal < bVal) return sortDir === 'asc' ? -1 : 1;
    if (aVal > bVal) return sortDir === 'asc' ? 1 : -1;
    return 0;
  });

  const handleSort = (field) => {
    if (sortField === field) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDir('asc');
    }
  };

  const SortIcon = ({ field }) => {
    if (sortField !== field) {
      return <span className="text-gray-300 dark:text-gray-600 ml-1">↕</span>;
    }
    return <span className="text-blue-500 ml-1">{sortDir === 'asc' ? '↑' : '↓'}</span>;
  };

  const thClass = "px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide cursor-pointer hover:text-gray-700 dark:hover:text-gray-200 select-none";

  return (
    <div>
      {/* Filter */}
      <div className="mb-4">
        <div className="relative">
          <svg className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            type="text"
            placeholder="Filter modules..."
            value={filter}
            onChange={e => setFilter(e.target.value)}
            className="w-full pl-10 pr-4 py-2 text-sm border border-gray-300 dark:border-gray-600
              bg-white dark:bg-gray-800 text-gray-900 dark:text-white
              rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>

      {/* Table */}
      <div className="overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700">
        <table className="w-full">
          <thead className="bg-gray-50 dark:bg-gray-800/50">
            <tr>
              <th className={thClass} onClick={() => handleSort('name')}>
                Module <SortIcon field="name" />
              </th>
              <th className={thClass} onClick={() => handleSort('role')}>
                Role <SortIcon field="role" />
              </th>
              <th className={thClass} onClick={() => handleSort('aiStatus')}>
                Status <SortIcon field="aiStatus" />
              </th>
              <th className={thClass} onClick={() => handleSort('durationMs')}>
                Duration <SortIcon field="durationMs" />
              </th>
              <th className={thClass} onClick={() => handleSort('logCount')}>
                Log Entries <SortIcon field="logCount" />
              </th>
              <th className={thClass} onClick={() => handleSort('errorCount')}>
                Errors <SortIcon field="errorCount" />
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
            {sorted.map((mod, i) => {
              const status = getStatus(mod);
              const isExpanded = expandedRow === i;

              return (
                <React.Fragment key={mod.name || i}>
                  <tr
                    className="bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-750
                      cursor-pointer transition-colors"
                    onClick={() => setExpandedRow(isExpanded ? null : i)}
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-gray-400 dark:text-gray-500">
                          {isExpanded ? '▼' : '▶'}
                        </span>
                        <span className="font-mono text-sm font-medium text-gray-900 dark:text-white">
                          {mod.name}
                        </span>
                        {mod.source === 'ai' && (
                          <span className="text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400
                            px-1.5 py-0.5 rounded">AI</span>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                      {mod.role || '—'}
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${status.bg} ${status.text}`}>
                        {status.label}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 font-mono">
                      {mod.durationMs != null ? `${mod.durationMs}ms` : '—'}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                      {mod.logCount || 0}
                    </td>
                    <td className="px-4 py-3">
                      <span className={`text-sm font-medium ${mod.errorCount > 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-400 dark:text-gray-600'}`}>
                        {mod.errorCount || 0}
                      </span>
                    </td>
                  </tr>

                  {/* Expanded row */}
                  {isExpanded && (
                    <tr className="bg-blue-50 dark:bg-blue-900/10">
                      <td colSpan={6} className="px-6 py-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                          {mod.messages && mod.messages.length > 0 && (
                            <div>
                              <p className="font-semibold text-gray-700 dark:text-gray-300 mb-2">Sample Log Entries:</p>
                              <ul className="space-y-1">
                                {mod.messages.map((msg, j) => (
                                  <li key={j} className="font-mono text-xs text-gray-600 dark:text-gray-400
                                    bg-white dark:bg-gray-800 rounded px-3 py-1.5 truncate">
                                    {msg}
                                  </li>
                                ))}
                              </ul>
                            </div>
                          )}
                          {mod.callsTo && mod.callsTo.length > 0 && (
                            <div>
                              <p className="font-semibold text-gray-700 dark:text-gray-300 mb-2">Calls To:</p>
                              <div className="flex flex-wrap gap-2">
                                {mod.callsTo.map(dep => (
                                  <span key={dep}
                                    className="font-mono text-xs bg-white dark:bg-gray-800 border border-gray-200
                                      dark:border-gray-700 text-gray-700 dark:text-gray-300 px-2 py-1 rounded">
                                    {dep}
                                  </span>
                                ))}
                              </div>
                            </div>
                          )}
                          {mod.firstLine && (
                            <div className="text-gray-500 dark:text-gray-400 text-xs">
                              Lines: {mod.firstLine}–{mod.lastLine}
                              {mod.firstSeen && ` | First seen: ${mod.firstSeen}`}
                            </div>
                          )}
                        </div>
                      </td>
                    </tr>
                  )}
                </React.Fragment>
              );
            })}
          </tbody>
        </table>

        {sorted.length === 0 && (
          <div className="text-center py-8 text-gray-400 dark:text-gray-600 text-sm">
            No modules match "{filter}"
          </div>
        )}
      </div>

      <p className="mt-2 text-xs text-gray-400 dark:text-gray-600">
        Showing {sorted.length} of {modules.length} modules • Click a row for details
      </p>
    </div>
  );
}

export default ModulesTable;
