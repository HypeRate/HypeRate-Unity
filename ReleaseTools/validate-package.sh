#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PACKAGE_ROOT="$REPOSITORY_ROOT/Packages/io.hyperate.unity"

jq -e '
  .name == "io.hyperate.unity" and
  (.displayName | length > 0) and
  (.version | test("^[0-9]+\\.[0-9]+\\.[0-9]+([-.][0-9A-Za-z.-]+)?$")) and
  (.unity | length > 0) and
  (.unityRelease | length > 0) and
  (.author.name | length > 0) and
  (.description | length > 0)
' "$PACKAGE_ROOT/package.json" >/dev/null

PACKAGE_VERSION="$(jq -r '.version' "$PACKAGE_ROOT/package.json")"
if ! grep -Fq "## [$PACKAGE_VERSION]" "$PACKAGE_ROOT/CHANGELOG.md"; then
  echo "CHANGELOG.md has no entry for $PACKAGE_VERSION." >&2
  exit 1
fi

if rg -n 'namespace[[:space:]]+[^[:space:]]*Unity' \
  "$PACKAGE_ROOT/Runtime" \
  "$PACKAGE_ROOT/Editor" \
  "$PACKAGE_ROOT/Tests"; then
  echo "Custom namespaces must not include the Unity trademark." >&2
  exit 1
fi

if find "$PACKAGE_ROOT" -name '.DS_Store' -o -name 'HypeRateSettings.asset' | grep -q .; then
  echo "The package contains a generated or local-only file." >&2
  exit 1
fi

while IFS= read -r source_file; do
  if [[ ! -f "$source_file.meta" ]]; then
    echo "Missing meta file: ${source_file#"$REPOSITORY_ROOT/"}.meta" >&2
    exit 1
  fi
done < <(find \
  "$PACKAGE_ROOT/Runtime" \
  "$PACKAGE_ROOT/Editor" \
  "$PACKAGE_ROOT/Tests" \
  "$PACKAGE_ROOT/Samples~" \
  -type f ! -name '*.meta' | sort)

while IFS= read -r package_path; do
  relative_path="${package_path#"$PACKAGE_ROOT/"}"
  if (( ${#relative_path} >= 150 )); then
    echo "Package path is 150 characters or longer: $relative_path" >&2
    exit 1
  fi
done < <(find "$PACKAGE_ROOT" -mindepth 1 -print | sort)

echo "Validated io.hyperate.unity $PACKAGE_VERSION"
