# Git Workflow Rules

## Branch Strategy
- **Main Branch Protection**: The 'main' branch contains ONLY stable, integrated software.
- **Feature Development**: ALL new features must be developed in separate branches named `feature/[description]`. 
- **Integration**: Updates are merged into 'main' ONLY after the feature is complete and tested.

## AI Behavior Constraints
- **NEVER** commit new features directly to the 'main' branch. 
- **ALWAYS** create a new branch named `feature/[task-name]` when starting new work.
- **ALWAYS** sync with 'main' (`git pull`) before creating a new feature branch.
- **Commit Standards**: Use Conventional Commits (e.g., "feat: add login", "fix: resolve crash").

## Conflict Resolution
- If a merge conflict occurs, pause and ask the user for clarification before overwriting code.