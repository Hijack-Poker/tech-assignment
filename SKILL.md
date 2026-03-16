---
name: project-bootstrap
description: Bootstrap an agentic development workflow for a new project. Use when starting a new project, setting up Claude Code workflow, creating project skills, or when asked to "bootstrap", "setup workflow", or "init project". Generates .claude/ config, skills, hooks, memory bank, and decision log — all customized to the project.
---

# Project Bootstrap

This skill sets up a complete agentic development workflow for a new project. It generates all the `.claude/`, `memory-bank/`, and `decisions/` artifacts needed for orchestrator-driven development with worktree-isolated agents.

## When to Use

- Starting a new project from scratch
- Adding agentic workflow to an existing project that lacks `.claude/` setup
- User says "bootstrap", "setup workflow", "init project", or similar

## Interactive Flow

Follow these steps in order. Use AskUserQuestion for each phase. Skip questions already answered by a PRD.

### Step 1: Check for PRD

Ask the user:
> Do you have a PRD, requirements doc, or project brief? If so, provide the file path or paste it.

If provided:
- Read the document
- Extract: project name, goals, requirements, deadlines, stack hints, module structure, target users
- Use extracted info to pre-fill subsequent questions and memory bank files
- Confirm what you extracted before proceeding

If not provided: proceed to manual Q&A.

### Step 2: Gather Project Info

Collect the following. Use sensible defaults. Skip what the PRD already answered.

**Identity**
- Project name (used in CLAUDE.md header, package name, commit scopes)
- One-line description
- GitHub repo URL or name (optional)

**Stack** (light touch — the workflow is language-agnostic)
- Primary language and version (e.g., Python 3.12, TypeScript 5.x, Go 1.22, Rust)
- Package manager (e.g., uv, npm, pnpm, cargo, go modules)
- Three quality gate commands that must pass before any merge:
  - Lint command (e.g., `uv run ruff check .`, `npm run lint`, `cargo clippy`)
  - Type check command (e.g., `uv run pyright`, `npx tsc --noEmit`, skip if N/A)
  - Test command (e.g., `uv run pytest`, `npm test`, `cargo test`)

**Architecture**
- Primary modules/packages — these become commit scopes (e.g., `api`, `auth`, `db`, `ui`, `config`, `ci`, `deps`)
- Key external services/APIs (databases, LLM providers, storage, etc.)
- Deployment target (Railway, Vercel, AWS, Docker, local only, etc.)

**Workflow Preferences** (all default to yes)
- TDD workflow? (red-green-refactor)
- Worktree isolation for agents?
- Memory bank for cross-session persistence?
- Decision log for architectural decisions?

### Step 3: Confirm and Generate

Present the complete file list with a one-line summary of each. Wait for user confirmation before generating.

After confirmation:
1. Generate all files using the Write tool
2. Make hooks executable: `chmod +x .claude/hooks/*.sh`
3. Offer to `git add .claude/ memory-bank/ decisions/ && git commit -m "chore: bootstrap agentic development workflow"`

---

## File Specifications

Below are the specifications for every file this skill generates. Use the collected project info to customize each file. Do NOT use rigid `{{placeholder}}` templating — generate natural, project-specific content.

---

### `.claude/CLAUDE.md` — Master Rules

Generate a file with this structure, customized to the project:

