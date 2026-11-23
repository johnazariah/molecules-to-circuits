# Tentative Table of Contents — one-to-one chapter mapping

This maps every current manuscript file to a proposal-facing practical chapter title, a one- to two-sentence goal, and the chapter's actual first-level (`##`) section headings as they appear in the manuscript today. Chapter titles quoted from the manuscript are the current working titles; the "practical title" is the proposed Apress-facing title.


## Foreword
- **File:** `manuscript/foreword.md`
- **Manuscript title:** Front matter (`## Preface`)
- **Practical title:** Preface & Reader's Guide
- **Goal:** Frame the translation-layer problem and the book's chemistry→math→code teaching order; set prerequisites and how to run the labs.
- **First-level headings:**
- Preface

## Part I — The Molecule

### Chapter 1
- **File:** `manuscript/01-electronic-structure.md`
- **Manuscript title:** Chapter 1: The Electronic Structure Problem
- **Practical title:** The Electronic Structure Problem
- **Goal:** Why molecular energies are hard classically and where quantum simulation enters; introduce H₂ and H₂O as running examples.
- **First-level headings:**
- In This Chapter
- The Question
- A Molecule Is a Collection of Charges
- The Born–Oppenheimer Approximation: Freezing the Nuclei
- Basis Sets: Making the Infinite Finite
- Molecular Orbitals for H₂
- The Six Configurations of H₂
- Second Quantization: A Preview
- The Numbers: H₂ Integrals in STO-3G
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

### Chapter 2
- **File:** `manuscript/02-notation.md`
- **Manuscript title:** Chapter 2: The Notation Minefield
- **Practical title:** Fixing the Notation Minefield
- **Goal:** Nail down indices, spin-orbital conventions, and the FockMap API/version so every later derivation is unambiguous and runnable.
- **First-level headings:**
- In This Chapter
- The Problem
- Convention 1: Chemist's Notation (Mulliken)
- Convention 2: Physicist's Notation (Dirac)
- The Conversion: One Equation to Rule Them All
- The Hamiltonian: Which Convention Goes Where?
- The Symmetries That Save You (and Can Mislead You)
- The $\frac{1}{2}$ Prefactor
- A Complete Example: H₂ Two-Body Integrals
- The Companion Library: FockMap
- FockMap's Convention
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

### Chapter 3
- **File:** `manuscript/03-spin-orbitals.md`
- **Manuscript title:** Chapter 3: From Spatial to Spin-Orbital Integrals
- **Practical title:** From Spatial to Spin-Orbital Integrals
- **Goal:** Turn one- and two-body spatial integrals into the spin-orbital tensors the encoders consume.
- **First-level headings:**
- In This Chapter
- Why We Need Spin-Orbitals
- Spin-Orbital Indexing
- One-Body Expansion
- Two-Body Expansion
- The Complete Integral Tables
- From Tables to Code
- A Preview of What's Coming
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

## Part II — The Machine

### Chapter 4
- **File:** `manuscript/04-qubits-gates-circuits.md`
- **Manuscript title:** Chapter 4: The Quantum Computer's Vocabulary
- **Practical title:** The Quantum Computer's Vocabulary
- **Goal:** Introduce qubits, gates, and circuits as the target representation (introductory; condensable/referenceable per Apress positioning).
- **First-level headings:**
- In This Chapter
- Two Worlds, One Problem
- Qubits: The Power of Superposition
- Operations on a Single Qubit
- The Hard Part: Entanglement
- Is This Enough? Universality
- The Cost Table
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

## Part III — Encoding

### Chapter 5
- **File:** `manuscript/05-visual-encodings.md`
- **Manuscript title:** Chapter 5: A Visual Guide to Encodings
- **Practical title:** A Visual Guide to Encodings
- **Goal:** Build intuition for what a fermion-to-qubit encoding is before the algebra.
- **First-level headings:**
- In This Chapter
- Where We Stand
- The Tempting (Wrong) Idea
- What Makes Electrons Different from Qubits
- Jordan–Wigner: The Z-Chain
- Bravyi–Kitaev: Partial Parity Sums
- Tree Encodings: Improving the Logarithmic Constant
- The Complete Picture
- One More Thing: Number Operators Stay Cheap
- Choosing an Encoding: A Decision Guide
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

