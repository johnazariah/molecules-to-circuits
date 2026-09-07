#!/usr/bin/env python3
"""Check that print, sample and MyST read the same ordered source inventory."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
MANUSCRIPT = ROOT / "manuscript"


def entries(name):
    lines = (MANUSCRIPT / name).read_text(encoding="utf-8").splitlines()
    result = [line.strip() for line in lines if line.strip() and not line.startswith("#")]
    if len(result) != len(set(result)):
        raise ValueError(f"{name} contains duplicate sources")
    for name in result:
        if Path(name).name != name or not (MANUSCRIPT / name).is_file():
            raise ValueError(f"Invalid manuscript source: {name}")
    return result


def check():
    book, sample = entries("Book.txt"), entries("Sample.txt")
    if any(name not in book for name in sample):
        raise ValueError("Sample.txt must select sources from Book.txt")
    if sample != [name for name in book if name in sample]:
        raise ValueError("Sample.txt must preserve Book.txt reading order")
    chapters = [name for name in book if re.match(r"^\d\d-", name)]
    if [name[:2] for name in chapters] != [f"{n:02d}" for n in range(1, 24)]:
        raise ValueError("Book.txt must include Chapters 01-23 exactly once, in order")
    if book.index("code-reading.md") >= book.index("02-notation.md"):
        raise ValueError("The code-reading bridge must precede Chapter 2")
    for required in ("foreword.md", "appendix-cookbook.md", "appendix-theory.md",
                     "selected-solutions.md", "references.md"):
        if required not in book:
            raise ValueError(f"Missing supporting matter: {required}")
    myst = (ROOT / "myst.yml").read_text(encoding="utf-8")
    web = re.findall(r"^\s*- file: manuscript/(\S+)\s*$", myst, re.MULTILINE)
    if web != book:
        raise ValueError("MyST's ordered file TOC differs from manuscript/Book.txt")
    if re.search(r"^\s*exports:", myst, re.MULTILINE):
        raise ValueError("Use Makefile PDF/EPUB outputs, not duplicate MyST exports")
    print(f"Manifests agree: {len(chapters)} chapters, "
          f"{len(book) - len(chapters)} supporting files, {len(sample)} sample bodies")


if __name__ == "__main__":
    try:
        check()
    except (OSError, ValueError) as error:
        sys.exit(f"manifests: {error}")