```markdown
# {PROJECT_NAME} — Development Rules

## Dev Cycle

Plan -> Tasks -> Agents -> Review -> Merge

1. **Plan**: Decompose work into discrete tasks using TaskCreate/TaskUpdate.
2. **Tasks**: Each task = one agent in an isolated worktree.
3. **Agents**: Dispatch via Agent tool. Parallel when independent, sequential when dependent.
4. **Review**: Verify each agent's output: all quality gates must pass.
5. **Merge**: Auto-merge worktrees when all checks pass. Stop and review on failure.

## Core Rules

- **Delegate, don't implement.** The orchestrator plans, decomposes, dispatches, and reviews. It does not write application code directly unless the fix is trivial (< 5 lines). All coding is done by spawned agents.
- **Use {PACKAGE_MANAGER} for everything.** Never use pip/npm-global/other direct installers.
- **Conventional commits.** `type(scope): description`. See commit skill.
- **{TDD_RULE if opted in}** Red-green-refactor. Write failing test first, then implement, then clean up. See tdd-workflow skill.
- **Worktrees for isolation.** Each agent gets its own git worktree. Never edit the main working tree directly from an agent.
- **Reference skills in agent prompts.** Always name relevant skills when dispatching agents so they load automatically.
- **Memory bank.** Read all `memory-bank/` files at session start. Update after completing significant work. See memory-bank skill.
- **Decision log.** Record significant architectural and technical decisions in `decisions/`. See decision-log skill.
- **Plan persistence.** All plans must be written to `memory-bank/plans/` with descriptive filenames. Reference the active plan in `memory-bank/active-context.md`.
- **Context management.** The context-guard hook warns at 58% usage. When warned, follow the Context Handoff Protocol: finish current atomic work, write continuation state to `memory-bank/active-context.md`, save the plan to `memory-bank/plans/`, and tell the user to clear context.

## Auto-Merge Conditions

Merge an agent's worktree when ALL pass:
1. `{LINT_COMMAND}` -- zero violations
2. `{TYPECHECK_COMMAND}` -- zero errors (skip if N/A)
3. `{TEST_COMMAND}` -- all tests pass

If any fails, the agent must fix or escalate to the orchestrator.

## Skills Reference

| Skill | Purpose |
|-------|---------|
| orchestration | Agent dispatch, worktree workflow, task management |
| commit | Conventional commit format and pre-commit checklist |
| pr | Branch naming, PR template, review checklist |
| tdd-workflow | Red-green-refactor TDD cycle |
| code-quality | Lint, typecheck, test config and standards |
| memory-bank | Session start protocol, knowledge persistence |
| decision-log | Architecture decision records |
| {DOMAIN_SKILLS} | {if any domain-specific skills are relevant} |
```

---

### `.claude/settings.json` — Permissions and Hooks

Generate a settings file with permissions customized to the project's stack. Structure:

```json
{
  "permissions": {
    "allow": [
      "Bash({PACKAGE_MANAGER} *)",
      "Bash({LINT_TOOL} *)",
      "Bash({TYPECHECK_TOOL} *)",
      "Bash({TEST_TOOL} *)",
      "Bash(git status*)",
      "Bash(git log*)",
      "Bash(git diff*)",
      "Bash(git branch*)",
      "Bash(git checkout*)",
      "Bash(git switch*)",
      "Bash(git merge*)",
      "Bash(git add*)",
      "Bash(git commit*)",
      "Bash(git stash*)",
      "Bash(git worktree*)",
      "Bash(git remote*)",
      "Bash(git fetch*)",
      "Bash(git pull*)",
      "Bash(git push *)",
      "Bash(git rev-parse*)",
      "Bash(git show*)",
      "Bash(git tag*)",
      "Bash(git config --get*)",
      "Bash(gh *)",
      "Bash(ls *)",
      "Bash(ls)",
      "Bash(cat *)",
      "Bash(head *)",
      "Bash(tail *)",
      "Bash(echo *)",
      "Bash(mkdir *)",
      "Bash(cd *)",
      "Bash(pwd)",
      "Bash(which *)",
      "Bash(env)",
      "Bash(grep *)",
      "Bash(find *)",
      "Bash(wc *)",
      "Bash(sort *)",
      "Bash(tree *)",
      "Bash(chmod *)",
      "Bash(touch *)",
      "Bash(cp *)",
      "Bash(mv *)",
      "Bash(jq *)",
      "Bash(date *)",
      "Bash(realpath *)",
      "Bash(dirname *)",
      "Bash(basename *)",
      "Read",
      "Edit",
      "Write",
      "Glob",
      "Grep",
      "WebFetch",
      "WebSearch"
    ],
    "deny": [
      "Bash(git push --force*)",
      "Bash(git push -f *)",
      "Bash(git reset --hard*)",
      "Bash(git clean -f*)",
      "Bash(git branch -D main)",
      "Bash(git branch -D master)",
      "Bash(rm -rf /)",
      "Bash(rm -rf ~*)",
      "Bash(rm -rf .*)",
      "Bash(rm -rf .git*)"
    ]
  },
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "\"$CLAUDE_PROJECT_DIR\"/.claude/hooks/protect-env.sh"
          }
        ]
      },
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "\"$CLAUDE_PROJECT_DIR\"/.claude/hooks/block-destructive-git.sh"
          }
        ]
      }
    ]
  }
}
```

