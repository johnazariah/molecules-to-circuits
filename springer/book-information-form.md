# Springer Book Information Form

> **Historical proposal draft — retained for provenance, not a current
> submission packet.** This form and its neighbouring PDF predate the expanded
> September 2026 manuscript. Their page counts, universal execution claims
> and electronic-publication answers must not be reused as current facts.
> Earlier book versions are publicly available through GitHub Pages,
> GitHub Releases and Zenodo. See the repository README for the current
> scope and edition status; a new publisher submission requires the author's
> approval. The original draft below is preserved rather than silently
> presented as a newly approved proposal.

## (1) Title of the book

| Field | Answer |
|:------|:-------|
| **Title** | From Molecules to Quantum Circuits |
| **Subtitle** | A Computational Guide to Fermion-to-Qubit Encodings |

## (2) Author's / editor's affiliation

| Field | Author 1 — Affiliation 1 |
|:------|:---------|
| **Name** | John S |
| **Surname** | Azariah |
| **Department** | Centre for Quantum Software and Information, School of Computer Science |
| **Institution** | University of Technology Sydney |
| **Address** | <!-- TODO: postal address --> |
| **Postal Code** | NSW 2007 |
| **Email ID** | john.azariah@student.uts.edu.au |
| **Website** | https://github.com/johnazariah |
| **ORCID** | 0009-0007-9870-1970 |

| Field | Author 1 — Affiliation 2 |
|:------|:---------|
| **Department** | Azure Core |
| **Institution** | Microsoft Corporation |
| **Address** | Redmond, WA |
| **Postal Code** | 98052 |
| **Email ID** | johnaz@microsoft.com |

> Add additional authors in a second table if needed.

## (3) Author's / editor's profile (~300 words)

John Azariah is a software engineer, language designer, and quantum computing researcher with over 30 years of experience in professional software development at Microsoft. His career spans foundational contributions to Microsoft applications like Office, Microsoft tools and languages like J#, and Microsoft Azure Services like Azure Batch, Azure Kubernetes Service (AKS), and Azure Storage. He served as compiler lead and language designer behind Microsoft Q#, Microsoft's quantum programming language, before releasing Azure Quantum as well.

John holds a BA in Computer Science and Chemistry from Dartmouth College and is currently a PhD candidate in Quantum Computing at the University of Technology Sydney under the supervision of Dr Chris Ferrie at the Centre for Quantum Software and Information.

He is the creator of the FockMap library, an open-source F# framework for symbolic Fock-space operator algebra that serves as the computational backbone of this book. FockMap provides a composable, type-safe pipeline for constructing qubit Hamiltonians, performing qubit tapering, and compiling Trotter circuits — with zero runtime dependencies on any quantum computing platform.

John's unique combination of academic grounding in Chemistry and Computer Science; deep industry software engineering expertise; and active academic research in quantum computing positions him to bridge the gap between theoretical quantum chemistry and practical circuit compilation. He maintains a technical blog at https://johnazariah.github.io covering topics in quantum computing, functional programming, and software engineering. His pedagogical approach of bridging the gap between relatable problems and discovering underlying structure reflects a commitment to getting to the bottom of things so his readers can understand the _why_ while applying it to the _what_.

<!-- TODO: adjust word count / details as needed -->

## (4) About the book (~300 words + chapter-wise description)

This book is a self-contained, 23-chapter computational tutorial covering the complete pipeline from molecular electronic structure to quantum circuit compilation for quantum simulation. It addresses a critical gap in the literature: no existing resource walks a student through every coefficient, every sign, and every intermediate Pauli string while providing executable code that reproduces each result.

Starting from generated one-body and two-body integrals of H₂ in STO-3G, we
derive and independently verify the direct fermionic and Jordan–Wigner
Hamiltonians before comparing six encoding interfaces, physical-sector
tapering, product-formula circuits, CNOT costs, and OpenQASM/Q# export. Separate
PySCF calculations provide the H₂ dissociation reference and an H₂O FCI angular
scan at fixed experimental O–H length, keeping classical chemistry evidence
distinct from circuit construction.

