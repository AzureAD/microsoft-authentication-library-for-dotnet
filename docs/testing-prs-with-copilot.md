# Testing GitHub PRs Locally with Copilot Chat and Skills

This guide provides a comprehensive workflow for testing GitHub PRs locally with Copilot Chat and Skills integration in VS Code, specifically for the MSAL .NET repository.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Quick Start Commands](#quick-start-commands)
- [Step-by-Step Workflow](#step-by-step-workflow)
- [Copilot Chat Workspace Setup](#copilot-chat-workspace-setup)
- [Testing Skills-Related PRs](#testing-skills-related-prs)
- [Validation Checklists](#validation-checklists)
- [Quality Standards](#quality-standards)
- [Troubleshooting](#troubleshooting)
- [Example: Testing PR #5733](#example-testing-pr-5733)

## Prerequisites

### Required Software

**Git for Windows**
```powershell
# Download and install from: https://git-scm.com/download/win
# Or install via winget:
winget install --id Git.Git -e --source winget
```

**GitHub CLI (Recommended)**
```powershell
# Download from: https://cli.github.com/
# Or install via winget:
winget install --id GitHub.cli -e --source winget

# After installation, authenticate:
gh auth login
```

**Visual Studio Code**
```powershell
# Download from: https://code.visualstudio.com/
# Or install via winget:
winget install -e --id Microsoft.VisualStudioCode
```

**VS Code Extensions**
- GitHub Copilot
- GitHub Copilot Chat
- C# Dev Kit (for .NET development)

### Verify Installation

```powershell
# Verify Git
git --version

# Verify GitHub CLI
gh --version

# Verify VS Code
code --version
```

## Quick Start Commands

### Clone and Test a PR (GitHub CLI - Easiest Method)

```powershell
# Clone the repository
gh repo clone AzureAD/microsoft-authentication-library-for-dotnet
cd microsoft-authentication-library-for-dotnet

# Checkout a specific PR (e.g., PR #5733)
gh pr checkout 5733

# Open in VS Code
code .
```

### Clone and Test a PR (Plain Git Method)

```powershell
# Clone the repository
git clone https://github.com/AzureAD/microsoft-authentication-library-for-dotnet.git
cd microsoft-authentication-library-for-dotnet

# Fetch the PR branch
git fetch origin pull/5733/head:pr-5733

# Checkout the PR branch
git checkout pr-5733

# Open in VS Code
code .
```

## Step-by-Step Workflow

### 1. Clone the MSAL .NET Repository

Choose your preferred method:

**Option A: Using GitHub CLI (Recommended)**

```powershell
# Clone via GitHub CLI (automatically sets up remotes)
gh repo clone AzureAD/microsoft-authentication-library-for-dotnet
cd microsoft-authentication-library-for-dotnet
```

**Option B: Using Plain Git**

```powershell
# Clone via HTTPS
git clone https://github.com/AzureAD/microsoft-authentication-library-for-dotnet.git
cd microsoft-authentication-library-for-dotnet
```

**Option C: Clone Your Fork (For Contributors)**

```powershell
# Clone your fork (replace YOUR-USERNAME)
git clone https://github.com/YOUR-USERNAME/microsoft-authentication-library-for-dotnet.git
cd microsoft-authentication-library-for-dotnet

# Add upstream remote
git remote add upstream https://github.com/AzureAD/microsoft-authentication-library-for-dotnet.git
```

### 2. Checkout a PR Locally

**Method 1: GitHub CLI (Simplest)**

```powershell
# Checkout PR by number
gh pr checkout 5733

# View PR details
gh pr view 5733

# View PR diff
gh pr diff 5733
```

**Method 2: Plain Git (Manual Fetch)**

```powershell
# Fetch the PR branch (replace 5733 with PR number)
git fetch origin pull/5733/head:pr-5733

# Checkout the fetched branch
git checkout pr-5733

# View the commits
git log --oneline -10

# View changes
git diff origin/main...HEAD
```

**Method 3: From a Fork**

```powershell
# Find the PR author and branch name from the PR page on GitHub
# Example: User 'contributor' has branch 'feature-branch'

# Add contributor's fork as remote (if not already added)
git remote add contributor https://github.com/contributor/microsoft-authentication-library-for-dotnet.git

# Fetch the contributor's branches
git fetch contributor

# Checkout the PR branch
git checkout contributor/feature-branch

# Create a local tracking branch (optional)
git checkout -b pr-5733-local contributor/feature-branch
```

### 3. Open in VS Code

```powershell
# Open the repository in VS Code
code .

# Or open a specific folder within the repo
code .github/skills
```

### 4. Verify PR Checkout

```powershell
# Check current branch
git branch

# View recent commits
git log --oneline -5

# View files changed in PR
git diff origin/main...HEAD --name-only

# View full diff
git diff origin/main...HEAD
```

## Copilot Chat Workspace Setup

### Verify Workspace Grounding

Copilot Chat uses your workspace files to provide context-aware assistance. To verify it's properly grounded:

1. **Open Copilot Chat Panel**
   - Press `Ctrl+Alt+I` (Windows) or use Command Palette (`F1`) → "Copilot Chat: Focus on Chat View"

2. **Check Workspace Context**
   - Type `@workspace` in the chat
   - Ask: `@workspace what files are in the .github directory?`
   - Copilot should reference files from your local workspace

3. **Test Skill File Access**
   - Ask: `@workspace what skills exist in this repository?`
   - Ask: `@workspace show me the msal-mtls-pop-vanilla skill`
   - Copilot should find and reference skill files in `.github/skills/`

4. **Verify PR Branch Context**
   - Ask: `@workspace what changes are in the current branch compared to main?`
   - Copilot should be aware of files modified in the PR

### Enable Copilot Skills (If Available)

If your VS Code has Copilot Skills support:

1. Check for skills in the **Copilot Chat** panel
2. Skills appear as suggestions when typing `@`
3. Repository-defined skills are loaded from `.github/skills/`
4. Test skill activation: `@msal-mtls-pop-vanilla how do I use mTLS PoP?`

### Best Practices for Copilot Chat Testing

- **Use `@workspace` prefix** to ground responses in local repository context
- **Reference specific files** when asking questions: `@workspace look at .github/skills/msal-mtls-pop-vanilla/SKILL.md`
- **Test incremental context**: Start with broad questions, then narrow down
- **Verify code examples**: Ask Copilot to generate code based on PR changes
- **Test multi-file awareness**: Ask questions that require understanding multiple files

## Testing Skills-Related PRs

GitHub Agent Skills are markdown documents with YAML frontmatter stored in `.github/skills/`. They provide Copilot with specialized knowledge.

### Identify Skills Changes

```powershell
# View files changed in .github/skills/
git diff origin/main...HEAD --name-only | findstr ".github\\skills"

# View detailed changes to skill files
git diff origin/main...HEAD -- .github/skills/
```

### Review Skill Structure

A typical skill directory contains:
```
.github/skills/
├── msal-mtls-pop-vanilla/
│   ├── SKILL.md          # Main skill document with YAML frontmatter
│   └── HelperClass.cs    # Optional production helper code
└── README.md             # General skills documentation
```

### Validate YAML Frontmatter

Check that each `SKILL.md` file has proper frontmatter:

```yaml
---
skill_name: msal-mtls-pop-vanilla
version: 1.0.0
description: Provides guidance for acquiring mTLS Proof-of-Possession tokens using MSAL.NET
applies_to:
  - .NET
tags:
  - authentication
  - msal
  - mtls-pop
  - managed-identity
---
```

**Required Fields:**
- `skill_name`: Unique identifier (kebab-case)
- `version`: Semantic version
- `description`: Clear, concise explanation
- `applies_to`: Array of applicable technologies
- `tags`: Relevant keywords for discovery

### Test Skill Content

1. **Open the SKILL.md file**
   ```powershell
   code .github/skills/msal-mtls-pop-vanilla/SKILL.md
   ```

2. **Read through the content**
   - Check for clear explanations
   - Verify code examples compile
   - Ensure proper markdown formatting
   - Validate NuGet package versions
   - Check for proper using statements

3. **Ask Copilot to Use the Skill**
   ```
   @workspace How do I use mTLS PoP with MSAL.NET?
   @workspace Show me an example of System-Assigned Managed Identity with mTLS PoP
   @workspace What NuGet packages do I need for mTLS PoP?
   ```

## Validation Checklists

### Pre-Review Checklist

Before starting your review, verify:

- [ ] PR is checked out locally
- [ ] VS Code is open with the repository root as workspace
- [ ] Copilot Chat is active and responding
- [ ] `@workspace` commands return local file context
- [ ] Git shows correct branch: `git branch`

### Skill Content Checklist

When reviewing skill content (`SKILL.md` files):

#### Structure & Formatting
- [ ] YAML frontmatter is present and valid
- [ ] All required frontmatter fields are populated
- [ ] Markdown formatting is correct (headings, lists, code blocks)
- [ ] Code blocks have language identifiers (```csharp, ```powershell)
- [ ] Links are functional and point to correct resources

#### Technical Accuracy
- [ ] NuGet package versions are correct and up-to-date
- [ ] Using statements are complete
- [ ] Code examples compile without errors
- [ ] API calls use correct method signatures
- [ ] Configuration examples are accurate

#### Completeness
- [ ] Prerequisites are clearly stated
- [ ] Step-by-step instructions are provided
- [ ] Common scenarios are covered
- [ ] Error handling is demonstrated
- [ ] Security best practices are included

#### Copilot Integration
- [ ] Skill content is discoverable by Copilot
- [ ] `@workspace` queries return accurate answers
- [ ] Code generation produces correct patterns
- [ ] Examples can be copy-pasted successfully

### Helper Class Checklist

When reviewing helper classes (`.cs` files):

#### Code Quality
- [ ] Follows MSAL.NET coding conventions
- [ ] Uses `async`/`await` with `ConfigureAwait(false)`
- [ ] Includes `CancellationToken` parameters
- [ ] Implements `IDisposable` properly if needed
- [ ] Uses `ArgumentNullException.ThrowIfNull` for validation

#### Documentation
- [ ] XML documentation comments for public members
- [ ] Clear parameter descriptions
- [ ] Exception documentation
- [ ] Usage examples in comments

#### Production Readiness
- [ ] Error handling is comprehensive
- [ ] No hardcoded secrets or credentials
- [ ] Logging is appropriate
- [ ] Thread-safe where necessary

## Quality Standards

### What "Good" Looks Like

**Excellent Skill Documentation:**
- Clear, concise explanations that Copilot can parse
- Working code examples with full context (all using statements)
- Covers common scenarios and edge cases
- Includes troubleshooting guidance
- Self-contained examples (not dependent on external helpers)
- Proper error handling demonstrated
- Security considerations highlighted

**Effective Copilot Integration:**
- Copilot generates correct code from skill content
- Responses reference specific skill guidance
- Code suggestions follow skill patterns
- Multi-turn conversations maintain context

**Production-Ready Helper Classes:**
- Follow established MSAL.NET patterns
- Comprehensive error handling
- Proper async/await usage
- XML documentation
- Unit testable design

### Red Flags

- ❌ Code examples that don't compile
- ❌ Missing using statements
- ❌ Hardcoded credentials or secrets
- ❌ Incomplete error handling
- ❌ Outdated package versions
- ❌ Missing cancellation token support
- ❌ Broken links or references
- ❌ Copilot generates incorrect code from skill content

## Troubleshooting

### Copilot Not Using Local Context

**Problem:** Copilot responses don't reference local files or PR changes

**Solutions:**
1. Ensure you opened the correct folder in VS Code (workspace root)
   ```powershell
   # Close VS Code, reopen from repository root
   cd C:\path\to\microsoft-authentication-library-for-dotnet
   code .
   ```

2. Reload VS Code Window
   - Press `F1` → Type "Reload Window" → Enter

3. Use explicit `@workspace` prefix
   ```
   @workspace look at .github/skills/ and tell me what skills exist
   ```

4. Check VS Code Workspace Trust
   - You must trust the workspace for Copilot to access files
   - Check bottom-left corner for shield icon

### GitHub CLI Not Found

**Problem:** `gh` command not recognized

**Solutions:**
1. Install GitHub CLI (see [Prerequisites](#prerequisites))

2. Restart terminal after installation
   ```powershell
   # Close and reopen PowerShell
   ```

3. Add to PATH manually (if needed)
   ```powershell
   # Check installation location
   Get-Command gh
   
   # Typical location: C:\Program Files\GitHub CLI\
   ```

4. Use plain Git method instead (see [Method 2](#method-2-plain-git-manual-fetch))

### PR Checkout Fails

**Problem:** `git fetch origin pull/5733/head:pr-5733` fails

**Solutions:**
1. Verify PR number is correct
   ```powershell
   # Check PR exists on GitHub
   gh pr view 5733
   # Or visit: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/5733
   ```

2. Ensure you have internet connection
   ```powershell
   # Test connection
   Test-NetConnection github.com -Port 443
   ```

3. Check remote configuration
   ```powershell
   git remote -v
   # Should show: origin  https://github.com/AzureAD/microsoft-authentication-library-for-dotnet.git
   ```

4. Use alternative fetch syntax
   ```powershell
   # Fetch all PRs
   git fetch origin "+refs/pull/*/head:refs/remotes/origin/pr/*"
   
   # Checkout specific PR
   git checkout origin/pr/5733
   ```

### Skill Not Recognized by Copilot

**Problem:** Copilot doesn't seem to know about skill content

**Solutions:**
1. Verify skill file location
   ```powershell
   # Skills must be in .github/skills/ directory
   ls .github/skills/
   ```

2. Check YAML frontmatter syntax
   - Must start with `---`
   - Must end with `---`
   - Must be valid YAML

3. Reload VS Code
   - Press `F1` → "Reload Window"

4. Explicitly reference the skill file
   ```
   @workspace read .github/skills/msal-mtls-pop-vanilla/SKILL.md and tell me about mTLS PoP
   ```

### Code Examples Don't Compile

**Problem:** Skill code examples have compilation errors

**Solutions:**
1. Check NuGet package versions
   ```powershell
   # View package references
   findstr /s "Microsoft.Identity.Client" *.csproj
   ```

2. Verify using statements are complete
   ```csharp
   using Microsoft.Identity.Client;
   using Microsoft.Identity.Client.AppConfig;
   using System.Net.Http;
   using System.Net.Http.Headers;
   ```

3. Test compile the example
   - Create a test console app
   - Copy skill code example
   - Attempt to compile
   - Document any missing dependencies

### Permission Denied When Cloning

**Problem:** Git clone fails with authentication error

**Solutions:**
1. Configure Git credentials
   ```powershell
   # For HTTPS (will prompt for credentials)
   git config --global credential.helper wincred
   ```

2. Use GitHub CLI authentication
   ```powershell
   gh auth login
   # Follow interactive prompts
   ```

3. Use SSH instead of HTTPS (if configured)
   ```powershell
   git clone git@github.com:AzureAD/microsoft-authentication-library-for-dotnet.git
   ```

## Example: Testing PR #5733

This section provides a complete walkthrough using PR #5733 as an example.

### Step 1: Setup

```powershell
# Navigate to your projects folder
cd C:\Projects

# Clone the repository
gh repo clone AzureAD/microsoft-authentication-library-for-dotnet

# Navigate into the repository
cd microsoft-authentication-library-for-dotnet

# Checkout PR #5733
gh pr checkout 5733

# View PR details
gh pr view 5733

# Open in VS Code
code .
```

### Step 2: Identify Changes

```powershell
# View files changed in this PR
git diff origin/main...HEAD --name-only

# Focus on skills changes
git diff origin/main...HEAD --name-only | findstr ".github\\skills"

# View detailed skill changes
git diff origin/main...HEAD -- .github/skills/
```

### Step 3: Review Skill Content

Open the changed skill files in VS Code:
- Navigate to `.github/skills/` in Explorer
- Open `SKILL.md` files
- Review YAML frontmatter
- Read through content sections
- Check code examples

### Step 4: Test with Copilot Chat

Ask targeted questions to verify Copilot can use the skill:

```
@workspace What skills are added or modified in this PR?

@workspace How do I use mTLS PoP with System-Assigned Managed Identity?

@workspace Show me a complete code example for using mTLS PoP with MSAL.NET

@workspace What NuGet packages do I need for mTLS PoP?

@workspace What version of MSAL.NET is required for mTLS PoP?

@workspace How do I call Microsoft Graph API with mTLS PoP tokens?
```

### Step 5: Validate Code Examples

Copy code examples from the skill to a test file:

1. Create test console app (optional):
   ```powershell
   dotnet new console -n MtlsPopTest
   cd MtlsPopTest
   ```

2. Add NuGet packages from skill:
   ```powershell
   dotnet add package Microsoft.Identity.Client --version 4.82.1
   ```

3. Copy skill code example into `Program.cs`

4. Verify it compiles:
   ```powershell
   dotnet build
   ```

### Step 6: Provide Feedback

Based on your testing, provide feedback on the PR:

**✅ Good to Merge:**
- Skill content is accurate and complete
- Copilot generates correct code from skill
- Code examples compile successfully
- Documentation is clear and helpful

**🔄 Needs Changes:**
- Missing using statements
- Outdated package versions
- Code examples don't compile
- Incomplete explanations
- Copilot generates incorrect code

**💬 Leave PR Comment:**
Navigate to the PR on GitHub and comment on specific files or lines with your findings.

## Quick Reference

### Essential Commands

```powershell
# Clone repository
gh repo clone AzureAD/microsoft-authentication-library-for-dotnet

# Checkout PR
gh pr checkout <PR_NUMBER>

# View PR info
gh pr view <PR_NUMBER>

# View PR diff
gh pr diff <PR_NUMBER>

# View changed files
git diff origin/main...HEAD --name-only

# Open in VS Code
code .

# Check current branch
git branch

# View git status
git status

# Return to main branch
git checkout main

# Update main branch
git pull origin main
```

### Copilot Chat Commands

```
# General workspace queries
@workspace what files are in this repository?
@workspace what skills exist?
@workspace what changes are in this branch?

# Skill-specific queries
@workspace how do I use mTLS PoP?
@workspace show me an example of managed identity authentication
@workspace what NuGet packages do I need?

# Code generation
@workspace generate code for acquiring mTLS PoP token with system-assigned managed identity
@workspace create a helper class for calling Graph API with PoP tokens
```

### File Paths to Check

```
.github/skills/                           # Skills directory
.github/skills/<skill-name>/SKILL.md      # Main skill document
.github/skills/<skill-name>/*.cs          # Helper classes
.github/skills/README.md                  # Skills overview
```

## Additional Resources

- [MSAL.NET Documentation](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)
- [GitHub CLI Documentation](https://cli.github.com/manual/)
- [VS Code Copilot Chat](https://code.visualstudio.com/docs/copilot/copilot-chat)
- [Contributing Guide](../CONTRIBUTING.md)
- [GitHub Skills Documentation](../.github/skills/README.md)

## Feedback and Contributions

If you find issues with this guide or have suggestions for improvement:
1. Open an issue in the repository
2. Submit a PR with your improvements
3. Tag it with `documentation` label

---

**Last Updated:** February 2026  
**Maintainers:** MSAL.NET Team