Add stack-specific permissions to the allow list. For example:
- Python: `Bash(python *)`, `Bash(python3 *)`, cache cleanup `Bash(rm *.pyc)`, `Bash(rm -rf __pycache__*)`, `Bash(rm -rf .pytest_cache*)`, `Bash(rm -rf .ruff_cache*)`
- Node: `Bash(node *)`, `Bash(npx *)`
- Go: `Bash(go *)`
- Rust: `Bash(cargo *)`

---

### `.claude/settings.local.json` — Local Overrides

```json
{
  "permissions": {
    "allow": [
      "Bash({PACKAGE_MANAGER_RUN}:*)",
      "Bash(git:*)"
    ]
  }
}
```

---

### `.claude/hooks/protect-env.sh` — Block Writes to Secrets

Generate this file VERBATIM:

```bash
#!/bin/bash
# protect-env.sh -- Block writes to .env files and other secret files
# PreToolUse hook for Edit|Write tools
# Exit 0 = allow, Exit 2 = block (stderr sent to Claude as feedback)

INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty')

if [ -z "$FILE_PATH" ]; then
  exit 0
fi

BASENAME=$(basename "$FILE_PATH")

PROTECTED_PATTERNS=(".env" ".env.local" ".env.production" ".env.staging" "credentials" "secrets")

for pattern in "${PROTECTED_PATTERNS[@]}"; do
  if [[ "$BASENAME" == *"$pattern"* ]]; then
    echo "BLOCKED: Cannot write to '$FILE_PATH' -- matches protected pattern '$pattern'. Secret files must be managed manually." >&2
    exit 2
  fi
done

exit 0
```

---

### `.claude/hooks/block-destructive-git.sh` — Block Dangerous Git Commands

Generate this file VERBATIM:

```bash
#!/bin/bash
# block-destructive-git.sh -- Block destructive git commands
# PreToolUse hook for Bash tool
# Exit 0 = allow, Exit 2 = block (stderr sent to Claude as feedback)

INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

if [ -z "$COMMAND" ]; then
  exit 0
fi

# Block: git push --force / -f
if echo "$COMMAND" | grep -qE 'git\s+push\s+.*(-f|--force)'; then
  echo "BLOCKED: Force push is not allowed. Use regular 'git push' instead." >&2
  exit 2
fi

# Block: git reset --hard
if echo "$COMMAND" | grep -qE 'git\s+reset\s+--hard'; then
  echo "BLOCKED: 'git reset --hard' is not allowed. Use 'git stash' or 'git checkout -- <file>' for targeted changes." >&2
  exit 2
fi

# Block: git clean -f
if echo "$COMMAND" | grep -qE 'git\s+clean\s+-[a-zA-Z]*f'; then
  echo "BLOCKED: 'git clean -f' is not allowed. Remove untracked files manually." >&2
  exit 2
fi

# Block: git branch -D on main/master
if echo "$COMMAND" | grep -qE 'git\s+branch\s+-D\s+(main|master)'; then
  echo "BLOCKED: Cannot delete the main/master branch." >&2
  exit 2
fi

exit 0
```

---

### `.claude/skills/orchestration/SKILL.md` — Agent Orchestration

Generate with this structure, customized to the project:

```markdown
---
name: orchestration
description: Agent orchestration patterns for {PROJECT_NAME}. Use when starting work on features, planning task decomposition, dispatching agents, coordinating parallel work, managing worktrees, reviewing agent output, or handling agent failures.
---

# Orchestration Patterns

## Feature Decomposition

1. **Identify layers.** Most features touch: data model, business logic, UI/API, tests.
2. **Identify dependencies.** Which tasks must complete before others can start?
3. **Size tasks.** Each task = one agent session. If description exceeds 3 paragraphs, split further.
4. **Create tasks.** Use TaskCreate with clear acceptance criteria.

### Example

```
Task 1: Create data model                    [independent]
Task 2: Create business logic                [blocked by 1]
Task 3: Create API/UI layer                  [blocked by 2]
Task 4: Create storage adapter               [independent]
Task 5: Wire end-to-end                      [blocked by 1-4]
Task 6: Add CLI/entry point                  [blocked by 5]
```

## Agent Dispatch

### Parallel (independent tasks)

```
For each independent task:
  1. TaskUpdate -> in_progress
  2. Dispatch Agent with isolation: "worktree"
  3. Include in prompt: task description + relevant skill names
