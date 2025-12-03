# GitHub Workflows

This document describes the GitHub Actions workflows configured for this repository.

## Overview

The repository uses GitHub Actions for continuous integration, deployment, and security scanning. All workflows are configured to use .NET 10.0, the latest version of the .NET platform.

## Workflows

### Build & Test

**Workflow File:** `.github/workflows/build-and-test.yml`

**Triggers:**
- Pull requests to `master` branch
- Pushes to `master` branch
- Manual workflow dispatch

**What it does:**
- Runs on multiple operating systems (Ubuntu, Windows, macOS)
- Builds the solution in Release configuration
- Runs all unit tests
- Collects code coverage data
- Uploads test results and coverage artifacts

**Key Features:**
- Dependency caching for faster builds
- Parallel execution across different OS
- Test result retention for 30 days
- Concurrency control to cancel outdated runs

### Deploy to NuGet

**Workflow File:** `.github/workflows/deploy.yml`

**Triggers:**
- Pushes to `master` branch
- Version tags (v*)
- Manual workflow dispatch

**What it does:**
- Builds the solution in Release configuration
- Runs all tests to ensure quality
- Generates NuGet packages
- Publishes packages to NuGet.org

**Requirements:**
- `NUGET_API_KEY` secret must be configured in repository settings

**Key Features:**
- Environment protection (production)
- Automatic package versioning from tags
- Skip duplicate packages
- Full git history for proper versioning

### CodeQL Security Analysis

**Workflow File:** `.github/workflows/codeql.yml`

**Triggers:**
- Pull requests to `master` branch
- Pushes to `master` branch
- Weekly schedule (Mondays at midnight UTC)
- Manual workflow dispatch

**What it does:**
- Performs static code analysis for security vulnerabilities
- Checks for common security issues in C# code
- Reports findings to GitHub Security tab
- Uses extended security queries

**Key Features:**
- Automated weekly scans
- Security-extended query suite
- Integration with GitHub Advanced Security

### Dependency Review

**Workflow File:** `.github/workflows/dependency-review.yml`

**Triggers:**
- Pull requests to `master` branch

**What it does:**
- Reviews dependency changes in pull requests
- Checks for known vulnerabilities
- Fails on moderate or higher severity issues
- Comments summary in pull requests

**Key Features:**
- Automatic vulnerability detection
- Pull request comments
- Configurable severity thresholds

### PR Labeler

**Workflow File:** `.github/workflows/labeler.yml`
**Configuration:** `.github/labeler.yml`

**Triggers:**
- Pull requests opened, synchronized, or reopened

**What it does:**
- Automatically labels pull requests based on changed files
- Uses patterns defined in `.github/labeler.yml`

**Labels Applied:**
- `area: core` - Changes to core repository
- `area: memory` - Changes to in-memory implementation
- `area: ef-core` - Changes to Entity Framework Core implementation
- `area: middleware` - Changes to middleware components
- `tests` - Test-related changes
- `documentation` - Documentation updates
- `ci/cd` - CI/CD workflow changes
- `dependencies` - Dependency updates

## Dependabot

**Configuration:** `.github/dependabot.yml`

**What it does:**
- Automatically creates pull requests for dependency updates
- Monitors both NuGet packages and GitHub Actions
- Runs weekly on Mondays
- Groups related dependencies together

**Update Strategies:**
- **NuGet packages**: Weekly updates, grouped by category (testing, EF Core, etc.)
- **GitHub Actions**: Weekly updates for workflow actions

**Key Features:**
- Automatic PR creation
- Configurable reviewers
- Automatic labeling
- Grouped updates for related packages

## Status Badges

The repository README includes status badges for:
- Build & Test workflow
- Deploy to NuGet workflow
- CodeQL Security Analysis workflow

Badges automatically update based on the latest workflow runs.

## Best Practices

### For Contributors

1. **Pull Requests**: All workflows run automatically on PRs
2. **Testing**: Ensure tests pass locally before pushing
3. **Security**: Address CodeQL findings promptly
4. **Dependencies**: Review Dependabot PRs regularly

### For Maintainers

1. **Secrets Management**: Keep `NUGET_API_KEY` secure and up-to-date
2. **Branch Protection**: Require workflows to pass before merging
3. **Security Alerts**: Monitor GitHub Security tab regularly
4. **Dependency Updates**: Review and merge Dependabot PRs weekly

## Troubleshooting

### Build Failures

1. Check workflow logs in GitHub Actions tab
2. Verify .NET 10.0 SDK compatibility
3. Ensure all dependencies are restored
4. Check for breaking changes in dependencies

### Test Failures

1. Review test output in workflow artifacts
2. Run tests locally to reproduce
3. Check for environment-specific issues
4. Verify test data and fixtures

### Deployment Issues

1. Verify `NUGET_API_KEY` secret is set correctly
2. Check NuGet.org service status
3. Review package versioning
4. Ensure all tests pass before deployment

### Security Alerts

1. Review CodeQL findings in Security tab
2. Address moderate+ severity issues promptly
3. Use dependency review feedback in PRs
4. Keep dependencies up-to-date

## Configuration Files

- `.github/workflows/` - Workflow definitions
- `.github/dependabot.yml` - Dependabot configuration
- `.github/labeler.yml` - PR labeler patterns

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [CodeQL Documentation](https://codeql.github.com/docs/)
- [Dependabot Documentation](https://docs.github.com/en/code-security/dependabot)
- [.NET 10.0 Documentation](https://learn.microsoft.com/en-us/dotnet/)

## Workflow Permissions

All workflows use minimal required permissions:

- **Build & Test**: Read contents
- **Deploy**: Read contents, write packages
- **CodeQL**: Read contents, write security events
- **Dependency Review**: Read contents, write pull requests
- **PR Labeler**: Read contents, write pull requests

This follows the principle of least privilege for security.
