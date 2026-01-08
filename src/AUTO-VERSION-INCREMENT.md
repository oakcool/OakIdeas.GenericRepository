# Automatic Version Increment Guide

## Overview

This repository supports **automatic version increment** through multiple mechanisms:

1. **GitVersion** - Automatic semantic versioning based on git history (Recommended)
2. **Manual Scripts** - PowerShell and Bash scripts for manual version bumping
3. **Git Tags** - Traditional tag-based versioning
4. **Manual Edit** - Direct edit of Directory.Build.props

## Method 1: GitVersion (Automatic - Recommended)

### How It Works

GitVersion automatically calculates version numbers based on:
- Git commits since last version tag
- Branch names
- Commit messages with semantic version hints

### Setup

The repository is already configured with `GitVersion.yml`. The GitHub Actions workflow automatically uses GitVersion when:
- Pushing to `master` without a tag
- No manual version is specified

### Version Calculation

**Current base version:** `0.0.6`

Each commit to `master` automatically increments the version:
- Commit 1: `0.0.6-alpha.1`
- Commit 2: `0.0.6-alpha.2`
- Commit 3: `0.0.6-alpha.3`

### Controlling Version Increments

You can control how versions increment using commit messages:

```bash
# Patch increment (default)
git commit -m "fix: bug fix +semver:patch"

# Minor increment
git commit -m "feat: new feature +semver:minor"

# Major increment (breaking change)
git commit -m "feat!: breaking change +semver:major"

# Skip version increment
git commit -m "docs: update readme +semver:skip"
```

### Creating Stable Releases

To create a stable release version:

```bash
# Tag with desired version
git tag v1.0.0
git push origin v1.0.0
```

The tag overrides GitVersion and publishes exactly `1.0.0`.

### Example Workflow

```bash
# Regular development
git add .
git commit -m "feat: add new feature"
git push origin master
# → Publishes 0.0.6-alpha.1

# Another commit
git commit -m "fix: bug fix"
git push origin master
# → Publishes 0.0.6-alpha.2

# Ready for stable release
git tag v0.1.0
git push origin v0.1.0
# → Publishes 0.1.0 (stable)

# Update base version for next cycle
# Edit GitVersion.yml: next-version: 0.1.0
git commit -m "chore: update base version +semver:skip"
git push
```

## Method 2: Manual Scripts (Semi-Automatic)

Use these scripts when you want explicit control over version increments.

### PowerShell Script (Windows)

```powershell
# Increment patch version (0.0.6 → 0.0.7)
./increment-version.ps1

# Increment minor version (0.0.6 → 0.1.0)
./increment-version.ps1 -IncrementType Minor

# Increment major version (0.0.6 → 1.0.0)
./increment-version.ps1 -IncrementType Major

# Change suffix
./increment-version.ps1 -Suffix beta

# Create stable release (remove suffix)
./increment-version.ps1 -RemoveSuffix

# Then commit and push
git add src/Directory.Build.props
git commit -m "Bump version to 0.0.7-alpha"
git push origin master
```

### Bash Script (Linux/Mac)

```bash
# Make script executable (first time only)
chmod +x increment-version.sh

# Increment patch version (0.0.6 → 0.0.7)
./increment-version.sh

# Increment minor version (0.0.6 → 0.1.0)
./increment-version.sh --type minor

# Increment major version (0.0.6 → 1.0.0)
./increment-version.sh --type major

# Change suffix
./increment-version.sh --suffix beta

# Create stable release (remove suffix)
./increment-version.sh --remove-suffix

# Then commit and push
git add src/Directory.Build.props
git commit -m "Bump version to 0.0.7-alpha"
git push origin master
```

## Method 3: Git Tags (Simple & Clean)

Best for stable releases or specific version requirements.

```bash
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions will:
1. Extract version `1.0.0` from the tag
2. Override any other versioning
3. Build and publish packages with exactly `1.0.0`

## Method 4: Manual Edit

Edit `src/Directory.Build.props` directly:

```xml
<VersionPrefix>0.0.7</VersionPrefix>
<VersionSuffix>alpha</VersionSuffix>
```

Then commit and push:

```bash
git add src/Directory.Build.props
git commit -m "Bump version to 0.0.7-alpha"
git push origin master
```

## Version Priority in GitHub Actions

When multiple version sources exist, this is the priority:

1. **Manual workflow input** (highest priority)
2. **Git tag** (v* tags)
3. **GitVersion calculated** (automatic)
4. **Directory.Build.props** (fallback)

## Recommended Workflows

### For Active Development

Use **GitVersion** (automatic):
- Just commit and push
- Versions increment automatically
- Example: `0.0.6-alpha.1`, `0.0.6-alpha.2`, etc.

```bash
git add .
git commit -m "feat: implement new feature"
git push origin master
# GitVersion automatically publishes with incremented version
```

### For Pre-releases (Beta, RC)

Use **manual scripts** or **edit Directory.Build.props**:

```bash
# Change to beta
./increment-version.ps1 -Suffix beta
git add src/Directory.Build.props
git commit -m "Bump to 0.0.7-beta"
git push origin master
```

### For Stable Releases

Use **git tags**:

```bash
git tag v1.0.0
git push origin v1.0.0
# Publishes exactly 1.0.0 (stable)
```

### For Hotfixes

Use **git tags** with patch increment:

```bash
# Assuming current stable is 1.0.0
git tag v1.0.1
git push origin v1.0.1
# Publishes exactly 1.0.1 (hotfix)
```

## GitVersion Configuration

The `GitVersion.yml` file controls automatic versioning:

```yaml
# Base version (update after each major/minor release)
next-version: 0.0.6

