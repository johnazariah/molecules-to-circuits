---
description: "Use when reviewing, verifying, or production-planning a technical book manuscript repo. Covers bootstrap variables, single-audit-file discipline, correctness-first workflow, notebook parity, and release planning."
---

# Book Review Bootstrap

Edit only the bootstrap block below when copying this file into another book repo.

## Bootstrap Block

- Book title: `From Molecules to Quantum Circuits`
- Short thesis: `Make the translation from molecular integrals to encoded Hamiltonians and logical circuits inspectable. Distinguish H2 construction from the separate fixed-bond PySCF water reference.`
- Intended reader: `Readers with linear algebra and introductory QM, not assumed chemistry, F#, Pauli algebra or measurement statistics.`
- Primary artifact: `BOTH: ordered print/EPUB sources and MyST HTML`
- Notebook policy: `Executable F# scripts and Python references, not notebooks; faithful companions with explicitly labelled toy demonstrations.`
- Reference policy: `Central bibliography in manuscript/references.md with primary metadata and accessible links; no local PDF archive currently maintained.`
- Authoritative audit file: `.review/CORRECTNESS-AUDIT.md`
- Authoritative action-plan file: `.review/ACTION-PLAN.md`
- Default operating mode: `Review-only for a new review request. Current September book implementation is explicitly authorised; publication, rights and submissions are not. Respect coordinator file ownership.`

Everything below this line should usually stay unchanged across repos.

## Core Operating Mode

- Start in review-only mode.
- Do not edit manuscript chapters unless the user explicitly asks for implementation.
- You may update review artifacts, planning artifacts, and reference inventories when requested.
- Prefer using the specialist book agents and skills when they exist in the repo.

## Artifact Discipline

- Keep one authoritative correctness audit file.
- Keep one authoritative action-plan file.
- Do not create review-file sprawl.
- Append later verification as dated addenda to the main audit file instead of creating standalone verification markdown files.
- If the repo already has review artifacts, reuse and consolidate rather than fork a parallel structure.

## Default Review Order

1. Read the repo structure, manuscript, build config, notebooks, figures, and existing review artifacts.
2. Run a correctness audit first.
3. Convert findings into a ranked action plan.
4. After fixes land, verify them against the action plan by appending dated addenda to the same audit file.
5. Once correctness is materially closed, run a whole-book style and approachability pass.
6. Then replace the defect backlog mindset with a production roadmap: structure freeze, editorial pass, figure program, notebook integrity, citations, output polish, external readers, and release criteria.

## What the Correctness Audit Must Cover

- factual accuracy;
- mathematical correctness;
- algorithmic correctness;
- internal consistency across chapters;
- misleading intuitions or overclaims;
- prose/notebook mismatches; and
- quantitative claims, benchmark numbers, and resource estimates that could damage trust if wrong.

## How to Rank Findings

- High severity: wrong, misleading, or trust-damaging.
- Medium severity: internally inconsistent, weakly supported, or liable to confuse a careful reader.
- Low severity: minor friction, terminology cleanup, or local clarity issues.

Always put findings first and summaries second.

## Notebook Rules

- Treat notebooks as part of the book's credibility story, not as optional extras.
- Judge notebooks on both execution and honesty.
- A notebook may be simplified and still be good, but only if the prose frames it honestly.
- Classify notebooks explicitly when needed: keep and validate, relabel and repair, or rewrite.

## Reference Rules

- If the reference policy says central papers belong in `.references`, ensure the most important cited papers are present there.
- Focus on claims a skeptical technical reader would actually challenge.
- Prefer freely accessible versions when possible.

## Whole-Book Style Rules

- Review the manuscript as a book object, not just as isolated chapters.
- Look for drift in voice, pacing, density, handoff patterns, and pedagogical contract.
- Distinguish real comprehension issues from taste-driven copyediting.

## Production-Planning Trigger

Switch from review backlog to production roadmap only when major correctness issues are mostly closed.

The production roadmap should cover:

- structure freeze;
- editorial sequencing;
- figure program;
- notebook integrity;
- citation sweep;
- build and output checks;
- external reader workflow; and
- release-candidate discipline.

## Rules for V2 or New Editions

- Treat earlier audit and action-plan files as historical context, not as clutter.
- Reuse the same authoritative paths when possible.
- Append new dated addenda rather than starting a fresh forest of review files.
- Do not assume older findings are still open; re-verify against the current manuscript.
- Distinguish clearly between regressions, newly introduced issues, and issues previously fixed.

## Default User-Facing Posture

- Be rigorous and direct.
- Prefer necessary fixes over optional polish.
- Say explicitly when there are no findings in a given pass.
- Do not confuse "no findings in this pass" with "nothing more could ever be improved."

## If Specialist Agents Exist

Use them roughly as follows:

- `book-production-lead` for end-to-end orchestration.
- `book-correctness-auditor` for trust-critical subject review.
- `book-fix-verifier` for staged follow-up checks.
- `book-style-reviewer` for whole-book consistency.
- `book-notebook-auditor` for companion notebook credibility.
- `book-production-planner` for the final roadmap.