```

### Sequential (dependent tasks)

```
1. Dispatch agent for Task A
2. When A completes and checks pass -> merge worktree
3. TaskUpdate A -> completed
4. TaskUpdate B -> in_progress
5. Dispatch agent for Task B
```

### Agent Prompt Template

When dispatching an agent, always include:

```
Task: [clear description with acceptance criteria]

Follow these skills:
- tdd-workflow: write failing tests first, then implement
- code-quality: lint, typecheck, test standards
- [domain skill if relevant]

Working directory: [worktree path]
Run `{PACKAGE_MANAGER_SYNC}` before starting.
Run `{LINT_COMMAND} && {TYPECHECK_COMMAND} && {TEST_COMMAND}` before finishing.

If this task relates to a past decision, see: decisions/NNNN-relevant-title.md
```

## Worktree Workflow

### Create and dispatch

Use `isolation: "worktree"` parameter in the Agent tool call. This automatically creates an isolated git worktree for the agent.

### Verify (pre-merge)

After agent completes, run in the worktree:

```bash
{LINT_COMMAND}
{TYPECHECK_COMMAND}
{TEST_COMMAND}
```

### Auto-merge decision

```
All checks pass?
  YES -> merge with: git merge --no-ff <branch> -m "<conventional commit>"
         clean up worktree and branch
  NO  -> send errors back to agent (up to 2 retries)
         if still failing -> escalate to orchestrator
```

### Cleanup

```bash
git worktree remove <worktree-path>
git branch -d <branch-name>
```

IMPORTANT: Always clean up worktrees and branches immediately after merging. Do not leave stale worktrees accumulating.

### CRITICAL: Nested Worktree Warning

Agents spawned while CWD is inside a worktree will create NESTED worktrees. This causes merge conflicts because nested worktrees branch from the parent worktree, not from main. Always ensure agents are spawned from the MAIN working tree.

### Update After Merge

After merging agent work:
1. Update `memory-bank/progress.md` -- mark completed features, add known issues
2. Update `memory-bank/active-context.md` -- shift priorities, note what's next
3. If new architectural pattern was introduced, update `memory-bank/system-patterns.md` AND create a `decisions/NNNN-*.md` entry

## Plan Persistence

All plans must be saved to `memory-bank/plans/` as markdown files with descriptive names (e.g., `memory-bank/plans/add-auth-middleware.md`). This ensures plans survive context clears.

When creating a plan:
1. Write the plan to `memory-bank/plans/{descriptive-name}.md`
2. Reference it in `memory-bank/active-context.md` under "Active Plan"
3. As tasks complete, update the plan file (mark steps done, add notes)

When resuming after context clear:
1. Read `memory-bank/active-context.md` to find the active plan
2. Read the plan file to see what's done and what remains
3. Continue from where the plan left off

## Context Handoff Protocol

The context-guard hook (PostToolUse) fires a warning when context usage reaches 58%. When you see this warning:

1. **Stop at the nearest clean boundary.** Finish your current atomic unit of work (complete the current agent merge, finish the current file edit). Do NOT start new tasks or dispatch new agents.

2. **Write continuation state to `memory-bank/active-context.md`:**
   ```markdown
   ## Continuation State
   ### What Was Being Done
   [Current task/plan name and description]

   ### Done So Far
   - [Specific: files created, tests passing, agents merged, etc.]

   ### What Remains
   - [Specific next steps, in order]

   ### Key Context for Resumption
   - [File paths that matter]
   - [Decisions made during this session]
   - [Gotchas or issues discovered]
   - [Branch state, pending worktrees, unmerged agents]

   ### Active Plan
   See: memory-bank/plans/{plan-name}.md
   ```

3. **Update the plan file** in `memory-bank/plans/` — mark completed steps, add notes about in-progress work.

4. **Tell the user:**
   > Context is at {X}%. I've saved continuation state to `memory-bank/active-context.md`. Clear context with `/clear` and say "resume" to pick up where I left off.

### Resuming After Context Clear

When the user says "resume" or starts a new session:
1. Follow the memory-bank session start protocol (read all memory-bank/ files)
2. Read `memory-bank/active-context.md` — the "Continuation State" section tells you exactly where to pick up
3. Read the active plan file
4. Continue execution from the next incomplete step

### Context Budget Guidelines

- **0-20%**: Context building — reading files, understanding codebase, loading memory bank
- **20-58%**: Active work — the productive window for dispatching agents and merging
- **58%+**: Handoff zone — stop new work, write continuation state
- **90%**: Claude auto-compresses (lossy) — you want to be done well before this

## Review Criteria

Before merging an agent's work:

1. **Task complete** -- acceptance criteria met
2. **Quality gates pass** -- all three checks green
3. **Tests exist** -- every new module has tests
4. **No scope creep** -- only changed what's in the task
5. **No secrets** -- no API keys or credentials in code

## Error Handling

### Agent fails quality checks
1. Send error output back to agent
2. Agent gets up to 2 retries
3. If still failing -> orchestrator intervenes

### Agent produces wrong output
1. Do NOT merge
2. Create new task with correction instructions
3. Dispatch new agent

### Merge conflicts
1. Do NOT auto-merge
2. Orchestrator resolves conflicts
3. Re-run quality checks
4. Then merge

## Task Status Flow

```
pending -> in_progress -> completed
               |
          (blocked) -> create blocker task -> resolve -> resume
