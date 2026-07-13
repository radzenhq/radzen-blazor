#!/usr/bin/env bash
#
# Guard-the-guard: prove the WASM-safety/determinism analyzer fence actually fails the build.
#
# The fence is subtle machinery (a globalconfig that silences RS0030/RS0031 everywhere, re-enabled
# as 'error' only for the PDF library trees via path-scoped .editorconfig sections, fed by
# BannedSymbols.txt). A silent regression - a stray severity flip, a moved file, a dropped
# AdditionalFiles wiring - would let banned APIs slip into the deterministic PDF output with a green
# build. This script plants a banned API in each fenced tree, builds, and asserts the build FAILS
# with RS0030. It restores the tree on exit no matter what.
#
# Usage: verify-analyzer-fence.sh [path-to-Radzen.Blazor-project-dir]
# Exit 0 => fence fired for every tree (good). Non-zero => fence did NOT fire (regression).

set -u

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="${1:-$(cd "$SCRIPT_DIR/../../Radzen.Blazor" && pwd)}"
CSPROJ="$PROJECT_DIR/Radzen.Blazor.csproj"
TFM="${FENCE_TFM:-net10.0}"

if [ ! -f "$CSPROJ" ]; then
  echo "FAIL: project not found at $CSPROJ" >&2
  exit 2
fi

# One probe per fenced tree; each uses a banned symbol that must trip RS0030.
TREES=("Documents/Pdf" "Documents/Crypto" "Documents/Codes" "Documents/Markdown")
PROBES=()

cleanup() {
  for p in "${PROBES[@]:-}"; do
    [ -n "$p" ] && rm -f "$p"
  done
}
trap cleanup EXIT

for tree in "${TREES[@]}"; do
  dir="$PROJECT_DIR/$tree"
  if [ ! -d "$dir" ]; then
    echo "FAIL: fenced tree missing: $dir" >&2
    exit 2
  fi
  probe="$dir/__FenceProbe.g.cs"
  PROBES+=("$probe")
  # Unique type per tree - identical type names across trees would raise CS0101 and mask RS0030.
  suffix="$(printf '%s' "$tree" | tr -cd 'A-Za-z')"
  cat > "$probe" <<EOF
namespace Radzen.Documents.FenceProbe { internal static class __FenceProbe$suffix {
  public static object Planted() => System.Guid.NewGuid();
} }
EOF
done

OUTPUT="$(dotnet build "$CSPROJ" -f "$TFM" -v q 2>&1)"
STATUS=$?

FAILURES=0
for tree in "${TREES[@]}"; do
  if ! printf '%s\n' "$OUTPUT" | grep -q "$tree/__FenceProbe.g.cs.*RS0030"; then
    echo "FAIL: fence did NOT fire in $tree (no RS0030 for planted banned API)" >&2
    FAILURES=1
  fi
done

if [ "$STATUS" -eq 0 ]; then
  echo "FAIL: build SUCCEEDED with a banned API planted - the fence is not enforced" >&2
  FAILURES=1
fi

if [ "$FAILURES" -eq 0 ]; then
  echo "OK: analyzer fence fired (RS0030) in all ${#TREES[@]} PDF library trees and failed the build"
  exit 0
fi

exit 1
