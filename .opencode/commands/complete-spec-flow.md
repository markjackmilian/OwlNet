---
description: Move spec to done, commit, merge feature branch into develop, optionally delete the feature branch
---

Complete the spec flow for the spec file `$ARGUMENTS`. Follow these steps **in order**, stopping immediately if any step fails:

## 1. Identify the spec file

- Search for a spec file matching `$ARGUMENTS` inside `specs/todo/`. The argument can be a partial name (e.g. `SPEC-003` or `favorites`).
- If no matching file is found in `specs/todo/`, report the error and stop.
- If multiple files match, list them and ask which one to use.

## 2. Move the spec to done

- Run: `git mv specs/todo/<matched-file> specs/done/<matched-file>`
- If the spec file has a `Status` field in the frontmatter/header (e.g. `> **Status:** Todo`), update it to `Done`.

## 3. Create a commit

- Stage all changes: `git add -A`
- Commit with message: `docs: move <spec-name> to done`

## 4. Merge feature branch into develop (if applicable)

- Check the current branch name with `git branch --show-current`.
- If the current branch is **not** `develop` and **not** `main` (i.e. it is a feature branch):
  1. Save the feature branch name.
  2. Switch to `develop`: `git checkout develop`
  3. Pull latest: `git pull origin develop`
  4. Merge the feature branch: `git merge <feature-branch> --no-ff`
  5. If the merge has conflicts, report them and stop — do NOT force resolve.
- If the current branch is already `develop` or `main`, skip this step and go to step 6.

## 5. Ask about feature branch deletion

- Ask the user: **"Do you want to delete the feature branch `<feature-branch>`?"**
- If the user says **yes**:
  1. Delete the local branch: `git branch -d <feature-branch>`
  2. Ask if the remote branch should also be deleted. If yes: `git push origin --delete <feature-branch>`
- If the user says **no**, skip deletion.

## 6. Summary

Print a summary of what was done:
- Spec file moved
- Commit hash (short)
- Whether merge was performed and into which branch
- Whether the feature branch was deleted (local/remote)