```

Always update task status:
- `in_progress` BEFORE dispatching the agent
- `completed` ONLY after merge is done
- Never mark completed if checks are failing
```

---

### `.claude/skills/commit/SKILL.md` — Commit Standards

Generate with project-specific scopes:

```markdown
---
name: commit
description: Conventional commit format and pre-commit workflow for {PROJECT_NAME}. Use when committing code, preparing changes for commit, or when asked about commit message format.
---

# Commit Standards

## Conventional Commit Format

```
type(scope): short description

Optional body explaining the "why" not the "what".

Co-Authored-By: Claude <noreply@anthropic.com>
```

## Types

| Type       | When to use                                    |
|------------|------------------------------------------------|
| `feat`     | New feature or capability                      |
| `fix`      | Bug fix                                        |
| `refactor` | Code change that neither fixes nor adds        |
| `docs`     | Documentation only                             |
| `test`     | Adding or updating tests                       |
| `chore`    | Build process, dependencies, tooling, config   |

## Scopes

{Generate a scopes table from the project's modules. Always include these universal scopes:}

| Scope       | Area                                |
|-------------|-------------------------------------|
| {module1}   | {description}                       |
| {module2}   | {description}                       |
| config      | Settings, environment, project config |
| ci          | CI/CD pipelines                     |
| deps        | Dependency changes                  |
| decisions   | Architecture decision records       |

## Pre-Commit Checklist

ALL must pass before committing:

```bash
{LINT_COMMAND}
{TYPECHECK_COMMAND}
{TEST_COMMAND}
```

Do not use `--no-verify` or skip checks.

## Atomic Commit Rules

- One logical change per commit
- Do not mix features with formatting
- Do not mix refactoring with new features
- Tests go in the same commit as the feature
- Dependency additions go with the code using them
```

---

### `.claude/skills/pr/SKILL.md` — Pull Request Workflow

Generate with project-specific examples:

```markdown
---
name: pr
description: Pull request workflow for {PROJECT_NAME}. Use when creating PRs, preparing branches for review, or when asked about PR format, branch naming, or review checklists.
---

# Pull Request Workflow

## Branch Naming

Format: `type/short-description`

Examples: `feat/auth-middleware`, `fix/null-response`, `refactor/extract-service`

## PR Creation

```bash
gh pr create --title "type(scope): description" --body "$(cat <<'EOF'
## Summary
- Bullet points describing the change

## Changes
- `path/to/file.ext` -- what changed

## Test Plan
- [ ] `{LINT_COMMAND}` passes
- [ ] `{TYPECHECK_COMMAND}` passes
- [ ] `{TEST_COMMAND}` passes
- [ ] Manual verification: [describe]