### Chapter 6
- **File:** `manuscript/06-building-hamiltonian.md`
- **Manuscript title:** Chapter 6: Building the Qubit Hamiltonian
- **Practical title:** Building the Qubit Hamiltonian
- **Goal:** Construct the 15-term Jordan–Wigner H₂ Hamiltonian term by term, in code.
- **First-level headings:**
- In This Chapter
- Configuration Energies and Couplings
- What We Have and What We'll Do
- The Recipe
- The Complete 15-Term Hamiltonian
- Reading the Hamiltonian: What's Hartree–Fock, What's Quantum
- The FockMap Parity Gate
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

### Chapter 7
- **File:** `manuscript/07-six-encodings.md`
- **Manuscript title:** Chapter 7: Six Encodings, One Interface
- **Practical title:** Six Encodings, One Interface
- **Goal:** Present JW, BK, Parity, binary tree, ternary tree, and Vlasov behind a single interface and compare them.
- **First-level headings:**
- In This Chapter
- Why Would We Want a Different Encoding?
- Do Different Encodings Give the Same Answer?
- Why Equivalence Holds: The CAR Theorem
- Six Encodings in Six Lines
- Where the Differences Emerge: Scaling
- The Three Frameworks
- Defining Custom Encodings
- A Decision Framework
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

### Chapter 8
- **File:** `manuscript/08-building-vlasov.md`
- **Manuscript title:** Chapter 8: Building a Tree Encoding
- **Practical title:** Building a Tree Encoding from Scratch
- **Goal:** Implement a tree (Vlasov) encoding end to end to show the encoding machinery has no black boxes.
- **First-level headings:**
- In This Chapter
- The Goal
- Step 1: Define the Tree Shape
- Step 2: Plug It Into the Framework
- Step 3: Compare the Tree Shapes
- Step 4: Verify Correctness
- The Weight Comparison
- The Recipe for Any Tree
- Key Takeaways
- Exercises
- Further Reading

### Chapter 9
- **File:** `manuscript/09-verification.md`
- **Manuscript title:** Chapter 9: Checking Our Answer
- **Practical title:** Checking Our Answer
- **Goal:** Verify the encoded Hamiltonian's matrix and spectrum against an independent Full-CI reference (−1.1372838345 Ha).
- **First-level headings:**
- In This Chapter
- The Verification Problem
- From Pauli Sum to Matrix
- The Eigenspectrum of H₂
- How Good Is This?
- Cross-Encoding Verification
- A Verification Checklist
- Encoding Stage Complete
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

## Part IV — Tapering

### Chapter 10
- **File:** `manuscript/10-why-tapering.md`
- **Manuscript title:** Chapter 10: Why Tapering?
- **Practical title:** Why Tapering?
- **Goal:** Motivate qubit reduction via symmetries and the cost of skipping it.
- **First-level headings:**
- In This Chapter
- The Observation
- What Tapering Gains You
- The Two Levels of Tapering
- When Tapering Is Exact
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

### Chapter 11
- **File:** `manuscript/11-diagonal-z2.md`
- **Manuscript title:** Chapter 11: Diagonal Z₂ Symmetries
- **Practical title:** Diagonal Z₂ Symmetries
- **Goal:** Find and use diagonal Z₂ symmetries to remove qubits safely.
- **First-level headings:**
- In This Chapter
- The Detection Algorithm
- Sectors: Choosing Eigenvalues
- How Sector Fixing Modifies Terms
- The Convenience Helper
- Validation
- Key Takeaways
- Sector Selection: A Practical Workflow
- Common Mistakes
- Further Reading

### Chapter 12
- **File:** `manuscript/12-clifford-tapering.md`
- **Manuscript title:** Chapter 12: General Clifford Tapering
- **Practical title:** General Clifford Tapering
- **Goal:** Generalize tapering with Clifford rotations and physical-sector selection.
- **First-level headings:**
- In This Chapter
- The Limitation of Diagonal Tapering
- The Binary Pauli Representation
- Finding All Symmetry Generators
- Clifford Rotation: Making Generators Diagonal
- Applying the Clifford to the Hamiltonian
- The Unified Pipeline
- Worked Example: Heisenberg Model
- Key Takeaways
- Further Reading

