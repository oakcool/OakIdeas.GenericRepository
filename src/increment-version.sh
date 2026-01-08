#!/bin/bash
set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Default values
INCREMENT_TYPE="patch"
NEW_SUFFIX=""
REMOVE_SUFFIX=false

# Help text
show_help() {
    cat << EOF
Usage: ./increment-version.sh [OPTIONS]

Automatically increments the version in Directory.Build.props

OPTIONS:
    -t, --type TYPE      Increment type: major, minor, or patch (default: patch)
    -s, --suffix SUFFIX  Set version suffix (e.g., alpha, beta, rc1)
    -r, --remove-suffix  Remove version suffix for stable release
    -h, --help           Show this help message

EXAMPLES:
    ./increment-version.sh
        Increments patch version: 0.0.6-alpha -> 0.0.7-alpha
    
    ./increment-version.sh --type minor
        Increments minor version: 0.0.6-alpha -> 0.1.0-alpha
    
    ./increment-version.sh --suffix beta
        Changes suffix: 0.0.6-alpha -> 0.0.7-beta
    
    ./increment-version.sh --remove-suffix
        Creates stable version: 0.0.6-alpha -> 0.0.7

EOF
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -t|--type)
            INCREMENT_TYPE="$2"
            shift 2
            ;;
        -s|--suffix)
            NEW_SUFFIX="$2"
            shift 2
            ;;
        -r|--remove-suffix)
            REMOVE_SUFFIX=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo -e "${RED}Error: Unknown option $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

# Validate increment type
case "$INCREMENT_TYPE" in
    major|minor|patch) ;;
    *)
        echo -e "${RED}Error: Invalid increment type '$INCREMENT_TYPE'. Must be: major, minor, or patch${NC}"
        exit 1
        ;;
esac

# Find Directory.Build.props
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROPS_FILE="$SCRIPT_DIR/Directory.Build.props"

if [ ! -f "$PROPS_FILE" ]; then
    echo -e "${RED}Error: Directory.Build.props not found at: $PROPS_FILE${NC}"
    exit 1
fi

echo -e "${CYAN}📄 Reading version from: $PROPS_FILE${NC}"

# Read current version
CURRENT_VERSION=$(grep -oP '<VersionPrefix[^>]*>\K[^<]+' "$PROPS_FILE" || true)
if [ -z "$CURRENT_VERSION" ]; then
    echo -e "${RED}Error: Could not find VersionPrefix in Directory.Build.props${NC}"
    exit 1
fi

echo -e "${GRAY}📌 Current version: $CURRENT_VERSION${NC}"

# Read current suffix
CURRENT_SUFFIX=$(grep -oP '<VersionSuffix[^>]*>\K[^<]+' "$PROPS_FILE" || true)
if [ -n "$CURRENT_SUFFIX" ]; then
    echo -e "${GRAY}📌 Current suffix: $CURRENT_SUFFIX${NC}"
fi

# Parse version components
if [[ $CURRENT_VERSION =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
    MAJOR="${BASH_REMATCH[1]}"
    MINOR="${BASH_REMATCH[2]}"
    PATCH="${BASH_REMATCH[3]}"
else
    echo -e "${RED}Error: Invalid version format: $CURRENT_VERSION. Expected format: X.Y.Z${NC}"
    exit 1
fi

# Increment version
case "$INCREMENT_TYPE" in
    major)
        MAJOR=$((MAJOR + 1))
        MINOR=0
        PATCH=0
        echo -e "${YELLOW}⬆️  Incrementing MAJOR version${NC}"
        ;;
    minor)
        MINOR=$((MINOR + 1))
        PATCH=0
        echo -e "${YELLOW}⬆️  Incrementing MINOR version${NC}"
        ;;
    patch)
        PATCH=$((PATCH + 1))
        echo -e "${YELLOW}⬆️  Incrementing PATCH version${NC}"
        ;;
esac

NEW_VERSION="$MAJOR.$MINOR.$PATCH"
echo -e "${GREEN}✨ New version: $NEW_VERSION${NC}"

# Update version in file
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' "s|<VersionPrefix[^>]*>[^<]*</VersionPrefix>|<VersionPrefix>$NEW_VERSION</VersionPrefix>|g" "$PROPS_FILE"
else
    # Linux
    sed -i "s|<VersionPrefix[^>]*>[^<]*</VersionPrefix>|<VersionPrefix>$NEW_VERSION</VersionPrefix>|g" "$PROPS_FILE"
fi

# Handle suffix
if [ "$REMOVE_SUFFIX" = true ]; then
    echo -e "${YELLOW}🗑️  Removing version suffix${NC}"
    if [[ "$OSTYPE" == "darwin"* ]]; then
        sed -i '' "s|<VersionSuffix[^>]*>[^<]*</VersionSuffix>|<VersionSuffix></VersionSuffix>|g" "$PROPS_FILE"
    else
        sed -i "s|<VersionSuffix[^>]*>[^<]*</VersionSuffix>|<VersionSuffix></VersionSuffix>|g" "$PROPS_FILE"
    fi
    FINAL_SUFFIX=""
elif [ -n "$NEW_SUFFIX" ]; then
    echo -e "${YELLOW}🏷️  Setting suffix to: $NEW_SUFFIX${NC}"
    if [[ "$OSTYPE" == "darwin"* ]]; then
        sed -i '' "s|<VersionSuffix[^>]*>[^<]*</VersionSuffix>|<VersionSuffix>$NEW_SUFFIX</VersionSuffix>|g" "$PROPS_FILE"
    else
        sed -i "s|<VersionSuffix[^>]*>[^<]*</VersionSuffix>|<VersionSuffix>$NEW_SUFFIX</VersionSuffix>|g" "$PROPS_FILE"
    fi
    FINAL_SUFFIX="$NEW_SUFFIX"
else
    FINAL_SUFFIX="$CURRENT_SUFFIX"
fi

# Display final version
if [ -n "$FINAL_SUFFIX" ]; then
    FINAL_VERSION="$NEW_VERSION-$FINAL_SUFFIX"
else
    FINAL_VERSION="$NEW_VERSION"
fi

echo ""
echo -e "${CYAN}═══════════════════════════════════════${NC}"
echo -e "  ${GREEN}Final Version: $FINAL_VERSION 🎉${NC}"
echo -e "${CYAN}═══════════════════════════════════════${NC}"
echo ""

echo -e "${GREEN}✅ Updated Directory.Build.props${NC}"
echo ""
echo -e "${CYAN}Next steps:${NC}"
echo -e "${GRAY}  1. Review the changes: git diff Directory.Build.props${NC}"
echo -e "${GRAY}  2. Commit: git add Directory.Build.props && git commit -m 'Bump version to $FINAL_VERSION'${NC}"
echo -e "${GRAY}  3. Push: git push origin master${NC}"
echo ""
echo -e "${CYAN}Or create a tag for immediate release:${NC}"
echo -e "${GRAY}  git tag v$FINAL_VERSION && git push origin v$FINAL_VERSION${NC}"
echo ""