Generated with Claude Code
EOF
)"
```

## Review Checklist

### Code Quality
- [ ] No hardcoded secrets or API keys
- [ ] No commented-out code blocks
- [ ] Follows project conventions

### Testing
- [ ] New code has corresponding tests
- [ ] All tests pass
- [ ] Edge cases covered

### Tooling
- [ ] Lint check passes
- [ ] Type check passes (if applicable)

### Architecture
- [ ] No circular imports/dependencies
- [ ] Config in config files, not hardcoded

### Git Hygiene
- [ ] Conventional commit format
- [ ] Atomic commits
- [ ] Branch name follows type/description convention
```

---

### `.claude/skills/tdd-workflow/SKILL.md` — TDD Workflow

Only generate if user opted into TDD. Generate with language-appropriate examples:

```markdown
---
name: tdd-workflow
description: Test-Driven Development workflow for {PROJECT_NAME}. Use when writing tests, implementing features with TDD, or when asked about test-first development, red-green-refactor, or testing patterns.
---

# TDD Workflow

## Red-Green-Refactor Cycle

### 1. RED -- Write a Failing Test
- Understand the requirement
- Write a test that describes the expected behavior
- Run `{TEST_COMMAND}` -- confirm it FAILS
- Commit: `test(scope): add failing test for [feature]`

### 2. GREEN -- Make It Pass
- Write the MINIMUM code to make the test pass
- No extra features, no "while I'm here" changes
- Run `{TEST_COMMAND}` -- confirm it PASSES
- Commit: `feat(scope): implement [feature]`

### 3. REFACTOR -- Improve the Code
- Clean up the implementation
- Remove duplication, improve naming
- Run `{TEST_COMMAND}` -- confirm tests still PASS
- Commit: `refactor(scope): clean up [feature]`

## Test Quality Rules

**Good tests:**
- Test behavior, not implementation
- One assertion per test (when practical)
- Descriptive names: `test_should_[behavior]_when_[condition]` (or equivalent in your language)
- Independent -- no shared mutable state between tests
- Fast -- mock external dependencies

## Test Structure (Arrange-Act-Assert)

{Generate a language-appropriate example using the project's test framework}

## When to Mock

**DO mock:** External APIs, databases, file system (when testing logic), third-party services
**DON'T mock:** The code you're testing, simple utility functions

## TDD Checklist

Before implementing any feature:
1. Ask: "What test would prove this works?"
2. Write that test
3. Run it -- watch it fail (RED)
4. Implement the minimum to pass
5. Run it -- watch it pass (GREEN)
6. Clean up if needed (REFACTOR)
7. Repeat for the next behavior
```

---

### `.claude/skills/code-quality/SKILL.md` — Code Quality Standards

Generate with the project's specific tooling:

```markdown
---
name: code-quality
description: Code quality standards for {PROJECT_NAME}. Use when writing code, reviewing code, running quality checks, or when asked about linting, type checking, testing conventions, or code style.
---

# Code Quality Standards

## Quality Gate Commands

```bash
{LINT_COMMAND}        # Lint check
{TYPECHECK_COMMAND}   # Type check
{TEST_COMMAND}        # Run tests
```

All three must pass before committing, creating PRs, or merging.

## Linter/Formatter

{Describe the project's lint/format setup. Reference the config file (pyproject.toml, .eslintrc, etc.) and key rules.}

## Type Checking

{Describe the type checking setup if applicable. Reference config and strictness level.}

## Testing

{Describe the test runner config. Reference test directory structure and conventions.}

## Module Structure

{Generate a tree showing the project's intended module/package layout based on the scopes and architecture discussed.}

## Quality Checklist

Before considering code complete:
1. Lint check passes -- zero violations
2. Type check passes -- zero errors (if applicable)
3. All tests pass
4. New code has tests
5. No secrets or credentials in code
```

---

### `.claude/skills/memory-bank/SKILL.md` — Memory Bank

Generate mostly universal, with project name customized:

