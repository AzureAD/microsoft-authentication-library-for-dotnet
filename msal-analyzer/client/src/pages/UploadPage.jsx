import React, { useState, useCallback } from 'react';
import FileUploadZone from '../components/FileUploadZone';
import ProgressIndicator from '../components/ProgressIndicator';

/**
 * Upload page - entry point for log analysis.
 * Handles file selection, upload, and analysis request.
 */
function UploadPage({ onAnalysisComplete }) {
  const [selectedFile, setSelectedFile] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [statusMessage, setStatusMessage] = useState('');
  const [error, setError] = useState(null);

  const handleFileSelected = useCallback((file) => {
    setSelectedFile(file);
    setError(null);
  }, []);

  const handleAnalyze = useCallback(async () => {
    if (!selectedFile) return;

    setIsLoading(true);
    setError(null);
    setProgress(10);
    setStatusMessage('Uploading file...');

    const formData = new FormData();
    formData.append('logFile', selectedFile);

    try {
      // Simulate progress stages
      const progressTimer = simulateProgress(setProgress, setStatusMessage);

      const response = await fetch('/api/analyze', {
        method: 'POST',
        body: formData,
        referrerPolicy: 'same-origin',
        signal: AbortSignal.timeout(120000),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || `Request failed: ${response.status}`);
      }

      const data = await response.json();

      clearInterval(progressTimer);
      setProgress(100);
      setStatusMessage('Analysis complete!');

      // Brief pause to show 100%
      await new Promise(resolve => setTimeout(resolve, 500));

      onAnalysisComplete(data);
    } catch (err) {
      const message = err.response?.data?.error || err.message || 'Analysis failed. Please try again.';
      setError(message);
      setIsLoading(false);
      setProgress(0);
      setStatusMessage('');
    }
  }, [selectedFile, onAnalysisComplete]);

  const handleRemoveFile = () => {
    setSelectedFile(null);
    setError(null);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-96 py-16">
        <ProgressIndicator
          fileName={selectedFile?.name}
          progress={progress}
          statusMessage={statusMessage}
        />
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto">
      {/* Hero section */}
      <div className="text-center mb-10">
        <div className="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-br from-blue-500 to-blue-700
          rounded-2xl shadow-lg mb-5">
          <svg className="w-8 h-8 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
              d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
          </svg>
        </div>
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-3">
          Analyze Your MSAL Logs
        </h2>
        <p className="text-gray-500 dark:text-gray-400 text-lg max-w-lg mx-auto">
          Upload an MSAL log file to get AI-powered insights, performance metrics,
          and interactive flow diagrams.
        </p>
      </div>

      {/* Upload zone */}
      <FileUploadZone
        onFileSelected={handleFileSelected}
        isLoading={isLoading}
      />

      {/* Selected file preview */}
      {selectedFile && (
        <div className="mt-4 p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800
          rounded-xl flex items-center justify-between gap-4">
          <div className="flex items-center gap-3 min-w-0">
            <div className="w-10 h-10 bg-blue-100 dark:bg-blue-900/40 rounded-lg flex items-center justify-center flex-shrink-0">
              <svg className="w-5 h-5 text-blue-600 dark:text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <div className="min-w-0">
              <p className="font-medium text-blue-900 dark:text-blue-200 truncate">{selectedFile.name}</p>
              <p className="text-sm text-blue-600 dark:text-blue-400">
                {formatFileSize(selectedFile.size)}
              </p>
            </div>
          </div>
          <button
            onClick={handleRemoveFile}
            className="text-blue-400 hover:text-blue-600 dark:hover:text-blue-300 flex-shrink-0"
            aria-label="Remove file"
          >
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="mt-4 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800
          rounded-xl flex items-start gap-3">
          <svg className="w-5 h-5 text-red-500 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
          </svg>
          <div>
            <p className="font-medium text-red-800 dark:text-red-300">Analysis Failed</p>
            <p className="text-sm text-red-600 dark:text-red-400 mt-1">{error}</p>
          </div>
        </div>
      )}

      {/* Analyze button */}
      <button
        onClick={handleAnalyze}
        disabled={!selectedFile || isLoading}
        className={`
          mt-6 w-full py-4 px-6 rounded-xl font-semibold text-base transition-all duration-200
          flex items-center justify-center gap-2
          ${selectedFile && !isLoading
            ? 'bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white shadow-md hover:shadow-lg'
            : 'bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-600 cursor-not-allowed'
          }
        `}
      >
        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M13 10V3L4 14h7v7l9-11h-7z" />
        </svg>
        {isLoading ? 'Analyzing...' : 'Analyze Log File'}
      </button>

      {/* Feature highlights */}
      <div className="mt-12 grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { icon: '🤖', label: 'AI Analysis', desc: 'Claude-powered insights' },
          { icon: '📊', label: 'Metrics', desc: 'Performance data' },
          { icon: '🔄', label: 'Flow Diagrams', desc: 'Module interactions' },
          { icon: '📄', label: 'Export', desc: 'HTML reports' },
        ].map(feature => (
          <div key={feature.label}
            className="text-center p-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
            <div className="text-2xl mb-2">{feature.icon}</div>
            <p className="font-semibold text-sm text-gray-800 dark:text-gray-200">{feature.label}</p>
            <p className="text-xs text-gray-500 dark:text-gray-400">{feature.desc}</p>
          </div>
        ))}
      </div>
    </div>
  );
}

/**
 * Simulates gradual progress updates while waiting for the server.
 */
function simulateProgress(setProgress, setStatusMessage) {
  const stages = [
    { progress: 25, message: 'Uploading file...' },
    { progress: 45, message: 'Parsing log structure...' },
    { progress: 65, message: 'Extracting modules...' },
    { progress: 80, message: 'Running AI analysis...' },
    { progress: 90, message: 'Generating insights...' },
  ];

  let i = 0;
  const interval = setInterval(() => {
    if (i < stages.length) {
      setProgress(stages[i].progress);
      setStatusMessage(stages[i].message);
      i++;
    } else {
      clearInterval(interval);
    }
  }, 1500);

  return interval;
}

function formatFileSize(bytes) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export default UploadPage;
