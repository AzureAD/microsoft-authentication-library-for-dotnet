# MSAL.NET Agent

This is the master Copilot agent for the MSAL.NET repository. It orchestrates sub-agents for common engineering workflows.

## Available Sub-Agents

| Agent | Description | Location |
|-------|-------------|----------|
| **Release Agent** | Automates MSAL.NET NuGet releases end-to-end | [`.github/agents/release/`](.github/agents/release/) |

## How to Use

### Via Copilot Chat

Ask Copilot to perform a task and it will delegate to the appropriate sub-agent:

```
"Release MSAL 4.85.0"           → delegates to Release Agent
"Run the release checklist"      → delegates to Release Agent
```

### Architecture

```
MSAL.NET Master Agent (AGENTS.md)
└── Release Agent (.github/agents/release/)
    ├── Pre-release validation (5 automated checks)
    ├── Pipeline trigger (OneBranch ADO pipeline)
    ├── Post-release automation (GitHub Action)
    └── Release report generation
```

## Future Sub-Agents

The following agents are planned for future implementation:

- **Triage Agent** — Auto-triage GitHub issues, label, assign, and suggest related code
- **Dependency Agent** — Monitor and update dependencies, handle CG alerts
- **Sample Update Agent** — Update samples to latest MSAL version after release
- **Performance Agent** — Run benchmarks and report regressions

## Contributing

To add a new sub-agent:

1. Create a folder under `.github/agents/<agent-name>/`
2. Add an `AGENT.md` file with the agent's instructions and capabilities
3. Reference it in this file's "Available Sub-Agents" table
4. Document the agent's prompts and expected behavior
