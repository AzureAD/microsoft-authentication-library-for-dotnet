import React from 'react';

const FINDING_CONFIG = {
  info: {
    bg: 'bg-blue-50 dark:bg-blue-900/20',
    border: 'border-blue-300 dark:border-blue-700',
    titleColor: 'text-blue-900 dark:text-blue-300',
    textColor: 'text-blue-800 dark:text-blue-400',
    icon: '💡',
    badge: 'bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-400',
  },
  warning: {
    bg: 'bg-yellow-50 dark:bg-yellow-900/20',
    border: 'border-yellow-300 dark:border-yellow-700',
    titleColor: 'text-yellow-900 dark:text-yellow-300',
    textColor: 'text-yellow-800 dark:text-yellow-400',
    icon: '⚠️',
    badge: 'bg-yellow-100 dark:bg-yellow-900/40 text-yellow-700 dark:text-yellow-400',
  },
  error: {
    bg: 'bg-red-50 dark:bg-red-900/20',
    border: 'border-red-300 dark:border-red-700',
    titleColor: 'text-red-900 dark:text-red-300',
    textColor: 'text-red-800 dark:text-red-400',
    icon: '🔴',
    badge: 'bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-400',
  },
  performance: {
    bg: 'bg-green-50 dark:bg-green-900/20',
    border: 'border-green-300 dark:border-green-700',
    titleColor: 'text-green-900 dark:text-green-300',
    textColor: 'text-green-800 dark:text-green-400',
    icon: '⚡',
    badge: 'bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-400',
  },
  security: {
    bg: 'bg-purple-50 dark:bg-purple-900/20',
    border: 'border-purple-300 dark:border-purple-700',
    titleColor: 'text-purple-900 dark:text-purple-300',
    textColor: 'text-purple-800 dark:text-purple-400',
    icon: '🔒',
    badge: 'bg-purple-100 dark:bg-purple-900/40 text-purple-700 dark:text-purple-400',
  },
};

/**
 * Displays a list of analysis findings with icons, descriptions, and recommendations.
 */
function FindingsPanel({ findings }) {
  if (!findings || findings.length === 0) {
    return (
      <div className="text-center py-12 text-gray-400 dark:text-gray-600">
        <svg className="w-12 h-12 mx-auto mb-3 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1}
            d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p>No findings to report.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {findings.map((finding, i) => {
        const config = FINDING_CONFIG[finding.type] || FINDING_CONFIG.info;

        return (
          <div
            key={i}
            className={`rounded-xl border p-5 ${config.bg} ${config.border}`}
          >
            <div className="flex items-start gap-3">
              <span className="text-xl flex-shrink-0 mt-0.5">{config.icon}</span>
              <div className="flex-1 min-w-0">
                <div className="flex flex-wrap items-center gap-2 mb-2">
                  <h4 className={`font-semibold ${config.titleColor}`}>
                    {finding.title}
                  </h4>
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium capitalize ${config.badge}`}>
                    {finding.type}
                  </span>
                </div>

                <p className={`text-sm mb-3 ${config.textColor}`}>
                  {finding.description}
                </p>

                {finding.recommendation && (
                  <div className="flex items-start gap-2">
                    <svg className={`w-4 h-4 flex-shrink-0 mt-0.5 ${config.textColor}`}
                      fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <p className={`text-sm ${config.textColor} opacity-80`}>
                      <strong>Recommendation:</strong> {finding.recommendation}
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

export default FindingsPanel;
