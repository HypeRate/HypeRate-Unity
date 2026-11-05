#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PACKAGE_ROOT="$REPOSITORY_ROOT/Packages/io.hyperate.unity"
UNITY_EDITOR="${1:-/Volumes/Skynet/6000.6.8f1/Unity.app/Contents/MacOS/Unity}"

if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Unity Editor not found or not executable: $UNITY_EDITOR" >&2
  echo "Pass the Unity executable as the first argument." >&2
  exit 1
fi

PACKAGE_VERSION="$(sed -n 's/^[[:space:]]*"version":[[:space:]]*"\([^"]*\)",/\1/p' "$PACKAGE_ROOT/package.json")"
if [[ -z "$PACKAGE_VERSION" ]]; then
  echo "Could not read the package version." >&2
  exit 1
fi

STAGING_PROJECT="$(mktemp -d /tmp/hyperate-asset-store.XXXXXX)"
OUTPUT_DIRECTORY="$REPOSITORY_ROOT/Builds"
OUTPUT_PACKAGE="$OUTPUT_DIRECTORY/HypeRate-for-Unity-$PACKAGE_VERSION.unitypackage"
EXPORT_LOG="$OUTPUT_DIRECTORY/asset-store-export-$PACKAGE_VERSION.log"

cleanup() {
  if [[ -n "${STAGING_PROJECT:-}" && -d "$STAGING_PROJECT" && "$STAGING_PROJECT" == /tmp/hyperate-asset-store.* ]]; then
    rm -rf "$STAGING_PROJECT"
  fi
}
trap cleanup EXIT

mkdir -p \
  "$STAGING_PROJECT/Assets/HypeRate" \
  "$STAGING_PROJECT/Assets/HypeRate/Documentation" \
  "$STAGING_PROJECT/Assets/HypeRate/Samples" \
  "$STAGING_PROJECT/Assets/Editor" \
  "$OUTPUT_DIRECTORY"

cp -R "$PACKAGE_ROOT/Runtime" "$STAGING_PROJECT/Assets/HypeRate/Runtime"
cp -R "$PACKAGE_ROOT/Editor" "$STAGING_PROJECT/Assets/HypeRate/Editor"
cp -R "$PACKAGE_ROOT/Samples~/". "$STAGING_PROJECT/Assets/HypeRate/Samples/"
cp -R "$PACKAGE_ROOT/Documentation~/". "$STAGING_PROJECT/Assets/HypeRate/Documentation/"
cp "$PACKAGE_ROOT/README.md" "$STAGING_PROJECT/Assets/HypeRate/Documentation/README.md"
cp "$PACKAGE_ROOT/Third Party Notices.md" "$STAGING_PROJECT/Assets/HypeRate/Documentation/Third Party Notices.md"
cp "$SCRIPT_DIR/AssetStoreExporter.cs" "$STAGING_PROJECT/Assets/Editor/AssetStoreExporter.cs"

HYPERATE_EXPORT_PATH="$OUTPUT_PACKAGE" "$UNITY_EDITOR" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$STAGING_PROJECT" \
  -executeMethod HypeRate.ReleaseTools.AssetStoreExporter.Export \
  -logFile "$EXPORT_LOG"

if [[ ! -s "$OUTPUT_PACKAGE" ]]; then
  echo "Unity did not create the expected package. See: $EXPORT_LOG" >&2
  exit 1
fi

echo "Created $OUTPUT_PACKAGE"
echo "Unity log: $EXPORT_LOG"
