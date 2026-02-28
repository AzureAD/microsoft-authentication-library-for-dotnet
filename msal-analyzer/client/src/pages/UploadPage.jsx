handleAnalyze = async () => {
    const abortController = new AbortController();
    const signal = abortController.signal;

    // Simulate progress and file selection logic here

    try {
        const response = await fetch('/api/analyze', {
            method: 'POST',
            body: formData,
            referrerPolicy: 'same-origin',
            signal: AbortSignal.timeout(120000)
        });

        if (!response.ok) {
            throw new Error(`Error: ${response.statusText}`);
        }

        const result = await response.json();
        // Handle successful response

    } catch (error) {
        console.error('An error occurred during fetch:', error);
        // Handle errors
    }
};

