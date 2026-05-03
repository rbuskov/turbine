---
description: Create a git commit with a short summary of changes (no Claude attribution)
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git add:*), Bash(git commit:*), Bash(git log:*)
---

## Context

- Current status: !`git status`
- Staged changes: !`git diff --cached`
- Unstaged changes: !`git diff`
- Recent commits (for style reference): !`git log --oneline -10`

## Task

Create a git commit following these rules:

1. If there are unstaged changes, stage the relevant files with `git add`. Do not stage unrelated files.
2. Write a **short, clear commit message** that summarizes what changed and why (imperative mood, e.g. "Add", "Fix", "
   Refactor"). Match the style of recent commits shown above.
3. Keep the subject line under 72 characters. Add a brief body only if the change genuinely needs more context.
4. **Do NOT include any Claude or AI attribution.** Specifically:
    - Do NOT add `🤖 Generated with [Claude Code](https://claude.com/claude-code)`
    - Do NOT add `Co-Authored-By: Claude <noreply@anthropic.com>`
    - Do NOT mention Claude, Anthropic, or AI anywhere in the message
5. Run the commit using a standard `git commit -m "..."` (no HEREDOC trailer with attribution).
6. After committing, show the resulting `git log -1` so I can verify the message.

$ARGUMENTS