Every formula has a corresponding executable computation in the companion FockMap library, an open-source F# framework for symbolic Fock-space operator algebra. The book includes 10 interactive laboratory sessions.

**Chapter-wise description:**

| Stage | Chapters | Description |
|:------|:---------|:------------|
| **The Molecule** | Ch 1–3 | Electronic structure, mathematical notation, spin-orbital formalism |
| **The Machine** | Ch 4 | Qubits, gates, and circuits |
| **Encoding** | Ch 5–9 | Visual introduction to encodings, Hamiltonian construction, six encoding schemes, Vlasov tree, spectral verification |
| **Tapering** | Ch 10–13 | Motivation, diagonal Z₂ symmetries, Clifford rotation tapering, benchmarks |
| **Circuits** | Ch 14–17 | Time evolution, Trotter decomposition, CNOT staircase compilation, gate cost analysis |
| **The Pipeline** | Ch 18–21 | End-to-end pipeline, H₂O bond angle, VQE/QPE algorithms, OpenQASM 3.0 / Q# export |
| **Horizons** | Ch 22–23 | Scaling analysis, future directions |
| **Appendices** | A–B | API cookbook, mathematical background |

## (5) Unique selling points (USPs)

- **Complete pipeline in one place:** The only resource that covers the entire path from molecular integrals to compiled quantum circuits, with no steps treated as a black box.
- **Six encodings side by side:** Comprehensive comparison of Jordan–Wigner, Bravyi–Kitaev, Parity, binary tree, ternary tree, and Vlasov encodings — most resources cover only one or two.
- **Every intermediate result shown:** Every coefficient, sign, and Pauli string is derived explicitly, enabling readers to verify each step.
- **Executable companion code:** All formulas are backed by runnable F# scripts using the open-source FockMap library — readers can reproduce every result.
- **Two realistic molecules:** H₂ and H₂O are developed end-to-end, including computing physically meaningful results (dissociation curve, bond angle).
- **Platform-agnostic:** Unlike tutorials tied to Qiskit or Cirq, this book exports to both OpenQASM 3.0 and Q#, focusing on the mathematics rather than a specific SDK.
- **Hands-on lab sessions:** 10 interactive F# lab scripts guide the reader through building encodings from scratch.
- **Bridges three communities:** Accessible to quantum computing researchers, computational chemists, and software engineers alike, with prerequisites clearly stated and all necessary background developed in-text.

## (6) Type of work

**Monograph**

## (7) Originality of the work

| Option | Applicable? |
|:-------|:------------|
| New | **Yes** |
| Previously published with Springer | No |
| Previously published with other publisher | No |
| Previously published electronically | No |
| Translated | No |
| Revised | No |

> **Note:** The content is original and has not been peer-reviewed or published by any academic publisher.

## (8) Main discipline(s) / sub-discipline(s) / MSC codes

| # | Discipline |
|:--|:-----------|
| 1 | Quantum Computing |
| 2 | Quantum Chemistry / Quantum Simulation |
| 3 | Computational Physics |
| 4 | Mathematical Physics |
| 5 | Computer Science — Formal Methods |
| 6 | Physical Chemistry — Electronic Structure |
| 7 | Applied Mathematics |
| 8 | Scientific Computing |

**MSC 2010 codes:**

| Code | Description |
|:-----|:------------|
| 81P68 | Quantum computation |
| 81V55 | Molecular physics |
| 81-01 | Instructional exposition (quantum theory) |
| 81Q05 | Closed and approximate solutions to the Schrödinger equation |
| 15A66 | Clifford algebras, spinors |
| 68Q12 | Quantum algorithms and complexity |
| 92E10 | Molecular structure |

## (9) Courses (for textbooks)

