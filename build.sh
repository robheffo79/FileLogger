#!/bin/bash
###############################################################################
# Build script for HeffernanTech.Extensions.FileLogging NuGet package
#
# Cleans the build, runs tests, and creates a NuGet package.
# Exits with code 0 on success, non-zero on failure.
#
# Usage: ./build.sh [OPTIONS]
# Options:
#   --release      Build in Release configuration (default: Debug)
#   --no-tests     Skip running tests
#   -h, --help     Show this help message
###############################################################################

set -euo pipefail

# Configuration
CONFIGURATION="Debug"
RUN_TESTS=true

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --release)
            CONFIGURATION="Release"
            shift
            ;;
        --no-tests)
            RUN_TESTS=false
            shift
            ;;
        -h|--help)
            grep '^#' "$0" | tail -n +2
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

PROJECT_NAME="HeffernanTech.Extensions.FileLogger"

# Helper functions
print_header() {
    echo -e "${CYAN}========================================"
    echo -e "${CYAN}HeffernanTech.Extensions.FileLogging"
    echo -e "${CYAN}Build & Package Script"
    echo -e "${CYAN}========================================${NC}"
    echo ""
}

print_step() {
    echo -e "${YELLOW}[$1/4] $2${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${CYAN}$1${NC}"
}

# Main build process
print_header

# Step 1: Clean
print_step "1" "Cleaning..."
if ! dotnet clean --configuration "$CONFIGURATION" --verbosity quiet 2>/dev/null; then
    print_error "Clean failed"
    exit 1
fi
print_success "Clean successful"
echo ""

# Step 2: Build
print_step "2" "Building ($CONFIGURATION)..."
if ! dotnet build --configuration "$CONFIGURATION" --no-incremental; then
    print_error "Build failed"
    exit 1
fi
print_success "Build successful"
echo ""

# Step 3: Tests
if [ "$RUN_TESTS" = true ]; then
    print_step "3" "Running tests..."
    TEST_OUTPUT=$(dotnet test "HeffernanTech.Extensions.FileLogger.Tests/HeffernanTech.Extensions.FileLogger.Tests.csproj" \
        --configuration "$CONFIGURATION" --no-build --verbosity quiet 2>&1)
    if echo "$TEST_OUTPUT" | grep -q "Passed!" && echo "$TEST_OUTPUT" | grep -q "Failed:.*0"; then
        print_success "All tests passed"
    else
        print_error "Tests failed"
        echo "$TEST_OUTPUT" | tail -5
        exit 1
    fi
else
    print_step "3" "Skipping tests (--no-tests)"
fi
echo ""

# Step 4: Pack
print_step "4" "Creating NuGet package..."

# Create output directory
PACK_OUTPUT="../nupkg"
mkdir -p "$PACK_OUTPUT"

if ! dotnet pack \
    --configuration "$CONFIGURATION" \
    --no-build \
    --output "$PACK_OUTPUT" \
    --verbosity minimal; then
    print_error "Pack failed"
    exit 1
fi
print_success "Package created successfully"

# Find and display package info
NUPKG=$(find "$PACK_OUTPUT" -name "HeffernanTech.Extensions.FileLogging.*.nupkg" -type f -printf '%T@ %p\n' | sort -rn | head -1 | cut -d' ' -f2-)

if [ -n "$NUPKG" ]; then
    SIZE=$(numfmt --to=iec-i --suffix=B "$( stat -f%z "$NUPKG" 2>/dev/null || stat -c%s "$NUPKG" 2>/dev/null )")
    FILENAME=$(basename "$NUPKG")
    VERSION="${FILENAME#HeffernanTech.Extensions.FileLogging.}"
    VERSION="${VERSION%.nupkg}"
    FULLPATH=$(cd "$(dirname "$NUPKG")" && pwd)/$(basename "$NUPKG")

    echo ""
    print_info "Package: $FILENAME"
    print_info "Version: $VERSION"
    print_info "Size: $SIZE"
    print_info "Path: $FULLPATH"
fi

echo ""
echo -e "${GREEN}========================================"
echo -e "${GREEN}✓ Build complete!"
echo -e "${GREEN}========================================${NC}"
exit 0
