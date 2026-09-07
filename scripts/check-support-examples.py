#!/usr/bin/env python3
"""Execute the complete F# scripts printed in the supporting matter."""

from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
count = 0
for source in ("code-reading.md", "appendix-cookbook.md"):
    text = (ROOT / "manuscript" / source).read_text(encoding="utf-8")
    examples = re.findall(
        r"^### Complete script: `([^`]+\.fsx)`\n(?:(?!^### ).)*?^```fsharp\n(.*?)^```",
        text, re.MULTILINE | re.DOTALL,
    )
    if not examples:
        raise RuntimeError(f"No complete-script examples found in {source}")
    for name, code in examples:
        if Path(name).name != name:
            raise RuntimeError(f"Invalid example filename: {name}")
        with tempfile.TemporaryDirectory(prefix="book-example-") as directory:
            path = Path(directory) / name
            path.write_text(code, encoding="utf-8")
            subprocess.run(["dotnet", "fsi", str(path)], cwd=directory, check=True)
        count += 1
print(f"Executed {count} complete supporting-matter examples")
