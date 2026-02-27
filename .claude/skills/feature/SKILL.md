# /feature — Create GitFlow Feature Branch

Create a new feature branch following GitFlow conventions.

## Usage
- `/feature <feature-name>` — e.g., `/feature add-payment-service`

## Instructions

1. Ensure working directory is clean (`git status`). If there are uncommitted changes, warn the user and ask how to proceed.
2. Switch to `develop` branch: `git checkout develop`
3. Pull latest: `git pull origin develop`
4. Create and switch to feature branch: `git checkout -b feature/<feature-name>`
5. Confirm the branch was created and report current status.

Do NOT push the branch to remote — the user prefers to push from Visual Studio.
