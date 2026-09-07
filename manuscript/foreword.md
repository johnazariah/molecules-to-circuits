# From Molecules to Quantum Circuits

*A Computational Guide to Fermion-to-Qubit Encodings*

**John S Azariah**

Centre for Quantum Software and Information, University of Technology Sydney

> **Rights:** Manuscript and rendered book © 2026 John S Azariah. All rights
> reserved. Companion source code is available under the MIT License; see
> `LICENSE-CODE`.

---

*For my parents, who believed before I did.*

---

## Preface

Every chemistry textbook tells you that water's bond angle is 104.5°
and waves at VSEPR theory. Every quantum computing textbook tells you
that fermions need to be encoded as qubits and shows you the
Jordan-Wigner transform. Very few resources connect these two worlds
with enough detail to show what has to happen between a chemistry
calculation and a circuit, or which part of that work an encoding
library actually does.

This book follows that translation layer. It does not turn circuit
construction into an energy measurement by giving it a more ambitious name.

We walk through the translation from molecular data to logical quantum
circuits with two running examples: the hydrogen molecule
(H₂), because it is the simplest system that exhibits all the
essential structure, and the water molecule (H₂O), because it gives us
a useful, larger chemistry reference against which to ask what remains.

For H₂ in the STO-3G basis at a bond length of 0.74 Å, generated
integrals lead to a checked Jordan-Wigner Hamiltonian, comparisons between
encodings, and logical product-formula circuits. Nuclear repulsion is a
separate energy contribution. State preparation and energy estimation
are separate algorithmic tasks, not hidden services supplied by the
Hamiltonian builder.

For water, the supplied calculation is a **classical PySCF FCI angular
scan at fixed experimental O–H length, 0.9584 Å**. Its sampled minimum
is conditional on that length, basis and grid. It is neither a full
geometry optimisation nor a water energy calculated by running a
FockMap circuit. The distinction matters: a reference calculation can
teach us what a quantum pipeline would have to reproduce without
pretending that the pipeline has already done it.

Core transformations have executable FockMap companions, while PySCF scripts
produce the independent chemistry references. The companions expose signs,
coefficients, and intermediate Pauli strings and fail closed when a live
package does not reproduce the canonical matrix. Exercises remain at the end
of each chapter for readers who want to deepen their understanding.

The book follows a deliberate pedagogical ordering: chemistry and
physics first, mathematical formalism second, executable code third.
We believe the question "why does this matter?" should always be
answered before the question "how does this work?" — and both should
be answered before "how do I compute it?"

### Who This Book Is For

- **Graduate students** starting in quantum chemistry simulation who
  need to understand the encoding layer between chemistry and circuits
- **Physicists** crossing into quantum computing who know Hamiltonians
  but not encodings
- **Software engineers** building quantum simulation pipelines who
  need the physics explained carefully
- **Lecturers** building a course module on quantum simulation who
  want homework exercises with verifiable answers

### What You Need to Know

We assume familiarity with linear algebra (vectors, matrices,
eigenvalues) and introductory quantum mechanics (wavefunctions,
bra-ket notation, the hydrogen atom). We do *not* assume prior
knowledge of:

- Chemistry, including molecular orbitals and electronic-structure methods
- Second quantization or Fock space
- Pauli algebra or qubit representations
- Fermion-to-qubit encodings
- F# or functional programming
- Measurement statistics

The chapters introduce these as they become useful. Read the unnumbered
**Reading and Running the Code** bridge between Chapters 1 and 2 before
the first coefficient factory. Later chapters explain the particular
tree, record, circuit and statistical constructions they need; the
bridge is not a requirement to learn a programming language in advance.

### How to Read This Book

The 23 chapters are organized into seven stages, following the quantum
simulation pipeline:

1. **The Molecule** (Chapters 1–3) — from molecules to integrals
2. **The Machine** (Chapter 4) — qubits, gates, and circuits
3. **Encoding** (Chapters 5–9) — from fermions to qubits
4. **Tapering** (Chapters 10–13) — removing redundant qubits
5. **Circuits** (Chapters 14–17) — Trotterization and cost analysis
6. **The Pipeline** (Chapters 18–21) — integration, fixed-bond scan, algorithms, export
7. **Horizons** (Chapters 22–23) — scaling and what comes next

You can read straight through (recommended for first reading), or
jump to a specific stage if you already know the earlier material.
Each chapter begins with "In This Chapter" learning objectives and
ends with a summary and exercises. The appendices are retrieval aids:
Appendix A collects selected API contracts and complete small programs;
Appendix B collects notation and conventions. Neither is a store of
prerequisites that you were expected to read before Chapter 1.

For a first reading, work through the derivations and trace the small
examples before running the longer companions. For a practical reading,
keep the repository beside the book and compare the named intermediate
results, not just the last printed energy. A successful execution tells
you that a program ran; the physical sector, matrix ordering and energy
convention tell you what its result means.

Exercises range from a hand calculation to a bounded programming task.
Try the calculation first, then use the companion to inspect the result.
**Selected Worked Solutions** gives reasoning for selected questions,
identified by chapter title and exercise name rather than just a number.
It is not a complete instructor answer key. An open-ended investigation
does not acquire a unique answer merely because it appears at the end
of a chapter.

### The Companion Software

The F# encoding and circuit companions use the pinned FockMap library:

- **Source code:** https://github.com/johnazariah/encodings
- **NuGet package:** `dotnet add package FockMap --version 0.9.0`
- **Web documentation:** https://johnazariah.github.io/encodings/

The library is open-source (MIT license) and runs on Windows, macOS,
and Linux via .NET 10. The book repository, including the Python
chemistry references and F# entry points, is at
<https://github.com/johnazariah/molecules-to-circuits>.
Python/PySCF dependencies are listed separately in `requirements-data.txt`;
installing FockMap does not install them.

Run scripts from the repository root, for example
`dotnet fsi labs/01-first-encoding.fsx`. The code-reading bridge explains
package restoration, paths and the distinction between a complete script,
a contextual excerpt and pseudocode. Numerical agreement within a stated
tolerance is the portable target; reproducing archived bytes additionally
requires the recorded numerical environment.

### Acknowledgements

This work grew out of research at the Centre for Quantum Software
and Information at the University of Technology Sydney.

I am grateful to Dr Chris Ferrie for his patient guidance of my
exploration, to Dr Guang Hao Low for sparking the journey, and to
Dr Stephen Jordan and Dr Helmut Katzgraber for their continued
encouragement.

And to my family, for bearing all my burdens.

*Sydney, March 2026*

---

### About the Author

John S Azariah is a software engineer, language designer, and quantum
computing researcher whose work sits at the intersection of chemistry,
computation, and executable mathematics. He earned a BA in Computer
Science and Chemistry from Dartmouth College, a combination that now
finds a natural expression in quantum simulation.

Over more than three decades in professional software development, he
has worked through successive technology shifts including desktop
software, the internet, cloud computing, quantum programming, and AI.
His career has included roles on Microsoft Excel and Microsoft Project,
early SharePoint prototypes, Visual J#.NET, large-scale Azure systems,
and principal engineering work across Azure Batch, Azure Kubernetes
Service, and AI systems in APAC. He was also compiler lead and one of
the language designers behind Microsoft Q#.

Alongside industry work, he has maintained a longstanding interest in
functional programming, scientific computing, and software craft. He is
currently a Principal AI SME at Microsoft and is pursuing a PhD in
Quantum Computing at the University of Technology Sydney under
Dr. Chris Ferrie.
