# MSAL Log Analyzer

A powerful web application for analyzing Microsoft Authentication Library (MSAL) logs with AI-powered insights, interactive visualizations, and professional reporting.

## Features

- 📤 **Drag-and-drop log upload** - Easy file upload with real-time progress
- 🤖 **AI-powered analysis** - Claude AI integration for intelligent log parsing
- 📊 **Performance metrics** - Total duration, HTTP calls, cache hits, token source
- 🔄 **Interactive flow diagrams** - Mermaid-based module interaction visualizations
- 📋 **Module details** - Tabular view of all modules with timings and status
- 💡 **Key findings** - Automated insights and recommendations
- 📄 **HTML export** - Download beautiful reports for sharing
- 🌓 **Dark/Light theme** - Comfortable viewing in any environment
- 📱 **Mobile responsive** - Works on all screen sizes

## Tech Stack

### Frontend
- React 18
- Tailwind CSS
- Mermaid.js (diagrams)
- React Dropzone (file upload)
- Recharts (metrics visualization)

### Backend
- Express.js
- Claude AI (Anthropic)
- Multer (file handling)
- Helmet (security headers)
- CORS

## Quick Start

### Prerequisites
- Node.js 18+
- npm 9+
- Anthropic API key (for Claude AI)

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd msal-analyzer

# Install all dependencies
npm run install:all

# Configure environment
cp server/.env.example server/.env
# Edit server/.env and add your ANTHROPIC_API_KEY
```

### Development

```bash
# Start both frontend and backend in development mode
npm run dev
```

- Frontend: http://localhost:3000
- Backend API: http://localhost:3001

### Production

```bash
# Build the frontend
npm run build

# Start the server (serves frontend + API)
npm start
```

## Docker

### Using Docker Compose

```bash
# Copy and configure environment
cp server/.env.example server/.env
# Edit server/.env and add your ANTHROPIC_API_KEY

# Build and start
docker-compose -f docker/docker-compose.yml up --build

# Access the app at http://localhost:3001
```

### Using Docker directly

```bash
docker build -f docker/Dockerfile -t msal-analyzer .
docker run -p 3001:3001 -e ANTHROPIC_API_KEY=your_key msal-analyzer
```

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `ANTHROPIC_API_KEY` | Your Claude AI API key | Required |
| `PORT` | Server port | `3001` |
| `NODE_ENV` | Environment | `development` |
| `MAX_FILE_SIZE` | Max upload size in bytes | `10485760` (10MB) |

## Usage

1. Open the application in your browser
2. Drag and drop an MSAL log file (or click to browse)
3. Wait for AI analysis to complete
4. Explore the dashboard with:
   - Performance metrics overview
   - Module interaction flow diagram
   - Module details table
   - Key findings and recommendations
5. Export the report as HTML for sharing

## API Reference

### POST /api/analyze
Analyzes an MSAL log file.

**Request:** `multipart/form-data`
- `logFile`: The MSAL log file to analyze

**Response:**
```json
{
  "success": true,
  "report": {
    "summary": { ... },
    "modules": [ ... ],
    "mermaidDiagram": "...",
    "findings": [ ... ],
    "rawInsights": "..."
  }
}
```

### GET /api/health
Health check endpoint.

## License

MIT
