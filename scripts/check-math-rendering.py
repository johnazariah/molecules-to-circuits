#!/usr/bin/env python3
"""Reject equations that Pandoc would leave as raw TeX in the EPUB."""

from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]


def main():
    sources = [
        str(ROOT / "manuscript" / line.strip())
        for line in (ROOT / "manuscript/Book.txt").read_text().splitlines()
        if line.strip() and not line.lstrip().startswith("#")
    ]
    result = subprocess.run(
        ["pandoc", *sources, "--to=html5", "--mathml"],
        cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True,
        check=False,
    )
    if result.stderr:
        print(result.stderr, file=sys.stderr, end="")
    if result.returncode or "Could not convert TeX math" in result.stderr:
        print("MathML acceptance failed; refusing raw-TeX equation fallback.", file=sys.stderr)
        return 1
    print(f"MathML acceptance passed for all {len(sources)} manuscript sources.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
