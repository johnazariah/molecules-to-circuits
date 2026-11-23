# Apress Book Proposal

**Proposed title:** Molecules to Quantum Circuits
**Proposed subtitle:** A Hands-On Pipeline from Electronic Structure to Exportable Quantum Circuits
**Author:** John S. Azariah
**Date:** 10 August 2026
**Series/positioning:** Apress professional / practitioner technology

> Note to the editor: this is the second, separate book proposal you invited after reviewing the quantum-applications proposal. It is intended to be evaluated and approved on its own, independently of *Demystifying Quantum Computing for Engineers*. The two books share an author and a hands-on philosophy but target different reader needs; neither depends on the other.

---

## 1. Author and contact

| Field | Answer |
|:------|:-------|
| Name | John S. Azariah |
| Email | John.Azariah@student.uts.edu.au |
| LinkedIn | https://www.linkedin.com/in/johnaz/ |
| GitHub | https://github.com/johnazariah |
| ORCID | 0009-0007-9870-1970 |
| Affiliation (academic) | Centre for Quantum Software & Information, University of Technology Sydney, NSW 2007, Australia |
| Affiliation (industry) | Azure Core, Microsoft Corporation, Redmond, WA |
| Mailing address | [MAILING ADDRESS — SEE PRIVATE COPY] |
| Phone | [PHONE — SEE PRIVATE COPY] |

## 2. Author profile and platform

John Azariah is a software engineer, language designer, and quantum-computing researcher with 30+ years of professional software development at Microsoft. His work spans foundational contributions to Microsoft Office, leading the Q# compiler team, and architecting core services for Azure Quantum. He holds a BA in Computer Science and Chemistry from Dartmouth College and is a PhD candidate in quantum computing at the University of Technology Sydney, supervised by Dr Christopher Ferrie at the Centre for Quantum Software & Information.

He is the creator of FockMap, the open-source F# library that is the computational backbone of this book. His combination of production software engineering, a chemistry-and-CS academic background, and active quantum-computing research positions him to write for engineers and scientists at the same time. Platform: 3 patents; 30+ conference and community talks (including FSharpConf 2023); an active GitHub presence and technical blog; and a professional network across the .NET, F#, and quantum-computing communities.

## 3. Book description (practical overview)

Molecules to Quantum Circuits is a hands-on engineering guide to the one layer of quantum chemistry simulation that almost every other resource treats as a black box: the translation from a molecule's electronic structure to an exportable quantum circuit. Between the molecule and the circuit sits the fermion-to-qubit encoding — the step that turns creation and annihilation operators into the Pauli strings a quantum computer actually runs. This book opens that box and keeps it open, exposing every coefficient, sign, and intermediate Pauli string along the way.

The book follows one concrete pipeline end to end. Starting from generated one- and two-body integrals for the hydrogen molecule in the STO-3G basis, it builds the 15-term Jordan–Wigner Hamiltonian by hand and in code, then verifies its matrix and full spectrum against an independent PySCF/Full-CI reference (ground-state energy −1.137 Ha). That verified result becomes the acceptance target for six encodings — Jordan–Wigner, Bravyi–Kitaev, Parity, balanced binary tree, balanced ternary tree, and Vlasov tree — presented behind a single interface and compared directly. From there the pipeline continues through Z₂ symmetry tapering (diagonal and Clifford), Trotterized time evolution with explicit CNOT-staircase compilation and gate-cost analysis, and finally circuit export to OpenQASM 3.0, Q#, and JSON.

Every transformation is executable. The companion is FockMap, an open-source F# library published on NuGet, built around a deliberately raw, physicist-facing Hamiltonian API, canonical H₂ fixtures with published release provenance, and an extensive automated test suite. Ten interactive lab scripts and ten data-generation/verification scripts let readers reproduce every number in the book, one command at a time, from a one-click Codespaces environment. A second running example — a fixed-bond water angular scan — is used honestly to mark exactly where the translation layer ends and where separate algorithmic tasks (state preparation, VQE/QPE energy estimation) begin.

