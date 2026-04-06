# 🤖 GitHub Copilot — Complete Agents & Commands Guide (2026)

---

## Table of Contents

1. [What is an Agent?](#1-what-is-an-agent)
2. [Agent Modes in VS Code](#2-agent-modes-in-vs-code)
3. [Slash Commands Reference](#3-slash-commands-reference)
4. [Chat Variables & Context (`#`)](#4-chat-variables--context-)
5. [Chat Participants (`@`)](#5-chat-participants-)
6. [The `/init` Command — Deep Dive](#6-the-init-command--deep-dive)
7. [AGENTS.md — The AI Config File](#7-agentsmd--the-ai-config-file)
8. [Custom Agents](#8-custom-agents)
9. [Copilot CLI — Terminal Agent](#9-copilot-cli--terminal-agent)
10. [Copilot Cloud Agent](#10-copilot-cloud-agent)
11. [MCP Servers Integration](#11-mcp-servers-integration)
12. [Custom Slash Commands (Prompt Files)](#12-custom-slash-commands-prompt-files)
13. [Copilot Memory](#13-copilot-memory)
14. [Keyboard Shortcuts](#14-keyboard-shortcuts)
15. [Best Practices 2026](#15-best-practices-2026)

---

## 1. What is an Agent?

An **agent** in GitHub Copilot is an autonomous AI assistant that works on complex tasks without constant prompting. Unlike inline autocomplete (which just suggests the next lines), an agent:

- Takes a **goal**, not just a prompt
- Breaks the goal into multiple **steps**
- Edits **multiple files** across your project
- Runs **terminal commands** on your behalf
- **Self-corrects** when something goes wrong
- Iterates until the task is complete

> 💡 Think of it as the difference between asking someone "write the next sentence" vs. "build this entire feature."

---

## 2. Agent Modes in VS Code

In VS Code, Copilot Chat supports three distinct modes:

| Mode | When to Use | Control Level |
|------|-------------|---------------|
| **Ask mode** | Questions, explanations, planning | Read-only |
| **Edit mode** | Targeted changes to selected files | You pick files |
| **Agent mode** | Autonomous multi-file tasks, commands | Fully autonomous |

### Switching Modes

- Use the **mode selector** in the Chat input (top of the Chat panel)
- In **Agent mode**, Copilot will ask your approval before running terminal commands
- Use **Edit mode** when you want to remain in control of which files change

---

## 3. Slash Commands Reference

Slash commands are shortcuts for common tasks. Type `/` in the Chat input to see the full list.

### VS Code Chat Slash Commands

| Command | What it does | Example |
|---------|-------------|---------|
| `/init` | Generates custom instructions for your project (AGENTS.md) | `/init` |
| `/explain` | Explains selected code in plain language | `/explain` |
| `/fix` | Analyzes and proposes fixes for bugs or errors | `/fix null pointer exception` |
| `/tests` | Generates unit tests for selected code | `/tests using Jest` |
| `/doc` | Generates documentation/JSDoc/comments | `/doc` |
| `/new` | Creates a new project or file scaffold | `@workspace /new React app with TypeScript` |
| `/clear` | Clears the current chat context | `/clear` |
| `/help` | Shows help and available commands | `/help` |

### Copilot CLI Slash Commands

| Command | What it does |
|---------|-------------|
| `/init` | Creates/updates AGENTS.md for your repo |
| `/model` | Switch or compare AI models |
| `/clear` | Wipes accumulated context |
| `/session` | Manage or share your current session |
| `/resume` | Resume a previous session by ID |
| `/cwd` | Confirm the current working directory scope |
| `/add-dir` | Grant Copilot access to a specific directory |
| `/list-files` | Browse files without navigating complex UIs |
| `/list-dirs` | Browse directories |
| `/usage` | Monitor activity and request usage |
| `/fleet` | Coordinate parallel subagents for the same task |
| `/feedback` | Send feedback to GitHub |
| `/help` | Full help reference |
| `/exit` / `/quit` | End the session cleanly |
| `/experimental show` | Access preview features |
| `/changelog` | See latest CLI updates |

> 💡 **Tip:** If you only remember 3 CLI commands, start with `/clear`, `/cwd`, and `/model`. They give you immediate control over context, scope, and output quality.

---

## 4. Chat Variables & Context (`#`)

Use `#` to inject specific context into any prompt without describing everything manually.

| Variable | What it attaches |
|----------|-----------------|
| `#file` | A specific file from your workspace |
| `#codebase` | Your entire workspace as context |
| `#block` | A selected block of code |
| `#class` | A specific class |
| `#symbol` | A specific function or symbol |
| `#git:staged` | Your staged git changes (perfect for commit messages) |
| `#terminalOutput` | The last terminal output |
| `#selection` | Currently selected text in editor |

**Example prompts using `#`:**
```
/tests #file:src/auth.service.ts using Jest with mocks
/fix #terminalOutput
/doc #class:UserController
Generate a commit message for #git:staged
```

---

## 5. Chat Participants (`@`)

`@` participants are domain-specific experts embedded in Copilot Chat.

| Participant | Domain | Example |
|-------------|--------|---------|
| `@workspace` | Your entire codebase & project | `@workspace /new Express API` |
| `@github` | GitHub.com — issues, PRs, repos | `@github list open issues` |
| `@vscode` | VS Code settings and features | `@vscode how do I enable format on save` |
| `@terminal` | Terminal & shell help | `@terminal how to kill port 3000` |

You can also use **third-party chat participants** installed via VS Code extensions (e.g., database agents, cloud agents).

---

## 6. The `/init` Command — Deep Dive

`/init` is one of the most important commands for agentic development in 2026. It configures your project so every AI interaction is context-aware from the start.

### What `/init` does

1. **Analyzes your codebase** — reads project structure, config files, and existing AI conventions
2. **Discovers existing rules** — finds `.cursor/rules/`, `.cursorrules`, `.github/copilot-instructions.md`
3. **Generates AGENTS.md** — creates or enriches a comprehensive AI configuration file
4. **Documents your project** — build commands, test commands, lint rules, code style, naming conventions

### How to run it

**In VS Code Chat:**
```
/init
```
Copilot will analyze your workspace and generate a `.github/copilot-instructions.md` or `AGENTS.md`.

**In Copilot CLI:**
```bash
copilot init
```

**What gets documented (target ~150 lines):**

| Section | What it captures |
|---------|-----------------|
| **Build commands** | `npm run build`, `make`, etc. |
| **Test commands** | How to run all tests AND single tests |
| **Lint commands** | ESLint, Prettier, etc. |
| **Code style** | Import organization, formatting rules |
| **Types** | TypeScript/type annotation requirements |
| **Naming conventions** | Files, variables, functions, classes |
| **Error handling** | Patterns used in your codebase |

### Best Practice
Run `/init` **once per repository**, at the beginning of a project or when onboarding AI assistance for an existing codebase. Update it when your conventions change.

---

## 7. AGENTS.md — The AI Config File

`AGENTS.md` is a Markdown file that gives AI agents permanent instructions about your project. It is automatically included in every prompt sent within that repository.

### File locations

| Scope | Location |
|-------|----------|
| Repository | `AGENTS.md` (root) or `.github/copilot-instructions.md` |
| Subdirectory | `src/AGENTS.md` (applies only to that folder) |
| User (global) | User-level settings in VS Code |
| Organization | GitHub organization level (auto-detected) |

### Example AGENTS.md structure

```markdown
# Project: My App — Copilot Instructions

## Stack
- Runtime: Node.js 22 / TypeScript 5.4
- Framework: NestJS 10
- Testing: Jest + Supertest
- Linting: ESLint + Prettier

## Build & Test Commands
- Build: `npm run build`
- Run all tests: `npm test`
- Run single test: `npx jest path/to/file.test.ts`
- Lint: `npm run lint`

## Code Style
- Use `async/await`, never `.then()` chains
- Always define return types on public methods
- File naming: `kebab-case.ts`
- Class naming: `PascalCase`

## Error Handling
- Use NestJS `HttpException` for API errors
- Always log errors with the Logger service, never `console.log`

## Important Conventions
- Never commit directly to `main` — always use feature branches
- DTOs must be validated with `class-validator`
```

### Priority rules (when files conflict)

System-level agent > Repository-level agent > Organization-level agent

---

## 8. Custom Agents

Custom agents are specialized versions of Copilot for specific roles. They are defined as `.agent.md` files.

### File locations

| Scope | Path |
|-------|------|
| Repository | `.github/agents/` |
| User | User data folder |
| Organization | Org-level settings |

### Creating a Custom Agent

Create `.github/agents/security-auditor.agent.md`:

```markdown
---
name: security-auditor
description: >
  I am a security expert. I check code files thoroughly for potential
  security issues including exposed credentials, SQL injection, XSS,
  and vulnerable dependencies. Use me with the word "seccheck".
tools:
  - read_file
  - search_codebase
  - create_github_issue
---

# Security Auditor

Review the provided code for:
- Exposed secrets or credentials
- Cross-site scripting (XSS) vulnerabilities
- SQL injection risks
- Authentication bypass possibilities
- Vulnerable dependencies

Create a GitHub issue with findings if any are found.
```

### How agents are invoked

Copilot can **automatically select** the right agent based on your prompt, or you can explicitly call one:

**CLI:**
```bash
copilot --agent=security-auditor --prompt "Check /src/app/validator.go"
```

**VS Code Chat:**
```
Use the security-auditor agent to review #file:src/auth.ts
```

### Built-in default agents (2026)

Copilot CLI ships with specialized built-in agents it delegates tasks to automatically:

- **Frontend engineer** — React, CSS, accessibility
- **Backend engineer** — APIs, databases, auth
- **Test engineer** — unit/integration/e2e tests
- **DevOps** — Docker, CI/CD, infrastructure
- **Security auditor** — vulnerability scanning
- **Documentation writer** — README, JSDoc, API docs

---

## 9. Copilot CLI — Terminal Agent

GitHub Copilot CLI brings the full agent experience to your terminal. Install once, use everywhere.

### Installation

```bash
npm install -g @github/copilot
copilot auth  # authenticate with your GitHub account
```

### Modes

Press **Shift+Tab** to cycle between:

| Mode | Behavior |
|------|----------|
| **Ask/Execute** | Default — prompts and executes tasks |
| **Plan** | Builds a structured implementation plan before acting |
| **Autopilot** | Executes without step-by-step approval |

### Session management

```bash
copilot                           # Start new interactive session
copilot --resume=SESSION-ID       # Resume a previous session
copilot --agent=my-agent --prompt "..."  # One-shot with specific agent
```

### Multi-agent with /fleet

```bash
/fleet run the same refactor across all microservices
```
This runs parallel subagents and converges their work into one result.

### Scope and security

```bash
/add-dir ./src          # Grant access to a specific directory only
/cwd                    # Confirm current scope
```

> ⚠️ **Security reminder:** Every file change and command requires your explicit approval before being applied. Always review before accepting.

---

## 10. Copilot Cloud Agent

The **Copilot cloud agent** runs in GitHub's cloud and works asynchronously — you can kick it off and come back to a pull request when it's done.

### How it works

1. Go to your repository's **Agents tab** (or assign an issue to Copilot)
2. Write your task as a prompt
3. Copilot opens a **new branch** and starts a PR automatically
4. You are added as a reviewer
5. Monitor progress in the **session log**
6. **Steer** the agent mid-task if it goes the wrong direction
7. Review the diff and merge when satisfied

### Tracking sessions

Active sessions can be tracked from:
- GitHub.com (Agents tab)
- VS Code (Open in VS Code button)
- GitHub Mobile
- Copilot CLI (`copilot --resume=SESSION-ID`)
- JetBrains IDEs

### Steering mid-task

```
Use our existing ErrorHandler utility class instead of writing 
custom try-catch blocks for each endpoint.
```
Each steering message uses 1 premium request.

---

## 11. MCP Servers Integration

**Model Context Protocol (MCP)** servers extend Copilot with external tools and data sources.

### Built-in MCP server

Copilot CLI comes pre-configured with the **GitHub MCP server**, giving agents the ability to:
- Read/create/merge pull requests
- Browse and create issues
- Work with branches
- Check CI/CD status

### Adding MCP servers

**In VS Code:**
1. Open Command Palette → `MCP: Add Server`
2. Or edit `.vscode/settings.json`:

```json
{
  "mcp": {
    "servers": {
      "my-database": {
        "type": "stdio",
        "command": "npx",
        "args": ["-y", "@my-org/database-mcp"]
      }
    }
  }
}
```

**In Copilot CLI:**
```bash
copilot mcp add https://my-mcp-server.com/sse
```

### Popular MCP servers (2026 ecosystem)

- **GitHub** (built-in) — PRs, issues, branches
- **Postgres / MySQL** — query databases
- **Filesystem** — file operations beyond the workspace
- **Browser** — web scraping and automation
- **Slack** — team communication
- **Linear / Jira** — project management

---

## 12. Custom Slash Commands (Prompt Files)

Create your own `/commands` using Prompt Files in VS Code.

### Setup

1. Create directory: `.github/prompts/`
2. Create a file: `review.prompt.md`
3. Add YAML frontmatter + instructions:

```markdown
---
description: Perform a security and style review of the selected code.
applyTo: "**/*.ts"
---

# Code Review

Review the provided code for:
- Security vulnerabilities
- Code style violations (check #file:.github/copilot-instructions.md)
- Performance issues
- Missing error handling

Provide specific, actionable feedback with line references.
```

4. Now use it as: `/review #file:src/auth.ts`

### Global prompt files (available in all projects)

Open Command Palette → `Chat: Configure Prompt Files` → choose **User Profile**.

### Real-world example: Auto commit messages

Create `.github/prompts/gcm.prompt.md`:

```markdown
---
description: Generate a semantic git commit message from staged changes.
---

# Git Commit Message Generator

Analyze the staged changes in the context.
Generate a commit message following Conventional Commits:
- Format: `type(scope): description`
- Types: feat, fix, docs, style, refactor, test, chore
- Keep the title under 72 characters
- Add a body if the change is complex

Context: {{context}}
```

Use with: `/gcm #git:staged`

---

## 13. Copilot Memory

Copilot Memory allows the agent to **build a persistent understanding** of your repository across sessions.

- Stores "memories" — coding conventions, patterns, preferences
- Deduced as Copilot works over time
- Reduces the need to repeatedly explain the same context
- Used by Copilot cloud agent and code review features
- Stored per-repository

> 💡 This is why running `/init` early matters — it seeds memory with accurate project context from day one.

---

## 14. Keyboard Shortcuts

| Action | Mac | Windows / Linux |
|--------|-----|----------------|
| Open Chat panel | `Ctrl+Cmd+I` | `Ctrl+Alt+I` |
| Inline Chat (in editor) | `Cmd+I` | `Ctrl+I` |
| Accept inline suggestion | `Tab` | `Tab` |
| Dismiss inline suggestion | `Esc` | `Esc` |
| Next suggestion | `Alt+]` | `Alt+]` |
| Previous suggestion | `Alt+[` | `Alt+[` |
| Open Chat Customizations | Command Palette → `Chat: Open Chat Customizations` | same |
| Switch agent mode | Mode selector in Chat panel | same |

---

## 15. Best Practices 2026

### 🚀 Getting started with a new project
1. Open your repo in VS Code
2. Run `/init` to generate AGENTS.md with project conventions
3. Review and customize the generated file
4. Commit it to source control

### 🛡️ Security
- Grant CLI/agent file access **only to needed directories** using `/add-dir`
- Never expose API keys or secrets in prompts or AGENTS.md
- Always review suggested commands **before approving**
- Enable GitHub **secret scanning** for AI-generated code
- Require CI checks on all AI-generated PRs

### 🔁 Daily workflow
- Use `/clear` when switching between tasks to reset context
- Prefer `/tests`, `/fix`, and `/doc` over writing custom prompts for repetitive tasks
- Use `#git:staged` context for commit message generation
- Use **Agent mode** for complex, multi-file features; **Edit mode** for targeted changes
- Use `Shift+Tab` in CLI to enter **Plan mode** before big tasks

### 🧠 Getting better results
- Be specific: `"Add input validation to the register endpoint using class-validator"` > `"Fix the register function"`
- Narrow context with `#file:path/to/file.ts` instead of dumping everything
- Use `@workspace` for cross-project questions
- Chain commands: use `/explain` to understand code first, then `/refactor`
- Treat all suggestions as **drafts** — always run tests before merging

### 📁 File organization for AI
```
.github/
  copilot-instructions.md    # Global instructions
  prompts/                   # Custom slash commands
    review.prompt.md
    gcm.prompt.md
  agents/                    # Custom agents
    security-auditor.agent.md
    frontend-expert.agent.md
AGENTS.md                    # Root-level agent instructions
```

---

## Quick Reference Card

```
SETUP
  /init                → Generate AGENTS.md for your project
  copilot init         → Same, from the terminal

CODING
  /explain             → Explain selected code
  /fix                 → Fix bugs/errors
  /tests               → Generate unit tests
  /doc                 → Generate documentation

NAVIGATION
  @workspace           → Ask about your whole codebase
  @github              → Interact with GitHub issues/PRs
  #file:path           → Attach a file as context
  #git:staged          → Attach staged changes

CLI CONTROL
  /clear               → Reset context
  /model               → Switch AI model
  /cwd                 → Check working directory
  /fleet               → Run parallel subagents
  /resume=ID           → Resume a previous session

AGENTS
  --agent=NAME         → Use a specific custom agent
  Shift+Tab            → Switch CLI modes (ask/plan/autopilot)
```

---

*Guide based on official GitHub Copilot documentation — April 2026*
