import React from 'react';

/**
 * Displays performance metrics as cards in a responsive grid.
 * Supports animated values and colored indicators.
 */
function MetricsCards({ summary, stats }) {
  const tokenSourceColor = {
    cache: 'text-green-600 dark:text-green-400',
    refresh: 'text-yellow-600 dark:text-yellow-400',
    network: 'text-blue-600 dark:text-blue-400',
    unknown: 'text-gray-500 dark:text-gray-400',
  };

  const statusConfig = {
    success: { bg: 'bg-green-50 dark:bg-green-900/20', text: 'text-green-700 dark:text-green-400', dot: 'bg-green-500', label: 'Success' },
    partial: { bg: 'bg-yellow-50 dark:bg-yellow-900/20', text: 'text-yellow-700 dark:text-yellow-400', dot: 'bg-yellow-500', label: 'Partial' },
    failure: { bg: 'bg-red-50 dark:bg-red-900/20', text: 'text-red-700 dark:text-red-400', dot: 'bg-red-500', label: 'Failure' },
    unknown: { bg: 'bg-gray-50 dark:bg-gray-700/40', text: 'text-gray-600 dark:text-gray-400', dot: 'bg-gray-400', label: 'Unknown' },
  };

  const status = statusConfig[summary?.status] || statusConfig.unknown;
  const httpCount = summary?.httpCallCount ?? stats?.httpCallCount ?? 0;
  const cacheHits = summary?.cacheHitCount ?? stats?.cacheHitCount ?? 0;
  const errorCount = summary?.errorCount ?? stats?.errorCount ?? 0;

  const metrics = [
    {
      label: 'Duration',
      value: summary?.totalDurationMs != null
        ? summary.totalDurationMs >= 1000
          ? `${(summary.totalDurationMs / 1000).toFixed(1)}s`
          : `${summary.totalDurationMs}ms`
        : '—',
      sub: 'End-to-end',
      icon: (
        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
      iconBg: 'bg-purple-100 dark:bg-purple-900/30',
      iconColor: 'text-purple-600 dark:text-purple-400',
      valueColor: 'text-gray-900 dark:text-white',
    },
    {
      label: 'HTTP Calls',
      value: httpCount,
      sub: 'Network requests',
      icon: (
        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" />
        </svg>
      ),
      iconBg: 'bg-blue-100 dark:bg-blue-900/30',
      iconColor: 'text-blue-600 dark:text-blue-400',
      valueColor: 'text-gray-900 dark:text-white',
    },
    {
      label: 'Cache Hits',
      value: cacheHits,
      sub: 'Token cache',
      icon: (
        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
        </svg>
      ),
      iconBg: 'bg-green-100 dark:bg-green-900/30',
      iconColor: 'text-green-600 dark:text-green-400',
      valueColor: 'text-gray-900 dark:text-white',
    },
    {
      label: 'Token Source',
      value: summary?.tokenSource || '—',
      sub: 'Acquisition method',
      icon: (
        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
        </svg>
      ),
      iconBg: 'bg-yellow-100 dark:bg-yellow-900/30',
      iconColor: 'text-yellow-600 dark:text-yellow-400',
      valueColor: tokenSourceColor[summary?.tokenSource] || 'text-gray-900 dark:text-white',
    },
    {
      label: 'Errors',
      value: errorCount,
      sub: 'In log file',
      icon: (
        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
      ),
      iconBg: errorCount > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-green-100 dark:bg-green-900/30',
      iconColor: errorCount > 0 ? 'text-red-600 dark:text-red-400' : 'text-green-600 dark:text-green-400',
      valueColor: errorCount > 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-900 dark:text-white',
    },
    {
      label: 'Status',
      value: null,
      sub: null,
      customContent: (
        <div>
          <div className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-sm font-medium ${status.bg} ${status.text}`}>
            <span className={`w-2 h-2 rounded-full ${status.dot}`} />
            {status.label}
          </div>
        </div>
      ),
      icon: (
        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      ),
      iconBg: `${status.bg}`,
      iconColor: status.text,
      valueColor: status.text,
    },
  ];

  return (
    <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
      {metrics.map((metric) => (
        <div
          key={metric.label}
          className="bg-white dark:bg-gray-800 rounded-xl p-5 border border-gray-200 dark:border-gray-700
            shadow-sm hover:shadow-md transition-shadow"
        >
          {/* Icon */}
          <div className={`w-10 h-10 rounded-lg ${metric.iconBg} ${metric.iconColor} flex items-center justify-center mb-3`}>
            {metric.icon}
          </div>

          {/* Label */}
          <p className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-1">
            {metric.label}
          </p>

          {/* Value */}
          {metric.customContent ? (
            metric.customContent
          ) : (
            <>
              <p className={`text-2xl font-bold ${metric.valueColor} capitalize`}>
                {metric.value}
              </p>
              {metric.sub && (
                <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">{metric.sub}</p>
              )}
            </>
          )}
        </div>
      ))}
    </div>
  );
}

export default MetricsCards;