# Master branch settings
branches:
  master:
    tag: alpha              # Suffix for pre-releases
    increment: Patch        # Auto-increment patch version
    is-mainline: true

# Commit message patterns
major-version-bump-message: '\+semver:\s?(breaking|major)'
minor-version-bump-message: '\+semver:\s?(feature|minor)'
patch-version-bump-message: '\+semver:\s?(fix|patch)'
no-bump-message: '\+semver:\s?(none|skip)'
```

### Updating Base Version

After a stable release, update the base version:

```yaml
# Change this line in GitVersion.yml
next-version: 1.0.0  # Update to match your release
```

## Comparison of Methods

| Method | Automation | Control | Best For |
|--------|-----------|---------|----------|
| GitVersion | High | Medium | Active development |
| Manual Scripts | Medium | High | Pre-releases |
| Git Tags | Low | Very High | Stable releases |
| Manual Edit | None | Very High | Special cases |

## Examples

### Example 1: Sprint Development

```bash
# Sprint start - let GitVersion handle versioning
git commit -m "feat: add user authentication"
git push
# → 0.0.6-alpha.1

git commit -m "fix: login validation"
git push
# → 0.0.6-alpha.2

git commit -m "feat: add password reset"
git push
# → 0.0.6-alpha.3

# Sprint end - ready for beta
./increment-version.ps1 -Suffix beta
git add src/Directory.Build.props
git commit -m "Bump to 0.0.7-beta for testing"
git push
# → 0.0.7-beta
```

### Example 2: Release Preparation

```bash
# Beta testing phase
./increment-version.ps1 -Suffix rc1
git add src/Directory.Build.props
git commit -m "Release candidate 1"
git push
# → 0.1.0-rc1

# More testing...
./increment-version.ps1 -Suffix rc2
git add src/Directory.Build.props
git commit -m "Release candidate 2"
git push
# → 0.1.0-rc2

# Ready for production
git tag v0.1.0
git push origin v0.1.0
# → 0.1.0 (stable)

# Update base version for next cycle
# Edit GitVersion.yml: next-version: 0.1.0
git add GitVersion.yml
git commit -m "chore: update base version +semver:skip"
git push
```

### Example 3: Hotfix Release

```bash
# Current production: 1.0.0
# Bug discovered in production

# Create hotfix branch (optional)
git checkout -b hotfix/critical-bug

# Fix the bug
git commit -m "fix: critical security issue +semver:patch"

# Merge to master
git checkout master
git merge hotfix/critical-bug

# Create hotfix tag
git tag v1.0.1
git push origin v1.0.1
# → 1.0.1 (stable hotfix)
```

## Troubleshooting

### GitVersion Not Working

**Issue:** GitVersion not calculating versions

**Solution:**
```bash
# Check GitVersion.yml syntax
# Ensure fetch-depth: 0 in GitHub Actions (already configured)
# Verify GitVersion actions version: v1.1.1
```

### Version Not Incrementing

**Issue:** Published version same as previous

**Solution:**
1. Check if using GitVersion - commits increment automatically
2. If using Directory.Build.props - manually increment
3. If using tags - ensure tag is pushed: `git push origin v1.0.0`

### Wrong Version Published

**Issue:** Published with unexpected version

**Solution:**
1. Check version priority (manual > tag > GitVersion > props)
2. Review GitHub Actions logs: "Extract version" step
3. Verify GitVersion configuration

## Best Practices

✅ **Do:**
- Use GitVersion for daily development
- Use tags for stable releases
- Update `next-version` in GitVersion.yml after major releases
- Use semantic commit messages for better version control
- Test versions before stable release (alpha → beta → rc → stable)

❌ **Don't:**
- Mix versioning methods inconsistently
- Forget to update base version after releases
- Skip testing pre-release versions
- Tag without proper testing

## Additional Resources

- [GitVersion Documentation](https://gitversion.net/docs/)
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [VERSIONING.md](VERSIONING.md) - Comprehensive versioning guide
- [GITHUB-ACTIONS-DEPLOYMENT.md](GITHUB-ACTIONS-DEPLOYMENT.md) - Deployment guide
