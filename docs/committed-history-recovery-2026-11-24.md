# Committed-history recovery: 24 November 2026

The six surviving session histories below were copied from local Git objects
into an independent clone and pushed unchanged to
<https://github.com/johnazariah/molecules-to-circuits>.
The primary checkout and its worktree metadata were not modified.

## Preserved histories

Source branches have the prefix `refs/heads/johnazariah-`.
Remote recovery branches have the prefix `refs/heads/recovery/2026-11-24/`.
The suffix column applies to both prefixes.

| Session | Suffix | Exact source and remote recovery tip | Consolidation coverage |
| --- | --- | --- | --- |
| `04aacd0a-7d1c-42b1-bd1f-56d09044dc31` | `book-and-blog-review` | `f8616342cb86524fb90f6d000be7a8d8db7d806b` | Accepted parent manuscript; used as the consolidation base. |
| `7092245d-14fe-403b-a869-55cdec53c03e` | `book-executable-integrity` | `b03d956ac5a259525f0228ee40b8e53c21e3268c` | All six child commits already represented by equivalent parent patches. |
| `8a4cab2d-40bf-4a6a-852d-d8f85592f2da` | `book-assembly-and-production` | `15b506b7aeb3df50de9e1892047ad6e4aff30741` | All six child commits already represented by equivalent parent patches. |
| `d3ca0849-057c-47ed-a087-361a4fd6def6` | `book-foundations-expansion` | `fb0c46bb24fad4b205cd9716f7c52a4ae1d1216a` | Child patch already represented by `e4cc7a8742b2660a439e94c38e4341ad285a8d77`. |
| `f274c54d-9749-4ffa-8dfd-843e9c7f4861` | `book-encodings-and-tapering` | `70f3f84fa9e8574b8303769600184e791f73740a` | Child patch already represented by `d84d877972725d13ef4202bdbe7a703238249db9`. |
| `681e3e4e-baa8-4bd6-9f72-99fd05c472f4` | `book-simulation-and-algorithms` | `a2ad5ed899cda82d4d4223959499bcfe60e0bf9f` | Child patch already represented by `08a7398bae0999a09574799db343666829d16a06`. |

`git cherry` found no unmatched child patches against the parent review tip.
Stable patch IDs independently confirmed the correspondence. Child commits were
not replayed: the normal manuscript, code, labs and publication tooling retain
the parent's integrated versions and subsequent corrections. The recovery
branches retain every original child commit and its original tree.

For executable integrity, the child-to-parent mappings are:

```text
92936874 -> 41378575
9d808826 -> 63d55fd1
cd503b84 -> 5616b9d1
dc498454 -> 8ed7e878
9fc196d4 -> 7998cc98
b03d956a -> 622efb4d
```

For assembly and production:

```text
234e1e84 -> 66cb16b6
60b57303 -> 09197208
773c1190 -> 919e28b8
3aac1a67 -> b6056101
8201b6d8 -> 89e386ad
15b506b7 -> 3d77efc3
```

## Integration and checks

The GitHub default branch was `main` at
`c5cf9dfc4543d35c9936fa1b39307e782016fe11`, an ancestor of the accepted parent
review tip. The primary local `main`,
`e3bb6fa9f47c1d3c12b271d0734f3a54fe9f7698`, is also an ancestor.
The consolidation branch is `recovery/2026-11-24/consolidated`, comprising the
accepted parent history plus this manifest. It is suitable for a non-force,
fast-forward update of `main`; the actual published branch hashes must be
checked after pushing. Other GitHub branches are left unchanged.

Checks passed in the independent recovery clone:

- Full copied-object `git fsck --full --no-reflogs`.
- `make manifest-check`: 23 chapters, six supporting sections and five sample bodies.
- `python3 scripts/check-book-contract.py` and all 15 existing contract tests.
- `make math-check`: MathML conversion of all 29 manuscript sources.
- `make tooling-check`: all 12 publication-tool tests.
- `make semantic-check`: the existing bounded executable-integrity gate, including
  chemistry reference verification, spectral controls, input/provenance checks,
  isolated generation failure controls, six-encoding CAR checks, physical-sector
  tapering, algorithm examples and circuit export.
- `git diff --check`.

No canonical chemistry data were regenerated. This recovery did not run external
circuit importers, all numbered labs, full publication builds or long experiments.

## Missing content and deletion restriction

The external `mac_workspace` volume was unavailable. Worktree directories behind
the missing volume could not be inspected. This operation preserves the six
surviving committed histories only; it does not recover or establish the safety
of uncommitted changes, untracked files, unsaved drafts or ignored build products.
It does not establish that local deletion is safe. No files or sessions were
removed or archived, and no symlinks or worktree registrations were repaired.
