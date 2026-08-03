#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
corpus_dir="${RADZEN_EXTERNAL_CORPUS:-$repo_root/artifacts/external-validation-corpus}"

fixtures=(
  plain-text
  truetype-subset
  tables
  image
  gradients
  overlapping-z-order
  flat-lists
  tagged-accessible
  tagged-pdfa-level-a
  encrypted
  signed
  timestamped
)

for tool in qpdf gs verapdf dotnet; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "FATAL: required tool '$tool' is not on PATH"
    exit 2
  fi
done

rm -rf "$corpus_dir"
mkdir -p "$corpus_dir"

echo "== exporting corpus to $corpus_dir =="
RADZEN_EXTERNAL_CORPUS="$corpus_dir" dotnet test "$repo_root/Radzen.Blazor.Tests/Radzen.Blazor.Tests.csproj" \
  --filter "FullyQualifiedName~ExternalValidationCorpusExport" \
  || { echo "FATAL: corpus export test failed"; exit 3; }

for name in "${fixtures[@]}"; do
  if [ ! -s "$corpus_dir/$name.pdf" ]; then
    echo "FATAL: fixture $name.pdf was not exported"
    exit 3
  fi
done

echo
echo "== qpdf --check =="
for name in "${fixtures[@]}"; do
  file="$corpus_dir/$name.pdf"
  if [ "$name" = "encrypted" ]; then
    qpdf_args=(--password=user --check)
  else
    qpdf_args=(--check)
  fi

  if ! output="$(qpdf "${qpdf_args[@]}" "$file" 2>&1)"; then
    echo "FAIL qpdf $name"
    echo "$output"
    exit 4
  fi

  if printf '%s\n' "$output" | grep -q "WARNING"; then
    echo "FAIL qpdf $name (warnings)"
    echo "$output"
    exit 4
  fi

  echo "PASS qpdf $name"
done

echo
echo "== ghostscript =="
for name in "${fixtures[@]}"; do
  file="$corpus_dir/$name.pdf"
  gs_args=(-dNOPAUSE -dBATCH -dQUIET -sDEVICE=nullpage)
  if [ "$name" = "encrypted" ]; then
    gs_args+=(-sPDFPassword=user)
  fi

  if ! output="$(gs "${gs_args[@]}" "$file" 2>&1)"; then
    echo "FAIL ghostscript $name"
    echo "$output"
    exit 5
  fi

  if printf '%s\n' "$output" | grep -q "Error"; then
    echo "FAIL ghostscript $name (error output)"
    echo "$output"
    exit 5
  fi

  echo "PASS ghostscript $name"
done

echo
echo "== verapdf =="
verify_verapdf() {
  local name="$1"
  local flavour="$2"
  local file="$corpus_dir/$name.pdf"
  local output

  if ! output="$(verapdf --flavour "$flavour" "$file" 2>/dev/null)"; then
    echo "FAIL verapdf $name ($flavour)"
    echo "$output"
    exit 6
  fi

  if ! printf '%s\n' "$output" | grep -q 'isCompliant="true"'; then
    echo "FAIL verapdf $name ($flavour) not compliant"
    echo "$output"
    exit 6
  fi

  echo "PASS verapdf $name ($flavour)"
}

verify_verapdf tagged-accessible ua1
verify_verapdf tagged-pdfa-level-a 2a

echo
echo "external validation passed: ${#fixtures[@]} fixtures"