### Chapter 13
- **File:** `manuscript/13-tapering-benchmarks.md`
- **Manuscript title:** Chapter 13: Tapering Benchmarks
- **Practical title:** Tapering Benchmarks
- **Goal:** Quantify qubit and term reductions across encodings and molecules.
- **First-level headings:**
- In This Chapter
- Methodology
- Benchmark Results
- The Impact on Circuit Cost
- Tapering + Encoding: How to Benchmark the Combination
- Tapering Stage Complete
- How Much Tapering Is Enough?
- Key Takeaways
- Further Reading

## Part V — Circuits

### Chapter 14
- **File:** `manuscript/14-time-evolution.md`
- **Manuscript title:** Chapter 14: From Hamiltonian to Time Evolution
- **Practical title:** From Hamiltonian to Time Evolution
- **Goal:** Move from a static Pauli Hamiltonian to the exp(−iHt) circuit problem.
- **First-level headings:**
- In This Chapter
- What We Have, and What We Need
- Energy and Time: Two Sides of the Same Operator
- The Gap in Our Pipeline
- The Problem: Non-Commuting Terms
- The Trotter Idea
- First vs Second Order
- Beyond Trotterization: Qubitization
- What Comes Next
- Key Takeaways
- Further Reading

### Chapter 15
- **File:** `manuscript/15-trotter-formulas.md`
- **Manuscript title:** Chapter 15: Trotterization in Practice
- **Practical title:** Trotterization in Practice
- **Goal:** Build first- and second-order product-formula circuits with real error/step trade-offs.
- **First-level headings:**
- In This Chapter
- What Happens When Theory Meets H₂
- The H₂ Hamiltonian, One Last Time
- Second-Order Trotter: Symmetry for Accuracy
- Choosing the Time Step
- Quick CNOT Estimate (Before Gate Decomposition)
- How Many Trotter Steps Do I Need?
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

### Chapter 16
- **File:** `manuscript/16-cnot-staircase.md`
- **Manuscript title:** Chapter 16: The CNOT Staircase
- **Practical title:** The CNOT Staircase
- **Goal:** Compile Pauli-exponential terms into concrete CNOT + rotation gate sequences.
- **First-level headings:**
- In This Chapter
- The Big Picture
- The Decomposition Recipe
- Gate Counts
- Worked Example: ZZ Rotation (The Cheap Case)
- Worked Example: XXYY Rotation (The Expensive Case)
- Why This Makes Encoding Choice Concrete
- Key Takeaways
- Further Reading

### Chapter 17
- **File:** `manuscript/17-cost-analysis.md`
- **Manuscript title:** Chapter 17: Cost Analysis Across Encodings
- **Practical title:** Cost Analysis Across Encodings
- **Goal:** Count gates and CNOTs per encoding and expose the compilation cost trade-offs.
- **First-level headings:**
- In This Chapter
- The Optimization Stack
- The Full Cost Formula
- H₂ Direct JW Reference
- What a Molecular Benchmark Must Contain
- Key Takeaways
- Common Mistakes
- Exercises
- Further Reading

## Part VI — The Pipeline

### Chapter 18
- **File:** `manuscript/18-complete-pipeline.md`
- **Manuscript title:** Chapter 18: The Question We Can Now Answer
- **Practical title:** The End-to-End Pipeline
- **Goal:** Run integrals → encoding → tapering → circuit in one pass; reproduce the H₂ dissociation curve.
- **First-level headings:**
- In This Chapter
- The Payoff
- The Script
- What Each Line Does
- The Acceptance Table
- A Closer Look at the Output
- Extra Credit: The H₂ Dissociation Curve
- What Generalizes
- Key Takeaways
- Further Reading

