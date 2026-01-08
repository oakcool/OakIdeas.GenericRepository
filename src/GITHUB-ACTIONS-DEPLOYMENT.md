# GitHub Actions Deployment Guide

## Overview

The repository uses GitHub Actions to automatically publish NuGet packages. The workflow is configured to work seamlessly with the centralized versioning system in `Directory.Build.props`.

## Workflow File

**Location:** `.github/workflows/deploy.yml`

## How It Works

The workflow supports three different versioning strategies:

### 1. Version from Directory.Build.props (Default)

When you push to `master` without a tag, the workflow uses the version specified in `Directory.Build.props`.

**Example:**
```xml
<!-- Directory.Build.props -->
<VersionPrefix>0.0.7</VersionPrefix>
<VersionSuffix>alpha</VersionSuffix>
```

**Workflow Steps:**
1. Checks out the code
2. Builds with version `0.0.7-alpha` from Directory.Build.props
3. Runs tests
4. Creates packages with version `0.0.7-alpha`
5. Publishes to NuGet.org

**How to Use:**
```bash
# 1. Update Directory.Build.props
vim src/Directory.Build.props

# 2. Commit and push
git add src/Directory.Build.props
git commit -m "Bump version to 0.0.7-alpha"
git push origin master
```

### 2. Version from Git Tag (Recommended for Releases)

When you push a tag starting with `v`, the workflow extracts the version from the tag and overrides `Directory.Build.props`.

**Example:**
```bash
git tag v1.0.0
git push origin v1.0.0
```

**Workflow Steps:**
1. Extracts version `1.0.0` from tag `v1.0.0`
2. Overrides any version in Directory.Build.props
3. Builds with `/p:Version=1.0.0`
4. Runs tests
5. Creates packages with version `1.0.0`
6. Publishes to NuGet.org

**Tag Formats:**
- Stable: `v1.0.0`, `v2.1.3`
- Pre-release: `v1.0.0-alpha`, `v2.0.0-beta.1`, `v3.0.0-rc.2`

**How to Use:**
```bash
# For stable release
git tag v1.0.0
git push origin v1.0.0

# For pre-release
git tag v1.0.0-beta
git push origin v1.0.0-beta
```

### 3. Manual Workflow Dispatch

You can manually trigger the workflow from GitHub with a custom version.

**How to Use:**
1. Go to GitHub repository
2. Click **Actions** tab
3. Select **Deploy to NuGet** workflow
4. Click **Run workflow** button (right side)
5. Select branch (usually `master`)
6. Enter version in the input field (optional):
   - Leave empty: Uses Directory.Build.props version
   - Enter version: e.g., `0.0.8-preview`, `1.0.0`
7. Click **Run workflow**

## Prerequisites

### Required Secret

The workflow requires a secret named `NUGET_API_KEY` to be configured.

**Setup:**
1. Go to https://www.nuget.org/account/apikeys
2. Create a new API key with "Push" permission
3. Go to GitHub repository → Settings → Secrets and variables → Actions
4. Click **New repository secret**
5. Name: `NUGET_API_KEY`
6. Value: Paste your NuGet API key
7. Click **Add secret**

### Environment Protection (Optional)

The workflow uses a `production` environment. You can configure protection rules:

1. Go to Settings → Environments → production
2. Configure:
   - Required reviewers (recommended for stable releases)
   - Wait timer
   - Deployment branches (limit to `master` or tags)

## Version Priority

When multiple version sources are available, the workflow uses this priority:

1. **Manual input** (workflow_dispatch) - Highest priority
2. **Git tag** (`v*` tags)
3. **Directory.Build.props** - Default/fallback

## Examples

### Example 1: Regular Alpha Release

```bash
# Update Directory.Build.props
cat > src/Directory.Build.props << 'EOF'
<VersionPrefix>0.0.7</VersionPrefix>
<VersionSuffix>alpha</VersionSuffix>
EOF

# Commit and push
git add src/Directory.Build.props
git commit -m "Bump to 0.0.7-alpha"
git push origin master

# Workflow publishes: OakIdeas.GenericRepository.0.0.7-alpha.nupkg
```

### Example 2: Beta Release via Tag

```bash
# Create and push tag (no need to edit Directory.Build.props)
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1

# Workflow publishes: OakIdeas.GenericRepository.1.0.0-beta.1.nupkg
```

### Example 3: Stable Release via Tag

```bash
# Create and push tag
git tag v1.0.0
git push origin v1.0.0

# Workflow publishes: OakIdeas.GenericRepository.1.0.0.nupkg
```

### Example 4: Testing Version Manually

```bash
# Trigger workflow manually with test version
# Go to GitHub Actions → Deploy to NuGet → Run workflow
# Enter: 0.0.7-test.1
# Click: Run workflow

# Workflow publishes: OakIdeas.GenericRepository.0.0.7-test.1.nupkg
```

## Monitoring Deployments

### Check Workflow Status

1. Go to GitHub → Actions
2. Click on the workflow run
3. View logs for each step

### Check NuGet.org

1. Go to https://www.nuget.org/packages/OakIdeas.GenericRepository
2. Check if new version is listed
3. Note: It may take a few minutes to appear

### Check Workflow Artifacts

The workflow lists generated packages before publishing:
- Check the "List generated packages" step in the workflow logs

## Troubleshooting

### Version Not Incrementing

**Problem:** Published packages have wrong version

**Solution:**
- For tag-based: Verify tag format starts with `v`
- For Directory.Build.props: Check the file was committed and pushed
- For manual: Verify you entered the correct version
- Check workflow logs for "Extract version" step

### Package Already Exists

**Problem:** `Package already exists` error

**Solution:**
- The workflow uses `--skip-duplicate` flag
- Increment version in Directory.Build.props
- Or create a new tag with higher version

### API Key Errors

**Problem:** `401 Unauthorized` or `403 Forbidden`

**Solution:**
- Verify `NUGET_API_KEY` secret is configured
- Check API key hasn't expired
- Ensure API key has "Push" permission
- Regenerate key on NuGet.org if needed

### Build Failures

**Problem:** Build or test failures prevent deployment

**Solution:**
- Workflow only publishes if build and tests pass
- Check build logs for errors
- Fix issues locally and push again
- Tests are required to pass before publishing

## Best Practices

### For Development/Pre-releases

- Use Directory.Build.props with alpha/beta suffix
- Push to master to trigger deployment
- Example: `0.0.7-alpha`, `0.1.0-beta`

### For Stable Releases

- Use git tags without editing Directory.Build.props
- Tag format: `v1.0.0`, `v2.1.0`
- This keeps the process clean and traceable

### For Hotfixes

- Create tag with patch version: `v1.0.1`
- Push tag to deploy immediately
- Update Directory.Build.props afterward for consistency

### For Testing

- Use manual workflow dispatch
- Use unique version suffix: `0.0.7-test.1`, `0.0.7-preview.1`
- Test on a package before doing official release

## Workflow Permissions

The workflow has minimal permissions:
- `contents: read` - Read repository code
- `packages: write` - Publish to NuGet.org

## Related Documentation

- [VERSIONING.md](VERSIONING.md) - Comprehensive versioning guide
- [VERSION-UPDATE-GUIDE.md](VERSION-UPDATE-GUIDE.md) - Quick reference
- [../docs/workflows.md](../docs/workflows.md) - All GitHub workflows