| # | Course | Typical Institution |
|:--|:-------|:-------------------|
| 1 | Quantum Computing | UTS, MIT, ETH Zürich, Caltech |
| 2 | Quantum Chemistry Simulation | University of Toronto, TU Delft |
| 3 | Quantum Information Science | Stanford, University of Waterloo |
| 4 | Computational Quantum Physics | University of Oxford, MPQ Munich |
| 5 | Introduction to Quantum Algorithms | University of Maryland, UCL |
| 6 | Quantum Software Engineering | University of Technology Sydney |
| 7 | Advanced Topics in Quantum Computing | Harvard, University of Chicago |

## (10) Content level

| Level | Applicable? |
|:------|:------------|
| Lower undergraduate | |
| **Upper undergraduate** | **Secondary** |
| **Graduate** | **Primary** |
| **Professional** | **Secondary** |
| Practitioner | |
| **Research** | **Secondary** |
| Popular | |
| General | |

## (11) Estimated number of

| Metric | Count |
|:-------|:------|
| **Chapters** | 23 + 2 appendices (25 total) |
| **Manuscript pages** | ~166 |
| **Tables** | <!-- TODO: count tables --> |
| **Illustrations / Images** | 27 (25 diagrams + 2 data plots) |

## (12) Keywords (minimum 6)

1. fermion-to-qubit encoding
2. quantum chemistry simulation
3. Jordan–Wigner transform
4. Bravyi–Kitaev transform
5. qubit tapering
6. Trotterization
7. quantum circuit compilation
8. molecular Hamiltonian
9. Vlasov encoding
10. quantum computing tutorial

## (13) Author's / editor's earlier books

| Title | Publisher | ISBN | Year |
|:------|:---------|:-----|:-----|
| <!-- TODO: list any prior books or state "None" --> | | | |

## (14) Competition (Springer / non-Springer)

| Book | Publisher | Year | Why readers would choose *our* book |
|:-----|:---------|:-----|:------------------------------------|
| Nielsen & Chuang, *Quantum Computation and Quantum Information* | Cambridge University Press | 2010 | Covers QC foundations but not the chemistry-to-circuit pipeline; no executable code |
| Szabo & Ostlund, *Modern Quantum Chemistry* | Dover | 1996 | Develops electronic structure theory but stops before qubit encoding |
| McArdle et al., *Quantum Computational Chemistry* (review) | Rev. Mod. Phys. | 2020 | Algorithmic overview without step-by-step derivations or runnable code |
| Helgaker, Jørgensen & Olsen, *Molecular Electronic-Structure Theory* | Wiley | 2000 | Comprehensive reference but no quantum computing content |
| Hidary, *Quantum Computing: An Applied Approach* | Springer | 2021 | Broad QC textbook; encodings covered only briefly |
| Schuld & Petruccione, *Supervised Learning with Quantum Computers* | Springer | 2018 | ML-focused; does not cover quantum chemistry simulation pipeline |

> **Key differentiator:** No competing title covers the *complete* pipeline from molecular integrals through six encodings, tapering, Trotterization, and circuit export — with every intermediate result shown and executable.

## (15) Manuscript submission date

A complete first draft of the manuscript is available (166 pages, ~47,000 words, 23 chapters + 2 appendices). The author is considering adding material on qubitization — covering block encoding and the linear combination of unitaries (LCU) framework — to both the companion FockMap library and the book. Reformatting for Springer style and any requested revisions would constitute the remaining work.

| Field | Answer |
|:------|:-------|
| **Estimated delivery** | <!-- TODO: proposed date --> |

## (16) Experts in the topic

| # | Name | Affiliation | Email |
|:--|:-----|:------------|:------|
| 1 | Dr Guang Hao Low | Google Quantum AI | Confirmation requested |
| 2 | Dr Nathan Wiebe | Pacific Northwest National Laboratory (PNNL) / University of Toronto | Confirmation requested |
| 3 | Dr Stephen Jordan | Google Quantum AI | Confirmation requested |
