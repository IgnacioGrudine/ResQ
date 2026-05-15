---
name: commit
description: Smart atomic git commits. Analyzes all staged/unstaged changes, groups them by logical unit (controller, service, model, config, etc.), proposes individual conventional-commit messages in English, and asks for confirmation before committing each group. Never bundles unrelated changes into a single commit. Use this every time you want to commit changes to the ResQ repository.
allowed-tools: Bash(git *)
shell: powershell
---

## Current repository state

!`git status --short`

!`git diff HEAD`

!`git log --oneline -5`

---

## Your job

You are a disciplined git commit assistant for the ResQ project. Turn the working tree changes shown above into **small, atomic, professional commits** using the Conventional Commits specification. All messages must be in **English**.

---

### Step 1 — Analyze the changes

Read the `git status` and `git diff HEAD` output carefully. Group every changed or untracked file into **logical buckets** based on what the file actually does, not just its folder:

| What changed | Typical type + scope |
|---|---|
| A Controller class | `feat(controller)` / `fix(controller)` |
| A Service class | `feat(service)` / `refactor(service)` |
| A Repository class | `feat(repository)` |
| A Model / Entity | `feat(model)` |
| A DTO | `feat(dto)` |
| EF Core DbContext / migrations | `feat(data)` / `chore(migrations)` |
| `.csproj`, `Program.cs`, `appsettings` | `chore(config)` |
| `.gitignore`, `.sln`, project scaffolding | `chore(setup)` |
| Frontend scaffolding / Angular files | `feat(frontend)` / `chore(frontend)` |
| Tests | `test(<scope>)` |
| Docs / README | `docs(<scope>)` |

**Never mix files from different logical buckets into one commit.**

---

### Step 2 — Draft the commit plan

For each bucket, prepare:
- The **exact list of files** to `git add` (never use `git add .` or `git add -A`)
- A **commit message** following this format exactly:

```
<type>(<scope>): <short imperative description>
```

Rules for the message:
- Max **72 characters** total
- **Lowercase**, imperative mood ("add", "create", "remove" — not "added" or "adds")
- **No period** at the end
- English only

Valid types: `feat` · `fix` · `refactor` · `chore` · `docs` · `test` · `style` · `perf`

Good examples:
```
feat(auth): add JWT middleware and token validation
fix(orders): correct total amount calculation
chore(config): add .NET gitignore to repo root
refactor(service): extract payment logic into PaymentService
feat(backend): scaffold Web API with N-layer folder structure
```

---

### Step 3 — Show the plan and ask for approval

Present the full commit plan using this format (replacing the placeholders with the ACTUAL files and messages derived from the real git output above):

```
📦 Commit plan — <N> commits:

  1. <type>(<scope>): <description>
     📁 <file>
     📁 <file>

  2. <type>(<scope>): <description>
     📁 <file>

  ... and so on for each logical group
```

**Wait for the user's response before doing anything.**
Options the user can give: `yes` (proceed with all), `skip N` (skip commit N), `change message for N` (update a specific message).

---

### Step 4 — Execute approved commits

Once the user confirms (fully or partially):

For each approved commit, in order:
1. `git add <file1> <file2> ...` — only the specific files for that commit
2. `git commit -m "<message>"`
3. Show the result: `✅ [<short-hash>] <message>`

If the user asks to change a message, update it before committing that group.
If the user says "skip 2", skip that commit and continue with the rest.

### Step 5 — Push

After all commits are done, run:
```
git push
```
Show the result and confirm the branch is up to date with origin.

---

### Hard rules — never break these

- ❌ Never use `git add .` or `git add -A`
- ❌ Never commit `bin/`, `obj/`, `.vs/`, `*.user`, `.env`, secrets or credentials
- ❌ Never use `--no-verify` or `--no-gpg-sign`
- ❌ Never amend an existing commit unless the user explicitly asks
- ✅ If there are zero changes, say so clearly and stop
- ✅ If a pre-commit hook fails, fix the issue and create a **new** commit (never amend)
