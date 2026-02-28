import React, { useEffect, useRef, useState } from 'react';
import mermaid from 'mermaid';

let mermaidInitialized = false;

function MermaidDiagram({ diagram, darkMode }) {
  const containerRef = useRef(null);
  const [error, setError] = useState(null);
  const [svgContent, setSvgContent] = useState(null);
  const [rendered, setRendered] = useState(false);

  useEffect(() => {
    if (!mermaidInitialized) {
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
    }
  }, [darkMode]);

  useEffect(() => {
    if (!diagram) return;

    setError(null);
    setRendered(false);
    setSvgContent(null);

    const id = `mermaid-${Date.now()}`;

    // Re-initialize theme
    mermaid.initialize({
      startOnLoad: false,
      theme: darkMode ? 'dark' : 'default',
      securityLevel: 'loose',
    });

    (async () => {
      try {
        const result = await mermaid.render(id, diagram);

        if (result && result.svg) {
          // Use state instead of direct innerHTML
          setSvgContent(result.svg);
          setRendered(true);
        } else {
          setError('Mermaid returned no SVG content');
        }
      } catch (err) {
        console.error('Mermaid render error:', err);
        setError(`Failed to render diagram: ${err.message}`);
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
        {svgContent ? (
          <div dangerouslySetInnerHTML={{ __html: svgContent }} />
        ) : !rendered && !error ? (
          <div className="flex items-center gap-2 text-gray-400">
            <svg className="w-5 h-5 animate-spin" viewBox="0 0 24 24" fill="none">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            Rendering diagram...
          </div>
        ) : null}
      </div>
    </div>
  );
}

export default MermaidDiagram;
