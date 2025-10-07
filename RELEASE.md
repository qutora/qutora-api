# Release Guide

This project uses an automated GitHub Actions workflow to build and publish Docker images using **semantic versioning**

## How It Works

- Triggered on pushes to the `main` branch.
- Requires `[release]` in the commit message or pull request title.
- Version bump is based on tags in the message (default is patch).

## Version Bump Keywords

| Keyword       | Bump Type | Example Version Change |
|---------------|-----------|------------------------|
| `[major]`     | Major     | `v1.2.3` → `v2.0.0`     |
| `[minor]`     | Minor     | `v1.2.3` → `v1.3.0`     |
| _(default)_   | Patch     | `v1.2.3` → `v1.2.4`     |

If no keyword is given, a patch version is created.

## Release Examples

### Patch release:
```bash
git commit -m "[release] Fix typo in logs"
```

### Minor release:
```bash
git commit -m "[release][minor] Add search endpoint"
```

### Major release:
```bash
git commit -m "[release][major] Replace authentication method"
```

Then push to main:
```bash
git push origin main
```

## What It Does

When triggered:

- Detects the latest version from Docker Hub.
- Bumps the version (semver).
- Tags the Git commit.
- Builds and pushes Docker images:
  - `docker.io/qutora/qutora-api:<version>`
  - `docker.io/qutora/qutora-api:latest`
- Creates a GitHub release with notes.

## Required Secrets

Set these secrets in your GitHub repository:

- `DOCKER_USERNAME`
- `DOCKER_PASSWORD`

## Skipped Builds

If `[release]` is not in the commit or PR title, the workflow is skipped.

