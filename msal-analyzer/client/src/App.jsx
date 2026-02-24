import React, { useState, useEffect } from 'react';
import UploadPage from './pages/UploadPage';
import DashboardPage from './pages/DashboardPage';
import Header from './components/Header';

/**
 * Main application component.
 * Manages theme state and page routing between upload and dashboard views.
 */
function App() {
  const [darkMode, setDarkMode] = useState(() => {
    // Persist theme preference in localStorage
    const saved = localStorage.getItem('msal-analyzer-theme');
    if (saved) return saved === 'dark';
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  });

  const [analysisResult, setAnalysisResult] = useState(null);
  const [currentPage, setCurrentPage] = useState('upload'); // 'upload' | 'dashboard'

  // Apply dark mode class to html element
  useEffect(() => {
    document.documentElement.classList.toggle('dark', darkMode);
    localStorage.setItem('msal-analyzer-theme', darkMode ? 'dark' : 'light');
  }, [darkMode]);

  const handleAnalysisComplete = (result) => {
    setAnalysisResult(result);
    setCurrentPage('dashboard');
  };

  const handleNewAnalysis = () => {
    setAnalysisResult(null);
    setCurrentPage('upload');
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors duration-200">
      <Header
        darkMode={darkMode}
        onToggleDark={() => setDarkMode(d => !d)}
        onNewAnalysis={currentPage === 'dashboard' ? handleNewAnalysis : null}
      />

      <main className="container mx-auto px-4 py-8 max-w-7xl">
        {currentPage === 'upload' ? (
          <UploadPage onAnalysisComplete={handleAnalysisComplete} />
        ) : (
          <DashboardPage
            result={analysisResult}
            onNewAnalysis={handleNewAnalysis}
          />
        )}
      </main>
    </div>
  );
}

export default App;
