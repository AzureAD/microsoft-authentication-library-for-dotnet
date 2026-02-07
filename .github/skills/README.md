# GitHub Agent Skills

This folder contains **GitHub Agent Skills** for use with GitHub Copilot and other AI assistants.

## What Are Skills?

Skills are specialized guidance documents that help AI assistants provide better, more accurate assistance when working in this repository. They contain:

- Domain-specific knowledge about APIs, patterns, and best practices
- Code examples and templates
- Common pitfalls and troubleshooting guidance
- Links to relevant documentation

Skills are **passive** - they don't execute code or modify files. Instead, they provide context that helps AI assistants better understand your questions and generate more relevant responses.

## Supported AI Assistants

Skills work with:

- **GitHub Copilot** (in VS Code, Visual Studio, and other IDEs)
- **GitHub Copilot Chat** (workspace and inline chat)
- **Other AI coding assistants** that support skill files

## How to Use Skills

There are three ways to use skills:

### 1. Repository-Level (Automatic)

Skills in the `.github/skills/` folder are automatically available when:
- Working in a repository that contains them
- Using GitHub Copilot or compatible AI assistants

No configuration needed - skills are detected automatically.

### 2. User-Level (Global)

To use skills across all your projects:

1. Clone this repository or copy the `.github/skills/` folder
2. Add the skills folder to your global AI assistant configuration
3. Skills will be available in all projects

Refer to your AI assistant's documentation for configuration details.

### 3. Direct Reference in Chat

Explicitly reference a skill in your conversation:

```
@workspace Using the msal-mtls-pop-vanilla skill, help me implement mTLS PoP token acquisition
```

## Skill File Structure

Each skill is organized in its own folder:

```
.github/skills/
├── README.md                    (this file)
├── skill-name/
│   ├── SKILL.md                 (main skill documentation)
│   └── [supporting files]       (code samples, helpers, etc.)
```

### SKILL.md Format

Each `SKILL.md` file includes YAML frontmatter with metadata:

```yaml
---
skill_name: example-skill
version: 1.0.0
description: Brief description of what this skill covers
applies_to:
  - relevant-technology
  - relevant-api
tags:
  - tag1
  - tag2
---
```

The frontmatter helps AI assistants:
- Understand when to use the skill
- Match skills to your questions
- Provide relevant context

## Skills vs. Instructions

**Skills** (this folder):
- Domain-specific guidance for particular APIs or patterns
- Used when working with specific technologies
- Can include code samples and helpers
- Passive - provide context only

**Instructions** (`.github/copilot-instructions.md`):
- General repository conventions and practices
- Always active for all work in the repository
- Broader scope than individual skills

Both work together to help AI assistants provide better assistance.

## Available Skills

See the individual skill folders for documentation on each skill:

- **[msal-mtls-pop-guidance](./msal-mtls-pop-guidance/SKILL.md)** - Shared terminology and conventions for MSAL.NET mTLS Proof-of-Possession
- **[msal-mtls-pop-vanilla](./msal-mtls-pop-vanilla/SKILL.md)** - Direct PoP token acquisition for target resources (Graph/custom APIs)
- **[msal-mtls-pop-fic-two-leg](./msal-mtls-pop-fic-two-leg/SKILL.md)** - Token exchange pattern using assertions for cross-tenant/delegation scenarios

## Resources

- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [GitHub Skills Specification](https://github.com/github/skills-specification)
- [MSAL.NET Documentation](https://learn.microsoft.com/entra/msal/dotnet/)

## Contributing

To add a new skill:

1. Create a new folder under `.github/skills/`
2. Add a `SKILL.md` file with proper YAML frontmatter
3. Include any supporting code samples or helper files
4. Update this README to list the new skill
5. Submit a pull request

Keep skills focused and scoped to specific domains or patterns.
