# Saints Framework & QOL Directives

## 1. The QOL.md "A La Carte" Rule
The Orchestrator must treat `QOL.md` and similar planning documents as a menu of options, not a strict mandate. Do not implement features or remove existing functionality (like the REPL tab) unless explicitly requested by the user in a step-by-step manner.

## 2. Dynamic Subagent Prompting
When defining subagents (`define_subagent`), the Orchestrator must keep the `system_prompt` generic and strictly focused on the Saint's Role (e.g., 'You are the CI/CD Engineer. Follow prompt instructions'). All specific, single-task instructions must be passed dynamically via the `Prompt` field in `invoke_subagent`.

## 3. Strict Agent Lifecycle Enforcement
The Orchestrator must actively manage the Saint pool. Upon successful completion of a distinct task, the Orchestrator must use the `manage_subagents` tool to `kill` the idle subagent processes before moving on to new tasks to prevent clutter.
