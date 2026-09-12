#!/usr/bin/env bash
# Publish the current DeathTweaks release package to Thunderstore.
#
# Usage: scripts/publish-thunderstore.sh [--dry-run]
#
# The package zip is built by `dotnet build -c Release`; this script only uploads it.
# The API token is read from TCLI_AUTH_TOKEN, or from 1Password when the gitignored
# .publish.env at the repository root defines THUNDERSTORE_TOKEN_OP_REF (an op:// reference).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mod_dir="$repo_root/DeathTweaks"
dry_run=false
[[ "${1:-}" == "--dry-run" ]] && dry_run=true

command -v tcli >/dev/null || { echo "tcli is not installed: dotnet tool install -g tcli" >&2; exit 1; }

version="$(python3 -c "import json,sys; print(json.load(open(sys.argv[1]))['version_number'])" "$mod_dir/Package/manifest.json")"
zip="$repo_root/dist/DeathTweaks-$version.zip"

echo "Building release package $version"
dotnet build "$mod_dir/DeathTweaks.csproj" -c Release --nologo -v quiet
[[ -f "$zip" ]] || { echo "Package not found: $zip" >&2; exit 1; }

if [[ -z "${TCLI_AUTH_TOKEN:-}" ]]; then
    [[ -f "$repo_root/.publish.env" ]] && source "$repo_root/.publish.env"
    if [[ -n "${THUNDERSTORE_TOKEN_OP_REF:-}" ]] && ! $dry_run; then
        command -v op >/dev/null || { echo "1Password CLI (op) is not installed" >&2; exit 1; }
        TCLI_AUTH_TOKEN="$(op read "$THUNDERSTORE_TOKEN_OP_REF")"
        export TCLI_AUTH_TOKEN
    fi
fi

if $dry_run; then
    echo "Dry run: would publish $zip as Muindor-DeathTweaks-$version with $mod_dir/thunderstore.toml"
    exit 0
fi

[[ -n "${TCLI_AUTH_TOKEN:-}" ]] || { echo "No token: set TCLI_AUTH_TOKEN or THUNDERSTORE_TOKEN_OP_REF in .publish.env" >&2; exit 1; }

tcli publish --config-path "$mod_dir/thunderstore.toml" --file "$zip"
