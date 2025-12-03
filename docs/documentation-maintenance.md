# Documentation Maintenance Guide

> How to keep agent documentation current and relevant

## Overview

This repository includes comprehensive documentation to enable effective interaction with AI coding agents and assistants. This guide explains how to maintain these documents.

## Documentation Files

### Agent-Specific Documentation

1. **copilot-instructions.md** (Root)
   - **Purpose**: GitHub Copilot specific coding guidelines
   - **Update When**: 
     - Target framework changes
     - New coding standards adopted
     - New design patterns introduced
     - Dependencies updated
     - Build/test processes change

2. **AGENTS.MD** (Root)
   - **Purpose**: Comprehensive agent capabilities and workflows
   - **Update When**:
     - Repository structure changes
     - New development workflows adopted
     - Agent capabilities need clarification
     - Common tasks examples need updates
     - API compatibility guidelines change

3. **.github/copilot-context.md** (.github directory)
   - **Purpose**: Quick reference for GitHub Copilot
   - **Update When**:
     - Key commands change
     - Common patterns evolve
     - Project layout changes
     - Target frameworks update

4. **ai-context.md** (Root)
   - **Purpose**: General AI assistant context and overview
   - **Update When**:
     - Architecture changes
     - Design decisions evolve
     - Dependencies added/removed
     - Common scenarios change
     - Code style standards update

### General Documentation

5. **README.md** (Root)
   - **Agent Section Added**: Links to all agent-specific documentation
   - **Update When**: New documentation files are added

## Maintenance Schedule

### On Every Major Release
- [ ] Review all agent documentation for accuracy
- [ ] Update version numbers mentioned
- [ ] Verify code examples still work
- [ ] Check all links are valid
- [ ] Update target framework information if changed

### On Framework Updates
- [ ] Update copilot-instructions.md with new framework features
- [ ] Update .github/copilot-context.md with new capabilities
- [ ] Review ai-context.md for deprecated patterns

### On Architecture Changes
- [ ] Update AGENTS.MD repository structure section
- [ ] Update ai-context.md architecture overview
- [ ] Review examples in all documentation

### On New Features
- [ ] Add examples to relevant documentation
- [ ] Update common tasks in AGENTS.MD
- [ ] Add patterns to copilot-instructions.md if applicable

## Consistency Checklist

When updating documentation, ensure:

- [ ] **Consistency**: Information is consistent across all files
- [ ] **Accuracy**: Examples compile and run correctly
- [ ] **Completeness**: New patterns/features are documented everywhere relevant
- [ ] **Currency**: Version numbers and framework references are current
- [ ] **Links**: Internal links between documents work correctly

## Common Update Scenarios

### Scenario 1: New Dependency Added

Update these files:
1. **copilot-instructions.md**: Add to "Key Dependencies" section
2. **ai-context.md**: Add to "Dependencies Management" section
3. **AGENTS.MD**: Add to relevant sections if it affects workflows

### Scenario 2: Target Framework Updated

Update these files:
1. **copilot-instructions.md**: Update "Target Frameworks" section
2. **.github/copilot-context.md**: Update "Quick Facts" section
3. **ai-context.md**: Update "Quick Facts for AI Assistants"
4. **AGENTS.MD**: Update "Overview" section

### Scenario 3: New Repository Pattern Added

Update these files:
1. **copilot-instructions.md**: Add to "Design Patterns" section
2. **AGENTS.MD**: Add to "Key Patterns and Concepts" section
3. **ai-context.md**: Add to "Useful Patterns in This Codebase"

### Scenario 4: Build Process Changed

Update these files:
1. **copilot-instructions.md**: Update "Build and Test Commands"
2. **.github/copilot-context.md**: Update "Key Commands"
3. **AGENTS.MD**: Update "Build and Test Commands"
4. **ai-context.md**: Update "Before Committing" checklist

## Quality Standards for Documentation

All documentation should be:

- **Clear**: Easy to understand for both humans and AI
- **Concise**: Get to the point without unnecessary verbosity
- **Practical**: Include real, working examples
- **Current**: Reflect the actual state of the codebase
- **Complete**: Cover all essential information for the topic

## Tips for Writing Agent Documentation

### Do:
- ✅ Use concrete examples with code snippets
- ✅ Explain the "why" behind decisions
- ✅ Include both success and error scenarios
- ✅ Keep formatting consistent
- ✅ Use bullet points and lists for scanability
- ✅ Include commands that can be copy-pasted

### Don't:
- ❌ Make assumptions about prior knowledge
- ❌ Use vague or ambiguous language
- ❌ Include outdated examples
- ❌ Forget to explain acronyms on first use
- ❌ Duplicate information unnecessarily

## Validation Process

Before committing documentation updates:

1. **Build and Test**
   ```bash
   dotnet build src/OakIdeas.GenericRepository.sln -c Release
   dotnet test src/OakIdeas.GenericRepository.sln -c Release
   ```

2. **Verify Examples**
   - Test that code examples compile
   - Verify commands actually work
   - Check that paths and file references are correct

3. **Cross-Reference**
   - Ensure consistency across all documentation files
   - Verify internal links work
   - Check that related information is synchronized

4. **Review for Clarity**
   - Read from the perspective of someone new to the project
   - Check for confusing or ambiguous statements
   - Ensure technical terms are explained

## Quick Reference: What Goes Where

| Content Type | Primary Location | Also Mention In |
|-------------|------------------|-----------------|
| Coding standards | copilot-instructions.md | .github/copilot-context.md |
| Build commands | All files | Especially copilot-instructions.md |
| Architecture overview | ai-context.md | AGENTS.MD |
| Common patterns | copilot-instructions.md | ai-context.md |
| Agent capabilities | AGENTS.MD | - |
| Quick reference | .github/copilot-context.md | - |
| Development workflow | AGENTS.MD | copilot-instructions.md |
| Design decisions | ai-context.md | copilot-instructions.md |

## Future Enhancements

Consider adding:
- [ ] Video tutorials or screencasts (link from documentation)
- [ ] Interactive examples or playground
- [ ] FAQ section based on common questions
- [ ] Troubleshooting guide for common issues
- [ ] Performance optimization guide
- [ ] Security best practices guide

## Contact

For questions about documentation maintenance, please:
- Open an issue on GitHub
- Contact the maintainers
- Propose changes via Pull Request

---

**Note**: This guide itself should be updated when new documentation files are added or the maintenance process changes.

**Last Updated**: 2025-12-03  
**Next Review**: On next major release