### Chapter 19
- **File:** `manuscript/19-bond-angle.md`
- **Manuscript title:** Chapter 19: A Fixed-Bond Water Angle Scan
- **Practical title:** A Fixed-Bond Water Angle Scan
- **Goal:** Use a PySCF Full-CI angular scan for H₂O to show what the pipeline does and does not yet compute.
- **First-level headings:**
- In This Chapter
- The Question
- The Strategy: A Potential Energy Surface Scan
- Separate the Reference from the Circuit
- The Integrals
- The Scan
- The Result: Coarse Scan
- Zooming In: Fine Scan
- What Do the Numbers Mean?
- Why Does Water Bend?
- What This Scan Says About Encoding
- The Greenhouse Connection
- Key Takeaways
- Further Reading

### Chapter 20
- **File:** `manuscript/20-algorithms.md`
- **Manuscript title:** Chapter 20: Algorithms — VQE and QPE
- **Practical title:** Algorithms — VQE and QPE
- **Goal:** Show how the pipeline's Pauli Hamiltonian feeds VQE and QPE, and where state prep / energy estimation begin.
- **First-level headings:**
- In This Chapter
- From Exact Diagonalisation to Quantum Hardware
- VQE: The Near-Term Algorithm
- QPE: The Fault-Tolerant Algorithm
- VQE vs QPE: A Summary
- The Measurement Program in Practice
- What FockMap Does — and What It Doesn't
- Key Takeaways
- Further Reading

### Chapter 21
- **File:** `manuscript/21-circuit-export.md`
- **Manuscript title:** Chapter 21: Speaking the Hardware's Language
- **Practical title:** Speaking the Hardware's Language
- **Goal:** Export circuits to OpenQASM 3.0, Q#, and JSON for real toolchains.
- **First-level headings:**
- In This Chapter
- Three Formats, One Circuit
- OpenQASM: The Universal Format
- Q#: Azure Quantum
- JSON: The Python Bridge
- Same Verified Circuit, Three Formats
- When to Use Which
- What Export Does Not Solve
- Verification Checklist
- Key Takeaways
- Further Reading

## Part VII — Horizons

### Chapter 22
- **File:** `manuscript/22-scaling.md`
- **Manuscript title:** Chapter 22: Scaling — From H₂ to FeMo-co
- **Practical title:** Scaling — From H₂ to FeMo-co
- **Goal:** Honestly project resource costs from toy molecules to industrially relevant systems.
- **First-level headings:**
- In This Chapter
- The Scaling Landscape
- The Reproducible Encoding-Level Result
- From Operator Weight to a Molecular Benchmark
- FeMo-co: What the Active Space Tells Us
- Where Classical Methods Still Win
- Key Takeaways
- Further Reading

### Chapter 23
- **File:** `manuscript/23-whats-next.md`
- **Manuscript title:** Chapter 23: What Comes Next
- **Practical title:** What Comes Next
- **Goal:** Map the frontier (qubitization/LCU, better encodings) and where readers go next.
- **First-level headings:**
- In This Chapter
- What We Built
- Near-Term Extensions
- Bosonic Simulation
- Lattice Models
- The Deepest Connection
- Open Problems
- What the Pipeline Provides
- Key Takeaways
- Further Reading

## Appendices

### Appendix A
- **File:** `manuscript/appendix-cookbook.md`
- **Manuscript title:** Appendix A: Library Cookbook Reference
- **Practical title:** Library Cookbook Reference
- **Goal:** Quick-reference recipes for the FockMap API used throughout.
- **First-level headings:**
- Type System at a Glance
- Key Functions

### Appendix B
- **File:** `manuscript/appendix-theory.md`
- **Manuscript title:** Appendix B: Mathematical Background
- **Practical title:** Mathematical Background
- **Goal:** Self-contained refresher on the linear algebra and operator theory assumed in the main text.
- **First-level headings:**
- B.1 Why Encodings? (Theory Ch. 1)
- B.2 Second Quantization (Theory Ch. 2)
- B.3 Pauli Algebra (Theory Ch. 3)
- B.4 Jordan–Wigner Transform (Theory Ch. 4)
- B.5 Beyond Jordan–Wigner (Theory Ch. 5)
- B.6 Bosonic Operators (Theory Ch. 6)
- B.7 Mixed Systems (Theory Ch. 7)

## Back matter
- **File:** `manuscript/references.md`
- **Manuscript title:** References
- **Practical title:** References
- **Goal:** Consolidated bibliography.
- **First-level headings:**
- _(no first-level sections)_
