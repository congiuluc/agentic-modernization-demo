#!/usr/bin/env bash
set -euo pipefail

readonly version="1.0.76"
if [[ "$(uname -s)" != "Linux" ]]; then
    echo "This setup script requires the Linux development container." >&2
    exit 1
fi
case "$(uname -m)" in
    x86_64) architecture="x64" ;;
    aarch64|arm64) architecture="arm64" ;;
    *) echo "Unsupported Modernize CLI architecture: $(uname -m)" >&2; exit 1 ;;
esac

readonly archive="modernize_${version}_linux_${architecture}.tar.gz"
readonly release="https://github.com/microsoft/modernize-cli/releases/download/v${version}"
temporary="$(mktemp -d)"
trap 'rm -f "$temporary/$archive" "$temporary/checksums.txt" "$temporary/modernize"; rmdir "$temporary"' EXIT

curl --fail --silent --show-error --location --retry 3 "$release/$archive" --output "$temporary/$archive"
curl --fail --silent --show-error --location --retry 3 "$release/modernize_${version}_checksums.txt" --output "$temporary/checksums.txt"
checksum="$(awk -v archive="$archive" '$2 == archive || $2 == "*" archive { print $1 }' "$temporary/checksums.txt")"
if [[ ! "$checksum" =~ ^[a-fA-F0-9]{64}$ ]]; then
    echo "Missing or invalid SHA-256 checksum for $archive." >&2
    exit 1
fi
(
    cd "$temporary"
    printf '%s  %s\n' "$checksum" "$archive" | sha256sum --check --strict -
)
tar -xzf "$temporary/$archive" -C "$temporary" modernize
chmod 0755 "$temporary/modernize"
installed_version="$("$temporary/modernize" --version)"
if [[ "$installed_version" != "$version" && "$installed_version" != "$version+"* ]]; then
    echo "Expected Modernize $version, got: $installed_version" >&2
    exit 1
fi
install -D -m 0755 "$temporary/modernize" "$HOME/.local/bin/modernize"

"$HOME/.local/bin/modernize" --version
gh --version
pwsh --version
dotnet --list-sdks
java -version
mvn --version
docker --version
printf '\nReady for Assess -> Plan -> Approve -> Execute -> Validate. See README.md.\n'
printf 'No authentication, AI assessment, application startup, or deployment was performed.\n'