```markdown
---
name: memory-bank
description: Memory bank for cross-session knowledge persistence in {PROJECT_NAME}. Use at the start of every session to load project context, after completing significant work to update progress, when making architectural decisions, or when explicitly asked to "update memory bank".
---

# Memory Bank

The memory bank persists project knowledge across sessions. All files live in `memory-bank/` at the project root and are committed to git.

## Session Start Protocol

At the start of every session, read ALL memory bank files:

```
memory-bank/project-brief.md    -> project scope, goals, requirements
memory-bank/product-context.md  -> why we're building this, UX goals
memory-bank/system-patterns.md  -> architecture, design patterns, components
memory-bank/tech-context.md     -> tech stack, dependencies, constraints
memory-bank/active-context.md   -> current priorities, recent changes, next steps
memory-bank/progress.md         -> what's done, what's remaining, known issues
```

Read active-context.md and progress.md first -- they tell you where we left off.

## When to Update

| Trigger | Files to Update |
|---------|----------------|
| After merging agent work | progress.md, active-context.md |
| New architectural decision | system-patterns.md + create decisions/NNNN-*.md |
| New technology/dependency added | tech-context.md |
| Scope or requirements change | project-brief.md |
| UX or product direction change | product-context.md |
| Explicit "update memory bank" request | Review and update ALL files |
| Discovering new patterns | system-patterns.md |

## Update Rules

- **Update, don't append.** Rewrite sections to reflect current state. Remove stale information.
- **Keep files concise.** Each file should be scannable in under 30 seconds.
- **Be specific.** Include file paths, function names, concrete details -- not vague descriptions.
- **Track what changed.** In active-context.md, note what was just completed and what's next.
- **Progress is authoritative.** progress.md is the single source of truth for what's done vs remaining.
```

---

### `.claude/skills/decision-log/SKILL.md` — Decision Log (NEW)

```markdown
---
name: decision-log
description: Lightweight Architecture Decision Records for {PROJECT_NAME}. Use when making significant technical or architectural decisions, when asked about past decisions, or when evaluating trade-offs.
---

# Decision Log

Lightweight ADRs stored in `decisions/` at the project root, committed to git.

## When to Log a Decision

- Choosing between competing technologies or approaches
- Establishing a new pattern or convention
- Deviating from or superseding a previous decision
- Making a trade-off with consequences worth remembering
- Any decision a future session would benefit from knowing

## File Format

File naming: `decisions/NNNN-short-title.md` (zero-padded, sequential)

### Template

```markdown
# NNNN: Short Title

**Date:** YYYY-MM-DD
**Status:** accepted | superseded by NNNN | deprecated

## Context
What is the situation? What forces are at play?

## Decision
What did we decide? Be specific.

## Consequences
- What becomes easier?
- What becomes harder?
- What are the risks?

## Alternatives Considered
Brief notes on what else was evaluated and why it was rejected.
```

## Integration with Workflow

- **Orchestrator**: Before making a decision that matches the triggers above, check `decisions/` for related past decisions. Reference them in the new decision if relevant.
- **Memory bank**: After creating a new decision, update `memory-bank/system-patterns.md` if the decision introduces or changes an architectural pattern.
- **Commit**: Decision log entries use type `docs(decisions)`.
- **Agents**: When dispatching an agent for work related to a past decision, include the decision file path in the agent prompt so the agent has full context.

## Querying Decisions

- Scan filenames in `decisions/` for relevant titles
- Grep for keywords: `grep -rl "keyword" decisions/`
- Read active decisions when evaluating related trade-offs
```

---

### Memory Bank Files (in `memory-bank/`)

Generate six files, populated from the PRD if available, otherwise from the Q&A answers.

#### `memory-bank/project-brief.md`
```markdown
# Project Brief: {PROJECT_NAME}

## Overview
{One-line description}

## Goals
{Bullet list of project goals from PRD/Q&A}

## Requirements
{Key requirements, features, or user stories}

## Deadlines
{If known, otherwise "TBD"}

## Deliverables
{What needs to be shipped}
```

#### `memory-bank/product-context.md`
```markdown
# Product Context: {PROJECT_NAME}

## Problem
{What problem does this solve?}

## Solution
{How does this project solve it?}

## Target Users
{Who is this for?}

## User Experience
{How will users interact with it?}
```

#### `memory-bank/system-patterns.md`
```markdown
# System Patterns: {PROJECT_NAME}

## Architecture Overview
{High-level architecture — fill in as it emerges}

## Key Components
{List major components/modules}

## Design Decisions
See `decisions/` directory for detailed ADRs.

## Patterns in Use
{To be updated as patterns are established}
```

