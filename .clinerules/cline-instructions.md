# Cline AI Assistant Guidelines

## Core Principles

* Make changes incrementally and verify each step
* Always analyze existing code patterns before making changes
* Prioritize built-in tools over shell commands
* Follow existing project patterns and conventions
* Maintain comprehensive test coverage

## Tool Usage

### File Operations
* Use `read_file` for examining file contents instead of shell commands like `cat`
* Use `replace_in_file` for targeted, specific changes to existing files
* Use `write_to_file` only for new files or complete file rewrites
* Use `list_files` to explore directory structures
* Use `search_files` with precise regex patterns to find code patterns
* Use `list_code_definition_names` to understand code structure before modifications

### Command Execution
* Use `execute_command` sparingly, preferring built-in file operation tools when possible
* Always provide clear explanations for any executed commands
* Set `requires_approval` to true for potentially impactful operations

## Development Workflow

### Planning Phase (PLAN MODE)
* Begin complex tasks in PLAN mode to discuss approach
* Analyze existing codebase patterns using search tools
* Review related test files to understand testing patterns
* Present clear implementation steps for approval
* Ask clarifying questions early to avoid rework

### Implementation Phase (ACT MODE)
* Make changes incrementally, one file at a time
* Verify each change before proceeding
* Follow patterns discovered during planning phase
* Focus on maintaining test coverage
* Use error messages and linter feedback to guide fixes

## Code Modifications

### General Guidelines
* Follow .editorconfig rules strictly
* Preserve file headers and license information
* Maintain consistent XML documentation
* Respect existing error handling patterns
* Keep line endings consistent with existing files

### Quality Checks
* Verify changes match existing code style
* Ensure test coverage for new code
* Validate changes against project conventions
* Check for proper error handling
* Maintain nullable reference type annotations

## MCP Server Integration

* Use appropriate MCP tools when available for specialized tasks
* Access MCP resources efficiently using proper URIs
* Handle MCP operation results appropriately
* Follow server-specific authentication and usage patterns

## Error Handling

* Provide clear error messages and suggestions
* Handle tool operation failures gracefully
* Suggest alternative approaches when primary approach fails
* Roll back changes if necessary to maintain stability
