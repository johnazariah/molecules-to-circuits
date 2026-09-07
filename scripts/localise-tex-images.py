#!/usr/bin/env python3
"""Make generated TeX image paths local to an arXiv packaging directory."""

from pathlib import Path
import sys

path = Path(sys.argv[1])
text = path.read_text(encoding="utf-8")
for prefix in ("manuscript/mermaid-images/", "manuscript/figures/", "figures/"):
    text = text.replace(prefix, "")
path.write_text(text, encoding="utf-8")