The result is a practitioner's guide, not another survey. Readers who work through it can take real molecular integrals and produce a verified, tapered circuit exported to OpenQASM 3.0 and Q# — and explain every step in between. That exported circuit is a portable description, not a run-ready program: it still requires backend-specific transpilation, qubit mapping, and calibration, plus the separate state-preparation and algorithm (VQE/QPE) layers, before it yields energies on hardware. The book assumes only linear algebra and introductory quantum mechanics; second quantization, Pauli algebra, the encodings, and F# are all developed from scratch. It is written for quantum software engineers, computational chemists, and graduate students who need the encoding and compilation layer explained carefully, concretely, and reproducibly — with the mathematics kept independent of any single vendor SDK.

## 4. Unique selling points (exactly three)

1. **A book-length, fully executable, black-box-free pipeline from molecular integrals to exportable quantum circuits.** Built on the released open-source FockMap library (NuGet) with runnable F# labs and one-click Codespaces, every coefficient, sign, and Pauli string is inspectable and every result is reproducible — and independently checked against a classical PySCF/Full-CI reference.

2. **Six fermion-to-qubit encodings behind one interface, compared side by side.** Jordan–Wigner, Bravyi–Kitaev, Parity, binary tree, ternary tree, and Vlasov — where most resources cover one or two — with Z₂ tapering, Trotterization, and explicit CNOT/gate-cost accounting made concrete rather than cited.

3. **Platform-agnostic, engineering-grade output.** Circuits export to OpenQASM 3.0 and Q# (and JSON), so practitioners get portable artifacts and a trustworthy verification methodology instead of SDK lock-in. The book teaches the mathematics of the translation layer, not one vendor's API.

## 5. What readers will be able to do (outcomes)

After working through the book, a reader can:
- take one- and two-body molecular integrals and build a verified qubit Hamiltonian in any of six encodings;
- apply diagonal and Clifford Z₂ symmetry tapering to reduce qubit count, and justify which symmetries are physically valid;
- compile a Trotterized time-evolution circuit and estimate its CNOT and gate cost across encodings;
- export a circuit to OpenQASM 3.0 and Q# — a portable description that still needs backend-specific transpilation, qubit mapping, and calibration — and validate it against an independent classical reference; and
- state precisely where the translation layer ends and where state preparation and energy estimation (VQE/QPE) begin — avoiding the common overclaim that encoding "solves" the ground-state problem.

## 6. Target audience

- **Primary:** practicing quantum software engineers and computational scientists building or evaluating quantum-simulation pipelines.
- **Secondary:** graduate students in quantum computing and quantum chemistry, and physicists crossing into quantum computing who know Hamiltonians but not encodings.
- **Tertiary:** lecturers assembling a course module on quantum simulation (exercises have verifiable answers), and industry R&D teams choosing an encoding/compilation strategy.

**Assumed background:** linear algebra (vectors, matrices, eigenvalues) and introductory quantum mechanics (wavefunctions, bra–ket). **Developed from scratch:** second quantization, Pauli algebra, the six encodings, tapering, product-formula compilation, and F#.

## 7. Keywords

fermion-to-qubit encoding · quantum chemistry simulation · Jordan–Wigner transform · Bravyi–Kitaev transform · qubit tapering · Trotterization · quantum circuit compilation · molecular Hamiltonian · OpenQASM · VQE (variational quantum eigensolver) · quantum resource estimation · F# / functional programming

## 8. Source code and ancillary materials

