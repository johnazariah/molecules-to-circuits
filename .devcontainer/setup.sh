#!/usr/bin/env bash
set -euo pipefail

dry_run=false
if [[ $# -eq 1 && "$1" == "--dry-run" ]]; then
    dry_run=true
elif [[ $# -ne 0 ]]; then
    echo "Usage: $0 [--dry-run]" >&2
    exit 2
fi

if [[ "$dry_run" == false && ! -f /.dockerenv && ! -f /run/.containerenv &&
      "${CODESPACES:-}" != true ]]; then
    echo "Run setup inside the devcontainer; use --dry-run to inspect it on the host." >&2
    exit 1
fi

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"
venv="$root/.venv"
tools="$root/_build/tools"

# Keep these paths aligned with devcontainer.json's remoteEnv.
export PATH="$venv/bin:$tools/node_modules/.bin:$PATH"
export PYTHON="$venv/bin/python"
export MMDC="$tools/node_modules/.bin/mmdc"
export PLAYWRIGHT_BROWSERS_PATH="$tools/browsers"
export MMDC_NO_SANDBOX=1

run() {
    if [[ "$dry_run" == true ]]; then
        printf '  '
        printf '%q ' "$@"
        printf '\n'
    else
        "$@"
    fi
}

echo "Setting up the book's checkout-local development environment..."
run sudo apt-get update -qq
run sudo apt-get install -y -qq \
    texlive-xetex texlive-latex-extra texlive-fonts-recommended \
    fonts-dejavu lmodern pandoc

run python3 -m venv "$venv"
run "$PYTHON" -m pip install --quiet \
    -r "$root/requirements-data.txt" -r "$root/requirements.txt" playwright
run env PUPPETEER_SKIP_DOWNLOAD=true npm install --prefix "$tools" \
    --no-audit --no-fund @mermaid-js/mermaid-cli@11.12.0
run "$PYTHON" -m playwright install --with-deps chromium

run dotnet --version
run "$PYTHON" -m jupyter_book --version
run "$MMDC" --version

echo ""
if [[ "$dry_run" == true ]]; then
    echo "Dry run only; no dependencies installed."
else
    echo "Environment ready. New VS Code/Codespaces terminals inherit these settings:"
fi
printf '  export PATH=%q:%q:"$PATH"\n' "$venv/bin" "$tools/node_modules/.bin"
printf '  export PYTHON=%q\n' "$PYTHON"
printf '  export MMDC=%q\n' "$MMDC"
printf '  export PLAYWRIGHT_BROWSERS_PATH=%q\n' "$PLAYWRIGHT_BROWSERS_PATH"
echo "  export MMDC_NO_SANDBOX=1  # disposable container only"
echo ""
echo "  dotnet fsi labs/01-first-encoding.fsx"
echo "  make manifest-check tooling-check"
echo "  make sample"
echo "  make html"
echo "Real circuit importer dependencies are optional; see scripts/requirements-import.txt."
