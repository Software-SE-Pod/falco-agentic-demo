## What changed

<!-- One or two sentences. What did you change, and why? -->

## Why

<!-- The problem this solves. Link the issue: Closes #123 -->

## How to verify

```bash
dotnet test FalcoDemo.slnx
```

<!-- Add any manual steps a reviewer needs. -->

## Checklist

- [ ] `dotnet build` is clean (warnings are errors in this repo)
- [ ] `dotnet test` passes
- [ ] `dotnet fantomas src tests --check` passes
- [ ] New branches in `Domain.fs` have tests
- [ ] No secrets, tokens or connection strings in the diff
- [ ] No unrelated changes to `.github/workflows/`

## Copilot code review

Automatic Copilot review is enabled on this repository. Before requesting a human:

- [ ] I applied at least one Copilot comment, or
- [ ] I dismissed it with a written reason explaining why it does not apply
