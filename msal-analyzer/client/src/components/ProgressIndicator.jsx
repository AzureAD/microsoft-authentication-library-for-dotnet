import React from 'react';

/**
 * Animated progress indicator shown during log analysis.
 * Displays multi-step progress with real-time status updates.
 */
function ProgressIndicator({ fileName, progress, statusMessage }) {
  const steps = [
    { label: 'Uploading file', threshold: 20 },
    { label: 'Parsing log structure', threshold: 40 },
    { label: 'Extracting modules', threshold: 60 },
    { label: 'AI analysis', threshold: 80 },
    { label: 'Generating report', threshold: 100 },
  ];

  return (
    <div className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 p-8 max-w-lg mx-auto">
      {/* File name */}
      <div className="flex items-center gap-3 mb-6">
        <div className="w-10 h-10 bg-blue-100 dark:bg-blue-900/30 rounded-lg flex items-center justify-center flex-shrink-0">
          <svg className="w-5 h-5 text-blue-600 dark:text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
              d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </div>
        <div className="min-w-0">
          <p className="font-semibold text-gray-900 dark:text-white truncate">{fileName}</p>
          <p className="text-sm text-gray-500 dark:text-gray-400">Analyzing log file...</p>
        </div>
      </div>

      {/* Progress bar */}
      <div className="mb-6">
        <div className="flex justify-between text-sm mb-2">
          <span className="text-gray-600 dark:text-gray-400">{statusMessage || 'Processing...'}</span>
          <span className="font-semibold text-blue-600 dark:text-blue-400">{progress}%</span>
        </div>
        <div className="h-2.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
          <div
            className="h-full bg-gradient-to-r from-blue-500 to-blue-600 rounded-full transition-all duration-500 ease-out"
            style={{ width: `${progress}%` }}
          />
        </div>
      </div>

      {/* Steps */}
      <div className="space-y-3">
        {steps.map((step, i) => {
          const isComplete = progress >= step.threshold;
          const isActive = progress >= (steps[i - 1]?.threshold || 0) && progress < step.threshold;

          return (
            <div key={step.label} className="flex items-center gap-3">
              <div className={`
                w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0
                transition-all duration-300
                ${isComplete
                  ? 'bg-green-500'
                  : isActive
                  ? 'bg-blue-500 ring-4 ring-blue-100 dark:ring-blue-900/30'
                  : 'bg-gray-200 dark:bg-gray-700'
                }
              `}>
                {isComplete ? (
                  <svg className="w-3.5 h-3.5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                  </svg>
                ) : isActive ? (
                  <div className="w-2 h-2 bg-white rounded-full animate-pulse" />
                ) : (
                  <div className="w-2 h-2 bg-gray-400 dark:bg-gray-500 rounded-full" />
                )}
              </div>
              <span className={`text-sm ${
                isComplete
                  ? 'text-green-600 dark:text-green-400 font-medium'
                  : isActive
                  ? 'text-blue-600 dark:text-blue-400 font-medium'
                  : 'text-gray-400 dark:text-gray-600'
              }`}>
                {step.label}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default ProgressIndicator;
