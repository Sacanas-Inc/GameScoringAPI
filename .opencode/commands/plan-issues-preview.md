---
description: Preview untriaged GitHub issues (no PowerShell; agent connects to GitHub itself)
---

You are previewing which issues in this repository still need an implementation plan.
This is READ-ONLY: do NOT post comments and do NOT add labels.

- Repo: `Sacanas-Inc/GameScoringAPI`
- List open issues via the `gh` CLI if available, otherwise the GitHub REST API
  (`https://api.github.com/repos/Sacanas-Inc/GameScoringAPI/issues?state=open`),
  excluding pull requests.
- Token: `GITHUB_TOKEN`, else the `git:https://github.com` credential obtained
  with `git credential fill` (protocol=https, host=github.com).
- An issue already has a plan if a comment contains the marker
  `<!-- agent:plan -->` or a `## Plan` heading, or if it has the `has-plan` label.
- Skip issues labelled: question, discussion, wontfix, duplicate, blocked,
  invalid, help wanted.

Then:
1. List the open issues.
2. For each issue evaluate whether the information on the issue is unclear or insufficient. Report these as  `#<number> — <title>`, oldest first.
3. Work out the candidates: issues with no plan and sufficient information to create a plan.
3. Report candidates as a list of `#<number> — <title>`, oldest first.
4. Note anything that looks off (e.g. wrong URLs, empty candidates).

Connect to GitHub with your own tools; do not shell out to PowerShell to do the work.

Additional limits or instructions: $ARGUMENTS