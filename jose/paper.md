---
title: 'From Molecules to Quantum Circuits: A Computational Tutorial on Fermion-to-Qubit Encodings with Executable Examples'
tags:
  - quantum computing
  - quantum chemistry
  - fermion-to-qubit encoding
  - Jordan-Wigner
  - Bravyi-Kitaev
  - qubit tapering
  - Trotterization
  - F#
  - computational physics
  - pedagogy
authors:
  - name: John S Azariah
    orcid: 0009-0007-9870-1970
    corresponding: true
    affiliation: 1
affiliations:
  - name: Centre for Quantum Software and Information, School of Computer Science, Faculty of Engineering & Information Technology, University of Technology Sydney, NSW 2007, Australia
    index: 1
    ror: 03f0f6041
date: 7 March 2026
bibliography: paper.bib
---

# Summary

> **Historical educational-paper draft.** This packet is retained as a record
> of an earlier description, not as current book documentation or an approved
> new submission. The expanded September 2026 manuscript uses MyST/Jupyter
> Book 2, ten numbered labs, a code-reading bridge, two selected-reference
> appendices and worked solutions. Its chemistry references are separate
> from circuit construction. The older inventory and universal execution
> claims below are not the current reader contract; use the repository README
> and the revised manuscript for that purpose.

This educational resource is a self-contained computational tutorial
covering the complete pipeline from molecular electronic structure to
quantum circuit compilation for quantum simulation.  It is designed for
graduate students, advanced undergraduates, and researchers who know
some quantum mechanics and linear algebra but have not previously worked
with fermion-to-qubit encodings.

The tutorial comprises 23 chapters organized around six pipeline stages:

1. **Electronic structure** — Born–Oppenheimer approximation, basis
   sets, and molecular integrals, using H~2~/STO-3G as a running example.
2. **Encoding** — Six fermion-to-qubit encodings (Jordan–Wigner,
   Bravyi–Kitaev, Parity, balanced binary tree, balanced ternary tree,
   Vlasov tree)
   presented visually and algebraically, with side-by-side comparison.
3. **Tapering** — Qubit reduction via diagonal and general Z~2~
   symmetries, including Clifford rotation synthesis.
4. **Trotterization** — Suzuki–Trotter decomposition and CNOT staircase
   gate compilation, with cost analysis across encodings.
5. **Circuit output** — Generation of OpenQASM 3.0, Q\#, and
   Python-bridge circuits for execution on real quantum hardware.
6. **Algorithms** — Variational Quantum Eigensolver (VQE) measurement
   programs and Quantum Phase Estimation (QPE) resource estimation.

Every formula in the tutorial has a corresponding executable computation
in the FockMap library [@fockmap2026], an open-source F\# framework for
symbolic Fock-space operator algebra.  Readers can reproduce every
intermediate result — from individual Pauli string multiplications to
complete molecular Hamiltonians — by running the companion scripts.

The tutorial is accompanied by 15 hands-on laboratory exercises and a
15-chapter API cookbook that serves as reference documentation for the
underlying library.

# Statement of Need

Quantum simulation of molecular systems is among the most anticipated
applications of quantum computing [@feynman1982; @aspuruguzik2005].
The pipeline from a molecular Schrödinger equation to executable
quantum circuits passes through several mathematical translation layers:
second quantization, fermion-to-qubit encoding, symmetry reduction, and
Trotter decomposition.  Each layer involves notation choices, sign
conventions, and index manipulations that the research literature
compresses into a few lines.

Existing educational resources address parts of this pipeline but leave
gaps:

- **Quantum computing textbooks** (Nielsen and Chuang) cover
  foundations but not the chemistry-to-circuit pipeline.
- **Electronic structure textbooks** (Szabo and Ostlund) develop the
  chemistry but stop before encoding.
- **Review articles** [@mcardle2020] provide algorithmic overview but
  not step-by-step computation.
