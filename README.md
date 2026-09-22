# FalcoDemo.Api

A small **F# / [Falco](https://www.falcoframework.com/)** HTTP API, wrapped in the
full GitHub platform surface — rulesets, CODEOWNERS, Actions, Copilot code review,
a custom agent, a skill, and a scheduled agentic workflow.

The API itself is deliberately modest. The point of this repository is everything
around it.

## The API

In-memory product catalogue. No database, no ORM, no DI container — on purpose.

| Method | Route | Behaviour |
| --- | --- | --- |
| `GET` | `/health` | Liveness probe used by the CI smoke test. |
| `GET` | `/products` | All products, sorted by SKU, plus total inventory value. |
| `GET` | `/products/low-stock` | Products at or below a quantity threshold (`?threshold=`, default `20`). |
| `GET` | `/products/{id}` | One product, or `404`. `400` if the id is not a GUID. |
| `POST` | `/products` | Validates and creates. `422` lists **every** invalid field. |
| `DELETE` | `/products/{id}` | Idempotent removal, `204`. |

```bash
dotnet restore FalcoDemo.slnx
dotnet build   FalcoDemo.slnx --configuration Release --no-restore
dotnet test    FalcoDemo.slnx --configuration Release --no-build
dotnet run --project src/FalcoDemo.Api        # http://localhost:5000

curl http://localhost:5000/products
curl -X POST http://localhost:5000/products \
  -H 'Content-Type: application/json' \
  -d '{"sku":"STP-4001","name":"Whiteboard marker, 4-pack","unitPriceCents":899,"quantityOnHand":210}'

# See accumulated validation errors rather than just the first one:
curl -i -X POST http://localhost:5000/products \
  -H 'Content-Type: application/json' \
  -d '{"sku":"","name":"","unitPriceCents":0,"quantityOnHand":-1}'
```

## Layout

```
src/FalcoDemo.Api/
  Domain.fs      records + pure validation (no I/O, no ASP.NET types)
  Store.fs       ProductStore over a ConcurrentDictionary
  Handlers.fs    one Falco HttpHandler per endpoint, thin
  Program.fs     endpoint table and host wiring, nothing else
tests/FalcoDemo.Tests/
  Tests.fs       xUnit tests against Domain and Store
```

F# compile order is significant — new files go into the `.fsproj` in dependency order.

## The platform surface

| File | What it demonstrates |
| --- | --- |
| `.github/copilot-instructions.md` | Repository context Copilot loads automatically. Ask it something and check the **references** list in the reply — the file must appear there. That is the proof it loaded. |
| `.github/CODEOWNERS` | Path-based review routing. Bottom-up matching; last match wins. |
| `.github/workflows/ci.yml` | Least-privilege `permissions`, artifact upload on failure, a masked secret, a real smoke test. |
| `.github/workflows/repo-cleanup.md` | An **agentic workflow** — read-only permissions, `create-pull-request` safe output. |
| `.github/agents/repo-cleanup.md` | A **custom agent** with a deliberately restricted tool list. |
| `.github/skills/repo-cleanup/SKILL.md` | The agent's playbook: real commands, never-touch paths, PR shape. |
| `.github/dependabot.yml` | Grouped NuGet and Actions updates. |
| `.github/pull_request_template.md` | Includes an explicit Copilot-review step. |

### Copilot code review

Automatic review is enabled on this repository. Open any PR and Copilot reviews it
without being asked. The PR template requires you to either **apply** one of its
comments or **dismiss** one with a written reason — both are worth doing live.

### The repo-cleanup agent

The agent has **no `shell` or `bash` tool**, and that is the interesting part.

In an interactive Copilot session a human gets a confirmation prompt before any
terminal command runs. But this agent is also wired to a *scheduled* automation,
where no human is present to approve anything. Removing `shell` removes the risk
class outright rather than depending on a prompt nobody will see.

The consequence: the agent cannot run `dotnet test` itself. It proposes a diff, and
CI proves the diff. The agent's opinion is never what gets trusted.

Its PR gets no special standing — required checks, CODEOWNERS routing, Copilot
review, and no self-merge. A bot-authored PR is reviewed like anyone else's.

Run it from the **Agents** tab, or via `workflow_dispatch` on *Repo cleanup agent*.

> The agentic workflow is authored as Markdown with YAML frontmatter and compiled
> with [`gh aw`](https://github.com/githubnext/gh-aw):
> `gh extension install githubnext/gh-aw && gh aw compile`
> That produces `.github/workflows/repo-cleanup.lock.yml`, the Actions file that
> actually runs. Inspect the lock file before enabling it.

## Formatting

```bash
dotnet tool restore
dotnet fantomas src tests --check    # verify (this is the CI gate)
dotnet fantomas src tests            # fix
```
