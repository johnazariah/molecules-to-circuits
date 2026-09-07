#!/usr/bin/env python3
"""Render one stdin Mermaid diagram; print only its validated PNG path."""

import hashlib
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile
import zlib


def valid_png(path):
    if not path.is_file():
        return False
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        return False
    offset, header, pixels = 8, False, False
    while offset + 12 <= len(data):
        size = struct.unpack(">I", data[offset:offset + 4])[0]
        end = offset + 12 + size
        if end > len(data):
            return False
        kind = data[offset + 4:offset + 8]
        payload = data[offset + 8:end - 4]
        crc = struct.unpack(">I", data[end - 4:end])[0]
        if zlib.crc32(kind + payload) & 0xffffffff != crc:
            return False
        if offset == 8:
            if kind != b"IHDR" or size != 13:
                return False
            width, height = struct.unpack(">II", payload[:8])
            header = width > 0 and height > 0
        pixels = pixels or (kind == b"IDAT" and size > 0)
        if kind == b"IEND":
            return header and pixels and size == 0 and end == len(data)
        offset = end
    return False


def browser_path():
    explicit = os.environ.get("PUPPETEER_EXECUTABLE_PATH")
    if explicit:
        path = Path(explicit)
        if not path.is_file():
            raise RuntimeError(f"PUPPETEER_EXECUTABLE_PATH does not exist: {path}")
        return str(path)
    home = Path.home()
    roots = [home / ".cache/ms-playwright", home / "Library/Caches/ms-playwright"]
    if os.environ.get("LOCALAPPDATA"):
        roots.append(Path(os.environ["LOCALAPPDATA"]) / "ms-playwright")
    if os.environ.get("PLAYWRIGHT_BROWSERS_PATH"):
        roots.insert(0, Path(os.environ["PLAYWRIGHT_BROWSERS_PATH"]))
    patterns = (
        "chromium-*/chrome-linux/chrome",
        "chromium-*/chrome-linux64/chrome",
        "chromium-*/chrome-mac/Chromium.app/Contents/MacOS/Chromium",
        "chromium-*/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing",
        "chromium-*/chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing",
        "chromium-*/chrome-win/chrome.exe",
        "chromium-*/chrome-win64/chrome.exe",
    )
    for root in roots:
        for pattern in patterns:
            for path in sorted(root.glob(pattern), reverse=True):
                if path.is_file():
                    return str(path)
    return None  # Let Puppeteer use its own installed browser.


def render(source):
    directory = Path(os.environ.get("MERMAID_IMAGE_DIR", "manuscript/mermaid-images"))
    directory.mkdir(parents=True, exist_ok=True)
    # Change the cache namespace when changing rendering settings or CLI policy.
    digest = hashlib.sha256(b"book-mermaid-v2:white:scale3:width1600\n" + source).hexdigest()
    output = directory / f"diagram-{digest}.png"
    if valid_png(output):
        return output

    executable = os.environ.get("MMDC", "mmdc")
    if not shutil.which(executable):
        raise RuntimeError(
            f"Mermaid renderer not found: {executable}. Install mermaid-cli locally "
            "and set MMDC to its executable; refusing to emit raw diagram source."
        )
    with tempfile.TemporaryDirectory(prefix=".render-", dir=directory) as temporary:
        temporary = Path(temporary)
        source_file, image = temporary / "source.mmd", temporary / "diagram.png"
        source_file.write_bytes(source)
        config = {}
        browser = browser_path()
        if browser:
            config["executablePath"] = browser
        if os.environ.get("MMDC_NO_SANDBOX") == "1":
            config["args"] = ["--no-sandbox"]
        config_file = temporary / "puppeteer.json"
        config_file.write_text(json.dumps(config), encoding="utf-8")
        command = [executable, "-i", str(source_file), "-o", str(image),
                   "-b", "white", "-s", "3", "-w", "1600", "-p", str(config_file)]
        result = subprocess.run(command, stdout=sys.stderr, stderr=sys.stderr, check=False)
        if result.returncode:
            raise RuntimeError(f"Mermaid renderer failed with exit status {result.returncode}")
        if not valid_png(image):
            raise RuntimeError("Mermaid renderer returned success without a valid PNG")
        os.replace(image, output)
    return output


if __name__ == "__main__":
    try:
        print(render(sys.stdin.buffer.read()))
    except (OSError, RuntimeError) as error:
        sys.exit(f"render-mermaid: {error}")
