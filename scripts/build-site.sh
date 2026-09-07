#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

# Build HTML site using Jupyter Book / MyST
# Usage: ./scripts/build-site.sh
# Output: _build/html/

PYTHON="${PYTHON:-python3}"
"$PYTHON" scripts/check-book-manifests.py
"$PYTHON" -m jupyter_book --version

# Only this checkout's generated outputs are disposable. Never repair or change
# ownership of the reader's home-directory npm/browser caches.
rm -rf _build/html _build/site
HOST="${HOST:-127.0.0.1}" "$PYTHON" -m jupyter_book build --html --force --strict
test -s _build/html/index.html || {
    echo "HTML build did not produce _build/html/index.html" >&2
    exit 1
}
