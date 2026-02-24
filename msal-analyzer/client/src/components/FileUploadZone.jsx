import React, { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';

/**
 * Drag-and-drop file upload zone component.
 * Accepts MSAL log files (.log, .txt) up to 10MB.
 */
function FileUploadZone({ onFileSelected, isLoading, disabled }) {
  const [dragError, setDragError] = useState(null);

  const onDrop = useCallback((acceptedFiles, rejectedFiles) => {
    setDragError(null);

    if (rejectedFiles.length > 0) {
      const err = rejectedFiles[0].errors[0];
      if (err.code === 'file-too-large') {
        setDragError('File is too large. Maximum size is 10MB.');
      } else if (err.code === 'file-invalid-type') {
        setDragError('Invalid file type. Please upload a .log or .txt file.');
      } else {
        setDragError('File rejected: ' + err.message);
      }
      return;
    }

    if (acceptedFiles.length > 0) {
      onFileSelected(acceptedFiles[0]);
    }
  }, [onFileSelected]);

  const { getRootProps, getInputProps, isDragActive, isDragReject } = useDropzone({
    onDrop,
    accept: {
      'text/plain': ['.txt', '.log', '.text'],
      'application/octet-stream': ['.log'],
    },
    maxSize: 10 * 1024 * 1024, // 10MB
    maxFiles: 1,
    disabled: isLoading || disabled,
  });

  const borderColor = isDragReject || dragError
    ? 'border-red-400 dark:border-red-500'
    : isDragActive
    ? 'border-blue-500 dark:border-blue-400'
    : 'border-gray-300 dark:border-gray-600';

  const bgColor = isDragReject || dragError
    ? 'bg-red-50 dark:bg-red-900/10'
    : isDragActive
    ? 'bg-blue-50 dark:bg-blue-900/20'
    : 'bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-750';

  return (
    <div>
      <div
        {...getRootProps()}
        className={`
          relative border-2 border-dashed rounded-2xl p-12 text-center
          transition-all duration-200 cursor-pointer
          ${borderColor} ${bgColor}
          ${isLoading ? 'opacity-60 cursor-not-allowed' : ''}
        `}
      >
        <input {...getInputProps()} />

        {/* Icon */}
        <div className={`
          mx-auto mb-4 w-16 h-16 rounded-full flex items-center justify-center
          ${isDragActive ? 'bg-blue-100 dark:bg-blue-900/40' : 'bg-gray-100 dark:bg-gray-700'}
          transition-colors duration-200
        `}>
          {isDragActive ? (
            <svg className="w-8 h-8 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
            </svg>
          ) : (
            <svg className="w-8 h-8 text-gray-400 dark:text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          )}
        </div>

        {/* Text */}
        {isDragActive ? (
          <p className="text-lg font-semibold text-blue-600 dark:text-blue-400">
            Drop your log file here!
          </p>
        ) : (
          <>
            <p className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-1">
              Drop your MSAL log file here
            </p>
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
              or <span className="text-blue-600 dark:text-blue-400 font-medium">click to browse</span>
            </p>
            <div className="flex flex-wrap items-center justify-center gap-2">
              {['.log', '.txt', '.text'].map(ext => (
                <span key={ext}
                  className="px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400
                    rounded text-xs font-mono">
                  {ext}
                </span>
              ))}
              <span className="text-xs text-gray-400 dark:text-gray-500">• Max 10MB</span>
            </div>
          </>
        )}
      </div>

      {/* Error message */}
      {dragError && (
        <p className="mt-2 text-sm text-red-600 dark:text-red-400 flex items-center gap-1">
          <svg className="w-4 h-4 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
          </svg>
          {dragError}
        </p>
      )}
    </div>
  );
}

export default FileUploadZone;
