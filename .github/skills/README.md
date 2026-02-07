# GitHub Agent Skills for MSAL.NET

This directory contains GitHub Agent Skills to help developers work with the Microsoft Authentication Library for .NET (MSAL.NET).

## What Are Skills?

GitHub Agent Skills are specialized knowledge modules that enhance GitHub Copilot and other AI coding assistants. Each skill provides:

- **Contextual guidance** - Step-by-step instructions for specific development scenarios
- **Code examples** - Production-ready helper classes and implementation patterns
- **Best practices** - Recommended approaches aligned with MSAL.NET conventions

## Supported AI Assistants

These skills work with:
- **GitHub Copilot Chat** (Visual Studio, VS Code, CLI)
- **GitHub Copilot Workspace**
- **Compatible AI tools** that support GitHub Skills format

## How to Use Skills

### In GitHub Copilot

Skills are automatically discovered by Copilot when working in this repository. Simply ask questions related to the skill topics, and Copilot will leverage the relevant guidance.

**Example prompts:**
- "How do I acquire an mTLS PoP token using Managed Identity?"
- "Show me how to implement a two-leg token exchange flow"
- "What are the MSAL.NET conventions for certificate-based authentication?"

### Manual Reference

You can also browse and reference skills directly:
1. Navigate to `.github/skills/[skill-name]/`
2. Review the `SKILL.md` file for guidance
3. Copy helper classes (`.cs` files) into your project as needed

## Available Skills

Each skill folder contains:
- `SKILL.md` - The skill definition with YAML metadata and guidance
- Helper classes (`.cs` files) - Optional production-ready code

Browse the subfolders in this directory to explore available skills.

## Folder Structure

```
.github/skills/
├── README.md                          # This file
├── [skill-name-1]/
│   ├── SKILL.md                      # Skill definition
│   └── [optional helper files]       # Supporting code
├── [skill-name-2]/
│   ├── SKILL.md
│   └── [optional helper files]
└── ...
```

## Skills vs. Copilot Instructions

**Skills** provide modular, topic-specific guidance that AI assistants can reference when relevant.

**Copilot Instructions** (`.github/copilot-instructions.md`) apply globally to all interactions in the repository.

Both complement each other to provide comprehensive AI-assisted development support.

## Contributing

When adding new skills:
1. Create a new folder with a descriptive kebab-case name
2. Add a `SKILL.md` file with proper YAML frontmatter
3. Include optional helper classes following MSAL.NET coding conventions
4. Update this README if the skill introduces new categories

## Resources

- [MSAL.NET Documentation](https://aka.ms/msal-net)
- [GitHub Copilot Documentation](https://docs.github.com/copilot)
- [GitHub Skills Format Specification](https://docs.github.com/copilot/customizing-copilot/adding-custom-instructions-for-github-copilot)
