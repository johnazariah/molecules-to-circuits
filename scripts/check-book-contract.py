#!/usr/bin/env python3
"""Check book assembly, exercises and local resources, not mathematical truth."""

from __future__ import annotations

import argparse
from collections import Counter
from pathlib import Path
import re
import sys
from urllib.parse import unquote, urlsplit


SUPPORT_FILES = (
    "foreword.md",
    "code-reading.md",
    "appendix-cookbook.md",
    "appendix-theory.md",
    "selected-solutions.md",
    "references.md",
)
CHAPTER_NAME = re.compile(r"^(\d{2})-.+\.md$")
FENCE = re.compile(r"^ {0,3}(`{3,}|~{3,})(.*)$")
EXERCISES = re.compile(r"^ {0,3}#{1,6}\s+Exercises\b")
INLINE_LINK = re.compile(
    r"!?\[[^\]\n]*\]\(\s*(<[^>\n]+>|[^\s)]+)"
    r"(?:\s+[\"'][^)\n]*[\"'])?\s*\)"
)
REFERENCE_LINK = re.compile(r"^ {0,3}\[[^\]\n]+\]:\s*(<[^>\n]+>|\S+)")
LOCAL_SUFFIXES = {
    ".md", ".png", ".jpg", ".jpeg", ".svg", ".webp", ".pdf",
    ".fsx", ".py", ".json", ".csv", ".ipynb",
}


def read_text(path: Path, errors: list[str]) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as error:
        errors.append(f"{path}: cannot read: {error}")
        return None


def read_manifest(path: Path, errors: list[str]) -> list[str]:
    text = read_text(path, errors)
    if text is None:
        return []
    return [
        line.strip()
        for line in text.splitlines()
        if line.strip() and not line.lstrip().startswith("#")
    ]


def local_link_error(root: Path, source: Path, target: str) -> str | None:
    target = target.removeprefix("<").removesuffix(">")
    try:
        url = urlsplit(target)
    except ValueError as error:
        return f"invalid link {target!r}: {error}"
    if url.scheme or url.netloc or not url.path or url.path.startswith("/"):
        return None
    path = Path(unquote(url.path))
    if path.suffix.lower() not in LOCAL_SUFFIXES:
        return None
    destination = (source.parent / path).resolve()
    if not destination.is_relative_to(root):
        return f"relative resource escapes repository: {target}"
    if not destination.is_file():
        return f"missing relative resource: {target}"
    return None


def inspect_markdown(root: Path, path: Path, text: str) -> tuple[bool, list[str]]:
    errors = []
    exercises = False
    opening = None
    for number, raw_line in enumerate(text.splitlines(), start=1):
        line = re.sub(r"^(?: {0,3}> ?)+", "", raw_line)
        if opening is not None:
            marker, count, _ = opening
            if re.fullmatch(rf" {{0,3}}{re.escape(marker)}{{{count},}}\s*", line):
                opening = None
            continue
        fence = FENCE.match(line)
        if fence:
            marker = fence.group(1)
            opening = (marker[0], len(marker), number)
            continue
        if line.startswith("    ") or line.startswith("\t"):
            continue
        if EXERCISES.match(line):
            exercises = True
        targets = [match.group(1) for match in INLINE_LINK.finditer(line)]
        reference = REFERENCE_LINK.match(line)
        if reference:
            targets.append(reference.group(1))
        for target in targets:
            error = local_link_error(root, path, target)
            if error:
                errors.append(f"{path.relative_to(root)}:{number}: {error}")
    if opening is not None:
        errors.append(
            f"{path.relative_to(root)}:{opening[2]}: unclosed fenced code block"
        )
    return exercises, errors


def check_book(root: Path) -> list[str]:
    root = root.resolve()
    manuscript = root / "manuscript"
    errors = []
    names = read_manifest(manuscript / "Book.txt", errors)
    for name, count in Counter(names).items():
        if count > 1:
            errors.append(f"Book.txt: duplicate entry {name}")
    for name in SUPPORT_FILES:
        if name not in names:
            errors.append(f"Book.txt: missing supporting section {name}")

    chapter_numbers = [
        int(match.group(1))
        for name in names
        if (match := CHAPTER_NAME.fullmatch(name))
    ]
    if chapter_numbers != list(range(1, 24)):
        errors.append("Book.txt: numbered chapters must appear exactly once, in order 01-23")

    chapter_two = next(
        (name for name in names if CHAPTER_NAME.fullmatch(name) and name.startswith("02-")),
        None,
    )
    if "code-reading.md" in names and chapter_two is not None:
        if names.index("code-reading.md") > names.index(chapter_two):
            errors.append("Book.txt: code-reading.md must precede Chapter 2")

    manifested = set(names)
    for path in sorted(manuscript.glob("[0-9][0-9]-*.md")):
        if path.name not in manifested:
            errors.append(f"Book.txt: numbered manuscript omitted: {path.name}")

    for name in dict.fromkeys(names):
        relative = Path(name)
        if relative.is_absolute() or ".." in relative.parts or relative.suffix != ".md":
            errors.append(f"Book.txt: invalid manuscript entry {name!r}")
            continue
        path = (manuscript / relative).resolve()
        if not path.is_relative_to(manuscript.resolve()):
            errors.append(f"Book.txt: entry escapes manuscript: {name}")
            continue
        text = read_text(path, errors)
        if text is None:
            continue
        exercises, markdown_errors = inspect_markdown(root, path, text)
        errors.extend(markdown_errors)
        if CHAPTER_NAME.fullmatch(name) and not exercises:
            errors.append(f"manuscript/{name}: no Exercises section")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--root", type=Path, default=Path(__file__).resolve().parents[1],
        help="repository root (defaults to the directory containing scripts/)",
    )
    args = parser.parse_args()
    errors = check_book(args.root)
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        print(f"Book assembly contract failed: {len(errors)} issue(s).", file=sys.stderr)
        return 1
    print("Book assembly contract passed: 23 chapters, supporting sections, exercises and resources.")
    print("This structural check does not certify mathematical correctness or definition order.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
