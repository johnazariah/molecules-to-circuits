# Mounted book artifact recovery

The external volume became readable after the committed-history recovery.
All six source worktrees were inspected read-only. Their tracked file contents
match their recorded commits, with no staged edits or untracked files. The
mounted filesystem reports executable-bit changes across ordinary files; those
mode-only differences were not applied to the book.

The primary checkout also has no staged or unstaged content edits. Its untracked
`manuscript/sample.pdf` is preserved here.

## Preserved material

[The inventory](mounted-content-inventory.json) accounts for 568 selected source
files: 405 unique payloads are stored under `artifacts/`, 64 exact duplicates
point to those stored payloads, and 99 files already exist as blobs in the
published GitHub history. Each record includes its source, relative path, byte
count, SHA-256 and Git blob ID. Distinct versions retain separate provenance.

| Source | Selected files | New unique payloads | Duplicates | Already in GitHub history |
| --- | ---: | ---: | ---: | ---: |
| Parent book review | 251 | 188 | 28 | 35 |
| Executable integrity | 45 | 20 | 18 | 7 |
| Assembly and production | 153 | 108 | 14 | 31 |
| Foundations | 1 | 1 | 0 | 0 |
| Encodings and tapering | 2 | 2 | 0 | 0 |
| Simulation and algorithms | 1 | 0 | 1 | 0 |
| Primary checkout | 115 | 86 | 3 | 26 |

The archive contains publication outputs, historical HTML/content snapshots,
rendered diagrams, circuit/data exports, verification evidence and standalone
session check scripts. No manuscript or executable source edits needed to be
merged: the source contents were already covered by the committed recovery.

The accepted expanded outputs are:

- [291-page full PDF](artifacts/book-and-blog-review/session-files/revised-book-2026-09-19/molecules-to-circuits.pdf)
- [61-page sample PDF](artifacts/book-and-blog-review/session-files/revised-book-2026-09-19/molecules-to-circuits-sample.pdf)
- [EPUB](artifacts/book-and-blog-review/session-files/revised-book-2026-09-19/molecules-to-circuits.epub)
- [Original output acceptance hashes](artifacts/book-and-blog-review/session-files/revised-book-2026-09-19/verification/print-artifacts.json)

Their hashes match the original acceptance record exactly. Older drafts,
intermediate proofs and deliberately failing rendering examples are also
retained for evidence; they are not promoted to accepted editions.
Generated HTML snapshots omit reinstallable publisher runtimes and are
historical content evidence, not a self-contained website.

The `.fs` source-reference snapshots came from
[FockMap/encodings](https://github.com/johnazariah/encodings).
Its MIT notice is retained in [LICENSE-FockMap](LICENSE-FockMap).
Archived check scripts are preserved as written, including their original
machine-specific paths; they are evidence, not newly supported entry points.
Book text and rendered artifacts remain subject to the repository's
`MANUSCRIPT-RIGHTS`, not the source-code MIT licence.

All 568 records were resolved and hash-checked before committing. JSON payloads
were parsed, PDF signatures were checked, and the EPUB ZIP/CRC and mimetype
checks passed. Candidate bytes were scanned for common credential patterns
before copying. No canonical data were regenerated or source worktrees changed.

## Exclusions

This is a public repository. Private proposal documents and a CV in the
additional Apress proposal worktree were not opened or uploaded.
They remain local and unpreserved by this operation.

Reinstallable virtual environments, npm dependencies, downloaded wheels,
publisher distributions/templates, intermediate caches, vendor web runtimes
and Finder metadata were excluded. The inventory records their source locations
and reasons. The primary `springer-submission/` directory contained only Finder
metadata. Session research directories were empty and checkpoint directories
contained only empty-history indexes.

Application event logs, conversation databases, runtime state and locks were
not exported; unsaved editor buffers are not covered. No files or sessions were
removed or archived. The listed private documents and these exclusions mean
this recovery is not blanket clearance to delete local material.
