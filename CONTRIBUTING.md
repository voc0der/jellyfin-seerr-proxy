# Contributing to Jellyfin Seerr Proxy

Issues and pull requests are welcome!

## Getting Started

1. Fork the repository
2. Create a feature branch from `main`
3. Make your changes
4. Submit a pull request

## Building

```bash
dotnet build --configuration Release
```

## Testing

Run tests locally before opening a PR:

```bash
dotnet test --configuration Release
```

## Plugin GUID Safety

Before the first release, and any time plugin metadata changes, verify that the plugin GUID is consistent and not already used by a known catalog:

```bash
bash scripts/verify-plugin-guid.sh
CHECK_REMOTE_GUIDS=1 bash scripts/verify-plugin-guid.sh
```

Do not copy GUIDs from Jellyfin's plugin template or from another plugin repository.

## Linting

Run lint checks locally before opening a PR:

```bash
dotnet format whitespace --verify-no-changes
dotnet format style --verify-no-changes --severity warn
```

## Reporting Issues

- Search existing issues before opening a new one
- Include Jellyfin version, plugin version, and relevant logs
- Include Seerr version and whether the Jellyfin user is linked in Seerr

## Pull Requests

- Keep changes focused and minimal
- Test against a running Jellyfin instance before submitting
- Describe what your PR changes and why
