---
description: Start a new feature branch synchronized with main
---

1. Ask the user for the feature name (e.g., "login-page").
2. // turbo
   Run `git checkout main`
3. // turbo
   Run `git pull origin main`
4. // turbo
   Run `git checkout -b feature/{{user_input}}`