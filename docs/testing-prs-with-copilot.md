# Testing GitHub PRs Locally with Copilot Chat and Skills

> **Purpose**  
> Comprehensive guide for testing GitHub pull requests locally with Copilot Chat and Skills integration in VS Code. This guide is essential for contributors testing skill-related PRs, code reviewers validating Copilot skill implementations, and developers new to the local PR testing workflow.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start](#quick-start)
3. [Cloning the Repository](#cloning-the-repository)
4. [Checking Out a PR Locally](#checking-out-a-pr-locally)
   - [Method 1: GitHub CLI (Recommended)](#method-1-github-cli-recommended)
   - [Method 2: Plain Git](#method-2-plain-git)
5. [Opening in VS Code](#opening-in-vs-code)
6. [Configuring Copilot Chat](#configuring-copilot-chat)
7. [Testing Skills-Related PRs](#testing-skills-related-prs)
8. [Quality Validation Checklist](#quality-validation-checklist)
9. [Troubleshooting](#troubleshooting)
10. [Quick Reference Commands](#quick-reference-commands)

---

## Prerequisites

Before you begin, ensure you have the following installed on your Windows machine:

### Required Tools

1. **Git for Windows**
   - Download: [https://git-scm.com/download/win](https://git-scm.com/download/win)
   - Verify installation:
     ```bash
     git --version
     ```

2. **GitHub CLI (gh)** *(Recommended but optional)*
   - Download: [https://cli.github.com/](https://cli.github.com/)
   - Verify installation:
     ```bash
     gh --version
     ```
   - Authenticate with GitHub:
     ```bash
     gh auth login
     ```

3. **Visual Studio Code**
   - Download: [https://code.visualstudio.com/](https://code.visualstudio.com/)
   - Verify installation:
     ```bash
     code --version
     ```

4. **GitHub Copilot Extension for VS Code**
   - Install from VS Code Extensions marketplace
   - Search for "GitHub Copilot" and install
   - Ensure you have an active Copilot subscription

5. **.NET SDK** *(For testing MSAL.NET functionality)*
   - Download: [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
   - Verify installation:
     ```bash
     dotnet --version
     ```

---

## Quick Start

**TL;DR** – If you're already familiar with the workflow:

```bash
# Using GitHub CLI (fastest)
gh repo clone AzureAD/microsoft-authentication-library-for-dotnet
cd microsoft-authentication-library-for-dotnet
gh pr checkout 1234
code .

# Using plain Git
git clone https://github.com/AzureAD/microsoft-authentication-library-for-dotnet.git
cd microsoft-authentication-library-for-dotnet
git fetch origin pull/1234/head:pr-1234
git checkout pr-1234
code .
```

Then in VS Code:
1. Open Copilot Chat (Ctrl+Alt+I or Cmd+Alt+I)
2. Verify workspace grounding with: `@workspace What files are in this repository?`
3. Test skill with: `@workspace Show me examples of mTLS PoP authentication`

---

## Cloning the Repository

If you haven't already cloned the MSAL .NET repository:

### Using HTTPS (No authentication required for public repos)

```bash
git clone https://github.com/AzureAD/microsoft-authentication-library-for-dotnet.git
cd microsoft-authentication-library-for-dotnet
```

### Using SSH (If you have SSH keys configured)

```bash
git clone git@github.com:AzureAD/microsoft-authentication-library-for-dotnet.git
cd microsoft-authentication-library-for-dotnet
```

### Using GitHub CLI

```bash
gh repo clone AzureAD/microsoft-authentication-library-for-dotnet
cd microsoft-authentication-library-for-dotnet
```

---

## Checking Out a PR Locally

### Method 1: GitHub CLI (Recommended)

The GitHub CLI provides the simplest way to checkout and work with PRs.

#### Step 1: Checkout the PR

```bash
# Replace 1234 with the actual PR number
gh pr checkout 1234
```

This command automatically:
- Fetches the PR branch
- Creates and switches to a local tracking branch
- Sets up upstream tracking

#### Step 2: Verify the checkout

```bash
# View PR details
gh pr view

# See PR status, checks, and reviews
gh pr checks
```

#### Step 3: Update PR branch (if needed)

```bash
# Pull latest changes from the PR
git pull

# Or sync with the base branch
git fetch origin main:main
git merge main
```

### Method 2: Plain Git

If you don't have GitHub CLI, you can use standard Git commands.

#### Step 1: Fetch the PR branch

```bash
# Replace 1234 with the actual PR number
git fetch origin pull/1234/head:pr-1234
```

This creates a local branch named `pr-1234` from the PR's head commit.

#### Step 2: Checkout the branch

```bash
git checkout pr-1234
```

#### Step 3: Verify the checkout

```bash
# View current branch
git branch

# View recent commits
git log --oneline -5

# View files changed in this PR
git diff main...HEAD --name-only
```

#### Step 4: Update PR branch (if needed)

```bash
# Fetch latest changes
git fetch origin pull/1234/head:pr-1234

# If you have local changes, stash them first
git stash

# Update to latest PR commit
git reset --hard FETCH_HEAD

# Restore local changes if needed
git stash pop
```

### Alternative: Checkout PR Author's Branch

If you know the author and branch name:

```bash
# Add author's fork as a remote (one-time setup)
git remote add author-username https://github.com/author-username/microsoft-authentication-library-for-dotnet.git

# Fetch branches from author's fork
git fetch author-username

# Checkout the PR branch
git checkout author-username/branch-name
```

---

## Opening in VS Code

### Launch VS Code from Terminal

After checking out the PR, open the repository in VS Code:

```bash
# From the repository root directory
code .
```

### Verify Workspace

1. VS Code should open with the repository root as the workspace
2. Check the status bar (bottom-left) to confirm you're on the correct branch
3. Verify the Explorer pane shows the repository structure

### Recommended VS Code Settings

For optimal Copilot Chat experience:

1. **Open Settings** (`Ctrl+,` or `File > Preferences > Settings`)

2. **Enable Workspace Context**
   - Search for "Copilot Chat Context"
   - Ensure "GitHub Copilot Chat: Enable Workspace Context" is checked

3. **Configure File Exclusions** (Optional)
   - Search for "Files: Exclude"
   - Add patterns for files to exclude from Copilot context (e.g., `**/bin/**`, `**/obj/**`)

---

## Configuring Copilot Chat

### Verify Copilot Chat Installation

1. Open the Extensions view (`Ctrl+Shift+X`)
2. Search for "GitHub Copilot" and "GitHub Copilot Chat"
3. Ensure both extensions are installed and enabled
4. Sign in to GitHub Copilot if prompted

### Open Copilot Chat

**Primary Chat** (Full workspace context):
- Press `Ctrl+Alt+I` (Windows/Linux) or `Cmd+Alt+I` (Mac)
- Or click the Copilot Chat icon in the Activity Bar (left sidebar)

**Inline Chat** (File context):
- Press `Ctrl+I` (Windows/Linux) or `Cmd+I` (Mac)
- Or right-click in editor and select "Copilot > Start Inline Chat"

### Verify Workspace Grounding

It's critical to verify that Copilot Chat is using the local repository context. Test with these prompts:

#### Test 1: Repository Structure

```
@workspace What files are in this repository?
```

**Expected Response**: Copilot should list files and directories from the MSAL .NET repository structure (e.g., `src/`, `tests/`, `docs/`, `CONTRIBUTING.md`).

#### Test 2: Code Understanding

```
@workspace Where is the main MSAL client library code located?
```

**Expected Response**: Copilot should identify `src/client/Microsoft.Identity.Client/` or similar paths from the actual repository.

#### Test 3: PR-Specific Content

```
@workspace What changes were made in this branch compared to main?
```

**Expected Response**: Copilot should describe the changes specific to the PR you checked out.

### Troubleshooting Workspace Grounding

If Copilot gives generic responses instead of repository-specific answers:

1. **Reload VS Code Window**
   - Press `Ctrl+Shift+P` → Type "Reload Window" → Press Enter

2. **Verify Workspace Folder**
   - `File > Open Folder` → Select the repository root directory
   - Ensure you're not in a parent or subdirectory

3. **Clear Copilot Cache**
   - `Ctrl+Shift+P` → Type "Developer: Reload Window"
   - If issues persist, try `Ctrl+Shift+P` → "Developer: Reload Window with Extensions Disabled" then re-enable

4. **Check .gitignore and VS Code Settings**
   - Ensure important directories (like `.github/skills/`) are not excluded from workspace indexing

---

## Testing Skills-Related PRs

GitHub Copilot Skills in MSAL .NET are located in `.github/skills/` directory. Each skill contains:
- `SKILL.md` – Main skill documentation with YAML frontmatter
- Helper classes (`.cs` files) – Production-ready code examples
- `README.md` – Generic skill overview

### Skills Testing Checklist

Use this checklist when testing skill-related PRs:

#### 1. **Skill Discovery**

Test whether Copilot can discover the skill:

```
@workspace What Copilot skills are available in this repository?
```

**What to verify**:
- ✅ Copilot lists the new or modified skill
- ✅ Skill name matches the YAML frontmatter `skill_name`
- ✅ Description is clear and accurate

#### 2. **Skill Content Understanding**

Test whether Copilot understands the skill content:

```
@workspace Explain how to use [skill name] for [specific scenario]
```

**Example**:
```
@workspace Explain how to use mTLS PoP for acquiring tokens with Managed Identity
```

**What to verify**:
- ✅ Response references the skill documentation
- ✅ Code examples are accurate and match `SKILL.md`
- ✅ Prerequisites and MSAL version requirements are mentioned
- ✅ No hallucinations or outdated information

#### 3. **Code Generation from Skill**

Test whether Copilot can generate working code based on the skill:

```
@workspace Generate a complete example of [specific use case from skill]
```

**Example**:
```
@workspace Generate a complete example of acquiring an mTLS PoP token using UAMI with client ID
```

**What to verify**:
- ✅ Generated code compiles without errors
- ✅ All necessary `using` statements are included
- ✅ Code follows patterns from skill examples
- ✅ Correct MSAL APIs are used (e.g., `WithMtlsProofOfPossession()`)
- ✅ Helper classes are used appropriately (if referenced in skill)

#### 4. **Skill Accuracy and Completeness**

Manually review the skill documentation:

**YAML Frontmatter**:
- ✅ `skill_name` is unique and descriptive
- ✅ `version` is appropriate (e.g., `1.0.0` for new skills)
- ✅ `description` clearly explains the skill's purpose
- ✅ `applies_to` tags are accurate (e.g., `["msal-dotnet", "authentication"]`)
- ✅ `tags` are relevant and searchable

**Content Quality**:
- ✅ Prerequisites are clearly stated
- ✅ MSAL version requirements are specified
- ✅ Code examples are complete and self-contained
- ✅ All `using` statements are included in examples
- ✅ No dependencies on external helper classes in examples
- ✅ Examples use production-ready patterns (async/await, ConfigureAwait(false), etc.)
- ✅ Error handling is demonstrated
- ✅ Multiple scenarios are covered (e.g., different Managed Identity types)

**Helper Classes** (if included):
- ✅ Follow MSAL.NET coding conventions
- ✅ Include proper XML documentation comments
- ✅ Implement `IDisposable` correctly if managing resources
- ✅ Use `ConfigureAwait(false)` for async operations
- ✅ Include input validation (e.g., `ArgumentNullException.ThrowIfNull`)

#### 5. **Integration Testing**

Test the skill in realistic scenarios:

```
@workspace I need to authenticate a backend service using Managed Identity and call Microsoft Graph API. Show me how.
```

**What to verify**:
- ✅ Copilot recommends the appropriate skill
- ✅ Generated code uses skill patterns correctly
- ✅ Copilot adapts examples to the specific scenario
- ✅ Response includes relevant troubleshooting tips

#### 6. **Error Scenario Handling**

Test whether the skill addresses common errors:

```
@workspace What should I do if I get a "certificate not found" error when using mTLS PoP?
```

**What to verify**:
- ✅ Copilot references troubleshooting guidance from the skill
- ✅ Common errors are documented in `SKILL.md`
- ✅ Solutions are actionable and correct

#### 7. **Version Compatibility**

Test whether version requirements are enforced:

```
@workspace Can I use mTLS PoP with MSAL.NET version 4.60.0?
```

**What to verify**:
- ✅ Copilot correctly states minimum version requirements
- ✅ Version compatibility is clearly documented in skill

### Example Testing Prompts

Here are copy-paste ready prompts for common skill testing scenarios:

**Skill Discovery**:
```
@workspace List all GitHub Agent Skills in this repository
@workspace What authentication skills are available?
```

**MSAL mTLS PoP Skills**:
```
@workspace Show me how to acquire an mTLS PoP token using System-Assigned Managed Identity
@workspace How do I use mTLS PoP with Confidential Client?
@workspace Generate code for FIC two-leg flow with mTLS PoP
@workspace What's the difference between vanilla flow and FIC two-leg flow for mTLS PoP?
```

**Code Quality Verification**:
```
@workspace Review the code examples in .github/skills/[skill-name]/SKILL.md for correctness
@workspace Are all required using statements included in the skill examples?
@workspace Check if the skill examples follow MSAL.NET best practices
```

**Cross-Skill Testing**:
```
@workspace Compare the mTLS PoP vanilla flow skill with the FIC two-leg flow skill
@workspace When should I use each mTLS PoP skill?
```

---

## Quality Validation Checklist

Before approving a skill-related PR, verify the following:

### Documentation Quality

- [ ] **Clarity**: Skill documentation is clear and easy to understand
- [ ] **Completeness**: All use cases are covered with examples
- [ ] **Accuracy**: Technical information is correct and up-to-date
- [ ] **Formatting**: Markdown is properly formatted (headings, code blocks, lists)
- [ ] **Links**: All links are valid and point to correct resources

### Code Quality

- [ ] **Compilation**: All code examples compile without errors
- [ ] **Completeness**: Examples are self-contained with all necessary imports
- [ ] **Best Practices**: Code follows MSAL.NET conventions
  - [ ] `async`/`await` with `ConfigureAwait(false)`
  - [ ] `CancellationToken` parameters with defaults
  - [ ] Proper `IDisposable` implementation
  - [ ] Input validation (e.g., `ArgumentNullException.ThrowIfNull`)
- [ ] **Readability**: Code is well-formatted and commented where necessary

### Copilot Integration

- [ ] **YAML Frontmatter**: Valid and complete
- [ ] **Discoverability**: Copilot can find and reference the skill
- [ ] **Context Awareness**: Copilot uses skill content in responses
- [ ] **Code Generation**: Copilot generates correct code based on skill

### Testing

- [ ] **Manual Testing**: Code examples have been tested manually
- [ ] **Scenario Coverage**: Common use cases work as expected
- [ ] **Error Handling**: Error scenarios are handled gracefully

### Repository Standards

- [ ] **File Structure**: Follows `.github/skills/` structure
- [ ] **Naming Conventions**: File and directory names follow repository conventions
- [ ] **No Breaking Changes**: Existing skills are not negatively impacted
- [ ] **Git Hygiene**: Commits are clean and well-described

---

## Troubleshooting

### Common Issues and Solutions

#### Issue 1: Copilot Not Finding Skills

**Symptoms**:
- `@workspace What skills are available?` returns generic response
- Copilot doesn't reference skill content in responses

**Solutions**:
1. **Verify Workspace Folder**:
   ```bash
   # Ensure you're in the repository root
   pwd
   ```
   Output should end with `microsoft-authentication-library-for-dotnet`

2. **Reload VS Code Window**:
   - `Ctrl+Shift+P` → "Reload Window"

3. **Check .github Directory is Not Excluded**:
   - Open VS Code settings (`Ctrl+,`)
   - Search for "Files: Exclude"
   - Ensure `.github/` is not in the exclusion list

4. **Manually Open Skill File**:
   - Open `.github/skills/[skill-name]/SKILL.md` in VS Code
   - Ask Copilot: `@workspace Explain this skill`

#### Issue 2: PR Checkout Fails

**Symptoms**:
- `gh pr checkout` or `git fetch` returns error
- Branch not found

**Solutions**:
1. **Verify PR Number**:
   ```bash
   # List open PRs
   gh pr list
   ```

2. **Update GitHub CLI**:
   ```bash
   # Check for updates
   gh version
   ```

3. **Check Remote Configuration**:
   ```bash
   # Verify origin is set correctly
   git remote -v
   ```

4. **Manual Fetch**:
   ```bash
   # Force fetch the PR
   git fetch origin +refs/pull/1234/head:refs/remotes/origin/pr-1234
   git checkout origin/pr-1234
   ```

#### Issue 3: Copilot Gives Outdated Information

**Symptoms**:
- Copilot suggests deprecated APIs
- Version requirements are incorrect

**Solutions**:
1. **Check PR Branch**:
   ```bash
   # Ensure you're on the correct PR branch
   git branch
   ```

2. **Pull Latest Changes**:
   ```bash
   # Update PR branch
   git pull
   ```

3. **Explicitly Reference Skill**:
   ```
   @workspace Based on the skill documentation in .github/skills/[skill-name]/SKILL.md, how do I...?
   ```

#### Issue 4: Generated Code Doesn't Compile

**Symptoms**:
- Copilot-generated code has compilation errors
- Missing `using` statements

**Solutions**:
1. **Request Complete Example**:
   ```
   @workspace Generate a COMPLETE example with ALL using statements for [scenario]
   ```

2. **Reference Skill Examples Explicitly**:
   ```
   @workspace Use the exact code pattern from .github/skills/[skill-name]/SKILL.md for [scenario]
   ```

3. **Check MSAL Version**:
   ```bash
   # Verify MSAL.NET version in project
   dotnet list package | grep Microsoft.Identity.Client
   ```

#### Issue 5: Authentication Issues with GitHub CLI

**Symptoms**:
- `gh pr checkout` fails with authentication error

**Solutions**:
1. **Re-authenticate**:
   ```bash
   gh auth logout
   gh auth login
   ```

2. **Use SSH Instead of HTTPS**:
   ```bash
   gh auth login --git-protocol ssh
   ```

3. **Fallback to Plain Git**:
   Use [Method 2: Plain Git](#method-2-plain-git) instead

#### Issue 6: Merge Conflicts in PR Branch

**Symptoms**:
- Git reports merge conflicts
- PR is out of sync with base branch

**Solutions**:
1. **Stash Local Changes**:
   ```bash
   git stash
   ```

2. **Update PR Branch**:
   ```bash
   # Fetch latest PR state
   gh pr checkout 1234
   git pull
   ```

3. **Merge Base Branch** (if PR author hasn't):
   ```bash
   git fetch origin main:main
   git merge main
   # Resolve conflicts if any
   ```

   **Note**: For PRs you're reviewing (not authoring), prefer to notify the PR author to resolve conflicts.

---

## Quick Reference Commands

### Repository Operations

```bash
# Clone repository
gh repo clone AzureAD/microsoft-authentication-library-for-dotnet
# or
git clone https://github.com/AzureAD/microsoft-authentication-library-for-dotnet.git

# Navigate to repository
cd microsoft-authentication-library-for-dotnet

# View repository status
git status

# View current branch
git branch

# View remotes
git remote -v
```

### PR Operations

```bash
# List open PRs
gh pr list

# Checkout PR by number
gh pr checkout 1234

# View PR details
gh pr view

# View PR diff
gh pr diff

# Check PR CI status
gh pr checks

# Fetch PR branch (without GitHub CLI)
git fetch origin pull/1234/head:pr-1234
git checkout pr-1234

# Update PR branch
git pull
```

### VS Code Operations

```bash
# Open VS Code in current directory
code .

# Open specific file
code path/to/file.cs

# Open VS Code and wait for window to close
code --wait .
```

### Copilot Chat Quick Tests

```
# Verify workspace grounding
@workspace What files are in this repository?

# Discover skills
@workspace What Copilot skills are available?

# Test skill understanding
@workspace Explain how to use [skill name]

# Generate code from skill
@workspace Generate code for [scenario] using [skill name]

# Review changes
@workspace What changes were made in this branch?
```

### Git Branch Management

```bash
# List all branches
git branch -a

# Switch branches
git checkout branch-name

# Delete local branch
git branch -d pr-1234

# Force delete local branch
git branch -D pr-1234

# Return to main branch
git checkout main

# Update main branch
git pull origin main
```

### Debugging Commands

```bash
# View recent commits
git log --oneline -10

# View files changed in PR
git diff main...HEAD --name-only

# View detailed diff
git diff main...HEAD

# View specific file changes
git diff main...HEAD -- path/to/file.cs

# Check Git configuration
git config --list
```

---

## Additional Resources

- **MSAL .NET Repository**: [https://github.com/AzureAD/microsoft-authentication-library-for-dotnet](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)
- **MSAL .NET Documentation**: [https://learn.microsoft.com/en-us/entra/msal/dotnet/](https://learn.microsoft.com/en-us/entra/msal/dotnet/)
- **GitHub Copilot Documentation**: [https://docs.github.com/en/copilot](https://docs.github.com/en/copilot)
- **GitHub CLI Documentation**: [https://cli.github.com/manual/](https://cli.github.com/manual/)
- **Contributing to MSAL .NET**: [CONTRIBUTING.md](../CONTRIBUTING.md)

---

## Feedback and Improvements

If you encounter issues not covered in this guide or have suggestions for improvements:

1. **Open an Issue**: [GitHub Issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/new?template=documentation.md)
2. **Submit a PR**: Improvements to this guide are welcome!
3. **Ask on Stack Overflow**: Tag with `azure-ad-msal`

---

**Happy Testing! 🚀**