- **FockMap** open-source F# library — NuGet package `FockMap` 0.9.0; source at https://github.com/johnazariah/encodings; companion code MIT-licensed. Release provenance (commit + DLL SHA-256) is published in the book.
- **10 interactive lab scripts** (`labs/01..10-*.fsx`), each runnable with `dotnet fsi` (plus a shared `PauliMatrix.fsx` helper module).
- **10 companion scripts** for data generation and independent verification (F# + Python/PySCF).
- **One-click Codespaces / Dev Container** so readers reproduce every result without local setup.
- **Exported circuit artifacts** in OpenQASM 3.0, Q#, and JSON.
- **~27 figures** (25 diagrams + 2 computed data plots) and an API cookbook appendix.
- A companion research-evidence repository backs the book's computed numbers; the manuscript and labs have been correctness- and reproducibility-checked.

## 9. Table of contents (tentative)

The book is 23 chapters plus 2 appendices, organized in seven parts, each mapping one-to-one to an existing manuscript chapter. Full section-level headings are in the companion file `tentative-toc.md`.

| # | Practical chapter title | Goal |
|:--|:------------------------|:-----|
| — | Preface & Reader's Guide | Frame the translation-layer problem and the chemistry→math→code teaching order; set prerequisites and how to run the labs. |
| 1 | The Electronic Structure Problem | Why molecular energies are hard classically and where quantum simulation enters; introduce H₂ and H₂O as running examples. |
| 2 | Fixing the Notation Minefield | Nail down indices, spin-orbital conventions, and the FockMap API/version so every later derivation is unambiguous and runnable. |
| 3 | From Spatial to Spin-Orbital Integrals | Turn one- and two-body spatial integrals into the spin-orbital tensors the encoders consume. |
| 4 | The Quantum Computer's Vocabulary | Introduce qubits, gates, and circuits as the target representation (introductory; condensable/referenceable per Apress positioning). |
| 5 | A Visual Guide to Encodings | Build intuition for what a fermion-to-qubit encoding is before the algebra. |
| 6 | Building the Qubit Hamiltonian | Construct the 15-term Jordan–Wigner H₂ Hamiltonian term by term, in code. |
| 7 | Six Encodings, One Interface | Present JW, BK, Parity, binary tree, ternary tree, and Vlasov behind a single interface and compare them. |
| 8 | Building a Tree Encoding from Scratch | Implement a tree (Vlasov) encoding end to end to show the encoding machinery has no black boxes. |
| 9 | Checking Our Answer | Verify the encoded Hamiltonian's matrix and spectrum against an independent Full-CI reference (−1.1372838345 Ha). |
| 10 | Why Tapering? | Motivate qubit reduction via symmetries and the cost of skipping it. |
| 11 | Diagonal Z₂ Symmetries | Find and use diagonal Z₂ symmetries to remove qubits safely. |
| 12 | General Clifford Tapering | Generalize tapering with Clifford rotations and physical-sector selection. |
| 13 | Tapering Benchmarks | Quantify qubit and term reductions across encodings and molecules. |
| 14 | From Hamiltonian to Time Evolution | Move from a static Pauli Hamiltonian to the exp(−iHt) circuit problem. |
| 15 | Trotterization in Practice | Build first- and second-order product-formula circuits with real error/step trade-offs. |
| 16 | The CNOT Staircase | Compile Pauli-exponential terms into concrete CNOT + rotation gate sequences. |
| 17 | Cost Analysis Across Encodings | Count gates and CNOTs per encoding and expose the compilation cost trade-offs. |
| 18 | The End-to-End Pipeline | Run integrals → encoding → tapering → circuit in one pass; reproduce the H₂ dissociation curve. |
| 19 | A Fixed-Bond Water Angle Scan | Use a PySCF Full-CI angular scan for H₂O to show what the pipeline does and does not yet compute. |
| 20 | Algorithms — VQE and QPE | Show how the pipeline's Pauli Hamiltonian feeds VQE and QPE, and where state prep / energy estimation begin. |
| 21 | Speaking the Hardware's Language | Export circuits to OpenQASM 3.0, Q#, and JSON for real toolchains. |
| 22 | Scaling — From H₂ to FeMo-co | Honestly project resource costs from toy molecules to industrially relevant systems. |
| 23 | What Comes Next | Map the frontier (qubitization/LCU, better encodings) and where readers go next. |
| A | Library Cookbook Reference | Quick-reference recipes for the FockMap API used throughout. |
| B | Mathematical Background | Self-contained refresher on the linear algebra and operator theory assumed in the main text. |

## 10. Estimated length

| Metric | Current draft | Estimated Apress final |
|:-------|:--------------|:-----------------------|
| Words | ~46,000 | ~55,000–65,000 |
| Print pages | ~175 | ~240–280 |
| Chapters | 23 + 2 appendices | 23–25 + appendices |
| Code listings (inline) | ~43 F# listings + Python snippets | ~45–60 |
| Runnable lab scripts | 10 (`.fsx`) | 10–12 |
| Companion data/verify scripts | 10 (F# + Python) | 10+ |
| Figures | ~27 (25 diagrams + 2 data plots) | ~30 |

A complete first draft exists today; the delta to an Apress final is editorial (practitioner-facing chapter intros, condensed introductory material, expanded code walkthroughs, index/glossary), not new research.

## 11. Competition and complementary works

| Title / resource | Type | Source, year | How this book differs |
|:-----------------|:-----|:-------------|:----------------------|
| Nielsen & Chuang, *Quantum Computation and Quantum Information* | Book | Cambridge UP, 2010 | Foundational QC theory; no molecule→circuit pipeline and no runnable encoding code. |
| Hidary, *Quantum Computing: An Applied Approach* | Book | Springer, 2nd ed. 2021 | Broad applied QC with code; encodings covered only briefly, not the compilation pipeline. |
| Johnston, Harrigan & Gimeno-Segovia, *Programming Quantum Computers* | Book | O'Reilly, 2019 | Strong gate-level programming primer; not chemistry- or encoding-focused. |
| Szabo & Ostlund, *Modern Quantum Chemistry* | Book | Dover, 1996 | Classical electronic-structure foundation; stops before qubit encoding. |
| Helgaker, Jørgensen & Olsen, *Molecular Electronic-Structure Theory* | Book | Wiley, 2000 | Definitive classical reference; no quantum-computing content. |
| McArdle, Endo, Aspuru-Guzik, Benjamin & Yuan, "Quantum computational chemistry" | Review paper | Rev. Mod. Phys. 92, 015003 (2020) | Authoritative algorithmic map; not step-by-step, runnable, verification-first derivations. |
| OpenFermion documentation/tutorials | Online / library | Google, ongoing | Reference material for a Python library, not a pedagogical book; emphasizes a single encoding path and hides intermediates. |
| Qiskit Nature tutorials | Online | IBM, ongoing | SDK-tied tutorials; transforms are largely black-box and Qiskit-locked. |
| PennyLane quantum-chemistry demos | Online | Xanadu, ongoing | Demo-style and PennyLane-locked; no cross-encoding comparison or explicit intermediate exposure. |

**Differentiator:** We are not aware of another book-length treatment that teaches the *encoding and compilation layer itself* as an executable engineering pipeline — molecular integrals → second quantization → six encodings → tapering → Trotterization → OpenQASM/Q# — with every intermediate exposed, independently verified, and reproducible from an open-source library. This is neither another general quantum-computing textbook nor another quantum-chemistry textbook; it is the missing bridge between them.

## 12. Why now

Quantum-simulation toolchains are maturing, but the encoding/compilation layer remains the least-documented and most error-prone part of the stack for practitioners. Engineers arriving from software and chemistry need a reproducible, vendor-neutral account of how a molecule becomes a circuit. This book meets that need with released, testable code rather than pseudocode.

## 13. Delivery schedule

| Milestone | Target date |
|:----------|:------------|
| Proposal submission | 10 August 2026 |
| Revised, Apress-formatted sample chapters | 30 September 2026 |
| Complete Apress revision / full manuscript delivery | 31 March 2027 |

Dates are derived from current manuscript maturity (a complete first draft exists) and are confirmed by the author.

## 14. Proposed technical reviewers

Apress typically requests two reviewers. Both reviewers below have already agreed to review this book, regardless of publisher; their contact details are held in the author's private copy of this proposal.

| # | Name | Affiliation | Expertise | Email |
|:--|:-----|:------------|:----------|:------|
| 1 | Dr James Daniel Whitfield | Associate Professor, Dartmouth College | Quantum-chemistry simulation; fermion-to-qubit methods | [REVIEWER EMAIL — SEE PRIVATE COPY] |
| 2 | Dr Martin Suchara | IonQ (formerly Argonne National Laboratory) | Applied quantum computing and quantum simulation | [REVIEWER EMAIL — SEE PRIVATE COPY] |

## 15. Apress Open (open access)

Not at this stage; open to discussing it if institutional or sponsor funding is available. (Confirmed.)

## 16. Relationship to the author's other proposed Apress book

This proposal is deliberately standalone. *Molecules to Quantum Circuits* is a deep, single-domain engineering guide to the fermion-to-qubit encoding and circuit-compilation layer, built around one open-source library and a reproducible pipeline. The separately proposed *Demystifying Quantum Computing for Engineers* is application-first and breadth-oriented, surveying multiple industry problems. They share an author and a hands-on, no-hype philosophy but serve different readers and reading goals. The author requests that the two proposals be evaluated and approved independently; neither is a prerequisite for, or dependent on, the other.
