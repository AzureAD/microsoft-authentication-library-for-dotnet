async handleAnalyze() {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 120000); // 120s timeout

    try {
        const response = await fetch('/api/analyze', {
            method: 'POST',
            referrerPolicy: 'same-origin',
            signal: controller.signal,
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ /* your request payload here */ }),
        });

        if (!response.ok) {
            throw new Error('Network response was not ok: ' + response.statusText);
        }

        const data = await response.json();
        // Handle the response data
    } catch (error) {
        if (error.name === 'AbortError') {
            console.error('Fetch operation timed out');
        } else {
            console.error('Fetch error: ', error);
        }
    } finally {
        clearTimeout(timeoutId);
    }
}