# Proposal Strategy (internal)

Internal positioning notes for the Apress submission of *Molecules to Quantum Circuits*. Not for the publisher.

## 1. Title recommendation

- **Final (confirmed by author):** **Molecules to Quantum Circuits** — *A Hands-On Pipeline from Electronic Structure to Exportable Quantum Circuits*.
  - Rationale: "Molecules to Quantum Circuits" is punchier than the repo's current "From Molecules to Quantum Circuits" and reads better on an Apress spine; the subtitle leads with the practitioner outcome — an end-to-end, hands-on pipeline — and stays broad rather than tying the book to one language or SDK.
- **Alternate subtitle considered:** "An Engineer's Guide to Fermion-to-Qubit Encodings with Open-Source Tools" (leads with the specific technical hook and the open-source angle).
- **Alternate subtitle considered:** "Building and Verifying Quantum-Simulation Circuits with Open-Source F# Tools" (leads with the build/verify outcome and names F#).
- **Alternate title considered:** keep **From Molecules to Quantum Circuits** to preserve continuity with the existing repo, Zenodo DOI, and public README brand. Trade-off: slightly less crisp as a commercial title.

## 2. How to answer likely Apress practicality feedback

Apress will push on practitioner outcomes and executable content. We are strong here — lead with it:
- Emphasize the released NuGet library, ten one-command labs, Codespaces, and exported OpenQASM/Q# artifacts. This is not pseudocode.
- If asked to cut theory/intro: offer to condense the introductory chapters (electronic structure, qubit vocabulary — Ch 1, 4) and cross-reference freely available online material, while protecting the differentiating encoding/tapering/compilation chapters (Ch 5–17). Deeper theory there is our moat, not filler.
- If asked "why F#, not Python?": frame F# as an asset (type-safe, symbolic operator algebra; the library is the point), and offer a Python interop/bridge note plus JSON circuit export so Python users can consume outputs. Do not rewrite the book in Python.
- If asked about audience size: position primary readers as quantum software engineers and computational scientists, with course-module adoption as upside (verifiable exercises).

## 3. Relationship between the two books

- Keep them **separately approvable**. James invited this one after the applications proposal; do not let one gate the other.
- Positioning: applications book = breadth, application-first, multiple industries. This book = depth, single-domain, the encoding/compilation machinery. Shared author, shared no-hype/executable philosophy.
- In all publisher-facing text, state the relationship plainly and add "neither depends on the other."

## 4. Five contract-stage adaptation commitments (things we will offer)

1. Condense the introductory chapters and cross-reference free online material, per Apress professional positioning, without touching the differentiating middle.
2. Add practitioner-facing chapter openers ("what you'll build") and end-of-chapter "try it" lab callouts.
3. Guarantee every listing is in the companion repo, CI-checked, pinned to a FockMap release, and runnable in one command.
4. Provide Apress-formatted code listings plus a downloadable source archive and a Codespaces link as ancillary materials; add an index and glossary.
5. Align delivery to Apress production (sample chapters 30 September 2026; full revision 31 March 2027) and supply figures at print resolution.

## 5. What NOT to concede

- Do not drop the "expose every intermediate + independent verification" methodology — it is the core differentiator.
- Do not collapse to a single encoding or tie the book to one vendor SDK; keep six encodings and platform-agnostic export.
- Do not overclaim quantum advantage or imply encoding "solves" the ground state; keep the honest boundary (state prep / VQE / QPE are separate).
- Do not merge with, or make approval conditional on, the applications book.
- Do not abandon the open-source F#/FockMap companion to chase a Python-only expectation; offer interop instead.

## 6. Private notes (do not surface to publisher)

- **Foreword:** Foreword candidate: Guang Hao Low; fallback Hongbin Liu — approach only after availability/interest is confirmed. Do NOT name a planned/secured foreword in the proposal or email unless assent arrives.
- **Reviewer contact details** are kept only in `proposal/apress/private/` (never committed): James Daniel Whitfield and Martin Suchara emails.
- Both reviewers are confirmed: Dr James Daniel Whitfield and Dr Martin Suchara have already agreed to review this book, regardless of publisher. Their emails live only in the private copy.
