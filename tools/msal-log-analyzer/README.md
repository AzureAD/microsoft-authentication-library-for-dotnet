# MSAL Log Analyzer

A professional web application for analyzing MSAL (Microsoft Authentication Library) .NET debug logs. Upload log files to get instant performance metrics, interactive flow diagrams, error analysis, and exportable reports.

## Features

- **Drag-and-drop upload** — Drop `.log` or `.txt` files directly onto the page, or click to browse
- **Batch analysis** — Analyze up to 10 files at once
- **Performance metrics** — Total duration, cache hits/misses, HTTP call count, error count
- **Interactive Mermaid diagrams** — Module flow chart and authentication sequence diagram
- **Event timeline** — Scrollable log timeline with log-level color coding
- **Error/Warning report** — Filtered view of all errors, warnings, and exceptions
- **Exportable HTML reports** — Click "Export HTML" to download a self-contained report
- **Analysis history** — Browse, search, filter, and delete past analyses (stored in SQLite)
- **Favorites** — Star important analyses for quick access
- **Dark/Light theme** — Toggle with the moon/sun button
- **Mobile-responsive** — Works on phones and tablets

## Quick Start

### Prerequisites

- [Node.js](https://nodejs.org/) 18 or later
- npm 9 or later

### Install and Run

```bash
# From the repository root
cd tools/msal-log-analyzer/backend

# Install dependencies
npm install

# Start the server (default port: 3001)
npm start
```

Open <http://localhost:3001> in your browser.

### Configuration

Copy the example environment file and adjust as needed:

```bash
cp tools/msal-log-analyzer/.env.example tools/msal-log-analyzer/.env
```

| Variable            | Default       | Description                                          |
|---------------------|---------------|------------------------------------------------------|
| `PORT`              | `3001`        | HTTP port to listen on                               |
| `CORS_ORIGIN`       | `*`           | Allowed CORS origin (restrict in production)         |
| `NODE_ENV`          | `development` | Node environment                                     |
| `DB_DIR`            | `./data`      | Directory for the SQLite database                    |
| `ANTHROPIC_API_KEY` | *(unset)*     | Optional Claude AI API key for AI-powered summaries  |

## Docker

### Build and Run with Docker Compose

```bash
cd tools/msal-log-analyzer

# Build and start
docker compose -f docker/docker-compose.yml up --build -d

# View logs
docker compose -f docker/docker-compose.yml logs -f

# Stop
docker compose -f docker/docker-compose.yml down
```

### Build the Image Directly

```bash
cd tools/msal-log-analyzer
docker build -f docker/Dockerfile -t msal-log-analyzer .
docker run -p 3001:3001 -v msal-data:/app/data msal-log-analyzer
```

## API Reference

The backend exposes a REST API documented at `GET /api/docs`.

| Method | Endpoint                        | Description                              |
|--------|---------------------------------|------------------------------------------|
| POST   | `/api/analyze`                  | Upload and analyze a single log file     |
| POST   | `/api/analyze/batch`            | Upload and analyze up to 10 log files    |
| GET    | `/api/reports`                  | List all past analyses                   |
| GET    | `/api/reports/:id`              | Get a specific analysis                  |
| GET    | `/api/reports/:id/html`         | Export analysis as HTML report           |
| PATCH  | `/api/reports/:id/favorite`     | Toggle favorite status                   |
| DELETE | `/api/reports/:id`              | Delete an analysis                       |
| GET    | `/api/sessions/preferences`     | Get session preferences                  |
| PUT    | `/api/sessions/preferences`     | Update session preferences               |
| GET    | `/api/health`                   | Health check                             |

### Upload a Log File (curl example)

```bash
curl -X POST http://localhost:3001/api/analyze \
  -F "logFile=@/path/to/msal.log" \
  | jq .summary
```

## Architecture

```
tools/msal-log-analyzer/
├── backend/
│   ├── server.js          # Express.js entry point
│   ├── routes/
│   │   ├── analyze.js     # Upload + analysis endpoints
│   │   ├── reports.js     # History CRUD endpoints
│   │   └── sessions.js    # Session/preferences endpoints
│   ├── services/
│   │   ├── logParser.js       # MSAL log parsing engine
│   │   ├── diagramGenerator.js # Mermaid diagram generation
│   │   └── reportGenerator.js  # HTML report generation
│   ├── db/
│   │   └── database.js    # SQLite schema + connection
│   └── package.json
├── frontend/
│   └── index.html         # Single-page application (Tailwind CSS + Chart.js + Mermaid)
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
├── .env.example
└── README.md
```

## What Gets Parsed from MSAL Logs

The parser extracts:

- **Performance** — `total duration`, individual `Xms` timings
- **Cache** — cache hit/miss patterns
- **HTTP** — HTTP status codes and request counts
- **Modules** — TokenCache, AcquireTokenSilent, HttpManager, OAuth2Client, ManagedIdentity, BrokerPlugin, and 10+ more
- **Session** — correlation ID, client ID, tenant, authority, scopes
- **Errors** — `[Error]` lines, `MsalException`, `MsalServiceException`, etc.
- **Timeline** — timestamped event sequence

## License

MIT — See the repository [LICENSE](../../LICENSE) file.
