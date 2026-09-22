---
name: Repo cleanup agent
description: >-
  Runs the repo-cleanup custom agent on a weekly schedule and on demand. It
  proposes tidy-ups as a pull request through the create-pull-request safe
  output, so the agent itself never holds write access to the repository.
emoji: "🧹"

on:
  schedule:
    - cron: "0 9 * * 1"
  workflow_dispatch:

# Read-only. The agent cannot write to the repo; the safe-output job opens the PR
# in a separate, permission-scoped step.
permissions:
  contents: read
  pull-requests: read
  issues: read

engine:
  id: copilot

timeout-minutes: 20

tools:
  github:
    mode: local
    toolsets: [default]

imports:
  - ../agents/repo-cleanup.md
  - ../skills/repo-cleanup/SKILL.md

safe-outputs:
  create-pull-request:
    title-prefix: "[repo-cleanup] "
    labels: [automated, cleanup]
    draft: false
    max: 1
---

# Weekly repository cleanup

Scan `src/` and `tests/` for the maintenance issues defined in the repo-cleanup
skill, and propose the fixes as a single pull request.

## What to look for, highest value first

1. **Formatting drift** — lines over 120 characters, inconsistent indentation,
   trailing whitespace, a missing final newline. Match `.editorconfig` exactly.
2. **Dead code** — an unused `let` binding, an unreferenced private function, an
   `open` that nothing in the file uses. Remember that `TreatWarningsAsErrors` is
   on, so be certain before removing an `open`: some bring operators or type
   extensions into scope without an obvious call site.
3. **Commented-out code** with no explanatory note. Remove it; git history is the
   archive.
4. **Stale documentation** — a `///` comment describing behaviour the function no
   longer has, or a public function in `Domain.fs` / `Store.fs` with no `///` at all.

## Rules you must not break

- Do not change behaviour. If a fix would alter what a function returns or how it
  branches, leave the code alone and describe it under "Noticed but not fixed".
- Do not touch any path on the never-touch list in the skill file. That includes
  this workflow, the agent definition, `CODEOWNERS`, `dependabot.yml`, every
  `.fsproj`, and `FalcoDemo.slnx`.
- Do not modify or delete a test.
- Do not add, remove or upgrade a package.
- Do not edit more than 10 files. If there is more work than that, take the 10
  highest-value fixes and say so.

## If there is nothing to do

Open no pull request and report that the repository is clean. A run that produces
no PR is a successful run, not a failure.

## Pull request body

1. One-line summary.
2. A table of every file changed with a one-line reason.
3. A "Noticed but not fixed" section with anything out of scope and why.
4. The verification commands from the skill file, so a reviewer can copy them.

Remember that this PR has no special standing. It must pass `Build and test`,
`Format check` and `Smoke test the API`, it will be routed to a human by
`CODEOWNERS`, and Copilot code review will run on it. It cannot merge itself.
