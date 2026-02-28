import React, { useEffect, useRef, useState } from 'react';
import mermaid from 'mermaid';

// Ensure mermaid is initialized once
let mermaidInitialized = false;

/**
 * Renders a Mermaid diagram from a diagram string.
 * Handles initialization, rendering, and error display.
 */
function MermaidDiagram({ diagram, darkMode }) {
  const containerRef = useRef(null);
  const [error, setError] = useState(null);
  const [rendered, setRendered] = useState(false);

  useEffect(() => {
    if (!mermaidInitialized) {
      console.log('🔧 Initializing Mermaid...');
      try {
        mermaid.initialize({
          startOnLoad: false,
          theme: darkMode ? 'dark' : 'default',
          securityLevel: 'loose',
          fontFamily: 'ui-sans-serif, system-ui, sans-serif',
          sequence: {
            diagramMarginX: 20,
            diagramMarginY: 20,
            actorMargin: 50,
            noteMargin: 10,
            messageMargin: 35,
            mirrorActors: false,
            boxTextMargin: 5,
          },
        });
        mermaidInitialized = true;
        console.log('✅ Mermaid initialized successfully');
      } catch (err) {
        console.error('❌ Mermaid initialization error:', err);
      }
    }
  }, [darkMode]);

  useEffect(() => {
    if (!diagram || !containerRef.current) {
      console.warn('⚠️ Diagram or container ref missing');
      return;
    }

    console.log('📊 Starting diagram render...');
    console.log('📋 Diagram content:', diagram.substring(0, 150));
    console.log('🌓 Dark mode:', darkMode);

    setError(null);
    setRendered(false);

    const container = containerRef.current;
    const id = `mermaid-${Date.now()}`;

    // Clean up previous render
    container.innerHTML = '';

    // Re-initialize theme when darkMode changes
    try {
      mermaid.initialize({
        startOnLoad: false,
        theme: darkMode ? 'dark' : 'default',
        securityLevel: 'loose',
      });
      console.log('✅ Theme updated:', darkMode ? 'dark' : 'default');
    } catch (err) {
      console.error('❌ Theme initialization error:', err);
    }

    (async () => {
      try {
        console.log('🎯 Calling mermaid.render with id:', id);
        
        const result = await mermaid.render(id, diagram);
        
        console.log('✅ Mermaid render completed');
        console.log('📦 Result object:', result);
        console.log('🔑 Result keys:', Object.keys(result));
        console.log('📄 SVG content length:', result.svg ? result.svg.length : 'NO SVG');

        if (!container) {
          console.error('❌ Container ref lost');
          return;
        }

        if (result && result.svg) {
          console.log('✅ Setting SVG innerHTML');
          container.innerHTML = result.svg;
          setRendered(true);
          console.log('✅ Diagram rendered successfully');
        } else if (result && result.data) {
          console.log('✅ Setting SVG from result.data');
          container.innerHTML = result.data;
          setRendered(true);
          console.log('✅ Diagram rendered successfully (from result.data)');
        } else {
          console.error('❌ No SVG found in result:', result);
          setError('Mermaid returned no SVG content');
          container.innerHTML = `<pre class="text-xs p-4 bg-red-50 dark:bg-red-900/20 rounded">${diagram}</pre>`;
        }
      } catch (err) {
        console.error('❌ Mermaid render error:', err);
        console.error('📍 Error message:', err.message);
        console.error('📍 Error stack:', err.stack);
        
        setError(`Failed to render diagram: ${err.message}`);

        // Show the raw diagram text as fallback
        if (container) {
          container.innerHTML = `
            <div class="text-xs text-left p-4 bg-red-50 dark:bg-red-900/20 rounded overflow-auto font-mono">
              <div class="text-red-600 dark:text-red-400 mb-2 font-bold">⚠️ Diagram Syntax Error:</div>
              <pre class="text-red-600 dark:text-red-400">${err.message}</pre>
              <div class="text-gray-600 dark:text-gray-400 mt-4 mb-2">Diagram Code:</div>
              <pre class="bg-gray-100 dark:bg-gray-800 p-2 rounded text-xs overflow-x-auto">${diagram}</pre>
            </div>
          `;
        }
      }
    })();
  }, [diagram, darkMode]);

  if (!diagram) {
    return (
      <div className="flex items-center justify-center h-40 text-gray-400 dark:text-gray-600">
        <p>No diagram available</p>
      </div>
    );
  }

  return (
    <div className="w-full">
      {error && (
        <div className="mb-3 p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800
          rounded-lg text-yellow-800 dark:text-yellow-300 text-sm">
          ⚠️ {error}
        </div>
      )}
      <div
        ref={containerRef}
        className="mermaid-container overflow-x-auto min-h-32 flex items-center justify-center
          bg-white dark:bg-gray-900 rounded-xl p-4 border border-gray-200 dark:border-gray-700"
      >
        {!rendered && !error && (
          <div className="flex items-center gap-2 text-gray-400">
            <svg className="w-5 h-5 animate-spin" viewBox="0 0 24 24" fill="none">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            Rendering diagram...
          </div>
        )}
      </div>
    </div>
  );
}

export default MermaidDiagram;
