# GitHub Agent Skills for MSAL.NET

This folder contains **GitHub Agent Skills** that help AI-powered coding assistants understand and generate code for specific scenarios in the Microsoft Authentication Library for .NET (MSAL.NET).

## What Are Skills?

Skills are structured knowledge files that:
- Provide detailed guidance on specific development scenarios
- Include code examples and patterns that follow best practices
- Help AI assistants generate accurate, production-ready code
- Reduce the need for repeated explanations of complex workflows

## Supported AI Assistants

Skills in this repository work with:
- **GitHub Copilot** (with Copilot Chat and Copilot Workspace)
- **Other AI coding assistants** that support skill-based guidance

## How to Use Skills

### For AI Assistants

AI assistants automatically discover and use skills when:
1. A skill's `applies_to` patterns match files in your workspace
2. You explicitly reference a skill in your prompt (e.g., "Use the msal-mtls-pop-vanilla skill")
3. The assistant detects relevant keywords in your code or questions

### For Developers

You can also read skills directly:
1. Browse the skills folders to find relevant scenarios
2. Read the `SKILL.md` file for guidance and examples
3. Copy helper classes (`.cs` files) directly into your project
4. Adapt the examples to your specific needs

## File Structure

Each skill folder contains:
- **SKILL.md** - The main skill file with YAML frontmatter, guidance, and examples
- **Helper classes (`.cs` files)** - Production-ready C# classes you can use directly
- Additional documentation or examples as needed

Example structure:
```
.github/skills/
├── README.md                    (this file)
├── skill-name/
│   ├── SKILL.md                 (skill definition with YAML frontmatter)
│   ├── HelperClass1.cs          (production helper)
│   └── HelperClass2.cs          (production helper)
```

## Skills vs. Agent Instructions

**Skills** are different from **Agent Instructions** (`.github/copilot-instructions.md`):

| Aspect | Skills | Agent Instructions |
|--------|--------|-------------------|
| **Scope** | Specific scenarios or workflows | Repository-wide conventions |
| **Discovery** | By file patterns, keywords, or explicit reference | Always active in the repository |
| **Format** | Structured YAML + Markdown | Plain Markdown |
| **Examples** | Detailed code samples with helpers | High-level guidance |
| **Usage** | "How to implement X" | "How we write code in this repo" |

Use **skills** when you need guidance on a specific implementation pattern.  
Use **agent instructions** for general repository conventions and coding standards.

## Available Skills

Browse the subfolders in this directory to discover available skills. Each skill folder name describes its purpose.

## Resources

- [MSAL.NET Documentation](https://learn.microsoft.com/en-us/entra/msal/dotnet/)
- [MSAL.NET GitHub Repository](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)
- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)

## Contributing

When adding new skills:
1. Create a new folder with a descriptive name (use kebab-case)
2. Include a `SKILL.md` file with proper YAML frontmatter
3. Add production-ready helper classes as `.cs` files
4. Ensure examples are tested and follow repository conventions
5. Update this README if needed (though it's designed to be generic)

## License

Skills and helper classes in this folder are part of the MSAL.NET project and are licensed under the MIT License.
