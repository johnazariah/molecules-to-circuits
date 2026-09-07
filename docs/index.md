# From Molecules to Quantum Circuits

*A Computational Guide to Fermion-to-Qubit Encodings*

**John S Azariah**
Centre for Quantum Software and Information, University of Technology Sydney

---

[Earlier book releases](https://github.com/johnazariah/molecules-to-circuits/releases) · [FockMap Library](https://github.com/johnazariah/encodings)

This source tree is the expanded working manuscript. The existing May book
downloads are earlier editions, not automatically rebuilt copies of this source.

---

## About This Book

This 23-chapter computational guide teaches the translation from molecular
electronic structure to encoded Hamiltonians and logical circuits. It assumes
linear algebra and introductory quantum mechanics, not chemistry, F#, Pauli
algebra or measurement statistics.

Starting from the one-body and two-body integrals of hydrogen (H₂), we construct the qubit Hamiltonian explicitly under six fermion-to-qubit encodings, verify spectral equivalence, reduce qubit count via Z₂ symmetry tapering, decompose the tapered Hamiltonian into Trotter circuits with explicit CNOT gate counts, and export the result to OpenQASM 3.0 and Q#.

Core transformations have executable FockMap companions. State preparation
and energy estimation remain separate algorithmic tasks. Water supplies a
classical PySCF FCI angular scan at fixed experimental O–H length, not a
water energy obtained from a quantum circuit or a full geometry optimisation.

---

## Contents

### Part I — The Molecule
1. [The Electronic Structure Problem](01-electronic-structure)
- [Reading and Running the Code](code-reading)
2. [The Notation Minefield](02-notation)
3. [From Spatial to Spin-Orbital Integrals](03-spin-orbitals)

### Part II — The Machine
4. [The Quantum Computer's Vocabulary](04-qubits-gates-circuits)

### Part III — Encoding
5. [A Visual Guide to Encodings](05-visual-encodings)
6. [Building the Qubit Hamiltonian](06-building-hamiltonian)
7. [Six Encodings, One Interface](07-six-encodings)
8. [Building a Tree Encoding](08-building-vlasov)
9. [Checking Our Answer](09-verification)

### Part IV — Tapering
10. [Why Tapering?](10-why-tapering)
11. [Diagonal Z₂ Symmetries](11-diagonal-z2)
12. [General Clifford Tapering](12-clifford-tapering)
13. [Tapering Benchmarks](13-tapering-benchmarks)

### Part V — Circuits
14. [From Hamiltonian to Time Evolution](14-time-evolution)
15. [Trotterization in Practice](15-trotter-formulas)
16. [The CNOT Staircase](16-cnot-staircase)
17. [Cost Analysis Across Encodings](17-cost-analysis)

### Part VI — The Pipeline
18. [The Question We Can Now Answer](18-complete-pipeline)
19. [A Fixed-Bond Water Angle Scan](19-bond-angle)
20. [Algorithms — VQE and QPE](20-algorithms)
21. [Speaking the Hardware's Language](21-circuit-export)

### Part VII — Horizons
22. [Scaling — From H₂ to FeMo-co](22-scaling)
23. [What Comes Next](23-whats-next)

### Appendices
- [Selected FockMap API Reference](appendix-cookbook)
- [Notation and Convention Reference](appendix-theory)
- [Selected Worked Solutions](selected-solutions)
- [References](references)

---

## Citation

```bibtex
@misc{azariah2026molecules,
  author    = {Azariah, John S},
  title     = {From Molecules to Quantum Circuits: A Computational Guide to Fermion-to-Qubit Encodings},
  year      = {2026},
  doi       = {10.5281/zenodo.18917465},
  url       = {https://github.com/johnazariah/molecules-to-circuits}
}
```

## License

Code: MIT-licensed as described in the repository [README](https://github.com/johnazariah/molecules-to-circuits#license).
Manuscript text: © 2026 John S Azariah. All rights reserved.
