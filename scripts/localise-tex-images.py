#!/usr/bin/env python3
"""Copy only images referenced by generated TeX into its packaging directory."""

from pathlib import Path
import re
import shutil
import sys

path = Path(sys.argv[1])
text = path.read_text(encoding="utf-8")
root = Path(__file__).resolve().parents[1]
images = {}


def local_image(match):
    name = match.group(2)
    candidates = (root / name, root / "manuscript" / name)
    source = next((candidate.resolve() for candidate in candidates if candidate.is_file()), None)
    if source is None:
        raise ValueError(f"Missing TeX image: {name}")
    previous = images.get(source.name)
    if previous is not None and previous != source:
        raise ValueError(f"Image basename collision: {previous} and {source}")
    if (path.parent / source.name).resolve() == path.resolve():
        raise ValueError(f"Image would overwrite the TeX source: {source}")
    images[source.name] = source
    return match.group(1) + source.name + match.group(3)


result = re.sub(r"(\\includegraphics(?:\[[^\]]*\])?\{)([^{}]+)(\})", local_image, text)
for name, source in images.items():
    target = path.parent / name
    if source != target.resolve():
        shutil.copy2(source, target)
path.write_text(result, encoding="utf-8")