#### `memory-bank/tech-context.md`
```markdown
# Tech Context: {PROJECT_NAME}

## Stack
- Language: {language and version}
- Package Manager: {package manager}
- Framework(s): {frameworks}
- Linter: {lint tool and command}
- Type Checker: {typecheck tool and command}
- Test Runner: {test tool and command}

## External Services
{List APIs, databases, third-party services}

## Deployment
{Deployment target and method}

## Project Structure
{Initial module/package layout}

## Constraints
{Any known constraints, limitations, or non-functional requirements}
```

#### `memory-bank/active-context.md`
```markdown
# Active Context: {PROJECT_NAME}

## Current Phase
Project setup complete. Ready for first feature.

## Just Completed
- Bootstrapped agentic development workflow
- Created .claude/ config, skills, hooks
- Initialized memory bank and decision log

## Next Steps
{First features or tasks from PRD, or "Define first feature"}

## Active Decisions
- See decisions/0001-project-bootstrap.md for initial setup choices

## Blockers
None
```

#### `memory-bank/progress.md`
```markdown
# Progress: {PROJECT_NAME}

## Completed
- [x] Project bootstrapping
- [x] Development workflow setup (.claude/, skills, hooks)
- [x] Memory bank initialized
- [x] Decision log initialized

## In Progress
{Nothing yet, or first task if known}

## Not Started
{List features/tasks from PRD, or "TBD - awaiting feature planning"}

## Known Issues
None

## Test Count
0 tests
```

---

### Decision Log Seed Entry

#### `decisions/0001-project-bootstrap.md`

Generate a decision documenting the initial setup choices:

```markdown
# 0001: Project Bootstrap

**Date:** {TODAY}
**Status:** accepted

## Context
Setting up the development workflow for {PROJECT_NAME}. Need to establish tooling, workflow patterns, and team conventions.

## Decision
Adopted an agentic development workflow with:
- **Orchestrator pattern**: Main agent decomposes and delegates; spawned agents implement in isolated worktrees
- **Stack**: {language}, {package manager}, {lint tool}, {typecheck tool}, {test runner}
- **Quality gates**: {three commands} must pass before merge
- **Memory bank**: Six-file cross-session knowledge persistence
- **Decision log**: Lightweight ADRs in decisions/
- **TDD**: {yes/no}
- **Conventional commits**: type(scope): description

## Consequences
- Consistent workflow across all development sessions
- Knowledge persists across sessions via memory bank
- Architectural decisions are traceable via decision log
- Agents work in isolation, reducing conflicts
- Quality gates enforce standards automatically

## Alternatives Considered
- Manual workflow without orchestration — rejected for inconsistency across sessions
- Heavier ADR format — rejected in favor of lightweight decision log
```

---

## Post-Generation Summary

After generating all files, print a summary:

```
Project bootstrap complete for {PROJECT_NAME}.

Files created:
  .claude/CLAUDE.md              -- Master development rules
  .claude/settings.json          -- Permissions and hook config
  .claude/settings.local.json    -- Local permission overrides
  .claude/hooks/protect-env.sh   -- Blocks writes to .env/secrets
  .claude/hooks/block-destructive-git.sh -- Blocks force push, hard reset
  .claude/skills/orchestration/  -- Agent dispatch, worktree workflow, context handoff
  .claude/skills/commit/         -- Conventional commit standards
  .claude/skills/pr/             -- Pull request workflow
  .claude/skills/tdd-workflow/   -- TDD red-green-refactor (if opted in)
  .claude/skills/code-quality/   -- Lint, type, test standards
  .claude/skills/memory-bank/    -- Cross-session persistence
  .claude/skills/decision-log/   -- Architecture decision records
  memory-bank/                   -- 6 context files initialized
  memory-bank/plans/             -- Plan persistence directory
  decisions/0001-project-bootstrap.md -- Seed decision

Quick reference:
  Quality gates:  {LINT_COMMAND} && {TYPECHECK_COMMAND} && {TEST_COMMAND}
  Session start:  Read all memory-bank/ files (active-context.md first)
  Resume work:    Read active-context.md "Continuation State" + active plan file
  New feature:    Plan -> TaskCreate -> Agent(isolation: "worktree") -> Review -> Merge
  New decision:   Create decisions/NNNN-title.md, update system-patterns.md
  Context at 58%: Handoff protocol -- save state, tell user to /clear
```