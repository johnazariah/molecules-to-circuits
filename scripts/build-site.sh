#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

# Build HTML site using Jupyter Book / MyST
# Usage: ./scripts/build-site.sh
# Output: _build/html/

PYTHON="${PYTHON:-python3}"
"$PYTHON" scripts/check-book-manifests.py
"$PYTHON" - <<'PY'
from importlib.metadata import PackageNotFoundError, version
from pathlib import Path

requirement = next(
    line.strip() for line in Path("requirements.txt").read_text().splitlines()
    if line.startswith("jupyter-book==")
)
expected = requirement.split("==", 1)[1]
try:
    installed = version("jupyter-book")
except PackageNotFoundError:
    raise SystemExit("Jupyter Book is not installed; install requirements.txt in a virtual environment.")
if installed != expected:
    raise SystemExit(
        f"Jupyter Book {installed} does not match the tested publisher {expected}. "
        "Install requirements.txt in a virtual environment and set PYTHON to its interpreter. "
        "Version 2.1.5 produces static HTML with broken React hydration."
    )
PY
"$PYTHON" -m jupyter_book --version

# Only this checkout's generated outputs are disposable. Never repair or change
# ownership of the reader's home-directory npm/browser caches.
rm -rf _build/html _build/site
HOST="${HOST:-127.0.0.1}" "$PYTHON" -m jupyter_book build --html --force --strict
test -s _build/html/index.html || {
    echo "HTML build did not produce _build/html/index.html" >&2
    exit 1
}
