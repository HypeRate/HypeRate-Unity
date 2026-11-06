# Releasing HypeRate for Unity

The existing `HypeRate/HypeRate-Unity-SDK` repository is the canonical source for
both distribution channels. Runtime, Editor integration, documentation, tests,
and samples live in `Packages/io.hyperate.unity`.

## Versioning

Version 2 is a breaking replacement for the original singleton-based SDK. Keep
the version in `package.json` and the heading in `CHANGELOG.md` in sync.

## Release checks

1. Run `./ReleaseTools/validate-package.sh`.
2. Open the repository with the oldest supported Unity version and the latest
   supported Unity version.
3. Confirm a clean compile with no package-originated warnings.
4. Run the package tests.
5. Test Editor setup, connect, disconnect, reconnect, and Fast Enter Play Mode.
6. Import the Basic Usage sample.
7. Search the complete Git diff for credentials and private data.
8. Run Unity's Asset Store Validator against the generated package.

## GitHub UPM release

1. Set the stable version in `Packages/io.hyperate.unity/package.json`.
2. Update `Packages/io.hyperate.unity/CHANGELOG.md`.
3. Merge the release branch.
4. Create and push an annotated tag such as `v2.0.0`.
5. Create a GitHub release from the tag using the changelog entry.

Consumers install the tagged package with:

```text
https://github.com/HypeRate/HypeRate-Unity-SDK.git?path=/Packages/io.hyperate.unity#v2.0.0
```

## Asset Store package

Build the classic package used to update the existing Asset Store listing:

```bash
./ReleaseTools/build-asset-store-package.sh \
  /path/to/Unity.app/Contents/MacOS/Unity
```

The command creates `Builds/HypeRate-for-Unity-<version>.unitypackage` from the
same package source. Tests, repository tooling, and local credentials are not
included.

Upload the package through Asset Store Publishing Tools, run the Validator, and
submit it as an update to the existing product. The listing and documentation
must disclose HypeRate API access requirements, current commercial terms,
unsupported platforms, credential handling, and any AI-assisted functional
content required by the current submission guidelines.
