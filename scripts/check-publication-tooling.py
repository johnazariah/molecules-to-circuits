#!/usr/bin/env python3
"""Bounded publication-tool negative controls; no full manuscript rendering."""

import base64
import importlib.util
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
FILTER = ROOT / "manuscript/mermaid.lua"
SAMPLE_FILTER = ROOT / "manuscript/sample-filter.lua"
spec = importlib.util.spec_from_file_location("renderer", ROOT / "scripts/render-mermaid.py")
renderer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(renderer)
PNG = base64.b64decode(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
)


class PublicationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="book tools ")
        self.directory = Path(self.temporary.name)
        self.images = self.directory / "images with spaces"
        self.env = dict(os.environ, MERMAID_IMAGE_DIR=str(self.images), PYTHON=sys.executable)

    def tearDown(self):
        self.temporary.cleanup()

    def fake_renderer(self, body):
        executable = self.directory / "fake mmdc"
        executable.write_text(f"#!{sys.executable}\n" + body, encoding="utf-8")
        executable.chmod(0o755)
        self.env["MMDC"] = str(executable)

    def render(self, source="flowchart LR\n A --> B"):
        return subprocess.run(
            ["pandoc", "-f", "markdown", "-t", "json", "--lua-filter", str(FILTER)],
            input=f"```mermaid\n{source}\n```\n", text=True,
            capture_output=True, env=self.env, cwd=ROOT,
        )

    def assert_clean(self):
        self.assertEqual(list(self.images.glob(".render-*")), [])

    def test_missing_renderer_fails(self):
        self.env["MMDC"] = str(self.directory / "not installed")
        result = self.render()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("renderer not found", result.stderr)
        self.assert_clean()

    def test_false_renderer_fails(self):
        self.env["MMDC"] = "false"
        result = self.render()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("exit status", result.stderr)
        self.assert_clean()

    def test_success_without_output_fails(self):
        self.fake_renderer("pass\n")
        self.assertNotEqual(self.render().returncode, 0)
        self.assert_clean()

    def test_invalid_output_fails(self):
        self.fake_renderer(
            "import pathlib, sys\n"
            "pathlib.Path(sys.argv[sys.argv.index('-o')+1]).write_bytes(b'not a PNG')\n"
        )
        self.assertNotEqual(self.render().returncode, 0)
        self.assertEqual(list(self.images.glob("diagram-*.png")), [])
        self.assert_clean()

    def test_partial_output_on_failure_is_not_cached(self):
        self.fake_renderer(
            "import pathlib, sys\n"
            f"pathlib.Path(sys.argv[sys.argv.index('-o')+1]).write_bytes({PNG!r})\n"
            "sys.exit(7)\n"
        )
        self.assertNotEqual(self.render().returncode, 0)
        self.assertEqual(list(self.images.glob("diagram-*.png")), [])
        self.assert_clean()

    def test_valid_cache_skips_renderer_but_changed_source_fails(self):
        self.fake_renderer(
            "import pathlib, sys\n"
            f"pathlib.Path(sys.argv[sys.argv.index('-o')+1]).write_bytes({PNG!r})\n"
        )
        first = self.render()
        self.assertEqual(first.returncode, 0, first.stderr)
        self.assertIn('"Image"', first.stdout)
        image, = self.images.glob("diagram-*.png")
        self.assertTrue(renderer.valid_png(image))
        self.env["MMDC"] = "false"
        self.assertEqual(self.render().returncode, 0)
        self.assertNotEqual(self.render("flowchart LR\n B --> C").returncode, 0)
        image.write_bytes(PNG[:-12])
        self.assertNotEqual(self.render().returncode, 0)
        self.assert_clean()

    def test_png_crc_validation(self):
        path = self.directory / "image.png"
        path.write_bytes(PNG)
        self.assertTrue(renderer.valid_png(path))
        corrupted = bytearray(PNG)
        corrupted[20] ^= 1
        path.write_bytes(corrupted)
        self.assertFalse(renderer.valid_png(path))

    def test_mathml_gate_rejects_raw_tex_fallback(self):
        workspace = self.directory / "math-workspace"
        (workspace / "scripts").mkdir(parents=True)
        (workspace / "manuscript").mkdir()
        script = workspace / "scripts/check-math-rendering.py"
        script.write_bytes((ROOT / "scripts/check-math-rendering.py").read_bytes())
        (workspace / "manuscript/Book.txt").write_text("chapter.md\n")
        chapter = workspace / "manuscript/chapter.md"
        chapter.write_text(r"$E_{\mathrm{shift}}=0.5$" + "\n")
        valid = subprocess.run(
            [sys.executable, str(script)], capture_output=True, text=True
        )
        self.assertEqual(valid.returncode, 0, valid.stderr)
        chapter.write_text(r"$\bookundefinedcommand{x}$" + "\n")
        invalid = subprocess.run(
            [sys.executable, str(script)], capture_output=True, text=True
        )
        self.assertNotEqual(invalid.returncode, 0)
        self.assertIn("refusing raw-TeX", invalid.stderr)

    def test_sample_full_contents_without_omitted_bodies(self):
        selected_title = (ROOT / "manuscript/code-reading.md").read_text().splitlines()[0]
        text = (f"{selected_title}\n\nIncluded bridge text.\n\n"
                "# Appendix B: Notation and Convention Reference\n\n"
                "Excluded body.\n\n## Symbols\n\nNot in sample.\n\n"
                "# Selected Worked Solutions\n\nExcluded answer.\n")
        result = subprocess.run(
            ["pandoc", "-f", "markdown", "-t", "latex", "--lua-filter", str(SAMPLE_FILTER)],
            input=text, text=True, capture_output=True, cwd=ROOT, check=True,
        )
        self.assertIn("Included bridge text.", result.stdout)
        for title in ("Appendix B", "Symbols", "Selected Worked Solutions"):
            self.assertIn(title, result.stdout)
        for omitted in ("Excluded body.", "Not in sample.", "Excluded answer."):
            self.assertNotIn(omitted, result.stdout)
        self.assertIn("\\protect\\textemdash", result.stdout)
        self.assertNotIn("\\chapter{Appendix B", result.stdout)

    def test_figures_invalidate_each_print_format(self):
        with tempfile.TemporaryDirectory(prefix="book-make-") as directory:
            outputs = [Path(directory) / name for name in ("book.pdf", "sample.pdf", "book.epub")]
            for output in outputs:
                output.touch()
                os.utime(output, (4102444800, 4102444800))
            overrides = [f"OUT={outputs[0]}", f"SAMPLE_OUT={outputs[1]}", f"EPUB_OUT={outputs[2]}"]
            for target, output in zip(("all", "sample", "epub"), outputs):
                result = subprocess.run(
                    ["make", "-n", "-W", "manuscript/figures/h2_dissociation.png", target, *overrides],
                    text=True, capture_output=True, cwd=ROOT, check=True,
                )
                self.assertIn(f"-o {output}", result.stdout)

    def test_html_failure_stops_preview(self):
        workspace = self.directory / "workspace"
        (workspace / "scripts").mkdir(parents=True)
        (workspace / "manuscript").mkdir()
        (workspace / "manuscript/Book.txt").write_text("")
        (workspace / "Makefile").write_bytes((ROOT / "Makefile").read_bytes())
        (workspace / "scripts/build-site.sh").write_bytes(
            (ROOT / "scripts/build-site.sh").read_bytes()
        )
        python = self.directory / "fake-python"
        log = self.directory / "calls"
        python.write_text(
            f"#!{sys.executable}\n"
            "from pathlib import Path\nimport sys\n"
            f"with Path({str(log)!r}).open('a') as f: f.write(' '.join(sys.argv[1:])+'\\n')\n"
            "if 'build' in sys.argv: sys.exit(31)\n",
            encoding="utf-8",
        )
        python.chmod(0o755)
        result = subprocess.run(
            ["make", "preview", f"PYTHON={python}"], cwd=workspace,
            text=True, capture_output=True,
        )
        self.assertNotEqual(result.returncode, 0)
        calls = log.read_text()
        self.assertIn("jupyter_book build --html --force --strict", calls)
        self.assertNotIn("http.server", calls)

    def test_tex_package_copies_only_referenced_images(self):
        source = self.directory / "cache"
        source.mkdir()
        (source / "used.png").write_bytes(PNG)
        (source / "stale.png").write_bytes(PNG)
        package = self.directory / "package"
        package.mkdir()
        tex = package / "manuscript.tex"
        tex.write_text(r"\includegraphics[width=10pt]{" + str(source / "used.png") + "}")
        subprocess.run(
            [sys.executable, str(ROOT / "scripts/localise-tex-images.py"), str(tex)],
            check=True, capture_output=True,
        )
        self.assertEqual(tex.read_text(), r"\includegraphics[width=10pt]{used.png}")
        self.assertEqual((package / "used.png").read_bytes(), PNG)
        self.assertFalse((package / "stale.png").exists())


if __name__ == "__main__":
    unittest.main()