- **Software tutorials** (OpenFermion, Qiskit Nature) are platform-locked
  and treat encodings as black boxes.

No existing resource walks a student through *every coefficient, every
sign, and every intermediate Pauli string* while providing executable
code that reproduces each result.  This tutorial fills that gap.

# Learning Objectives

After completing the full tutorial, a student will be able to:

1. Construct one-body and two-body integral tables from a molecular
   geometry and basis set specification.
2. Translate a second-quantized Hamiltonian into a qubit Pauli-sum
   Hamiltonian using any of six encoding schemes.
3. Identify and exploit Z~2~ symmetries (both diagonal and
   general) to reduce qubit requirements.
4. Decompose a Pauli-sum Hamiltonian into a Trotter circuit and
   compute its CNOT gate cost.
5. Export the resulting circuit to OpenQASM 3.0 or Q\# for execution
   on quantum hardware or simulators.
6. Estimate VQE measurement costs and QPE resource requirements for
   a given molecular system and target precision.
7. Compare encodings quantitatively and make informed choices for
   specific hardware constraints.

# Pedagogical Approach

The tutorial follows three design principles:

**Computation precedes abstraction.**  Each concept is introduced
through a concrete numeric example (typically H~2~ in STO-3G) before
any general formula is stated.  The reader sees the answer before the
derivation, building intuition for what the formalism is trying to
express.

**Every formula is executable.**  The companion FockMap library
implements every algebraic operation appearing in the text.  Readers
are encouraged to modify coefficients, change basis sets, and compare
encodings interactively.  The library uses exact symbolic Pauli algebra
(not floating-point matrix multiplication), so intermediate results are
inspectable and exact.

**Visual before algebraic.**  Encoding schemes are first introduced
through diagrams (Mermaid flowcharts, ASCII art, comparison tables)
before the index-set or tree-based formalism is developed.  This is
particularly important for the Bravyi–Kitaev encoding, where the
connection between Fenwick trees and qubit assignments is opaque
without visual support.

# Content Structure

The tutorial is hosted as a static website built with Jekyll and
GitHub Pages, with MathJax for mathematical typesetting and Mermaid
for diagrams.  Each chapter is a self-contained Markdown page.

Satellite resources include:

- A **15-chapter API cookbook** covering every public type and function
  in the FockMap library, from Pauli operators through bosonic
  encodings and qubit tapering.
- A **7-chapter mathematical background** section covering second
  quantization, Pauli algebra, and encoding theory for readers who want
  formal derivations.
- **15 executable F\# interactive lab scripts** (`.fsx` files) with
  guided exercises, suitable for assignment in a graduate course.
- **10 standalone example scripts** for quick reference and
  copy-paste use.

# Instructional Design

The tutorial has been designed for use in three modes:

1. **Self-study:** A graduate student reads chapters 1–22 over a
   weekend, running the companion scripts to build intuition.
2. **Course module:** A lecturer assigns 3–4 chapters per week as
   readings, with the corresponding lab scripts as homework exercises.
3. **Reference:** A researcher uses the cookbook and API reference to
   solve a specific encoding or tapering problem.

Each chapter includes:

- "In This Chapter" learning objectives
- "Why This Matters" motivation
- "Try This Next" forward pointer
- Cross-links to the cookbook, theory appendix, and lab exercises

# Experience of Use

The tutorial materials have been developed iteratively during the
construction of the FockMap library and have been used for self-study
and informal peer instruction.  Formal classroom deployment is planned
for 2026–2027 at the University of Technology Sydney.

# Acknowledgements

This tutorial is dedicated to Dr. Guang Hao Low, whose encouragement
to study Bravyi–Kitaev encodings motivated both the library and this
educational resource.

The author thanks Dr Robin Kothari and Dr Stephen Jordan for bringing
Vlasov's Clifford-algebraic tree construction to his attention, which
led to the addition of the sixth encoding.

# References
