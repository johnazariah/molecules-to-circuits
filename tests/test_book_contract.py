import importlib.util
from pathlib import Path
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "check-book-contract.py"
SPEC = importlib.util.spec_from_file_location("book_contract", SCRIPT)
book_contract = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(book_contract)


class BookContractTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.manuscript = self.root / "manuscript"
        self.manuscript.mkdir()
        self.chapters = [f"{index:02d}-chapter.md" for index in range(1, 24)]
        self.names = [
            "foreword.md", self.chapters[0], "code-reading.md",
            *self.chapters[1:], "appendix-cookbook.md", "appendix-theory.md",
            "selected-solutions.md", "references.md",
        ]
        for name in self.names:
            text = "# Section\n"
            if name in self.chapters:
                text += "\n## Exercises\n\n1. Explain the worked example.\n"
            (self.manuscript / name).write_text(text, encoding="utf-8")
        self.write_manifest()

    def write_manifest(self):
        (self.manuscript / "Book.txt").write_text(
            "\n".join(self.names) + "\n", encoding="utf-8"
        )

    def write_chapter(self, text):
        (self.manuscript / self.chapters[0]).write_text(text, encoding="utf-8")

    def test_complete_book_passes_without_mutation(self):
        before = {path.name: path.read_bytes() for path in self.manuscript.iterdir()}
        self.assertEqual(book_contract.check_book(self.root), [])
        after = {path.name: path.read_bytes() for path in self.manuscript.iterdir()}
        self.assertEqual(before, after)

    def test_missing_exercises_fails(self):
        self.write_chapter("# Chapter\nOnly exposition.\n")
        self.assertTrue(any("no Exercises" in error for error in book_contract.check_book(self.root)))

    def test_exercise_heading_inside_code_does_not_count(self):
        self.write_chapter("# Chapter\n```markdown\n## Exercises\n```\n")
        self.assertTrue(any("no Exercises" in error for error in book_contract.check_book(self.root)))

    def test_duplicate_or_reordered_chapters_fail(self):
        self.names.append(self.chapters[0])
        self.write_manifest()
        errors = book_contract.check_book(self.root)
        self.assertTrue(any("duplicate entry" in error for error in errors))
        self.assertTrue(any("in order 01-23" in error for error in errors))

    def test_code_bridge_must_precede_first_factory(self):
        self.names.remove("code-reading.md")
        self.names.append("code-reading.md")
        self.write_manifest()
        self.assertTrue(any("must precede Chapter 2" in error for error in book_contract.check_book(self.root)))

    def test_support_and_orphan_chapters_fail(self):
        self.names.remove("appendix-theory.md")
        (self.manuscript / "24-extra.md").write_text("# Extra\n", encoding="utf-8")
        self.write_manifest()
        errors = book_contract.check_book(self.root)
        self.assertTrue(any("missing supporting section appendix-theory.md" in error for error in errors))
        self.assertTrue(any("numbered manuscript omitted: 24-extra.md" in error for error in errors))

    def test_missing_file_fails(self):
        (self.manuscript / self.chapters[10]).unlink()
        self.assertTrue(any("cannot read" in error for error in book_contract.check_book(self.root)))

    def test_manifest_escape_fails(self):
        self.names.append("../outside.md")
        self.write_manifest()
        self.assertTrue(any("invalid manuscript entry" in error for error in book_contract.check_book(self.root)))

    def test_missing_relative_resource_fails(self):
        self.write_chapter("# Chapter\n![plot](figures/missing.png)\n## Exercises\n")
        self.assertTrue(any("missing relative resource" in error for error in book_contract.check_book(self.root)))

    def test_local_reference_link_and_url_escaping(self):
        (self.manuscript / "a file.md").write_text("# Destination\n", encoding="utf-8")
        self.write_chapter(
            '# Chapter\n[Text](a%20file.md#anchor "Title")\n'
            '[reference]: <a file.md>\n## Exercises\n'
        )
        self.assertEqual(book_contract.check_book(self.root), [])

    def test_external_links_are_not_fetched(self):
        self.write_chapter(
            "# Chapter\n[Web](https://example.invalid/no.md)\n"
            "[Root](/book/page)\n[Section](#here)\n## Exercises\n"
        )
        self.assertEqual(book_contract.check_book(self.root), [])

    def test_links_in_fences_are_not_resource_dependencies(self):
        self.write_chapter(
            "# Chapter\n````markdown\n[Example](missing.md)\n"
            "```\n````\n## Exercises\n"
        )
        self.assertEqual(book_contract.check_book(self.root), [])

    def test_blockquote_fences_are_supported(self):
        self.write_chapter("# Chapter\n> ~~~text\n> [example](missing.md)\n> ~~~\n## Exercises\n")
        self.assertEqual(book_contract.check_book(self.root), [])

    def test_wrong_fence_kind_cannot_close(self):
        self.write_chapter("# Chapter\n## Exercises\n```text\n~~~\n")
        self.assertTrue(any("unclosed fenced" in error for error in book_contract.check_book(self.root)))

    def test_failure_does_not_modify_source(self):
        self.write_chapter("# Chapter\n[Missing](lost.md)\n")
        before = {path.name: path.read_bytes() for path in self.manuscript.iterdir()}
        self.assertNotEqual(book_contract.check_book(self.root), [])
        after = {path.name: path.read_bytes() for path in self.manuscript.iterdir()}
        self.assertEqual(before, after)


if __name__ == "__main__":
    unittest.main()
