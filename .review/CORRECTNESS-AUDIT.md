# CORRECTNESS AUDIT — From Molecules to Quantum Circuits

> **Implementation outcome, 2026-09-19:** The corrected and expanded manuscript
> has been integrated and independently rechecked. See the
> [final implementation acceptance](#final-implementation-acceptance--2026-09-19)
> for closure, measured outputs and retained publisher/rights boundaries.
> Earlier findings and intermediate failures below remain dated history.

> **Current review:** See the [2026-09-07 comprehensive review](#comprehensive-review--2026-09-07)
> below and the current dated section of `ACTION-PLAN.md`. The July finding count
> and findings immediately below are historical, not a count of defects still
> present in the current manuscript. Preserve their dated closure evidence.
> The later [Apress expansion review](#apress-expansion-and-definition-order-review--2026-09-07)
> adds the author's approximately 300-page planning target and a
> clarity-first teaching plan; it does not close the correctness findings.
> **Later authority:** John authorised full book implementation on 2026-09-07.
> The dated reviews below describe the pre-implementation source. Closure
> requires subsequent evidence, not a change of operating mode.

**Audit date:** 2026-07-22
**Repository:** `johnazariah/molecules-to-circuits`
**Branch:** `johnazariah-encodings-book-review`
**HEAD:** `2269f0d3eb87f0d22e1b275e56add2077ad6915d`
**Mode:** Review only; no manuscript, code, lab, build, or generated-data files were changed.

## Current finding count

| Severity | Count |
|---|---:|
| High | 10 |
| Medium | 11 |
| Low | 4 |
| **Total actionable** | **25** |

“Regression” below means that the May 2026 plan claimed the issue was fixed but the defect is present at current HEAD. “Newly identified” means absent from that plan; the available file-inspection tooling does not establish the exact commit in which it entered.

---

# Findings

## High

### CA-H001 — The canonical H₂ integral data are internally impossible and do not describe the stated 0.74 Å geometry

**Classification:** Newly identified systemic defect; supersedes the historical local HF-number fix.
**Targets:** `manuscript/02-notation.md:75-81`, `manuscript/02-notation.md:149-160`, `manuscript/03-spin-orbitals.md:152-178`, `manuscript/03-spin-orbitals.md:192-239`, `manuscript/18-complete-pipeline.md:35-56`.
**Local evidence:** `code/h2_dissociation_integrals.json:174-214`, `code/ch18-generate-h2-integrals.py:62-102`.

For real orbitals, multiplication of scalar orbital values gives

$$
[01\mid 01]=[01\mid 10].
$$

The manuscript itself states this within-bracket symmetry at `02-notation.md:115-119`, but then assigns these two expressions different values: `0.6975782469` and `0.1809312700` Ha. The extra `0.6975782469` value is not part of the committed PySCF tensor for 0.74 Å. The one-body numbers are also different from the committed 0.74 Å data.

The repository’s generated 0.74 Å record gives:

$$
\begin{aligned}
V_{nn}&=0.7151043390810812,\\
h_{00}=h_{11}&=-1.2533097866459775,\\
h_{22}=h_{33}&=-0.4750688487721778,\\
[00\mid00]&=0.6747559268144483,\\
[00\mid11]&=0.6637114013508137,\\
[01\mid01]=[01\mid10]&=0.18121046201519694,\\
[11\mid11]&=0.6976515044904634.
\end{aligned}
$$

The minimal machine-readable payload is `code/h2_0.74_fixture.json`: four
one-body entries plus the exact 32-entry two-body spin tensor, raw RHF
provenance, and checksum
`a9a85179ad7f9fef8ef2ad5a90f54ac14b1923f1775ff17fdb6d14a1e1557a14`.
The direct oracle command is `python3 code/ch09-verify-h2.py`.

**Why this matters:** These are the book’s source data. The wrong tensor propagates into the central 15-term Hamiltonian, spectrum, tapering examples, Trotter angles, and resource counts. Cross-encoding agreement cannot detect a bad common input.

**Minimal correction direction:** Choose one geometry and one generated integral artifact as canonical. For the stated 0.74 Å model, regenerate every spin-orbital key from `code/ch18-generate-h2-integrals.py`, replace the hand-maintained tables/snippets, and add an automated symmetry and PySCF parity check. Do not retain a fifth independent real-orbital ERI.

---

### CA-H002 — The worked “exchange” derivation is diagonal, and the published 15-term JW Hamiltonian has wrong coefficients

**Classification:** Regression of historical `p3-f5`, with a newly identified coefficient failure.
**Targets:** `manuscript/06-building-hamiltonian.md:150-192`, `manuscript/06-building-hamiltonian.md:196-216`, `manuscript/06-building-hamiltonian.md:240-279`, `manuscript/15-trotter-formulas.md:27-38`.

The operator used as the off-diagonal worked example,

$$
a_0^\dagger a_2^\dagger a_0 a_2,
$$

does not exchange occupations. It is a signed product of number operators and is diagonal in the occupation basis. It cannot produce `XXYY`, `XYYX`, `YXXY`, or `YYXX`. The off-diagonal H₂ coupling is the opposite-spin double excitation and its adjoint.

Using the committed 0.74 Å PySCF data and the authoritative FockMap signature
order \(P_0P_1P_2P_3\) (qubit 0 leftmost), the purely electronic JW
coefficients should be:

| String | Coefficient (Ha) |
|---|---:|
| `IIII` | -0.8121706072 |
| `IIIZ`, `IIZI` | -0.2234315369 each |
| `IZII`, `ZIII` | +0.1714128264 each |
| `IIZZ` | +0.1744128761 |
| `IZIZ`, `ZIZI` | +0.1206252348 each |
| `IZZI`, `ZIIZ` | +0.1659278503 each |
| `ZZII` | +0.1686889817 |
| four four-body strings | magnitude \(0.1812104620/4=0.0453026155\) each |

Under the current FockMap/JW sign convention, the four-body sign pattern must be regenerated and then frozen by a matrix parity test; it should not retain the current magnitude `0.1744`, which is the spurious `0.697578.../4`.

**Why this matters:** Chapter 6 is advertised as the by-hand derivation on which the rest of the book rests. The present algebra says a diagonal operator creates coherence and then reports a Hamiltonian for a different, invalid integral tensor.

**Minimal correction direction:** Replace the example with an actual pair-excitation term, derive it through all four ladder operators, regenerate the entire table from the corrected integral tensor, and verify the resulting \(16\times16\) matrix against a direct fermionic construction.

---

### CA-H003 — The H₂ eigenspectrum, HF/FCI labels, and correlation energy are wrong

**Classification:** Regression of historical `p6-f2`; historical `p3-f1` is superseded by the bad source dataset.
**Targets:** `manuscript/09-verification.md:43-76`, `manuscript/09-verification.md:82-104`, `manuscript/09-verification.md:125-152`, `manuscript/18-complete-pipeline.md:154`, `manuscript/21-circuit-export.md:242-253`.
**Local evidence:** `code/h2_dissociation.csv:1-8`.

At 0.74 Å the committed PySCF reference is:

$$
\begin{aligned}
E_\mathrm{HF}^\mathrm{total} &= -1.1167593074\ \mathrm{Ha},\\
E_\mathrm{FCI}^\mathrm{total} &= -1.1372838345\ \mathrm{Ha},\\
E_\mathrm{FCI}^\mathrm{el} &= -1.1372838345-0.7151043391\\
&= -1.8523881736\ \mathrm{Ha},\\
E_\mathrm{corr} &= -0.0205245271\ \mathrm{Ha}\\
&\approx -12.88\ \mathrm{kcal/mol}.
\end{aligned}
$$

The manuscript instead reports `-1.1422` total and `-1.8573` electronic. Those values contradict the committed dissociation dataset. The full sector table must also be recomputed: it is not enough to replace only the lowest two-electron value.

There is an additional internal check failure. Evaluating the displayed Chapter 6 Pauli table on the fully occupied state does not give the Chapter 9 four-electron eigenvalue `-2.2001` Ha.

**Why this matters:** The book repeatedly says numerical diagonalisation independently validates the pipeline. It currently validates neither the stated geometry nor the displayed Hamiltonian.

**Minimal correction direction:** After CA-H001/002, regenerate and publish all 16 eigenvalues with particle number and \(M_S\) labels. Use the values above for the 0.74 Å HF and two-electron FCI checkpoints, and update every downstream occurrence.

---

### CA-H004 — The H₂ “complete pipeline” does not compute the claimed energy, and its displayed input is incomplete

**Classification:** Newly identified prose/code contradiction.
**Targets:** `manuscript/18-complete-pipeline.md:25-97`, `manuscript/18-complete-pipeline.md:158-220`.
**Local evidence:** `code/ch18-pipeline.fsx:81-180`, `code/ch18-dissociation-scan.py:41-55`.

The manuscript’s standalone map at `18-complete-pipeline.md:37-56` contains only 12 two-body entries, not the 32 spin-orbital entries required by its own Chapter 3 expansion. It omits the opposite-spin terms that generate the physical pair coupling.

The prose says `exactGroundStateEnergy` performs exact diagonalisation and attributes the dissociation curve to the FockMap skeleton pipeline. The local F# companion instead sums only identity coefficients as “a proxy for the ground state” (`code/ch18-pipeline.fsx:172-180`). The plotted/CSV FCI values are generated separately by PySCF’s `fci.FCI`, not by that F# route.

**Why this matters:** This is the capstone and the strongest reproducibility claim in the book. A global phase coefficient is not a ground-state energy.

**Minimal correction direction:** Either implement a sector-aware exact eigensolver in the companion and demonstrate equality with `code/h2_dissociation.csv`, or state plainly that PySCF supplies the FCI reference while FockMap only constructs/costs circuits. Replace the incomplete map with generated data.

---

### CA-H005 — The Clifford-tapering algorithm and Heisenberg derivation are mathematically wrong

**Classification:** Regression of historical `p4-f8`; source implementation status is cross-repo pending.
**Targets:** `manuscript/12-clifford-tapering.md:66-106`, `manuscript/12-clifford-tapering.md:174-223`.

The null space of the term-commutation matrix is the Pauli centralizer, but an arbitrary basis of that null space need not be a mutually commuting stabilizer set. Tapering requires an independent Abelian subgroup plus a phase-consistent Clifford map. The text currently equates the whole null space with simultaneously taperable generators.

The worked CNOT conjugation also drops an \(X_0\):

$$
\operatorname{CNOT}_{0\to1}(Y_0Y_1)\operatorname{CNOT}_{0\to1}
=-X_0Z_1,
$$

not \(-Z_1\). Therefore

$$
XX+YY+ZZ \longmapsto X_0-X_0Z_1+Z_1.
$$

In sector \(Z_1=+1\), the result is the scalar \(+I\); in sector \(Z_1=-1\), it is \(2X_0-I\). Across the sectors the eigenvalues are \(\{-3,1,1,1\}\), matching the original model. The manuscript’s tapered \(X_0\) with eigenvalues \(\pm1\) does not.

**Why this matters:** The chapter claims symbolic exactness and uses this example as proof of end-to-end tapering correctness.

**Minimal correction direction:** Re-derive generator selection as a commuting stabilizer problem, retain Pauli phases during Clifford conjugation, replace the worked table/eigenvalues, and gate publication claims on the `johnazariah/encodings` tapering audit.

---

### CA-H006 — Tapering order and sector selection are described in a way that changes the physical problem

**Classification:** Unresolved historical `p4-f9`, extended by a newly identified sector error.
**Targets:** `manuscript/11-diagonal-z2.md:80-100`, `manuscript/11-diagonal-z2.md:146-159`, `manuscript/11-diagonal-z2.md:189-197`, `manuscript/13-tapering-benchmarks.md:102-123`, `manuscript/13-tapering-benchmarks.md:145-155`, `manuscript/17-cost-analysis.md:13-32`, `manuscript/17-cost-analysis.md:92-104`.

Two errors are conflated:

1. A JW qubit Hamiltonian cannot be tapered and then re-encoded with a different fermion-to-qubit map. Encoding occurs first. The valid comparison is “encode each fermionic Hamiltonian, then identify/taper symmetries in that representation.”
2. Sweeping sectors and taking the global minimum does **not** in general recover the molecular ground state with the required electron number and spin. A lower sector may describe an ion or a different spin state. The statement that a sector mismatch is “never lower” is false.

**Why this matters:** The stated optimization stack is not an executable transformation, and automatic `+1` or global-minimum sector choices can produce the wrong molecule.

**Minimal correction direction:** Rewrite every pipeline as `fermionic Hamiltonian → chosen encoding → symmetry discovery → map physical (N, M_S, point-group) quantum numbers to generator eigenvalues → taper → compile`. Use a sector sweep only as a diagnostic, never as a substitute for physical sector identification.

---

### CA-H007 — The H₂O PES is a PySCF reference scan at fixed experimental bond length, not the claimed FockMap end-to-end first-principles geometry calculation

**Classification:** Newly identified prose/code contradiction.
**Targets:** `manuscript/19-bond-angle.md:23-53`, `manuscript/19-bond-angle.md:59-107`, `manuscript/19-bond-angle.md:111-142`, `manuscript/19-bond-angle.md:146-203`, `manuscript/19-bond-angle.md:250-256`.
**Local evidence:** `code/ch19-bond-angle-scan.py:25-48`, `code/ch19-bond-angle-scan.py:80-100`, `labs/08-h2o-workshop.fsx:256-309`.

The committed scan calls PySCF RHF and `fci.FCI` directly. It does not export per-geometry integral JSON, call FockMap, taper a Hamiltonian, or diagonalise a FockMap matrix as the chapter says. The lab’s PES section reads precomputed **HF** energies, not the chapter’s FCI energies.

In addition, `BOND_LENGTH = 0.9584` Å is the experimental O–H distance. The calculation is a one-dimensional angular cut at fixed empirical bond length, not a full geometry optimization “without empirical input.” A true equilibrium geometry calculation must optimize the bond length and angle (or explicitly call the result a conditional angle scan).

The pseudocode also takes the smallest eigenvalue over a tapered qubit Hamiltonian without showing that the \(N=10\), singlet sector was selected. PySCF FCI does select the intended electron/spin sector; the displayed FockMap pseudocode does not establish that.

**Why this matters:** The bond-angle result is the book’s headline application.

**Minimal correction direction:** Relabel the existing result as “PySCF FCI angular scan at fixed experimental \(r_\mathrm{OH}\)” and describe FockMap as a separate encoding/cost exercise, or add a real sector-aware FockMap parity computation. Remove “no empirical input” unless both internal coordinates are optimized from an ab initio model.

---

### CA-H008 — The QPE resource formula and circuit-energy verification procedure are invalid

**Classification:** Historical `p6-f5` remains only partially corrected; the circuit verification error is newly identified.
**Targets:** `manuscript/20-algorithms.md:125-171`, `manuscript/21-circuit-export.md:242-253`, `manuscript/18-complete-pipeline.md:140-155`.

For \(U=e^{-iHt_0}\), QPE estimates phase

$$
\phi=-\frac{E t_0}{2\pi}\pmod 1.
$$

Ignoring confidence overhead, resolving energy to \(\epsilon\) requires approximately

$$
m\ge \left\lceil\log_2\frac{2\pi}{\epsilon t_0}\right\rceil,
$$

plus an energy shift/range choice that prevents phase aliasing. The text omits \(t_0\), then says \(\lceil\log_2(2\pi/\epsilon)\rceil\) gives 10 ancillas at \(\epsilon=1.6\) mHa; it gives 12, not 10.

Separately, simulating a Trotter step and measuring the resulting state’s energy is not a check that the circuit “produces” the FCI eigenvalue. Time evolution does not prepare a ground state, and exact \(e^{-iHt}\) preserves the energy expectation of the input state. QPE needs eigenstate-overlap preparation; VQE needs an ansatz and Hamiltonian measurements.

**Why this matters:** The current instructions would reject a correct time-evolution circuit or appear to validate an incorrect one.

**Minimal correction direction:** Specify \(t_0\), spectral shift/range, aliasing, state overlap, confidence, and controlled-simulation error in the QPE estimate. Verify export by comparing the circuit unitary/statevector to the intended product formula; verify energy only inside an explicit VQE/QPE workflow.

---

### CA-H009 — The book promises efficient ground-state extraction merely from compact state representation

**Classification:** Newly identified central overclaim.
**Targets:** `manuscript/01-electronic-structure.md:21-24`, `manuscript/09-verification.md:76-79`, `manuscript/22-scaling.md:23-27`, `manuscript/23-whats-next.md:123-131`.

Representing a state in \(n\) qubits avoids storing \(2^n\) amplitudes, but it does not by itself provide an efficient algorithm for preparing a molecular ground state or estimating its energy. General local-Hamiltonian ground-state estimation is hard, and QPE succeeds only in proportion to the trial state’s overlap with the target eigenstate. VQE has no general efficient-convergence guarantee.

Statements such as “extract the ground-state energy without the exponential cost” and “only a quantum computer can” omit these conditions.

**Why this matters:** This is the main motivation offered to readers and overstates what the subsequent pipeline establishes.

**Minimal correction direction:** Separate representation advantage from algorithmic advantage. State that quantum algorithms may offer favorable scaling for structured chemistry instances given efficient Hamiltonian access, adequate state preparation/overlap, and fault-tolerant resources; do not promise a generic exponential-cost removal.

---

### ordering-convention — String, occupation-integer, and matrix basis orders are not the same

**Classification:** Newly established cross-repository High blocker; the original
24 audit IDs remain unchanged.
**Targets:** `manuscript/05-visual-encodings.md`, `manuscript/09-verification.md`,
`manuscript/21-circuit-export.md`, `manuscript/appendix-theory.md`,
`code/ch09-verify-h2.py`, all state-labelled examples and exporters.

The verified conventions are:

1. Displayed FockMap signatures use character position \(i\) for qubit \(i\);
   qubit 0 is leftmost. For example,
   \(a_2^\dagger=\tfrac12\,\mathrm{ZZXI}-\tfrac{i}{2}\,\mathrm{ZZYI}\).
2. Displayed occupation kets use \(\lvert n_0n_1\ldots\rangle\), again with
   mode 0 leftmost.
3. The occupation integer uses place value \(2^j\) for mode \(j\). The H₂ HF
   occupation \(n_0=n_1=1\) is therefore integer `3`, conventionally printed
   as binary `0b0011`, even though the displayed ket is \(\lvert1100\rangle\).
4. The repaired dense matrix builders reverse the displayed signature and form
   \(P_{n-1}\otimes\cdots\otimes P_0\), so matrix rows equal occupation
   integers. \(\lvert1100\rangle\) is row `3` (`0b0011`).
5. Qiskit-style Pauli labels display qubit 0 on the right, so FockMap
   signatures require an explicit reversal at that boundary.

Spectrum agreement cannot detect a basis permutation. Energies may match while
eigenvectors, amplitudes, occupations, and state labels are wrong.

The repaired research tooling now verifies number-operator diagonals on all 16
states, \(a_2^\dagger=\tfrac12\,\mathrm{ZZXI}
-\tfrac{i}{2}\,\mathrm{ZZYI}\), H₂ HF label/integer/row `|1100>`/`3`/`3`,
JW-versus-direct element-wise error \(2.8\times10^{-16}\), and ground-state HF
amplitude squared `0.9873339`. BK can be isospectral without element-wise JW
equality because it uses a different encoded basis.

**Why this matters:** State preparation, sector selection, exported operators,
and every state-resolved worked example depend on the basis map, not only on
eigenvalues.

**Minimal correction direction:** Name index/place-value rules explicitly; add
number-operator tests on labelled basis states; check that the H₂ HF ket has
occupation integer and matrix row 3 plus the expected energy; and test one
FockMap-to-Qiskit-style Pauli-label reversal.

---

## Medium

### CA-M001 — “Diagonal = classical” and “correlation energy lives entirely in off-diagonal terms” are technically false

**Classification:** Newly identified.
**Targets:** `manuscript/06-building-hamiltonian.md:13-98`, `manuscript/06-building-hamiltonian.md:220-236`, `manuscript/09-verification.md:63-75`.

A diagonal Hamiltonian does not force every state’s density matrix to be diagonal; it only fails to create computational-basis coherence from a diagonal initial state. Degenerate diagonal Hamiltonians may have coherent ground states. More importantly, the energy lowering of a correlated state relative to the HF determinant contains both off-diagonal matrix-element contributions and changes in diagonal configuration populations. It is not “exactly” the expectation of the four off-diagonal Pauli terms.

The four-body H₂ terms are pair-excitation couplings, not a clean synonym for the chemistry exchange integral.

**Correction direction:** Present diagonal/off-diagonal structure as basis-dependent configuration energies and couplings. Explain that coupling enables configuration mixing, while the final correlation energy is the difference of full variational minima and includes both changed populations and interference terms.

---

### CA-M002 — The Trotter step rules and 1-norm bounds are presented as guarantees without the required hypotheses

**Classification:** Unresolved historical `p5-f7` and `p5-f11`.
**Targets:** `manuscript/15-trotter-formulas.md:95-107`, `manuscript/15-trotter-formulas.md:137-177`, `manuscript/15-trotter-formulas.md:181-195`.

\(\Delta t\le1/\|H\|_1\) is a heuristic scale, not a theorem that the approximation is accurate. The second-order `1/12` formula is not established from the cited generic nested-commutator expression for the chosen ordering. The numerical use of \(\|H\|_1\approx3.7\) also includes the identity/global-phase coefficient, which contributes no Trotter error.

Claims that second order “typically needs \(\sqrt N\) fewer steps,” that 52 steps “suffice,” and that H₂O costs 500,000 CNOTs need either an actual commutator evaluation or explicit heuristic labeling.

**Correction direction:** State a precise theorem for the exact product ordering and norm, exclude identity terms, compute the committed H₂ commutator bound, and label empirical step choices separately from rigorous upper bounds.

---

### CA-M003 — The molecular CNOT/resource tables are not reproducible and contradict one another

**Classification:** Unresolved historical systemic issue `S2` and `p4-f7`/`p5-f13`.
**Targets:** `manuscript/13-tapering-benchmarks.md:114-123`, `manuscript/17-cost-analysis.md:45-88`, `manuscript/22-scaling.md:31-45`, `manuscript/22-scaling.md:73-88`, `manuscript/22-scaling.md:113-123`.

No committed molecule-specific artifact produces the H₂O, LiH, N₂, or FeMo-co tables. Chapter 17 gives FeMo-co as roughly `500,000` JW versus `20,000` TT CNOTs/step (25×); Chapter 22 gives roughly \(10^7\) versus \(10^5\) (100×). Chapter 13 assigns about 1,800 CNOTs to a 14-qubit full-space H₂O model while Chapter 17 assigns the same number to a different 12-qubit frozen-core model.

The tables also infer full Hamiltonian cost from worst-case ladder-operator weight, which is insufficient: term distribution, orbital ordering, cancellation, tapering sector, and connectivity all matter.

**Correction direction:** Create one generated benchmark artifact with geometry, basis, active space, electron sector, orbital ordering, encoding version, tapering generators/sector, term list, Trotter ordering, and logical connectivity assumptions. All chapters should render or quote that artifact.

---

### CA-M004 — Binary and ternary trees do not have different asymptotic logarithmic classes

**Classification:** Newly identified.
**Targets:** `manuscript/05-visual-encodings.md:250-280`, `manuscript/05-visual-encodings.md:320-340`, `manuscript/07-six-encodings.md:106-117`, `manuscript/08-building-vlasov.md:194-211`.

The base of a logarithm is a constant factor:

$$
\log_3 n=\frac{\log_2 n}{\log_2 3}=\Theta(\log n).
$$

Thus “going below \(O(\log_2 n)\)” and a different “best asymptotic scaling” are misleading. Ternary-tree constructions improve the leading constant and attain a relevant Pauli-weight lower bound; they do not change \(\Theta(\log n)\) to a smaller asymptotic class.

**Correction direction:** Use \(\Theta(\log n)\) for both and state the exact/ceiling weight bounds or the approximately \(1/\log_2 3\) depth factor supported by Jiang et al.

---

### CA-M005 — Chapter 7 directly contradicts itself about H₂ Pauli strings

**Classification:** Unresolved historical `p3-f3`/`p5-f8`.
**Targets:** `manuscript/07-six-encodings.md:40-53`, `manuscript/07-six-encodings.md:187-199`.

Lines 49–53 correctly say the strings may differ, while line 191 says all encodings produce identical Pauli strings. The reported per-encoding H₂ costs in Chapter 17 also differ, which is incompatible with identical strings.

**Correction direction:** Say that this H₂ input happens to simplify to 15 terms in each of the six implementations, but the strings and weights differ. Restrict same-term-count claims to the demonstrated Clifford-equivalent implementations and tested input.

---

### CA-M006 — The greenhouse explanation makes the bent angle a necessary and nearly sufficient cause

**Classification:** Newly identified.
**Targets:** `manuscript/19-bond-angle.md:238-256`.

A permanent dipole is not required for greenhouse activity: a vibration is IR active when it changes the dipole moment, as linear CO₂ demonstrates. Water’s bent equilibrium structure gives it a permanent dipole and a strong rotational spectrum, and its vibrational modes also absorb IR, but the angle is not “literally the reason Earth has a habitable temperature.”

**Correction direction:** Describe the bend as one contributor to water’s dipole, rotational spectrum, and vibrational selection rules. Remove the sole-cause habitability claim.

---

### CA-M007 — Trust-critical hardware and resource claims lack adequate, versioned sources

**Classification:** Historical `p2-f8` and `p7-f10` remain unresolved/partial.
**Targets:** `manuscript/04-qubits-gates-circuits.md:59-101`, `manuscript/17-cost-analysis.md:60-88`, `manuscript/20-algorithms.md:145-171`, `manuscript/22-scaling.md:92-123`, `jose/paper.bib:4-11`.

The hardware gate times/error rates have no dated calibration source and line 61 applies 20–100 ns single-qubit timing across superconducting and trapped-ion devices. The QPE and physical-qubit totals are not derived from a cited resource model. `jose/paper.bib` is not a book bibliography and still contains the placeholder DOI `10.5281/zenodo.XXXXXXX`.

**Correction direction:** Add dated primary/vendor calibration citations, a reproducible resource model, and stable references for all quantitative FeMo/QPE/QEC claims. Replace or remove the placeholder DOI.

---

### CA-M008 — Appendix B is omitted from PDF/EPUB and one Pages workflow despite the project claiming two appendices

**Classification:** Newly identified publishing inconsistency.
**Targets:** `README.md:58-74`, `myst.yml:64-67`, `manuscript/Book.txt:1-25`, `.github/workflows/pages.yml:27-32`, `.github/workflows/release.yml:43-54`.

MyST includes both appendices, but `Book.txt` includes only `appendix-cookbook.md`; the release workflow builds PDF/EPUB from `Book.txt`, and `pages.yml` copies only that appendix. The repository’s own description says “2 appendices.”

**Correction direction:** Put `appendix-theory.md` in the canonical book manifest and every publishing path, or change the project description and MyST TOC to say there is only one.

---

### CA-M009 — The appendix calls the phase-free Pauli basis a group

**Classification:** Newly identified.
**Target:** `manuscript/appendix-theory.md:36-47`.

The \(4^n\) phase-free strings are an operator basis but are not closed under multiplication: \(XY=iZ\). The Pauli **group** includes phases (commonly \(\{\pm1,\pm i\}\), with convention-dependent quotients).

**Correction direction:** Distinguish the \(4^n\)-element phase-free Pauli basis from the Pauli group with phases.

---

### CA-M010 — The “previously undocumented” star-tree restriction is a novel theorem asserted without evidence

**Classification:** Newly identified; cross-repo pending.
**Targets:** `manuscript/07-six-encodings.md:123-166`, `manuscript/08-building-vlasov.md:5-19`, `labs/08-h2o-workshop.fsx:464-470`.

The manuscript elevates an internal investigation to a general theorem about the Seeley–Richard–Love index-set framework, but provides no proof, precise hypotheses, counterexample, or research citation.

**Correction direction:** Do not publish this as established fact until the `johnazariah/encodings-research` report supplies a formal statement and proof. Otherwise narrow it to a limitation of the current FockMap construction.

---

### CA-M011 — The detailed H₂ measurement-group table is not a valid qubit-wise-commuting partition

**Classification:** Newly identified; API behavior cross-repo pending.
**Targets:** `manuscript/20-algorithms.md:53-69`, `manuscript/20-algorithms.md:190-218`.

For the stated QWC rule, all non-identity Z-only H₂ terms can share one computational-basis group, while each of the four distinct full-support X/Y strings requires its own local basis. The displayed group sizes `5, 3, 3, 2, 2` do not reflect that partition and line 216 incorrectly says Group 1 contains all diagonal terms.

**Correction direction:** Print the actual `groupCommutingTerms` output from a pinned FockMap version and state whether grouping is QWC or general commuting. For QWC, expect one Z group plus four X/Y basis groups (with identity dropped or assigned arbitrarily).

---

## Low

### CA-L001 — Two spin/orbital implementation claims are overgeneralized

**Classification:** Newly identified.
**Targets:** `manuscript/03-spin-orbitals.md:38-49`, `manuscript/03-spin-orbitals.md:63-79`.

The \(Z^4\) spin-orbit scaling is a hydrogenic rule of thumb, not a universal molecular law. PySCF generally supplies spatial-orbital integrals and does not have a universal “default interleaved spin-orbital ordering”; the interleaving is imposed by conversion code.

**Correction direction:** Qualify \(Z^4\) as hydrogenic/rough and attribute interleaving to this book’s conversion/FockMap convention.

---

### CA-L002 — The stated HF share of the H₂O bending energy does not match the stated numbers

**Classification:** Newly identified.
**Target:** `manuscript/19-bond-angle.md:207-215`.

Using the values in the paragraph, \(0.113/0.126=0.897\), so HF accounts for about 90%, not 85%, of the stated bending energy.

**Correction direction:** Recompute from the underlying scan and report one consistent rounded percentage.

---

### CA-L003 — “This is exact. No approximations.” omits the model assumptions already made

**Classification:** Newly identified.
**Target:** `manuscript/01-electronic-structure.md:49-63`.

The displayed Hamiltonian is the nonrelativistic, point-charge Coulomb Hamiltonian; it omits relativistic/QED effects and other corrections. It is exact only within that model.

**Correction direction:** Replace the absolute statement with “exact within the nonrelativistic Coulomb model before Born–Oppenheimer separation.”

---

### CA-L004 — The advertised companion-script count does not match the current tree

**Classification:** Newly identified.
**Targets:** `README.md:58-74`, `Makefile:58-59`.

The repository currently contains nine executable source scripts under `code/` (five F# and four Python), while the project metadata says ten companion scripts. This may be a missing artifact or stale count.

**Correction direction:** Define what is counted and make the metadata and packaged files agree.

---

# Strengths

1. **The complex-orbital ERI symmetries are now stated correctly.** `manuscript/02-notation.md:107-131` distinguishes complex and real cases and fixes the historical conjugation error.
2. **The book now declares its bit/string ordering.** `manuscript/05-visual-encodings.md:13-16` makes the ket and Pauli-string conventions explicit.
3. **The canonical anticommutation relations and JW occupation convention are consistent.** `manuscript/05-visual-encodings.md:43-53` and `manuscript/appendix-theory.md:49-59` agree on \(a^\dagger=(X-iY)/2\) for \(|1\rangle=\) occupied.
4. **Nuclear repulsion is separated from the electronic Pauli Hamiltonian.** `manuscript/06-building-hamiltonian.md:118-128` and `:302-305` resolve the earlier \(V_{nn}\) ambiguity.
5. **The revised water narrative correctly says bending already occurs at HF.** `manuscript/19-bond-angle.md:185-215` no longer attributes the qualitative bend to correlation.
6. **The local PySCF CSV/JSON artifacts are mutually useful reference points.** In particular, `code/h2_dissociation.csv` and `code/h2_dissociation_integrals.json` make the H₂ numerical corruption diagnosable.
7. **The \(R_z\) half-angle convention is now explicit.** `manuscript/18-complete-pipeline.md:107-110` and `manuscript/21-circuit-export.md:55` agree on \(R_z(\theta)=e^{-i\theta Z/2}\).
8. **Foundational encoding, tapering, Trotter, VQE, and QPE literature is named.** The remaining citation problem is chiefly quantitative provenance and the absence of a coherent book bibliography, not a total absence of sources.

---

# Independent quantitative checks

No shell execution facility was available in this review session, so no repository commands were run and no generated data were modified. The following checks were independently re-derived from committed files:

| Check | Inputs | Result |
|---|---|---|
| Real-orbital ERI symmetry | `03-spin-orbitals.md:175-176` | `[01|01]` and `[01|10]` must be equal; current values differ by `0.5166469769` Ha |
| 0.74 Å HF energy | `h=−1.2533097866`, `[00|00]=0.6747559268`, `Vnn=0.7151043391` | \(2h+[00|00]+V_{nn}=-1.1167593074\) Ha |
| 0.74 Å electronic FCI energy | CSV total `−1.1372838345`, `Vnn=0.7151043391` | `−1.8523881736` Ha |
| Correlation energy | FCI total minus HF total | `−0.0205245271` Ha = about `−12.88 kcal/mol` |
| Correct four-body coefficient magnitude | transition ERI `0.1812104620` | `0.0453026155` Ha |
| Heisenberg CNOT conjugation | standard CNOT Pauli rules | \(YY\to-XZ\), not \(-Z\); sector spectra combine to \(\{-3,1,1,1\}\) |
| QPE ancilla arithmetic | \(\epsilon=0.0016,t_0=1\) | \(\lceil\log_2(2\pi/\epsilon)\rceil=12\), not 10 |
| H₂O bending fraction | stated `0.113/0.126` | `89.7%`, not 85% |
| FeMo determinant scale | central binomial \(\binom{108}{54}\) | about \(2.5\times10^{31}\), so “\(10^{30}\)” is only a loose order estimate |
| Appendix publication | MyST TOC vs `Book.txt`/workflows | Appendix B is absent from PDF/EPUB and one Pages path |

---

# Historical May 2026 plan status

The historical plan says 70 findings were fixed, but it does not enumerate a consistent set of 70: its severity tables, systemic-ID lists, and individual tables do not reconcile. Every item actually named in that file was nevertheless checked against current HEAD.

## Systemic items

| Historical item | Current status | Current evidence |
|---|---|---|
| S1 / `p4-f2,p5-f3,p7-f3,p1-f5` — H₂O 12 vs 14 | **Still closed** | Full 14-orbital and frozen-core 12-orbital contexts are now labeled (`17-cost-analysis.md:60-65`, `19-bond-angle.md:59-64`) |
| S2 / `p5-f1,p5-f4,p5-f5,p5-f6,p7-f1,p7-f5,p7-f6` — CNOT tables | **Unresolved** | FeMo tables still disagree; see CA-M003 |
| S3 / `p1-f1,p1-f6,p6-f1,p6-f3,h2o-correlation-fix` — correlation narrative | **Still closed** | `01-electronic-structure.md:21-24,161-164`; `19-bond-angle.md:207-215` |
| S4 / `p3-f2,p3-f9` — \(V_{nn}\) | **Still closed** | `06-building-hamiltonian.md:118-128,302-305`; vacuum energy remains electronic zero |

## Individually listed items

| Status | Historical IDs |
|---|---|
| **Still closed** | `p1-f2`, `p2-f1`, `p4-f1`, `p7-f2`, `p2-f2`, `p2-f3`, `p2-f4`, `p2-f5`, `p1-f4`, `p3-f4`, `p3-f6`, `p3-f7`, `p4-f3`, `p4-f4`, `p5-f2`, `p6-f4`, `p6-f6`, `p7-f4`, `p7-f7`, `p7-f8`, `p7-f9`, `p2-f6`, `p2-f7`, `p2-f9`, `p2-f10`, `p1-f7`, `p3-f8`, `p4-f5`, `p4-f6`, `p5-f9`, `p5-f10`, `p6-f7`, `p6-f8`, `p6-f9`, `p7-f11`, `p7-f12`, `p7-f13` |
| **Regression relative to claimed closure** | `p6-f2` (wrong H₂ values returned), `p3-f5` (exchange derivation is still wrong), `p4-f8` (Heisenberg example is completed but incorrect) |
| **Unresolved** | `p3-f3`, `p5-f7`, `p5-f8`, `p4-f7`, `p4-f9`, `p5-f11`, `p5-f13`, `p2-f8`, `p7-f14` |
| **Partially addressed, still open** | `p6-f5` (added \(2\pi\) but omitted evolution time/range), `p7-f10` (a citation was added but does not derive the stated overhead) |
| **Superseded by a broader current defect** | `p3-f1` and `p1-f3` by CA-H001/003; `p1-f8` because the lab and chapter now agree with each other but share the same invalid integral tensor |

No historical item is marked fixed merely because a number changed; the current derivation or local reference had to support it.

---

# Cross-repo pending items

These claims cannot be closed from this repository and must remain pending until the named reports are available.

## `johnazariah/encodings`

1. Verify CAR, adjoint relations, qubit ordering, and matrix equivalence for all six ladder maps over representative and boundary sizes.
2. Verify `computeHamiltonianWith` uses the manuscript’s physicist-index order, annihilator order, and \(1/2\) convention.
3. Verify the exact H₂ Pauli coefficients/signs from the corrected 0.74 Å tensor.
4. Verify `findCommutingGenerators` selects a mutually commuting independent subgroup, tracks phases, and maps physical sectors correctly.
5. Verify H₂ tapering really gives 4→2 qubits and five terms in the intended two-electron singlet sector for every encoding.
6. Verify skeleton/direct construction parity across geometries, including changing zero patterns.
7. Verify first/second-order rotation lists, identity handling, \(R_z\) angles, and reported CNOT statistics.
8. Verify `groupCommutingTerms` means QWC or general commutation and capture its actual H₂ grouping.
9. Verify `estimateShots`, `qpeResources`, OpenQASM, Q#, and JSON APIs exist with the semantics claimed by Chapters 20–21.
10. Verify custom `EncodingScheme` and arbitrary ternary-tree validation; local labs currently construct schemes without demonstrating CAR.

## `johnazariah/encodings-research`

1. Supply the precise theorem/proof, hypotheses, and literature relationship for the claimed star-tree restriction on the index-set framework.
2. Supply proof and exact constants for balanced ternary/Vlasov tree weight bounds and the claimed optimality/lower bound.
3. Establish that the implemented “Vlasov tree” corresponds to Vlasov’s construction, not only to a breadth-first tree inspired by it.
4. Supply reproducible H₂O/LiH/N₂/FeMo-co term and CNOT benchmark reports, including orbital ordering and active-space assumptions.
5. Supply any defensible fault-tolerant FeMo/QPE resource estimate used in Chapters 20 and 22.

---

# Reference gaps

1. The manuscript has chapter-level “Further Reading” lists but no coherent, linked book bibliography. `jose/paper.bib` is short, publication-specific, and contains a placeholder FockMap DOI.
2. Hardware calibration numbers in Chapter 4 need dated primary/vendor sources.
3. H₂O basis-comparison values need archived command output or generated tables, not only hard-coded summary print statements.
4. All molecule-level CNOT and fault-tolerant resource tables need a citable generated artifact.
5. The star-tree theorem and FockMap-specific design claims need a research report or must be narrowed to implementation observations.
6. The FeMo active-space citation is adequate for the approximate 54-electron/54-spatial-orbital model; it does not support the book’s new ternary-tree gate totals.

---

# Current assessment

**Not publication-ready on correctness.** The H₂ source tensor, central Hamiltonian derivation, spectrum, capstone execution story, tapering derivation, H₂O provenance, and QPE/circuit verification claims all require correction. The committed PySCF reference data and several repaired conventions provide a strong recovery path, but historical closure should not be relied upon.

---

# Cross-repository input: `johnazariah/encodings`

**Report received:** 2026-07-22
**Status:** Authoritative audit input, not fix verification. A dedicated upstream
implementation session is in progress. Items below remain open until that session
reports a corrected build, regression tests, and exact outputs.

## Confirmed blocking defects

1. **Full-Clifford tapering corrupts Pauli phases and spectra.**
   `src/Encodings/Tapering.fs:581-582` uses an incomplete CNOT phase condition.
   The current flip `cx && tz` must include the tableau factor; the audited
   condition is `cx && tz && (tx = cz)`. Minimal counterexamples are
   `XY -> -YZ` in the library versus the correct `XY -> +YZ`, and
   `YZ -> -XY` versus the correct `YZ -> +XY`, under `CNOT(0,1)`.
   On the corrected H₂/STO-3G input, current `FullClifford` tapering gives
   `-1.8319` Ha rather than `-1.8524` Ha electronic and no sector recovers the
   true ground state. `DiagonalOnly` tapering re-verifies correctly.
   **Mapped items:** CA-H005, CA-H006.
2. **The test suite does not protect tapering spectrum preservation.**
   All 794 current tests pass, but no property compares tapered spectra or
   energies with the corresponding untapered sectors; existing CNOT tapering
   tests only require non-empty output. **Mapped items:** CA-H005, CA-H006 and
   companion verification work.
3. **Book snippets need explicit feature-module imports.** `open Encodings` alone
   does not bring the feature modules into scope. Depending on the snippet, the
   required imports include `Encodings.JordanWigner`, `.Hamiltonian`,
   `.Tapering`, `.Trotterization`, `.CircuitOutput`, and the BK, tree, Majorana,
   bosonic, cost-analysis, or FCIDUMP modules actually used. Snippet parity
   remains blocked until the corrected upstream build supplies exact compile
   results.

## Authoritative convention result

`PauliRegister` stores **qubit 0 at the leftmost signature position**.
Qiskit-style Pauli labels place qubit 0 at the rightmost character, so that
string boundary requires reversal. OpenQASM gate operands are explicitly
indexed (`q[i]`) and do not define a Pauli-string display convention. The book
and source tests now use occupation-integer matrix rows and state-resolved
checks consistently; ordering-convention is implemented pending independent
final verification.

## Clean results that may be used after version pinning

- All six encodings preserve the CAR at representative sizes `n=4` and `n=6`.
- Jordan-Wigner, Bravyi-Kitaev, and ternary-tree spectra agree before broken
  Full-Clifford tapering is applied.
- Pauli multiplication phases, Majorana signs, weight scaling, Trotter
  decomposition, OpenQASM export, `DiagonalOnly` tapering, and bosonic encoders
  re-verify cleanly.
- The ladder convention is
  \(a^\dagger=\tfrac12(Z\text{-chain})X-\tfrac{i}{2}(Z\text{-chain})Y\), with
  the sign of the \(Y\) term reversed for \(a\).
- The independent chemistry anchors agree with this audit:
  \(E_\mathrm{el}=-1.852388\) Ha and
  \(E_\mathrm{total}\approx-1.137284\) Ha after adding
  \(V_{nn}=0.715104\) Ha.

The cross-repository report also observed `23` stored Pauli entries and
\(\lambda=2.6993\) for its corrected H₂ input. Local package execution resolved
the term-count discrepancy: eight entries have zero coefficients after
distribution, leaving the standard 15 nonzero JW terms. Book companions now
filter `Complex.Abs coefficient > 1e-12` before reporting counts. The
coefficient 1-norm is `2.6993` including the known identity term and
`1.8871072169` for measured non-identity terms. CA-H002/003 still require the
upstream implementation session's exact coefficient/matrix and regression-test
outputs before package parity closes.

These norms have units Ha and are distinct from Chapter 15's first-order
commutator sum
\(\Lambda_{\mathrm{comm}}=0.2861997180\ {\rm Ha}^2\). The audit rejects bare
“lambda” notation unless the definition, included terms, and units are stated.

**Superseded diagnosis retained for traceability:** a live book-companion run
with the raw tensor passed directly to the low-level factory produced:
`dotnet fsi code/ch06-building-hamiltonian.fsx`, using the canonical 32-entry
physicist tensor, produces the correct set of 15 nonzero signatures but
coefficients such as `IIII = -3.5608` and four-body magnitude `0.0906`.
The independent direct/JW reference requires `IIII = -0.8121706072` and
four-body magnitude `0.0453026155`. All six package encodings report the same
term count, so cross-encoding agreement cannot diagnose this shared
factory/index/prefactor mismatch. This exact reproduction has been sent to the
upstream implementation session; CA-H002/003 remain blocked.

An exact package trace **supersedes** the interim library-defect diagnosis.
`computeHamiltonianWith` is a low-level **operator-coefficient** factory:
package key `(i,j,k,l)` multiplies
\(a_i^\dagger a_j^\dagger a_k a_l\), with no hidden \(1/2\). It is not a raw
integral factory. FCIDUMP already converts raw integrals to that operator-level
contract; PR #5 `2e1ee0d` removes misleading dead `0.5` code and documents/tests
the distinction.

For a raw physicist tensor in the book convention,
\(\tfrac12\langle pq\mid rs\rangle
a_p^\dagger a_q^\dagger a_s a_r\), the package adapter must return
`0.5 * raw[p,q,l,k]` for package key `(p,q,k,l)`.

The controlled four-way experiment is therefore an adapter test:

| Caller adapter | `IIII` | `XXYY` | Interpretation |
|---|---:|---:|---|
| raw map passed directly | `-3.5607946918` | `+0.0906052310` | wrong API level |
| half only | `-2.6445866636` | `+0.0453026155` | magnitude fixed, sign wrong |
| swap only | `+0.1040374210` | `-0.0906052310` | sign fixed, doubled |
| half + swap | `-0.8121706072` | `-0.0453026155` | correct low-level adapter |

For the current raw path,
\(H=H_1-2H_{2,\mathrm{standard}}\), dense max error is `10.9944963384`,
and the two-electron spectrum is
`[-3.9385781287, -3.4182223622, -2.6933805142 x3, -2.2629940047]`.
With both corrections, dense max error is `2.59e-10`.

The eight additional stored signatures are exact zeros:
`XXXY`, `XXYX`, `XYXX`, `XYYY`, `YXXX`, `YXYY`, `YYXY`, `YYYX`.
They inflate unfiltered term/cost counts; package zero-pruning remains open.
Book paths filter at `1e-12`.

**Book implementation:** preserve the raw un-antisymmetrized physicist tensor,
unrestricted \(1/2\) Hamiltonian, \(a_s a_r\) order, and separate \(V_{nn}\);
adapt only at the low-level factory boundary. The explicit adapter now passes:

- all 15 coefficients and sentinels;
- JW dense matrix versus direct oracle, max error `6.661e-16`;
- all-six spectral moments, max relative error below `3e-15`;
- all numbered labs; and
- isolated Chapter 18 pipeline output.

CA-H002/003 now await final audit of `2e1ee0d` API wording and the package
zero-pruning decision, not a numerical book correction.

## Non-blocking upstream integration notes

- The README quick-start input produces five diagonal terms, not the claimed
  15-term molecular Hamiltonian.
- Upstream README/JOSS chapter and test counts are stale.
- The Trotter API drops imaginary coefficient parts and therefore needs a
  documented or enforced Hermitian-Hamiltonian precondition.
- Inclusive `0..n` Hamiltonian loops are benign but off by one; the bosonic
  argument order is easy to misuse and under-documented.

## Provisional `encodings` repair output

**Received:** 2026-07-22
**Tapering status:** Configuration table verified in PR #5 commit `b2f9bc3`.
Package Hamiltonian integration remains blocked on the missing builder/FCIDUMP
follow-up.

- The CNOT phase condition is now `cx && tz && (tx = cz)`. All 16 Pauli
  conjugations pass, and dense \(UHU^\dagger\) error is `4.4e-16`.
- The tapering tests use an oracle/adapted H₂ Hamiltonian with electronic ground
  energy `-1.852388` Ha; this is not evidence that the public raw builder is fixed.
- FullClifford's default `(+1,+1,+1)` sector reduces this JW instance from four
  to one qubit but has spectrum `[0, 0.2081]`; it is a genuine **non-ground**
  sector.
- Sector `(-1,-1,+1)` reduces the same encoded instance from four to one qubit
  and preserves `-1.852388` Ha. The union of all eight sector spectra equals
  the original 16 eigenvalues.
- JW `DiagonalOnly` is a four-to-four no-op for this full H₂ instance. Earlier
  BK diagonal reductions are encoding-specific and cannot be generalized.
- PR #5 reports 845 passing tests for the earlier tapering/docs/order scope.
  They do not cover the absent Hamiltonian/FCIDUMP/zero-pruning repair. The
  off-by-one follow-up also remains open because the attempted range underflows
  at `n=0`.

No bare “4→N” count is accepted. Every count must name the input, encoding,
symmetry method, generators, sector, and spectrum comparison. Executable book
paths currently stay untapered rather than silently using the default
non-ground sector.

The verified canonical H₂ tapering configurations are:

| Encoding | Method | Result | Removed | Sector status |
|---|---|---|---|---|
| JW | DiagonalOnly | 4→4 | none | no single-qubit Z generators |
| JW | FullClifford | 4→1 | `[0,1,2]` | default `+++` is non-ground; ground `--+` |
| BK | DiagonalOnly | 4→2 | `[1,3]` | qubits 1/3 fixed `+,+`; classic qualified H₂→2 case |
| BK | FullClifford | 4→1 | `[0,1,3]` | default `+++` not established as physical ground |

Both encodings have three Z₂ symmetries; the encoding changes which symmetries
are exposed as single-qubit Z operators. The synthetic cookbook 4→2 example is
separate and remains valid. Main baseline is 794 tests; 51 added cases give 845
on this commit.

The post-fix audit is **not closure-ready**. Remaining upstream blockers are:

1. the `0u .. n-1u` repair underflows at `n=0`; safe ranges/guards and zero-mode
   tests are required;
2. upstream README/cookbook still contain an unqualified full-H₂ 4→2 claim;
3. encoding/chapter/test counts remain stale in upstream docs;
4. “arbitrary tree” exceeds the implemented max-three-child contract and lacks
   a four-child rejection/validation explanation;
5. ordering must say Qiskit-style labels and visibly document q0-leftmost
   signatures;
6. public Trotter APIs must document/enforce the Hermitian-coefficient
   precondition; and
7. one H₂ test remains tautological and needs an independent expected result.

The book has already removed default-sector execution, bare taper counts, and
arbitrary-tree claims, but CA-H005/006, ordering-convention, and API parity stay
open until these source blockers are repaired and re-audited.

---

# Cross-repository input: `johnazariah/encodings-research`

**Report received:** 2026-07-22
**Status:** Scripts, notebook, canonical `physicist_tensor.json`, H₂/tree/BK/
Appendix/CNOT/bibliography/Δλ evidence are post-fix green. Final
Qiskit-style-label versus OpenQASM-operand wording audit, reachable artifact
delivery, and merge remain pending.

## Safe anchors

1. An exhaustive CAR census over all \(n^{n-1}\) rooted labelled trees for
   \(n=3,\ldots,6\) reproduces the star-only condition for the audited
   tree-to-index-set construction: exactly \(n\) trees pass at each size, and
   they are the stars. A ternary \(n=8\) candidate has CAR deviation `2`, while
   the star has deviation `0`. The provisional repair now tests passing stars
   and failing chain, balanced-binary, and Fenwick inputs explicitly.
2. The audited maximum-Pauli-weight table through \(n=24\) reproduces exactly
   against `Encodings.dll`. The live book companion independently extends the
   tested package values to \(n=64\): JW `64`, BK `7`, ternary `6`.
3. CAR, Majorana, and ladder-sign conventions agree between the research paper's
   Appendix A and the library. The repaired matrix tooling reverses displayed
   signatures so mode \(j\) has matrix/occupation place value \(2^j\); this now
   agrees with the book's state-resolved oracle.

## High blockers

1. `H2Demo.fsx`, `IntegralTables`, `DiagCheck`, and `Quick2x2` use the corrupted
   `0.6975782` value for `[01|01]`/`[01|10]`. The research repository's own
   `IntegralCheck` and physical benchmark require approximately `0.18121`,
   which reproduces total FCI energy near `-1.1373` Ha. This independently
   supports CA-H001/003 and disqualifies the older research chain as provenance.
2. Review section 9's total H₂ energy `-1.0471` Ha is unsupported and wrong.
   Its cross-encoding difference near `4.44e-16` proves only mutual agreement
   on a shared corrupted Hamiltonian.
3. The software-paper claim that the tree-derived index-set construction works
   for Fenwick trees conflicts with the exhaustive star-only result; the tested
   Fenwick construction fails. This does not invalidate the separately derived
   standard BK mapping, but it forbids deriving its correctness from the generic
   tree-to-index-set claim. **Mapped item:** CA-M010.
4. `papers/results/` contains no data, scripts, or figures supporting the
   molecular CNOT reductions, tapering totals, or coefficient-norm claims.
   Those numbers are not reproducible citations for this book.
   **Mapped items:** CA-M003, CA-M007.

## Medium blockers

- Appendix B's intermediate operators and anticommutators are wrong even though
  its final result is correct; do not cite that derivation.
- At \(n=64\), ternary maximum weight `6` implies `10`, not `8`, CNOTs under
  the standard staircase. The claimed `16x` reduction is about `12.6x`.
- BK Fenwick parent/update formulas need `lsb(j+1)`; the audited parity formula
  and implementation are correct.
- Research scripts have stale assembly paths/module imports, undeclared Python
  dependencies, and stale notebook outputs. They are not executable provenance
  until the repair session reproduces them.

## Book decisions pending repaired artifacts

- Preserve the directly reproduced star census and package weight anchors, but
  do not cite the inconsistent software-paper derivation as support.
- Keep the book's internal/external Pauli-order wording open until one bitstring,
  matrix, and exported-label example agrees across both repositories.
- Remove the H₂O/LiH/N₂/FeMo CNOT tables and fault-tolerant totals unless the
  repair session supplies versioned machine-readable inputs and outputs.
- Never use cross-encoding agreement as physical validation without an
  independent matrix or chemistry reference.

## Provisional labelled tapering evidence

The repaired research report now labels every row by encoding, method,
generator count, and sector:

| Encoding | Method | Result | Sector | Coefficient 1-norm |
|---|---|---|---|---:|
| JW | DiagonalOnly | 4→4, no generators | — | `2.699278` Ha |
| Ternary Tree | DiagonalOnly | 4→4, no generators | — | `2.699278` Ha |
| BK | DiagonalOnly | 4→2, two generators | `++` | `2.013074` Ha |
| Parity | DiagonalOnly | 4→2, two generators | `++` | `2.013074` Ha |
| Balanced Binary | DiagonalOnly | 4→2, two generators | `++` | `2.013074` Ha |

FullClifford rows remain upstream evidence pending the final library branch and
are not locally recomputed in `encodings-research`. No post-taper count or norm
is a general H₂ fact; all remain provisional until the final library audit.

---

# Primary-source provenance

**Audit received:** 2026-07-22
**Rule:** Citation adjacency is not support. Each entry records what the source
actually establishes and whether the book must correct, narrow, remove, or add
a generated artifact.

| ID | Claim | Primary source / artifact | What it actually supports | Decision |
|---|---|---|---|---|
| PR-001 | Canonical H₂/STO-3G integrals | Sun et al. (2020), DOI [10.1063/5.0006074](https://doi.org/10.1063/5.0006074); local PySCF 2.13 generator/verifier | PySCF method only; exact values require geometry, version, MO/tensor convention, symmetry checks, and committed output | **Correct; add artifact metadata** |
| PR-002 | H₂ HF/MP2/FCI energies | Sun et al. (2020); Seeley et al. (2012), DOI [10.1063/1.4768229](https://doi.org/10.1063/1.4768229); local direct/JW verifier | Local artifact establishes this geometry's values; cross-encoding agreement alone does not | **Correct and cite artifact/software** |
| PR-003 | “FCI is exact” | Seeley, Richard & Love (2012) plus the definition of finite-basis FCI | Exact only within the chosen finite orbital basis, Hamiltonian model, particle/spin sector | **Narrow** |
| PR-004 | Quantum representation/advantage | Kempe, Kitaev & Regev (2006), DOI [10.1137/S0097539705447256](https://doi.org/10.1137/S0097539705447256); Reiher et al. (2017), DOI [10.1073/pnas.1619152114](https://doi.org/10.1073/pnas.1619152114) | General local-Hamiltonian hardness and QPE overlap dependence; no generic chemistry speedup | **Narrow and cite** |
| PR-005 | Product-formula error | Childs et al. (2021), DOI [10.1103/PhysRevX.11.011020](https://doi.org/10.1103/PhysRevX.11.011020) | Ordering-dependent pair/nested-commutator bounds; not \(\Delta t\le1/\|H\|_1\) as a theorem or former numeric guarantees | **Correct/narrow; generated H₂ bound required** |
| PR-006 | H₂O 99° result | Sun et al. (2020); Hoy & Bunker (1979), DOI [10.1016/0022-2852(79)90019-5](https://doi.org/10.1016/0022-2852(79)90019-5); local CSV/plot/lab | PySCF method and experimental angle; exact grid minimum is a repository calculation at fixed \(r_{\rm OH}\) | **Narrow; add artifact metadata** |
| PR-007 | Larger-basis H₂O minima | No primary source located for the exact HF/MP2/CASCI table | No support for orbital selection, active space, state, scan, or exact minima | **Remove unless artifact is committed** |
| PR-008 | Water spectroscopy/climate | Shimanouchi (1972), DOI [10.6028/NBS.NSRDS.39](https://doi.org/10.6028/NBS.NSRDS.39); Shostak et al. (1991), DOI [10.1063/1.460471](https://doi.org/10.1063/1.460471); NASA water-vapour feedback | 1595 cm\(^{-1}\) bend, permanent dipole, dipole-change IR rule, qualified feedback; not sole-cause habitability | **Correct/narrow and cite** |
| PR-009 | Ternary-tree bounds | Jiang et al. (2020), DOI [10.22331/q-2020-06-04-276](https://doi.org/10.22331/q-2020-06-04-276); Vlasov (2022), DOI [10.12743/quanta.v11i1.199](https://doi.org/10.12743/quanta.v11i1.199); Seeley et al. (2012) | Jiang: Majorana weight \(\lceil\log_3(2n+1)\rceil\), average lower bound \(\log_3(2n)\), still \(\Theta(\log n)\); not FockMap correspondence | **Correct metadata/result and cite** |
| PR-010 | Star-only/tree implementation claims | Reproduced `encodings-research` exhaustive CAR census; literature does not certify FockMap correspondence | Star-only condition for the audited tree-derived index-set constructor; inconsistent paper derivation is not support | **Keep cross-repo blocked** |
| PR-011 | Tapering/sector rules | Bravyi et al. (2017), OA [arXiv:1701.08213](https://arxiv.org/abs/1701.08213) | Independent commuting symmetries and a selected eigenvalue sector; not a molecule's generator-to-\((N,M_S,\ldots)\) map | **Correct/narrow; generated sector tests required** |
| PR-012 | Shot/grouping formula | Wecker et al. (2015), DOI [10.1103/PhysRevA.92.042303](https://doi.org/10.1103/PhysRevA.92.042303); Yen et al. (2020), DOI [10.1021/acs.jctc.0c00008](https://doi.org/10.1021/acs.jctc.0c00008) | Independent-term worst-case one-norm bound; grouped shots need covariance, general commuting may need entangling measurement | **Narrow; API/group artifact required** |
| PR-013 | QPE formula/resources | Kitaev (1995), OA [arXiv:quant-ph/9511026](https://arxiv.org/abs/quant-ph/9511026); Reiher et al. (2017) | Phase-energy relation, range/aliasing, overlap and confidence conditions; no book-specific gate totals | **Correct/narrow; generated resources required** |
| PR-014 | Hardware gate table | IBM dated backend-calibration documentation; Quantinuum machine-specific validation documentation | Only named backend, revision, native gate, timestamp and benchmark definition; no platform constants | **Remove numerical table** |
| PR-015 | Molecule-level CNOT tables | None; `encodings-research/papers/results` is empty | No H₂O/LiH/N₂/FeMo benchmark provenance | **Removed** |
| PR-016 | FeMo-co active space/resources | Reiher et al. (2017); Lee et al. (2021), DOI [10.1103/PRXQuantum.2.030305](https://doi.org/10.1103/PRXQuantum.2.030305) | CAS(54e,54o) supports 108 spin orbitals; resource totals are architecture-specific and not interchangeable | **Retain active-space fact; remove mixed totals** |
| PR-017 | Generic physical/logical-qubit ratios | No supporting source; former citation was Gidney & Ekerå's RSA paper, DOI [10.22331/q-2021-04-15-433](https://doi.org/10.22331/q-2021-04-15-433) | Does not support generic chemistry error-correction ratios or totals | **Removed** |
| PR-018 | DFT/CCSD(T)/DMRG scope | Bowler & Miyazaki (2010), DOI [10.1088/0953-8984/22/7/074207](https://doi.org/10.1088/0953-8984/22/7/074207); Bartlett & Musial (2007), DOI [10.1103/RevModPhys.79.291](https://doi.org/10.1103/RevModPhys.79.291); White (1992), DOI [10.1103/PhysRevLett.69.2863](https://doi.org/10.1103/PhysRevLett.69.2863); Schollwock (2011), DOI [10.1016/j.aop.2010.09.012](https://doi.org/10.1016/j.aop.2010.09.012) | Conventional KS diagonalization is often cubic but linear-scaling variants exist; canonical single-reference CCSD(T) has an \(O(N^7)\) triples step; DMRG excels for low-entanglement 1D/quasi-1D structure | **Narrow and cite; no fixed atom limits or blanket quantum niche** |

## Citation corrections

- The relevant Jiang publication is *Quantum* **4**, 276, not *PRX Quantum*
  1, 010306; the latter DOI belongs to an atom-interferometry paper.
- The Vlasov DOI is `10.12743/quanta.v11i1.199`, not `.166`.
- The relevant general commutator-scaling source is Childs et al., *PRX* 11,
  011020 (2021). The cited `PRL 120, 250503 (2018)` is not that result;
  Childs & Su's lattice paper is *PRL* 123, 050503 (2019).
- The Tranter bibliography entry duplicates Sherrill and omits authors; correct
  DOI: [10.1002/qua.24969](https://doi.org/10.1002/qua.24969).
- The Zenodo concept DOI `10.5281/zenodo.18917465` is valid. Version `0.9.0`
  is `10.5281/zenodo.20148795`.

## Publication metadata blocker

### OWNER/RELEASE BLOCKER — mixed rights versus Zenodo archive metadata

The GitHub policy is explicit: companion code is MIT; manuscript text is
all-rights-reserved. `CITATION.cff` now points to that policy rather than
claiming one archive-wide SPDX license. The external Zenodo record still
conflicts:

- concept DOI: `10.5281/zenodo.18917465`;
- version `0.9.0` DOI: `10.5281/zenodo.20148795`;
- current Zenodo resource type: `software`;
- current Zenodo license: `MIT`;
- archive files combine manuscript and code.

The owner must choose and apply a release model. Before the next deposit:

1. decide whether to split book and code into separately licensed Zenodo
   records or use a mixed-rights archive that Zenodo can represent accurately;
2. update the version record's `resource_type`, `license`/rights field, and
   description so it does not grant MIT rights over the manuscript;
3. keep concept/version DOI relations and `version=0.9.0` accurate;
4. make GitHub README, `CITATION.cff`, Zenodo metadata, PDF/EPUB rights notice,
   and packaged code license files state the same scopes; and
5. inspect the deposited files and public landing page after publication.

This repository cannot repair the existing external record by adding another
local metadata file. Zenodo remains an owner/release blocker until the public
record is changed and checked.

Local artifacts now state the existing policy consistently:
`LICENSE-CODE`, `MANUSCRIPT-RIGHTS`, README, CFF license URL, rendered
PDF/EPUB rights notice, and release/lab packages. A package dry run contains
both rights files. This local consistency does **not** close the external
Zenodo blocker.

The FockMap placeholder DOI has been removed from `jose/paper.bib`; it must not
be replaced with the book DOI unless both citations refer to the same archived
work.

`manuscript/references.md` is the sole rendered bibliography source: it appears
once in `Book.txt`, `Sample.txt`, and `myst.yml`. `jose/paper.bib` is outside all
book manifests. The bibliography contains 18 unique DOI links and 3 additional
authoritative URLs; all 21 resolve (publisher endpoints returning HTTP 403
after DOI resolution were treated as access-controlled, not broken). No stale
Jiang, Vlasov, or placeholder DOI remains.

---

# Disposable-copy execution baseline

**Run:** 2026-07-22 against the repaired working tree; original worktree status
unchanged.

## Pass

- `make verify-data`: PySCF/tensor checksum, direct/JW reconstruction, all
  particle sectors, state ordering, coefficient norms, commutator sum, and
  byte-identical disposable regeneration.
- Labs 01, 04, 05, 06, 07, 08, and 09.
- Companions ch03, ch07, and ch11.
- `make data`: canonical CSV/JSON/PNG/metadata outputs reproduce byte-for-byte.
- MyST site: 27 pages, both appendices, References, no missing images.
- Current full PDF: 175 pages and 27 images; sample PDF: 55 pages; EPUB:
  27 content sections with 27 PNG assets.
- Local relative-link check and one Pages deployment workflow.
- Workflow artifact names match
  `molecules-to-circuits.pdf`, `molecules-to-circuits-sample.pdf`, and
  `molecules-to-circuits.epub`.

## Expected evidence gates

- Lab 02 and ch06/ch18 fail closed on the upstream H₂ coefficient mismatch.
- Labs 03/10 build dense \(16\times16\) matrices and compare all 16 spectral
  moments at relative tolerance `1e-7`; the current package fails moment 1 by
  `3.384e+00`, exposing the shared-Hamiltonian error.
- `make lab-check` executes labs and stops at the first upstream blocker.
- `make pipeline-check` proves canonical data immutability, rejects source-tree
  derived output, and propagates the upstream blocker.
- ch12 no longer presents a default-sector taper result; FullClifford closure
  remains with CA-H005/006.

No expected blocker is counted as a new finding. It remains attached to the
existing CA/CP item.

---

# Verification addendum — 2026-07-23

**Reviewed scope:** Current uncommitted remediation; all action-plan IDs;
canonical H₂ artifacts; `encodings-research` fixture commit
`1e000bbc9664b8e5cfef48608d07364279c0a54f`; and `encodings` PR #6 exact commit
`440540b54f093d7a9f259da7bf18b7df8da74270`.

## Result

**Correctness is materially closed.** All local high-severity findings except
the explicitly partial CA-H008 API round-trip gate are resolved. The FockMap
Hamiltonian/tapering/order source contract is accepted at PR #6 commit level.
CA-M010 and CA-M011 retain narrower integration/API-output gates. The book may
proceed to whole-book tone and clarity review, but it is not release-ready until
the operational package and external metadata gates below close.

## Superseded source diagnostics

Maintainer delegation to the reconciliation process superseded the earlier raw
API choice. The authoritative nonbreaking contract is now:

- legacy `computeHamiltonianWith` consumes full weighted operator coefficients
  and remains compatible;
- `rawPhysicistToWeightedFactory` maps a raw single-bar tensor query
  `(i,j,l,k)` to weighted key `(i,j,k,l)` with coefficient
  \(\tfrac12\langle ij\mid lk\rangle\);
- `computeHamiltonianFromPhysicistWith` and
  `computeHamiltonianFromPhysicist` expose that raw path directly; and
- cancellation-aware assembly returns exactly 15 stored/nonzero terms for the
  canonical H₂ input while preserving standalone tiny coefficients.

PR #5, its provisional commits, the `23 stored` diagnosis, pending zero
pruning, and the book-internal half-and-swap adapter are historical. PR #6 at
`440540b` is the accepted source. Merge and package publication are operational
gates, not open source-correctness findings.

## Canonical fixture acceptance

`code/physicist_spin_integrals.json` is byte-identical to the audited
`encodings-research` artifact:

| Identity | Value |
|---|---|
| Source commit | `1e000bbc9664b8e5cfef48608d07364279c0a54f` |
| Source path | `papers/results/h2_sto3g/physicist_spin_integrals.json` |
| Git blob SHA-1 | `e0477e70c0dfd35b865000bb23b7b31882b062d3` |
| File SHA-256 | `6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812` |
| Exact 4+32 map SHA-256 | `d5d8e2f7c83b1d1322f10217198d125b7c7cae1f99fa09a6e1e232ee38dd098c` |

The local PySCF regeneration is retained independently as
`local_pyscf_spin_integrals_sha256 = a9a85179...`; its maximum difference from
the canonical bytes is `2.22e-16`. It is no longer described as the
cross-repository canonical object.

Whole-tree search outside `.review` finds no obsolete `0.6975782469`,
`-1.1422`, `-1.8573`, `-2.2001`, or `-1.0471` value.

## Execution evidence

The exact PR #6 source was checked out detached and built with .NET SDK
`10.0.201`.

| Check | Result |
|---|---|
| `dotnet test test/Test.Encodings/Test.Encodings.fsproj -c Release` | **887/887 passed**, 0 warnings, 0 errors |
| `bash scripts/check-doc-samples.sh` in PR #6 | **19/19 documents executed and asserted** |
| `make verify-data` in a disposable book copy using the exact PR #6 DLL | **Passed** |
| `make pipeline-check` in that disposable copy | **Passed** |
| `make lab-check` in that disposable copy | **All 10 numbered labs passed** |
| Every `code/ch*.fsx` companion in that disposable copy | **All six passed** |
| Fixture file SHA-256 / git blob / semantic-map hash | **Exact values above** |
| Obsolete-value search outside review history | **No matches** |

The disposable verification changed only each copy's `#r "nuget: FockMap"` to
the exact built DLL path; repository companions retain their publication-facing
reference.

The public-package gate was also reproduced directly:

```text
code/ch03-spin-orbitals.fsx(44,5): error FS0039:
The value or constructor 'rawPhysicistToWeightedFactory' is not defined.
```

As of this pass, unversioned NuGet resolves to released FockMap `0.8.0`, while
PR #6 is open and no package containing `440540b` is published.

## Closed items

| ID | Verification result |
|---|---|
| CA-H001 | **Closed.** Exact provenance-pinned 4+32 fixture; symmetry, PySCF, HF, direct matrix, and generated-data checks pass. |
| CA-H002 | **Closed (correctness).** Named raw adapter, 15-term coefficient map, dense parity, and source API contract pass at PR #6. |
| CA-H003 | **Closed (correctness).** Full particle sectors, HF/FCI/correlation values, and all-six package spectra pass. |
| CA-H004 | **Closed (correctness).** Skeleton uses the named weighted adapter; no identity-proxy energy or output collision remains. |
| CA-H005 | **Closed (correctness).** Centralizer/subgroup distinction, CNOT phases, Heisenberg arithmetic, and all-sector spectrum preservation pass. |
| CA-H006 | **Closed (correctness).** Encode-before-taper order and explicit physical-sector rules are correct; default-sector molecular execution is absent. |
| CA-H007 | **Closed.** The H₂O result is a reproducible fixed-\(r_{\mathrm{OH}}\) PySCF FCI angular scan with no FockMap energy claim. |
| CA-H009 | **Closed.** Compact representation is separated from state preparation, estimation, and possible advantage. |
| ordering-convention | **Closed (correctness).** q0-leftmost signatures, occupation-integer rows, state labels, and Qiskit-style reversal agree. |
| CA-M001, CA-M002 | **Closed.** Correlation decomposition and generated first-order commutator bound are correct. |
| CA-M003 | **Closed by removal.** Unsupported molecule/resource tables remain absent. |
| CA-M004, CA-M005, CA-M006 | **Closed.** Logarithmic scope, encoding-output claims, and spectroscopy/climate causality are corrected. |
| CA-M007 | **Closed locally.** Unsupported hardware/resources and placeholder DOI are removed; external Zenodo is separate. |
| CA-M008, CA-M009 | **Closed.** Both appendices publish and Pauli basis/group language is correct. |
| CA-L001–CA-L004 | **Closed.** All terminology, arithmetic, model-scope, and script-inventory criteria pass. |

## Remaining items

1. **CA-H008 — Partial.** The phase/energy relation, \(t_0\), range,
   aliasing, overlap, confidence, controlled-simulation error, and 12-bit
   arithmetic are corrected. Imported QASM/Q#/JSON unitary parity and the exact
   `qpeResources`/`estimateShots` semantics were not part of PR #6 acceptance.
2. **CA-M010 — Partial.** The book correctly separates the reproduced
   star-only generic construction from supported max-three-child path-based
   trees. Broader research integration/merge remains.
3. **CA-M011 — Partial.** The five-basis QWC partition is mathematically valid;
   pinned `groupCommutingTerms` output/semantics remain unverified.
4. **Benchmark backlog — Open by design.** LiH/H₂O/N₂/FeMo molecule tables and
   fault-tolerant totals lack accepted artifacts and must remain removed.

## Operational and owner release gates

1. Merge PR #6, publish a FockMap package containing exact source commit
   `440540b`, replace every unversioned NuGet reference with that package
   version, and rerun the complete book and output baseline.
2. Correct the external Zenodo mixed-rights metadata so it does not grant MIT
   rights over the manuscript.
3. Preserve the absence of unsupported benchmark/resource tables unless a
   versioned generated artifact is accepted.

## New risks

- **Double adaptation:** Passing a weighted factory through the raw adapter, or
  a raw factory directly to `computeHamiltonianWith`, silently changes the
  Hamiltonian. Current code uses the two boundaries correctly; future edits
  must preserve that distinction.
- **Unversioned dependency drift:** A future NuGet release newer than the
  accepted source could change behavior. Pin the exact released version
  corresponding to PR #6.

## Current assessment

There are no remaining findings in the closed-item verification scope.
Correctness is sufficiently closed to begin the planned whole-book tone,
pacing, and clarity pass. This does not waive the partial items or make the
current unversioned-package tree a release candidate.

## Contract-change addendum — 2026-07-23 (later owner decision)

After the verification above, the owner explicitly selected a **breaking
raw-integral API**. This supersedes the addendum's description of PR #6 as the
final authoritative nonbreaking package contract.

Current interpretation:

- PR #6 commit `440540b54f093d7a9f259da7bf18b7df8da74270` remains an
  independently verified **audited base**. Its 887-test result, strict
  documentation harness, canonical fixture lock, cancellation-aware 15-term
  assembly, tapering fixes, ordering, and safety regressions remain valid
  evidence and required behavior.
- PR #6 is back in draft and is **not** the final package API. The new stack is
  expected to make existing Hamiltonian APIs consume raw integrals directly,
  preserve explicitly named weighted migration APIs, version and document the
  breaking change, and pass an independent audit.
- The canonical direct fixture, fermionic matrix, JW oracle, sector spectra,
  and all numerical anchors remain valid. No chemistry or direct-book
  correction is reopened.
- CA-H002, CA-H003, and CA-H004 package integration is reopened. CA-H005,
  CA-H006, ordering-convention, and CA-M005 remain closed on the audited base
  but require final-stack regression confirmation.
- Current book API code is frozen. Do not replace its PR #6 named raw/adapter
  calls with direct raw ingestion until an independently accepted final package
  SHA and migration contract are available.
- Package merge/release is no longer the only operational step; **final API
  contract acceptance must occur first**.

The whole-book tone and clarity pass may proceed because it does not depend on
the package call spelling. Final runtime validation and release-candidate status
remain blocked on the accepted package SHA.

Package-independent publication validation after the style pass is green:

- full PDF: 176 pages, 27 image placements;
- sample PDF: 55 pages, 9 image placements;
- EPUB: 29 content files and 27 PNG assets;
- strict MyST HTML: 27 pages, including both appendices and References; and
- Markdown fence and diff-whitespace checks: clean.

The first local output attempt exposed a missing `mmdc` executable while still
returning success. The build was rejected, Mermaid CLI was supplied in
session-local tooling, and all outputs were rebuilt with no omitted-diagram
markers. This does not alter the pending package-runtime gate.

## Final raw-API integration addendum — 2026-07-23

The independently audited breaking package contract is accepted at local
`encodings` commit
`8e562175e809dc282b5bf2ca21f1cb311e14d5fa`, whose parent is the PR #6 audited
base `440540b54f093d7a9f259da7bf18b7df8da74270`. The source worktree is clean.
The commit is local only at this checkpoint: no push, PR, tag, or NuGet package
yet exists.

### Accepted contract

- The primary builders `computeHamiltonian`, `computeHamiltonianWith`, their
  parallel/cached variants, `computeHamiltonianSkeletonFor`, and
  `applyCoefficients` consume raw single-bar physicist integrals:
  `(p,q,r,s) -> <pq|rs>`, with no caller-supplied half or index swap.
- The library assembles
  \(\tfrac12\langle pq\mid rs\rangle
  a_p^\dagger a_q^\dagger a_s a_r\).
- Explicit `...FromWeighted...` functions preserve the legacy contract:
  weighted key `(p,q,s,r)` and value
  `0.5 * <pq|rs>` are applied verbatim.
- `weightedToRawFactory` bridges legacy weighted data to primary raw builders;
  `antisymmetrizedToRawFactory` handles the double-bar quarter convention.
- FCIDUMP adapters now return raw physicist tensors; assembled FCIDUMP physics
  remains unchanged.
- Version `0.9.0`, a breaking-change changelog, and a migration guide document
  the boundary. The obsolete named-raw aliases remain only for transition.

### Book migration

All book H₂ factories already expose the canonical raw map. The migration
therefore removes the provisional PR #6 adapter layer:

- canonical JW paths call `computeHamiltonian rawFactory`;
- multi-encoding paths call `computeHamiltonianWith encoder rawFactory`;
- the dissociation skeleton calls `applyCoefficients skeleton rawFactory`
  directly; and
- no `computeHamiltonianFromPhysicist*`,
  `rawPhysicistToWeightedFactory`, hand-written half-and-swap adapter, or
  weighted factory remains in code, labs, README, or manuscript.

All executable and reader-facing package references are pinned to
`FockMap 0.9.0`. Until publication, this intentionally fails closed rather than
resolving released `0.8.0`.

### Verification evidence

| Check | Result |
|---|---|
| Exact accepted source | `8e562175e809dc282b5bf2ca21f1cb311e14d5fa` |
| Package tests | **897/897 passed** |
| Package executable-doc harness | **19/19 passed**, exact assertions retained |
| `make verify-data` against exact DLL | **Passed** |
| `make pipeline-check` against exact DLL | **Passed** |
| `make lab-check` against exact DLL | **All 10 numbered labs passed** |
| Every `code/ch*.fsx` companion against exact DLL | **All six passed** |
| Obsolete API warnings/search | **None** |
| Canonical fixture file SHA-256 | `6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812` unchanged |
| Canonical semantic-map SHA-256 | `d5d8e2f7c83b1d1322f10217198d125b7c7cae1f99fa09a6e1e232ee38dd098c` unchanged |
| Full PDF | **176 pages, 27 image placements** |
| Sample PDF | **55 pages, 9 image placements** |
| EPUB | **29 content files, 27 PNG assets** |
| Strict MyST HTML | **27 pages, 18 linked DOIs** |
| Markdown fences / diff whitespace | **Clean** |

The public-package gate currently fails as intended:

```text
error NU1102: Unable to find package FockMap with version (>= 0.9.0)
```

### Item status after final integration

- **CA-H002, CA-H003, CA-H004:** package integration now closed at exact source
  commit. Publication rerun remains operational.
- **CA-H005, CA-H006, ordering-convention, CA-M005:** required final-stack
  regressions pass.
- **CA-H008, CA-M010, CA-M011:** remain partial exactly as recorded above; the
  raw-Hamiltonian migration does not claim to close their separate API/research
  gates.
- **Zenodo mixed rights and unsupported benchmark backlog:** unchanged.

### Remaining release operations

1. Push `8e562175`, open and merge the reviewed package change.
2. Publish FockMap `0.9.0` from that accepted lineage.
3. Rerun the same book baseline against the public NuGet package and verify its
   package/source provenance.
4. Correct the external Zenodo rights metadata.

The book and local package integration are correctness-ready. The repository is
not a release candidate until the public-package and Zenodo operations close.

## Provisional-status correction — 2026-07-23

The coordination authority subsequently clarified that the independent package
auditor has **not** yet returned its exact-SHA verdict in the authoritative
thread. Therefore the preceding “Final raw-API integration addendum” records
valid **local implementation evidence**, but its package-acceptance and
item-closure wording is provisional.

Authoritative current status:

- `8e562175e809dc282b5bf2ca21f1cb311e14d5fa` is **implementation verified
  locally; external package audit pending**.
- The migrated book tree may remain as an uncommitted provisional integration,
  but it is frozen. Do not push, open a PR, commit, or mark package audit closure.
- The 897 package tests, strict documentation harness, full book runtime
  baseline, canonical fixture identity, and publication builds remain useful
  local evidence; they do not substitute for the independent exact-SHA verdict.
- CA-H002, CA-H003, and CA-H004 direct/book corrections remain closed, while
  candidate package integration is pending external audit.
- CA-H005, CA-H006, ordering-convention, and CA-M005 remain closed on the
  previously audited base; candidate-stack regression evidence is local pending
  the same verdict.
- An explicit **BREAKING-RELEASE READY** report closes the candidate gate. If
  the auditor reports a blocker, both package and book integration must follow
  the accepted successor SHA and rerun the same baseline.

Public-package publication, provenance rerun, and Zenodo mixed-rights correction
remain later operational gates. No release-candidate claim is permitted at this
stage.

## Independent package audit result — 2026-07-23

The independent auditor has now completed the `8e562175` runtime/package pass.
The raw-breaking implementation semantics pass in source inspection and probes,
and the existing suite is green. The exact SHA is still not
BREAKING-RELEASE READY because of three blockers:

1. remove the unreleased publication date from `CITATION.cff`; and
2. reconcile the test register to **25 test methods / 42 cases / suite 897**;
   and
3. add convention-sensitive Optimization regressions: raw two-body exact map,
   `weightedToRawFactory` migration parity, and a direct-weighted misuse
   negative control across the exposed routing.

This narrows, but does not remove, the provisional gate:

- the canonical fixture/oracle and current raw-primary book migration remain
  valid and frozen;
- no book code change is indicated by the audit;
- `8e562175` must not be pushed, opened, committed as package closure, or
  described as release-ready; and
- a tests-and-metadata successor SHA must receive the explicit
  **BREAKING-RELEASE READY** verdict.

After that verdict, rerun the exact package and book baselines against the
successor. If its runtime outputs are unchanged, package integration may close
at that SHA; NuGet publication/provenance and Zenodo rights remain subsequent
operational gates.

## Independent audit addendum — Optimization test coverage

The auditor identified a third release blocker. Optimization's raw semantics
are correct in source inspection and probes, but the repository tests are
currently one-body or convention-insensitive and therefore do not lock the
breaking two-body contract.

The final successor must add:

1. a raw two-body exact-map test through Optimization;
2. `weightedToRawFactory` migration-parity coverage through Optimization; and
3. a direct-weighted negative control proving that preweighted data fed to the
   raw Optimization path does not silently pass.

The successor has two metadata repairs and one test-coverage repair. This
remains package-test work only; the frozen book migration and canonical
fixture/oracle do not change.

## BREAKING-RELEASE READY successor verification — 2026-07-23

The package session delivered explicit **BREAKING-RELEASE READY** status for
successor `3e62bd71b113d78a6f5933858336e06af1e3d9c7`, whose parent is
`8e562175e809dc282b5bf2ca21f1cb311e14d5fa`.

The successor diff is exactly:

- `.project/test-register.md`;
- `CITATION.cff`; and
- `test/Test.Encodings/Optimization.fs`.

No `src/**/*.fs` file changed, so the raw-breaking runtime accepted at
`8e562175` is unchanged. The successor:

1. removes the unreleased CFF date;
2. reconciles the register to 25 Hamiltonian test methods / 42 cases and
   900 total tests (897 base + 3 new Optimization cases); and
3. locks Optimization's exposed raw routing with the required raw two-body
   exact-map, `weightedToRawFactory` migration-parity, and direct-weighted
   negative-control tests.

### Independent successor results

| Check | Result |
|---|---|
| Successor scope | **Tests and metadata only; no library source change** |
| Package tests | **900/900 passed**, 0 skipped |
| Documentation harness selftest | **Passed** |
| Executable documentation | **19/19 passed** with exact assertions |
| `dotnet fsdocs build --clean --strict` | **Passed** |
| Package worktree | **Clean** |
| CFF unreleased date | **Absent** |
| Test register | **900 total; 25 methods / 42 cases; Optimization row present** |

### Frozen-book verification against successor DLL

| Check | Result |
|---|---|
| `make verify-data` | **Passed** |
| `make pipeline-check` | **Passed** |
| `make lab-check` | **All 10 numbered labs passed** |
| Every `code/ch*.fsx` companion | **All six passed** |
| Obsolete FockMap API warnings/search | **None** |
| Canonical fixture file SHA-256 | `6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812` unchanged and byte-identical to package |
| Canonical git blob | `e0477e70c0dfd35b865000bb23b7b31882b062d3` unchanged |
| Full PDF | **176 pages, 27 image placements** |
| Sample PDF | **55 pages, 9 image placements** |
| EPUB | **29 content files, 27 PNG assets** |
| Strict MyST HTML | **27 pages, 18 DOI links** |
| Markdown fences / diff whitespace | **Clean** |

The first parallel book-runtime copy overlapped Mermaid image regeneration and
made `rsync` report disappearing generated files. This was a validation-harness
race, not a source failure. Runtime verification was rerun sequentially with
generated Mermaid images excluded from the disposable copy and passed fully.

### Final status

- **CA-H002, CA-H003, CA-H004:** closed at accepted successor
  `3e62bd71`.
- **CA-H005, CA-H006, ordering-convention, CA-M005:** final successor
  regressions pass.
- **CA-H008, CA-M010, CA-M011:** remain partial under their separate,
  previously recorded gates.
- **Unsupported multi-molecule/resource claims:** remain removed.
- **Package publication:** successor is local and unpushed; FockMap `0.9.0` is
  not yet available from NuGet. The pinned public-package run therefore fails
  closed with `NU1102`, as intended.
- **Zenodo mixed rights:** remains an owner/release blocker.

All three successor blockers are resolved. Package/book integration is
correctness-accepted at `3e62bd71`. Remaining work is external release
operation and the explicitly partial items, not another local Hamiltonian
repair.

## Designated-auditor status correction — 2026-07-23

The coordination authority clarified that the package session's
**BREAKING-RELEASE READY** label is not the designated independent package
auditor's one-delta verdict. Therefore the preceding successor addendum records
complete and valid **book-side verification**, but its final package-acceptance
wording is provisional.

Authoritative current wording:

- successor `3e62bd71b113d78a6f5933858336e06af1e3d9c7` is
  **book-side verified; package auditor pending**;
- the tests-and-metadata-only scope, 900-test result, documentation gates,
  frozen-book runtime, fixture identity, and publication outputs remain valid
  evidence;
- the uncommitted book integration remains frozen;
- do not push, open a PR, commit package closure, or promote release status
  solely from book-side verification; and
- if the designated auditor reports a blocker, follow its accepted successor
  SHA and rerun the same book baseline.

Direct book corrections remain closed. Final package integration status awaits
the designated auditor's explicit one-delta verdict; public NuGet provenance and
Zenodo rights remain later gates.

## Designated package audit verdict — runtime accepted, release workflow pending

The designated independent package auditor has now accepted all raw API code
and tests at `3e62bd71b113d78a6f5933858336e06af1e3d9c7`. This supersedes the
preceding “package auditor pending” status for runtime/package semantics.

One release-engineering blocker remains. Current tooling:

1. cannot explicitly release the staged/current `0.9.0` version;
2. cannot robustly insert `date-released` when that CFF field is absent; and
3. uses nonportable `grep -P`.

The final package-repository successor must add an explicit current/staged
`0.9.0` release mode, robust CFF date insertion, portable matching, and tests.

Current authoritative status is therefore:

- **runtime/package accepted; release workflow successor pending**;
- no further raw-Hamiltonian or book code change is indicated;
- the uncommitted book integration remains frozen until the workflow successor
  passes;
- do not push/open the package PR until that release-engineering gate closes;
  and
- after closure, human approval, NuGet publication/provenance, and Zenodo
  mixed-rights correction remain.

CA-H002, CA-H003, CA-H004, CA-H005, CA-H006, ordering-convention, and CA-M005
are closed for runtime/package correctness at `3e62bd71`. CA-H008, CA-M010, and
CA-M011 remain partial under their separate gates.

## Release-workflow successor — book-side verification

Release-engineering successor
`9e1afe267efefc4056f726ea98dadfed8af0214e` is a tooling-only child of
`3e62bd71b113d78a6f5933858336e06af1e3d9c7`. Its five-file diff changes release
scripts, the dispatch workflow, and release guidance/tests only. No runtime F#,
F# test, Hamiltonian fixture, or cookbook file changed.

Book-side checks:

| Check | Result |
|---|---|
| `scripts/test-release.sh` | **39 passed, 0 failed**; worktree unchanged |
| `scripts/release.sh --dry-run current` | **Would release v0.9.0**; no bump or mutation |
| Current/staged mode | **0.9.0 > latest v0.8.0**, CFF/CHANGELOG alignment validated |
| Absent/present CFF date | **Insert/replace, duplicate/malformed rejection, YAML parse pass** |
| Changelog finalization | **Passes, idempotent, missing heading rejected** |
| Executable `grep -P` scan | **None** in shell scripts or workflow |
| ShellCheck | **No error-severity findings** |
| Workflow YAML/options | **Parses; `auto,current,patch,minor,major`** |
| Package regression | **900/900 tests; documentation harness passes; clean tree** |
| Frozen-book runtime | **verify-data, pipeline-check, all 10 labs, all six companions pass** |

The Linux-only dispatch workflow still uses `sed -i` in the branch that changes
the fsproj version. The staged/current `0.9.0` path skips that branch; local
`release.sh` and shared helpers use temp-file-plus-move and are macOS/bash-3.2
portable. This does not block the staged release book-side, but it narrows the
handoff's broad portability claim and is explicitly left for the designated
release-workflow auditor to assess.

Authoritative status:

- **runtime/package accepted at `3e62bd71`;**
- **release-workflow successor `9e1afe2` book-side verified; designated
  release-workflow auditor pending;**
- book integration remains frozen and uncommitted;
- no push/PR or release closure may occur from book-side verification alone;
  and
- after designated acceptance, human approval, NuGet publication/provenance,
  and Zenodo rights remain.

## Final book-branch verification and selective clarity pass

The designated audit subsequently declared exact release-workflow SHA
`9e1afe267efefc4056f726ea98dadfed8af0214e` BREAKING-RELEASE READY.
Package head then advanced tooling-only to portability follow-up
`660c4839223037bc69909bb8fcb7d3be0f226b20`; no F#, cookbook, Hamiltonian, or
book API changed. The follow-up is book-side verified and awaits its designated
one-delta audit. This does not reopen the accepted raw-primary book migration.

### Selective whole-book clarity pass

A final specialist pass targeted only repeated cross-chapter bridge templates,
not local taste:

- Chapter 14 now states how Trotterization resolves the just-stated
  non-commuting-exponential problem instead of announcing that it “enters.”
- Chapter 20 now contrasts the algorithmic route directly instead of repeating
  “This is where ... enters.”
- Chapter 18 replaces the duplicate “Let's take stock” recap opener.
- Chapter 23 removes a redundant “In other words” before its closing QEC
  statement.

No technical meaning, notation, equation, citation, table, code fence, numeric
anchor, raw/weighted boundary, sector qualification, or review artifact was
changed by those four prose edits.

### Exact final verification

Package head during the final book verification:
`660c4839223037bc69909bb8fcb7d3be0f226b20`.

| Check | Result |
|---|---|
| Release successor scope | **Tooling only; no runtime/test-F#/cookbook change from `9e1afe2`** |
| Release harness | **42/42 passed; worktree unchanged** |
| `release.sh --dry-run current` | **Would release v0.9.0; no mutation** |
| Portability scan | **No executable `grep -P` or `sed -i`** |
| Workflow / shell validation | **YAML choices, shell syntax, ShellCheck error severity pass** |
| Package suite | **900/900 passed** |
| Semantic documentation harness | **Selftest + 19/19 executable documents passed** |
| Strict fsdocs | **Passed** |
| `make verify-data` against exact DLL | **Passed** |
| `make pipeline-check` against exact DLL | **Passed** |
| `make lab-check` against exact DLL | **All 10 numbered labs passed** |
| Every `code/ch*.fsx` companion | **All six passed; no obsolete warnings** |
| Canonical fixture SHA-256 | `6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812` |
| Canonical git blob | `e0477e70c0dfd35b865000bb23b7b31882b062d3` |
| Canonical semantic-map SHA-256 | `d5d8e2f7c83b1d1322f10217198d125b7c7cae1f99fa09a6e1e232ee38dd098c` |
| Generated oracle SHA-256 | `ab6a221add84cb6e862a6e34b696e1a1dc28d0cd41c1c09873f7248d43f0b439` |
| Full PDF | **176 pages, 27 image placements** |
| Sample PDF | **55 pages, 9 image placements** |
| EPUB | **29 content files, 27 PNG assets** |
| Strict MyST HTML | **27 pages, 18 DOI links** |
| Mermaid output | **25 rendered diagrams; no omission markers** |
| FockMap references | **12 executable references pinned to 0.9.0; no obsolete aliases** |
| Obsolete H₂ values | **Absent outside labelled review history** |
| Markdown fences / diff whitespace | **Clean** |

### Current release assessment

The book branch is verified and ready for commit/push/PR. Runtime/package
correctness is accepted; the final package release pin follows the designated
one-delta verdict for tooling-only `660c483`. FockMap `0.9.0` is **not yet
published**, so the public-package run remains fail-closed. Package
push/PR/merge, NuGet publication/provenance, Zenodo mixed-rights correction, and
the separate CA-H008/CA-M010/CA-M011 gates remain outside this book-branch
closure.

## Public FockMap 0.9.0 release verification — 2026-07-24

The package rollout gate is closed.

### Public provenance

| Artifact | Verified value |
|---|---|
| GitHub release | `v0.9.0`, published 2026-07-24 |
| Tag target / main | `96320a56786393269fd681c67c66df88058a8b8f` |
| NuGet package | `FockMap 0.9.0` present in the public v3 flat-container index |
| Public NuGet nupkg SHA-256 | `2911c0b5df790a8f7ed4b1756ca7f0af684a75f4a41b404a5ccefb713de6adf2` |
| Nuspec repository commit | `96320a56786393269fd681c67c66df88058a8b8f` |
| Published `Encodings.dll` SHA-256 | `0ba8ae967ea65d4945a336c1217f8939e41feb63b6f04af617b66537717d25c3` |

The public package exposes the accepted raw-primary contract and preserves the
named weighted migration APIs. Its canonical fixture and 15-term H₂ behavior
remain the accepted lineage.

### Public-package book verification

The repository was executed normally through its pinned
`#r "nuget: FockMap, 0.9.0"` references, with no local DLL substitution:

- `make verify-data` — passed;
- `make pipeline-check` — passed;
- `make lab-check` — all 10 numbered labs passed;
- every `code/ch*.fsx` companion — all six passed;
- obsolete API warnings — none; and
- worktree/deterministic-data drift — none.

The selective clarity pass was already complete and introduced no technical
change. Package-independent publication outputs remain verified:

- full PDF: 176 pages, 27 image placements;
- sample PDF: 55 pages, 9 image placements;
- EPUB: 29 content files, 27 PNG assets;
- strict MyST HTML: 27 pages, 18 DOI links; and
- canonical fixture/oracle hashes and Mermaid output remain unchanged.

### Final package-gate status

- CA-H002, CA-H003, CA-H004, CA-H005, CA-H006, ordering-convention, and
  CA-M005 are closed against the public released package.
- FockMap `0.9.0` is published and provenance-verified.
- CA-H008, CA-M010, and CA-M011 remain partial under their separate gates.
- Zenodo mixed-rights metadata remains an explicit owner/release blocker; this
  addendum does not claim it is fixed.

The book PR may now be marked ready and evaluated by its own CI. No remaining
package publication blocker applies to the book branch.

---

# Provenance correction — 2026-07-24

**Trigger:** The canonical `encodings-research` H₂/STO-3G physicist spin-integral
artifact was merged to `main`. Earlier verification records above (see the
2026-07-23 verification addendum) checked the pre-merge PR head
`1e000bbc9664b8e5cfef48608d07364279c0a54f`. Those dated records are preserved
verbatim as a faithful account of what was verified at that time; this
append-only entry updates the current canonical source-commit pointer without
altering any fixture bytes or numerical outputs.

**Correction:** The canonical source commit is now the merged `main` commit
`66ebdfe255c0cc6ba25a6d1b76b58401aee3ab06`. Because the fixture is
content-addressed, the git blob and file hashes are immutable and unchanged; the
merge only advances the commit that contains the identical blob.

## Current canonical fixture identity

`code/physicist_spin_integrals.json` is byte-identical to the merged
`encodings-research` artifact:

| Identity | Value |
|---|---|
| Source commit (current) | `66ebdfe255c0cc6ba25a6d1b76b58401aee3ab06` |
| Source commit (pre-merge PR head, historical) | `1e000bbc9664b8e5cfef48608d07364279c0a54f` |
| Source repo | `johnazariah/encodings-research` |
| Source path | `papers/results/h2_sto3g/physicist_spin_integrals.json` |
| Git blob SHA-1 | `e0477e70c0dfd35b865000bb23b7b31882b062d3` |
| File SHA-256 | `6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812` |
| Exact 4+32 map SHA-256 | `d5d8e2f7c83b1d1322f10217198d125b7c7cae1f99fa09a6e1e232ee38dd098c` |

The repo, path, git blob, file SHA-256, semantic-map SHA-256, and all fixture
bytes and numerical outputs are unchanged. Live metadata updated to the merged
commit: `code/physicist_spin_integrals.provenance.json`,
`code/h2_dissociation_integrals.json`, `code/h2_0.74_fixture.json`,
`code/h2_0.74_oracle.json`, and the `.review/ACTION-PLAN.md` canonical
upstream-fixture note.

---

# Comprehensive review — 2026-09-07

**Book baseline:** `c5cf9dfc4543d35c9936fa1b39307e782016fe11`.
**Mode:** Review and coordinated correction plan only, explicitly selected by
the author. No manuscript, companion, blog, publishing configuration, approval,
or publication-date changes are authorised by this review.
**Authority:** The corrected book source at this commit, not the old downloadable
book, and not the similarly numbered FockMap package release.

## Full-book correctness findings

**Primary scope:** Foreword, every one of the 23 chapters, both appendices,
references, and their executable companions. The blog and publication sections
below are secondary. This is a new full-book assessment, not a review limited
to changed lines or a repetition of the July audit.

The current manuscript has **five high-priority source findings**, followed
by the mathematical, algorithmic, consistency and completeness findings below.
They are grouped by the correction needed, not counted as one defect per
sentence. They do not invalidate the independently reproduced canonical
chemistry artifacts.

### BM-H01 — Restricting both two-electron index pairs requires antisymmetrised coefficients

**Severity:** High. **Classification:** Newly identified current algebraic error.
**Evidence:** `manuscript/02-notation.md:135-145`, especially line 143, says
restricting to `p < q` and `r < s` removes the prefactor without changing the
raw-integral convention. Combining the permutations also changes the
coefficient to the antisymmetrised integral:

$$
\begin{aligned}
\langle pq\Vert rs\rangle
  &=\langle pq\mid rs\rangle-\langle pq\mid sr\rangle,\\
H_2
  &=\frac12\sum_{pqrs}\langle pq\mid rs\rangle
       a_p^\dagger a_q^\dagger a_s a_r\\
  &=\frac14\sum_{pqrs}\langle pq\Vert rs\rangle
       a_p^\dagger a_q^\dagger a_s a_r\\
  &=\sum_{p<q,\ r<s}\langle pq\Vert rs\rangle
       a_p^\dagger a_q^\dagger a_s a_r .
\end{aligned}
$$

The coordinator reproduced a two-mode example with Coulomb coefficient
`J=0.7` and exchange `K=0.2`: unrestricted raw input gives occupied-pair
energy `0.5`, restricted raw input incorrectly gives `0.7`, and restricted
antisymmetrised input gives `0.5`. The correct matrices agree to rounding.

**Impact:** A reader following this convention advice can construct the wrong
Hamiltonian even with a correct encoder.
**Correction and acceptance:** Present all three conventions together and
distinguish raw integrals from already-weighted operator coefficients.
Preserve FockMap's accepted raw-primary contract. Require equivalent matrices
for a nonzero-exchange example; reject the restricted raw-only alternative.

### BM-H02 — Earlier examples still supply different H2 inputs from the canonical worked example

**Severity:** High. **Classification:** Residual source inconsistency associated
with CA-H001/CA-H003; the repaired central tables remain correct.
**Evidence:** `01-electronic-structure.md:184-200` gives spatial one-body values
`-1.2563` and `-0.4719`, then calls them the input to everything that follows.
`03-spin-orbitals.md:168-184` correctly gives the canonical 0.74-Angstrom
values `-1.2533097866` and `-0.4750688488`. The cross-spin example at
`03-spin-orbitals.md:140` still uses `0.6636`; Chapter 2's exercise at
`02-notation.md:248-252` combines old illustrative values with an asserted
"correct" electronic energy of `-1.89` Ha rather than the canonical
`-1.8523881736` Ha.

**Impact:** The book teaches one set of inputs and derives its accepted output
from another. The mismatches are not all legitimate rounding.
**Correction and acceptance:** Reconcile every canonical numerical example
with a named geometry, basis, convention and electronic/total energy label.
Explicitly mark hypothetical data as hypothetical. Replace the exercise that
tries to diagnose a unique convention error from one energy discrepancy with
distinguishable candidate tensors or matrices. Searching only for July's
previously flagged strings is not a complete numerical consistency check.

### BM-H03 — The matrix recipe contradicts the declared bit order and overstates spectrum checks

**Severity:** High. **Classification:** Residual of the ordering correction.
**Evidence:** `09-verification.md:35-37` constructs a matrix in displayed
signature order; lines 41-54 correctly reverse it for occupation-integer rows.
The unreversed tensor example in `04-qubits-gates-circuits.md:166` does not
name a different basis convention. Chapter 9 lines 9 and 20 claim eigenvalue
comparison catches all such errors, while its later paragraph correctly
explains that a basis permutation leaves eigenvalues unchanged.

**Correction and acceptance:** Whenever matrix rows are
$b=\sum_j n_j2^j$, use
$P_0P_1\cdots P_{n-1}\mapsto P_{n-1}\otimes\cdots\otimes P_0$.
An alternative abstract tensor basis is valid only if explicitly identified.
Require matrix elements, labelled occupations and spectra as complementary
checks: `ZIII` is negative on occupation-integer row 1, and displayed HF
`1100` remains integer/row 3. Delete "catches them all"/"only way" claims.
EC-M01 separately addresses the numerical weakness of the moment-based test.

### BM-H04 — The time-evolution chapter teaches the wrong primitive for ordinary VQE measurement

**Severity:** High. **Classification:** Newly identified algorithmic error.
**Evidence:** `14-time-evolution.md:51,81-86` says every VQE Pauli measurement
involves $e^{-i\theta P_k}$ and makes this the common primitive of VQE and QPE.
Measuring `XXYY`, for example, can use local basis changes, computational-basis
measurement and multiplication of outcomes. It does not require the
entangling evolution staircase. Evolution generated by $P$ commutes with $P$;
it is not itself the required change of measurement basis.

**Correction and acceptance:** Separate ansatz preparation, observable
measurement and Hamiltonian simulation. Show `XXYY` measurement beside
`XXYY` evolution, with separate gate counts. VQE may use Pauli rotations in
its ansatz; that does not make them mandatory for every observable
measurement. Keep QPE's controlled-simulation requirements distinct.

### BM-H05 — The opening and conclusion restore the water-pipeline claim that Chapter 19 disclaims

**Severity:** High. **Classification:** Residual source inconsistency associated
with CA-H004/CA-H007, not a wrong regenerated water dataset.
**Evidence:** Chapter 19 correctly identifies its classical PySCF backend
(`19-bond-angle.md:46-58`), but line 150 attributes its cheap fine pass to a
precomputed skeleton. `23-whats-next.md:29` says every pipeline arrow is a
FockMap call and every box has been applied to H2O; line 74 calls the angular
cut an equilibrium geometry. `01-electronic-structure.md:87` promises repeated
quantum-computer execution for the water scan, and `foreword.md:96-104`
attributes all computations to FockMap.

**Correction and acceptance:** Maintain two explicit tracks throughout:
FockMap constructs operators/circuits; PySCF supplies the demonstrated
reference energies. The water calculation reruns RHF/FCI at each angle,
uses all-electron STO-3G and fixed experimental O-H length 0.9584 Angstrom,
and finds its lowest sampled FCI angle at 99 degrees. It is not a full
geometry optimisation or a demonstrated per-geometry FockMap energy pipeline.
Correct the opening, objectives and closing inventory, not just the central
disclaimer. Implementing a larger capstone is an optional scope decision,
not necessary to make the existing reference calculation honest.

### Further mathematical and algorithmic findings

Each row is an actionable finding with its own acceptance boundary.
Paths are relative to `manuscript/`.

| ID / severity | Evidence | Finding | Correction and acceptance |
|---|---|---|---|
| **BM-M01 / Medium** | `02-notation.md:113-131,238,248` | Real physicist integrals inherit eightfold symmetry under different permutations; relabelling the tensor does not reduce it to fourfold. The real-chemist list also duplicates its final permutation. The conversion exercise's claimed inequality is false under its real-orbital assumptions. | Translate the full eight permutations; distinguish real- from complex-orbital assumptions. For the stated example, both queried physicist integrals equal the supplied real chemist integral. |
| **BM-M02 / Medium** | `03-spin-orbitals.md:36,59,111,128,247`; `05-visual-encodings.md:379` | Doubling four tensor axes creates 16 times the slots, not four; four spin-allowed blocks are a different count. Missing floors in spatial indices and wrong nonzero-count guidance compound this. Missing cross-spin blocks or fermion signs do not simply produce Hartree-Fock or forbid entanglement. | Use floor division for interleaved spin indices. For seven spatial orbitals, distinguish 196/38416 dense slots from at most 98/9604 spin-allowed entries. Evaluate the HF determinant with and without its cross-spin Coulomb term; describe changed physics, not "HF by omission". |
| **BM-M03 / Medium** | `04-qubits-gates-circuits.md:74,111-121,166` | Same-basis Bell correlations alone admit shared classical randomness; Pauli gates plus CNOT are not universal; the product-state box is not a formula for a general multiqubit state. | Compare a Bell state with its incoherent mixture across appropriate bases. State universality for arbitrary single-qubit rotations plus CNOT. Write a general state as a sum of computational-basis products, not one product. |
| **BM-M04 / Medium** | `05-visual-encodings.md:213-249,273` | The diagram puts node 6 below node 5 despite the latter storing only parity 4 XOR 5. Prefix queries are confused with update-ancestor paths. An eight-mode Fenwick root has three children, contradicting "at most two". | Repair the edge to parent 7; separate storage intervals, update traversal and prefix decomposition. Verify all displayed parities against all eight one-hot occupation inputs. This concerns the explanation, not evidence of a broken BK implementation. |
| **BM-M05 / Medium** | `05-visual-encodings.md:281-305`; `07-six-encodings.md:119-129,189`; `08-building-vlasov.md:67,90-100,218-233` | The ideal ternary maximum gives 4/5 at n=32/64, but the displayed tested helper gives 5/6 without explaining the construction difference. Tree nodes/leaves/modes and the Majorana pairing are insufficiently distinguished. | Separate literature bounds from measured helper weights; account for the root and every mode; derive one ladder operator from its Majorana pair. Explain the difference rather than declaring the package wrong or changing the common logarithmic class. |
| **BM-M06 / Medium** | `04-qubits-gates-circuits.md:172`; `07-six-encodings.md:129`; `12-clifford-tapering.md:86,112`; `15-trotter-formulas.md:207`; `20-algorithms.md:22`; `23-whats-next.md:46` | Categorical resource statements defeat the corrected cost model: only CNOTs matter, all terms inherit worst-case savings, an unsupported H2O total and 20-40% compiler saving, unconditional complexity dominance, and exaggerated classical-memory impossibility. | Use generated, model-specific costs. Explain variables in complexity comparisons. For 14 electrons in 20 spin-orbitals, fixed Ms=0 has 14400 determinants; full dense Fock-space storage is not the only classical method. Remove universal feasibility claims and unsupported percentages. |
| **BM-M07 / Medium** | `20-algorithms.md:73,80-94,141-142,223,272-274` | The shot derivation converts a worst-case variance inequality to equality; the independent-shot derivation is extended to grouped measurements without covariance. Fewer groups need not mean fewer shots. Summaries overstate the pending package-grouping evidence. | Keep the bound as an inequality; distinguish coefficient-only worst-case allocation from variance-informed allocation. Derive group-estimator covariance explicitly. The independent H2 bound is about 1.391 million shots for 1.6 mHa; this is not a universal grouped-measurement count. |
| **BM-M08 / Medium** | `20-algorithms.md:30,158-222`; `15-trotter-formulas.md:40-44,211` | The repaired QPE relation is sound, but depth is still dismissed as a fault-tolerant bottleneck, QFT is listed under classical cost, and an identity-phase warning loses the controlled-evolution qualification. A worked shifted H2 phase decode is missing. | Align all summaries; give one stated interval, shift, phase, 12-bit estimate and decoded electronic energy. Track offset, overlap, confidence and simulation error separately. Do not close CA-H008's exporter/resource-helper gate from this arithmetic. |
| **BM-M09 / Medium** | `14-time-evolution.md:154-173` | A query-complexity expression appears in the "Error scaling" column; the prose says error scales linearly in query count. Resource cost and approximation error are different quantities. | Compare error at a stated budget, or cost at fixed error. Define normalisation and source hypotheses; do not call a complexity expression an error or assert optimality without the precise theorem. |
| **BM-M10 / Medium** | `10-why-tapering.md:35-41,119-125`; `11-diagonal-z2.md:99-110,202-208`; `appendix-theory.md:17-19` | A single-qubit Z detector is described using the broader term "diagonal Z2 symmetry"; parity can be confused with fixed particle number/spin. An n-qubit lower bound is missing its full-CAR/Fock-space hypothesis. | Distinguish single-qubit Z candidates from multi-qubit diagonal Z strings, and N, Ms, total spin, parity and point-group labels. Give an example of different particle numbers sharing parity; scope the dimension bound so it does not appear to forbid tapering. |
| **BM-M11 / Medium** | `23-whats-next.md:59-65,100-116` | ADAPT-VQE is misdescribed as selecting Hamiltonian terms, qubit-ADAPT as only one/two-qubit operators, and lighter strings as necessarily better directions. Offline tapering is incorrectly said to measure syndromes. The "nobody has yet" claim is too broad. | Distinguish excitation/operator pools, gradient selection and completeness; use the primary ADAPT definitions. An adaptive example needs a useful nonzero gradient, not merely a light string. Tapering and QEC share stabilizer algebra, not identical physical operations. |
| **BM-M12 / Medium** | `21-circuit-export.md:19-25,39,95,130,236-250` | Universal OpenQASM 3 portability contradicts later version restrictions; Q# is said both to allocate qubits and receive them from its caller; resource estimation is conflated with hardware lowering. | Name each importer/version/subset and caller wrapper; correct ownership; separate estimation from compilation. Successful serialization or parsing is not imported-unitary equality. |
| **BM-M13 / Medium** | `19-bond-angle.md:240`; `23-whats-next.md:74` | A fixed-bond angular curvature is treated as sufficient for molecular normal-mode frequencies. | State coordinate constraints and kinetic/mass metric. A full harmonic normal-mode claim requires a suitable stationary geometry and mass-weighted Hessian; a one-coordinate cut is not that calculation. |
| **BM-M14 / Medium** | `appendix-theory.md:93-103`; `23-whats-next.md:78` | Finite bosonic truncation omits its commutator boundary defect, and d alternates between maximum occupation and number of levels. | Choose one level convention; state the finite-space boundary term and limits of the represented subspace. Update unary/binary/Gray qubit counts consistently. |
| **BM-M15 / Medium** | `02-notation.md:179`; `14-time-evolution.md:197`; `15-trotter-formulas.md:228`; `appendix-theory.md`; `references.md` | A nonexistent-in-preface JOSS citation, conflicting Suzuki titles for one journal record, missing foundational Further Reading entries and unexplained external "Theory Ch." references prevent a unified bibliography. | Consolidate the actual cited works and unambiguous destinations. Crossref resolves JMP 32,400-407 (1991), DOI `10.1063/1.529425`, to *General theory of fractal path integrals with applications to many-body theories and statistical physics*. Metadata resolution alone does not establish support for every claim. |
| **BM-M16 / Medium** | `07-six-encodings.md:146-147,166-167,199-203`; `appendix-theory.md:90-91` | Compressed "stars only" summaries do not distinguish finite census evidence, an enforced API restriction and an all-size theorem. | Retain the n=3 through 6 census range, identify tested shapes, and verify any separate API restriction. The central book correctly avoids the blog's SRL blame and universal path-tree claim. No supplied all-size proof should be assumed; narrow empirical/API scope is sufficient if that is the book's claim. |
| **BM-M17 / Medium** | `01-electronic-structure.md:19,161-163` | The opening makes ground-state energy determine whether a reaction happens and the boiling point without naming finite-temperature free energies, phase equilibrium or kinetic barriers. Its H2 "99% / remaining 1%" also lacks an electronic-versus-total energy convention. | Describe electronic energies as essential inputs, not a complete thermodynamic/kinetic prediction. Qualify the percentage using the canonical energy convention, or prefer the unambiguous correlation energy in Ha. Preserve the motivation without suggesting that one electronic ground-state calculation settles these additional problems. |
| **BM-L01 / Low** | `16-cnot-staircase.md:102`; `18-complete-pipeline.md:257-267`; `19-bond-angle.md:144` | Two CNOTs plus one single-qubit gate is called five gates; the verbal dissociation well-depth subtraction has the wrong sign; energy is said to drop on both sides of a minimum. | Use three gates; positive well depth is asymptote minus minimum, with De distinguished from zero-point-corrected D0; energy rises away from the minimum. |

**BM-M05, representation-invariant clarification from the final specialist
report:** `05-visual-encodings.md:383` says all encodings preserve Hamiltonian
term count, while `07-six-encodings.md:242` warns against assuming that
invariance. State the class being discussed. Clifford conjugation permutes
Pauli strings and preserves the exact simplified nonzero term count;
arbitrary valid encodings need not. Do not resolve the inconsistency by
insisting that the six supplied mappings must have different counts. Their
common H2 count is compatible with the narrower invariant.

## Completeness, consistency and readability

### BE-M01 — The promised F# novice route is incomplete

**Severity:** Medium. `foreword.md:63-73` assumes no F# or functional
programming, but `02-notation.md:175,203-220`, `03-spin-orbitals.md:190-211`
and `08-building-vlasov.md:71-100` require pipelines, partial application,
maps/options, pattern matching, arrays/lists, unsigned literals, recursion,
records and script/module conventions before teaching them. Reading `let`
as "define" does not supply that background.

Add a compact code-reading/running bridge before the first substantial
example. Label complete scripts, contextual excerpts and pseudocode. A
novice should be able to explain the first coefficient factory and execute
its named entry point without an unrelated language tutorial. Passing FSI
execution is not evidence that the novice route is sufficiently explained.

### BE-M02 — The missing worked bridges are at the book's most important transformations

**Severity:** Medium. `03-spin-orbitals.md:217` promises a step-by-step
15-term derivation, but `06-building-hamiltonian.md:114-149` jumps from the
correct coupling monomials to their collected Pauli expression.
`08-building-vlasov.md:90-100,182-205` delegates the Majorana construction
and leaves parts of CAR verification as comments.
`12-clifford-tapering.md:153-166` calls a literal sector explicitly derived
without showing the physical quantum numbers-to-generator calculation.
Chapter 18's objectives promise integration of every concept but its actual
capstone deliberately remains untapered.

The remedy is a small number of decisive demonstrations, not more general
exposition: one full coupling expansion, one tree-to-Majorana-to-ladder
construction, one H2 physical-sector-to-tapered-matrix example, and one
worked energy-estimation/phase-decoding example. The existing closed-shell
two-by-two H2 block is an economical bridge from the Pauli table to
correlation energy. Where a demonstration is deliberately excluded, narrow
the corresponding objective instead of implying it exists.

### BE-M03 — Eleven chapters lack the promised exercises, and some existing questions have wrong answers

**Severity:** Medium. The foreword promises exercises at every chapter's end.
They exist in Chapters **1-10, 15 and 17**, but not **11-14, 16 or 18-23**.
There is no accompanying answer bank adequate for the stated self-study and
lecturer use.

Specific defective exercises include the "two-electron" question about
ten-electron water (`01-electronic-structure.md:224`), the convention hint
and energy diagnosis in Chapter 2, the spin-allowed counts in Chapter 3,
and removing terms 12-15 rather than coupling terms 8-11
(`06-building-hamiltonian.md:300`). Chapter 7's frozen-core water exercise
requires a transformation/input artifact not supplied by that point.
Chapter 9's HF exercise mixes spatial and spin-orbital index notation.

Either supply the promised exercises or revise the chapter contract
deliberately. Each retained question needs checked inputs, a valid answer
or rubric, an attainable endpoint and selected worked solutions. Label
open-ended projects as projects; do not present missing-data tasks as
routine exercises with an obvious answer.

### BE-M04 — Revision-history language competes with the explanation

**Severity:** Medium. Examples include "The operator previously used here"
(`06-building-hamiltonian.md:116`), tables waiting for a fixed release
(`09-verification.md:122`; `17-cost-analysis.md:52-55`), the removed-table
story (`13-tapering-benchmarks.md:117-130`), "repaired workflow" and
"90%, not 85%" in Chapter 19, "12, not 10" in Chapter 20, and pending
"post-fix audit" language in Chapter 21.

Move historical corrections to this audit. Teach the current result and its
current limitation. Preserve actual safety conditions, especially tensor
conventions, sectors and controlled phases; remove only the demand that a
first-time reader understand an earlier draft. Package publication is
already closed, while particular export/grouping/theorem gates remain open.
Those are different facts and should not be compressed into "awaiting a fix".

### BE-M05 — The appendices are useful summaries, not the complete references they promise

**Severity:** Medium. Appendix A's "every public type and function" promise
exceeds its selected API tables; even Trotter/export surfaces used by the
book are absent. Appendix B promises formal derivations and proofs but mainly
summarises notation and results, with unexplained external theory references.
Either expand these artifacts or describe them as a selected API index and
mathematical/conventions summary. Essential conventions must remain
self-contained; external detail should have precise versioned destinations.

### BE-M06 — Figure context needs to survive reuse, and the early conceptual bridge needs visual support

**Severity:** Medium for standalone scientific interpretation; lower priority
than the incorrect Fenwick diagram.
The H2 source correctly explains the STO-3G atomic asymptote near
`-0.933164` Ha (`18-complete-pipeline.md:239`), but the plot also shows
`-1.0` Ha without identifying it as the complete-basis nonrelativistic
two-H limit. Label FCI "exact within STO-3G" and explain the difference as
basis error, not missing correlation. Prefer "restricted Hartree-Fock" for
the displayed dissociation comparison.

The water caption is already substantially correct. Add fixed
`r_OH=0.9584 Angstrom`, PySCF FCI/STO-3G and "lowest sampled angle" to the
portable figure context so reuse cannot turn it into a full geometry
optimisation.

The book has 25 Mermaid diagrams and two data plots, with no diagrams in
Chapters 1-4 and seven in Chapter 5. Add only visuals that solve a specific
comprehension problem: spatial orbitals to spin-orbitals/configurations,
the chemist/physicist index permutation, and Bell coherence versus classical
correlation. Repair the incorrect Fenwick visual before adding decorative
figures.

### Whole-book editorial judgement

The book has a coherent, recognisable voice. Concrete physical questions,
inspectable intermediate objects and direct explanations work well. The
personal notation discussion, Chapter 6's configuration-coupling explanation
and Chapter 12's corrected Heisenberg derivation are strengths.

The main readability problem is not polish. Introductory shortcuts, carefully
qualified derivations, audit-history caveats and emphatic conclusions
sometimes teach different versions of the same claim. A reader should not
have to decide whether the opening sentence or its later correction is the
rule to remember.

Keep the voice. Correct the first explanation, centralise stable conventions,
and use warnings at genuine risk boundaries. The chemistry-to-encoding
progression is strong; the tapering sequence needs a molecular worked bridge,
Chapter 17 needs a deliverable proportionate to its comparative title, and the
closing chapters must distinguish what has been built from what future
state-preparation and energy-estimation work still requires.

## Full-book coverage matrix

Every chapter and appendix received the integrated correctness, consistency,
readability and completeness pass. "Sound core" means the cited central
derivation survived this review; it is not a claim that the whole chapter is
free of the listed residuals.

| Source | Correctness / consistency | Readability / completeness judgement |
|---|---|---|
| Foreword | BM-H05 backend/promise residuals | Strong framing; F# and exercise promises exceed delivery |
| Ch 1 — Electronic structure | BM-H02 stale inputs; BM-H05 forward claim | Effective physical opening; repair data labels and water exercise |
| Ch 2 — Notation | BM-H01/H02, BM-M01 | Memorable motivation; antisymmetrisation and code bridge are essential |
| Ch 3 — Spin orbitals | Canonical table sound; BM-M02 | Good selection-rule progression; repair counts, floors and diagnoses |
| Ch 4 — Qubits/gates | BM-H03, BM-M03/M06 | Accessible but foundational explanations are overcompressed |
| Ch 5 — Visual encodings | JW example sound; BM-M02/M04/M05 | Repair the Fenwick visual; supply the Majorana/construction bridge |
| Ch 6 — Hamiltonian | Central coefficients/signs sound | Strong explanation; show the coupling expansion and fix exercise indices |
| Ch 7 — Six encodings | CAR principle and attribution repair sound; BM-M05/M06/M16 | Clear interface story; separate ideal/helper/census results and supply project inputs |
| Ch 8 — Tree encoding | Path-scope restraint sound; BM-M05 | Good shape-to-code intention; account for modes and show a checked operator |
| Ch 9 — Verification | Canonical spectrum sound; BM-H03 | Strong motivation, contradictory matrix/check recipes; EC-M01 affects companion guarantee |
| Ch 10 — Why tapering | Core sector exactness sound; BM-M10 | Good motivation; distinguish detector limitations from mathematical symmetries |
| Ch 11 — Diagonal symmetries | Toy substitution sound; BM-M10 | Clear toy; physical-sector bridge and exercises missing |
| Ch 12 — Clifford tapering | Subgroup/Heisenberg core sound; BM-M06/M10 | Strong worked derivation; molecular sector derivation and exercises missing |
| Ch 13 — Tapering benchmarks | Toy counts consistent; large unsupported tables remain removed | Opening promises real Hamiltonians more broadly than delivered; label toy scope |
| Ch 14 — Time evolution | BM-H04, BM-M09 | Good energy/time introduction; measurement and simulation must be separated |
| Ch 15 — Trotter formulas | H2 logical counts/bound sound; BM-M06/M08 | Useful quantitative sequence; repair phase takeaway and bibliography |
| Ch 16 — CNOT staircase | Decomposition sound; BM-L01 | Concrete and approachable; reconcile abstract/emitted gate counts; no exercises |
| Ch 17 — Cost comparison | JW statistics sound | Too thin for opening promise; replace historical withholding with actual accepted evidence |
| Ch 18 — Pipeline | Honest central two-track framing; BM-L01 | Capstone momentum; objectives must admit untapered/circuit-only scope |
| Ch 19 — Water angle | Accepted fixed-bond result sound; BM-H05/M13/L01 | Strong interpretation; remove skeleton claim and qualify vibrational extension |
| Ch 20 — Algorithms | Central QPE and QWC partition sound; BM-M06/M07/M08 | Useful split, inconsistent estimator/summary claims; worked phase example and exercises missing |
| Ch 21 — Export | Intended round-trip contract sound; BM-M12 | Practical structure; named importers/wrappers and actual parity evidence remain necessary |
| Ch 22 — Scaling | Scoped operator and FeMo active-space facts sound | Honest but brief; a roadmap, not a generated molecular benchmark |
| Ch 23 — Extensions | BM-H05, BM-M06/M11/M13/M14 | Closing claims exceed demonstrated scope and repeat incorrect shortcuts |
| Appendix A — Cookbook | Raw/weighted distinction sound in prose | Selected reference rather than complete API manual |
| Appendix B — Theory | Pauli basis/group repair sound; BM-M10/M14/M16 | Useful summary, not full proofs; external theory references need destinations |
| References | BM-M15 | Central list exists, but cited works and metadata are not fully reconciled |

### Evidence scope and limitations

The manuscript specialist traversed the complete chapter/appendix source
ranges and checked the displayed algebra and arithmetic. To close its
long-line display limitation, the coordinator extracted, wrapped and read all
76 manuscript paragraphs longer than 350 characters, in addition to directly
rereading the high-priority passages and the foundational, Fenwick,
shot-allocation and concluding sections. The restricted-integral and counting
counterexamples were reproduced numerically. This is a full-source technical
and editorial review, not a line-by-line copyediting rewrite. The separate
full companion review and fresh execution are described next.

Bibliography integration was reviewed across the book. The coordinator
resolved the conflicting Suzuki metadata and retrieved the title records for
qubitization and the two ADAPT papers. This is not a new full-text verification
of all 21 bibliography entries, every primary theorem, experimental constant
or vendor importer. Those limits must remain explicit. The audit establishes
internal mathematical and explanatory defects without pretending that a
resolved DOI certifies its surrounding claims.

### Overall assessment of the book

**Retain and correct; do not rewrite from scratch.** The central H2
tensor-to-Hamiltonian calculation, accepted numerical anchors, corrected
Heisenberg reduction, explicit cost model and independent reference chemistry
give the book a substantial foundation. The source is nevertheless not ready
as a consistently reliable teaching text: foundational errors, contradictory
summaries and missing learning steps remain.

Its defensible demonstrated scope is canonical H2 data to checked operators
and untapered logical product-formula circuits, supported by independent
classical chemistry references. Water is an instructive conditional reference
calculation, not a second completed quantum-energy pipeline. Either teach that
scope consistently or explicitly approve the work required for a larger
capstone. The book's readability improves most by correcting its first
explanations and completing a few decisive derivations, not by smoothing away
its author's voice.

## Executable evidence, correctness and chapter consistency

The companion specialist read every executable, shared helper and numerical
artifact listed below. Its tool context did not support execution; the
coordinator therefore performed the runtime part independently in an isolated
`git archive` copy of the reviewed commit. **All ten labs, all six F#
companions, `make pipeline-check` and `make verify-data` passed**, including
byte-identical regeneration of the committed H2/H2O data and plots.
The available environment was .NET SDK 10.0.302, PySCF 2.13.0, NumPy 2.0.2
and SciPy 1.13.1; chemistry was run with thread limits and a wall-time bound.
No canonical data in the book worktree were overwritten.

This is fresh evidence for the corrected numerical foundation. It is not
proof that every test establishes the stronger conclusion its prose claims.
The two negative controls below demonstrate that distinction.

### EC-M01 — Approximate spectral moments do not certify eigenvalues to the same tolerance

**Severity:** Medium. **Classification:** Reproduced verification-mathematics
defect, not evidence that the current six encoders produce wrong spectra.
**Evidence:** `labs/03-compare-encodings.fsx:150-168` claims agreement of
16 power sums fixes the eigenvalue multiset "to the stated numerical
tolerance". `labs/PauliMatrix.fsx:94-102,162-187` compares normalised moments
at `1e-7`; it does not calculate eigenvalues or bound the inversion's
conditioning.

For a concrete counterexample, take the committed spectrum and split its
repeated eigenvalue `-0.4750688487721783` into values displaced by `+1e-4`
and `-1e-4`. The coordinator's numerical reproduction gives a maximum
normalised moment discrepancy of `1.4094784656596758e-9`, which passes the
existing `1e-7` criterion, while the maximum sorted-eigenvalue discrepancy is
`1e-4`. Exact power-sum uniqueness is not the disputed fact; transferring a
finite numerical tolerance through that inverse problem is.

**Correction and acceptance:** Retain moment checks as diagnostics, but use
a dimension-, finiteness- and Hermiticity-checked eigensolver for a claimed
eigenvalue tolerance. Compare sorted eigenvalues with the independent oracle;
keep labelled state/sector checks separate. The split-degeneracy negative
control must be rejected by any gate claiming `1e-7` eigenvalue agreement.

### EC-M02 — The verification command repairs a corrupted oracle instead of detecting it

**Severity:** Medium. **Classification:** Reproduced evidence-integrity defect.
**Evidence:** `code/ch09-verify-h2.py:516-571` writes
`h2_0.74_oracle.json` without first comparing the existing oracle.
`Makefile:199-202` runs this writer before the downstream consumer checks.
In the isolated copy, the coordinator changed the oracle's `IIII`
coefficient to `123.0`. The verifier exited successfully and silently replaced
it with `-0.8121706072487134`.

**Impact:** The numerical derivation is useful, but a successful run does not
establish that the oracle originally supplied to the reader was correct.
It also makes a supposedly diagnostic command write accepted evidence.
**Correction and acceptance:** Default verification must be read-only and
reject corrupt coefficients, spectra, metadata and ordering anchors.
Put regeneration behind an explicit mode and output path. Demonstrate
nonmutation on success and failure, then prove deliberately corrupted
references are rejected.

### EC-M03 — The chemistry generators do not fail closed on solver nonconvergence

**Severity:** Medium. **Classification:** Source-confirmed robustness gap;
no unconverged result was observed in the accepted data or the fresh run.
**Evidence:** `code/ch18-generate-h2-integrals.py:111-120,213-223`,
`code/ch18-dissociation-scan.py:42-57`, and
`code/ch19-bond-angle-scan.py:30-49` accept RHF/FCI results without explicit
convergence and finite-result gates. The water generator promotes its coarse
CSV before completing the fine pass.
**Correction and acceptance:** Check solver convergence and finite
energies/tensors, record convergence settings, and promote a complete
validated output batch. A deliberately nonconvergent run must fail without
replacing accepted artifacts. This is not a request for an expensive new
basis-set study.

### EC-M04 — Standalone capstone input guards are weaker than the full verification suite

**Severity:** Medium. **Classification:** Input and parity coverage gap.
**Evidence:** `code/ch03-spin-orbitals.fsx:25-39` loads canonical-looking data
without binding it to provenance/checksums.
`code/ch18-pipeline.fsx:63-81,134-181,208-236` checks term count and two
coefficient sentinels, and parses scan records with regexes rather than
validating the complete expected grid. The wrapper's row-count check is
stronger than the standalone entry point.

Changing the equilibrium one-body coefficients `h00` and `h11` by opposite
amounts preserves the two named sentinels and term count while changing the
Hamiltonian. This is a source-level coverage counterexample, not an observed
corruption of the current fixture. Similarly, term and cost counts alone do
not establish direct-versus-skeleton operator equality across the scan.
**Correction and acceptance:** Reuse `System.Text.Json`, validate the exact
required geometry keys and metadata, and compare complete operators/matrices
where "verified" is claimed. Keep cost smoke tests and numerical parity
tests separately labelled. Extend output-isolation protection to the
canonical physicist tensor and its provenance file.

### EC-M05 — Scaling labs contradict the book's operator-versus-circuit distinction

**Severity:** Medium. **Classification:** Current companion/prose inconsistency.
**Evidence:** `labs/06-scaling.fsx:124-159` turns ladder-weight logarithms into
"1000-mode circuits from depth 1000 to depth ~10", and supplies generic
small/medium/large-molecule encoding recommendations.
`labs/07-trotter-cost.fsx:156-160` conflates individual encoded creation
operators with molecular Hamiltonian terms.
The source book correctly limits this inference
(`17-cost-analysis.md:59-74,84-94`).
**Correction and acceptance:** Call the measured quantity ladder-component
weight or illustrative single-Pauli staircase cost. Compute molecular costs
from the actual simplified Hamiltonian and separate count, depth, routing and
simulation error. Generate narrative comparisons from the measured table;
Lab 07's reference to `n=64` also needs reconciling with its displayed loop
ending at 32.

### EC-M06 — The allowed dependency range is wider than the archival verification contract

**Severity:** Medium. **Classification:** Reproducibility-contract gap.
**Evidence:** `requirements-data.txt:2-4` permits broad dependency ranges;
`code/ch09-verify-h2.py:350-373` requires an exact PySCF version and exact
regenerated floating-point semantic hash.
`scripts/check-data-idempotence.sh:23-44` requires byte-identical JSON, CSV
and PNG outputs. The fixture names PySCF 2.13.0 and NumPy 2.0.2.
**Impact:** A permitted installation can perform a numerically valid
calculation without meeting the archival byte contract. The current matching
environment passes; that does not establish portability across all allowed
versions or numerical backends.
**Correction and acceptance:** Separate tolerance-based scientific parity from
archival byte reproduction. Provide a tested lock/container and documented
platform assumptions for the latter, including plotting dependencies.

### EC-L01 — The water lab validates selected values but not its complete provenance

**Severity:** Low. **Classification:** Coverage gap, not a wrong water result.
**Evidence:** `labs/08-h2o-workshop.fsx:70-85,124-137` checks bond length,
counts, minima and a bending-energy ratio, but not the declared CSV hashes or
exact unique grids in `h2o_bond_angle_metadata.json`.
**Correction and acceptance:** Check metadata binding, finite values, the
complete grids and their overlapping rows before claiming validated
provenance. Retain its honest role as an analysis of precomputed PySCF data.

### EC-L02 — Several executable entry points retain misleading prerequisites or exercises

**Severity:** Low. **Classification:** Reader friction.
**Evidence:** The Chapter 06, 07, 11, 12 and 18 F# companions' line-5
prerequisite says `dotnet build --configuration Release`, although their
actual dependency path is pinned NuGet/FSI. Lab 02's bond-length exercise
(`labs/02-h2-molecule.fsx:165-168`) points readers towards changes to an
equilibrium-specific fixture/loader. The diagonal toys do not include the
exact `(+1,-1)` sector used in Chapter 11's worked example.
**Correction and acceptance:** Give runnable SDK/FSI instructions; supply a
parameterised extension without altering the canonical fixture; include the
chapter's exact toy-sector result as an assertion. Different example sectors
are not themselves mathematically incorrect.

### Complete companion coverage and disposition

All files below were source-reviewed. All runnable F# entries and the
generation/check paths were exercised in the coordinator's isolated run.
"Repair" means repair the identified scope or validation boundary, not
discard the working numerical foundation.

| Companion | Actual scope | Disposition |
|---|---|---|
| `code/ch03-spin-orbitals.fsx` | Loads generated canonical spin integrals | Keep; strengthen input/provenance binding |
| `code/ch06-building-hamiltonian.fsx` | Live JW construction and six-map counts | Keep; distinguish sentinels from full matrix parity |
| `code/ch07-six-encodings.fsx` | Ladder-weight/cost examples | Keep; state which maps the displayed experiment exercises |
| `code/ch11-diagonal-z2.fsx` | Explicit-sector diagonal toy | Keep; add expected-output assertions |
| `code/ch12-clifford-tapering.fsx` | Symplectic representation and symmetry detection, not completed tapering | Relabel/repair; add the worked Clifford reduction if promised |
| `code/ch18-pipeline.fsx` | Untapered construction, logical costs, JW QASM serialization, skeleton scan | Retain; repair guards and qualify untested export/taper stages |
| `code/ch09-verify-h2.py` | Independent numerical verifier and oracle writer | Retain derivation; split verification from generation |
| `code/ch18-generate-h2-integrals.py` | RHF/MO integral generation | Retain; enforce solver and output contracts |
| `code/ch18-dissociation-scan.py` | PySCF RHF/FCI dissociation reference | Retain; enforce solver contract |
| `code/ch19-bond-angle-scan.py` | Fixed-bond STO-3G PySCF RHF/FCI scan | Retain; enforce convergence and complete-batch promotion |
| `labs/01-first-encoding.fsx` | JW operator demonstration | Keep; assert pedagogical checkpoints |
| `labs/02-h2-molecule.fsx` | Canonical H2 Hamiltonian | Keep; repair parameter-extension instructions |
| `labs/03-compare-encodings.fsx` | Six maps, JW matrix, spectral moments | Repair the claimed eigenvalue-tolerance guarantee |
| `labs/04-custom-encoding.fsx` | Explicitly unvalidated candidate maps | Keep; a negative CAR example would help |
| `labs/05-custom-tree.fsx` | Weight-only custom-tree exploration | Keep; not a theorem/census certificate |
| `labs/06-scaling.fsx` | Ladder-weight census | Repair whole-circuit extrapolations |
| `labs/07-trotter-cost.fsx` | Logical gate-cost illustration | Repair scope and narrative/data consistency |
| `labs/08-h2o-workshop.fsx` | Analysis of committed classical reference data | Keep; strengthen metadata binding |
| `labs/09-qubit-tapering.fsx` | Diagonal symbolic toy | Keep; add chapter-matched assertions |
| `labs/10-vlasov-tree.fsx` | Built-in tree/string/weight comparisons | Keep; inherit repaired spectrum gate |
| `labs/PauliMatrix.fsx` | Matrix construction and moment diagnostics | Keep matrix routines; replace tolerance-controlled spectrum claim |
| `scripts/check-data-idempotence.sh` | Temporary-copy archival regeneration | Keep; distinguish byte identity from portable numerical parity |
| `scripts/check-ch18-output-isolation.sh` | Selected-file protection and cost smoke test | Keep; broaden protected inventory and label scope |

The numerical-artifact pass covered the canonical physicist tensor and its
provenance, the equilibrium fixture and oracle, the 18-point dissociation
integral/CSV pair, the 25-point coarse and 21-point fine water CSVs, and the
water metadata. The fresh regeneration reproduces these accepted artifacts;
there is no September evidence here that the repaired central chemistry
dataset is wrong.

### Residual gates are not equivalent to broken baseline chemistry

CA-H008 remains a separate export/QPE evidence gate: serialization does not
establish imported-unitary equality, correct controlled identity phase, or an
executed energy-estimation algorithm. CA-M011 remains a pinned
measurement-grouping-output gate; the valid hand-derived five-basis QWC
partition is not a record of a particular API execution. CA-M010 remains a
scoped research/theorem gate; the custom-tree labs do not supply a complete
CAR census or proof. None should be marked closed merely because all current
lab commands exit successfully.

## Publication and production findings

### BR-H01 — Readers downloading the book still receive the superseded incorrect version

**Severity:** High. **Classification:** Current distribution defect; not a
regression of the repaired source mathematics.
**Evidence:** `README.md:11` and `docs/index.md:10` direct readers to
`https://github.com/johnazariah/molecules-to-circuits/releases/latest`.
On 7 September this resolves to book `v0.9.0`, published
`2026-05-12T23:24:16Z`. Its actual `molecules-to-circuits.pdf` has 164 pages and
SHA-256 `f3ae4ff43300a63fb3d46558b62937e1d58dba392a9eb6037a6fce993be6a4da`.
Text extraction confirms the impossible `0.6975782469` integral, the obsolete
`-1.1422` total / `-1.8573` electronic H2 energies (physical PDF pages 77-78,
80 and 151), and the water calculation's "No empirical parameters" claim.
The tracked `manuscript/from molecules to quantum circuits.pdf` is another
stale, 161-page artifact containing the same superseded errors.

The book's HTML deployment at the reviewed commit succeeded on 24 July
(GitHub Actions run `30075578467`), and the live preface contains the corrected
PySCF/FockMap distinction. Fixing the Markdown therefore did not fix what a
reader receives through the download route.

**Impact:** A blog can agree with current source yet contradict the PDF it
recommends. The most consequential July corrections have not reached the
downloadable edition.
**Correction and acceptance:** Deliberately replace or mark the tracked PDF as
historical; publish a new, corrected book edition from one accepted source
commit, with matching PDF, EPUB and companion archive. Preserve historical
release provenance and add an explicit supersession/errata route rather than
silently treating the old release as current. Download the resulting public
artifacts and check their contents, not merely the release job's status.
Do not confuse **book v0.9.0 (May)** with **FockMap 0.9.0 (July)**.

### BR-H02 — The previously recorded Zenodo rights gate remains open

**Severity:** High. **Classification:** Existing owner/release blocker, confirmed
still live; not a new finding about mathematical correctness.
**Evidence:** The public API for record `20148795` on 7 September reports
`resource_type.type=software`, `license.id=mit-license`, version `v0.9.0`,
publication date `2026-05-13`, and a combined repository ZIP. `CITATION.cff`
points to that version and concept DOI. Current `MANUSCRIPT-RIGHTS` and the
book preface reserve rights in the manuscript, cover and figures while
`LICENSE-CODE` licenses code separately.

**Impact:** Citation and redistribution metadata describe a different rights
arrangement from the current book. This also affects which book material can
be exported into the public workbook/blog ecosystem.
**Correction and acceptance:** The owner must choose accurately represented
mixed records or separate book/code records and reconcile the public landing
pages, files, description, rights and version relationships. Do not imply that
changing present metadata retroactively revokes any valid earlier licence.
This review makes no legal-status change and does not claim the gate closed.

### BR-M01 — A successful PDF build can contain raw Mermaid instead of the teaching diagrams

**Severity:** Medium. **Classification:** Reproduced output-integrity defect.
**Evidence:** `manuscript/mermaid.lua:99-114` returns `nil` on a failed rendering
command, leaving the original code block. A negative control with `MMDC=false`
returned a Mermaid `CodeBlock` and exit status zero. More importantly, the
existing `make sample` command, run with an output path in session storage,
reported 25 `mmdc: command not found` failures and nevertheless exited
successfully with a 54-page PDF. Its only raster figure was the water plot;
the selected chapter's encoding diagrams were not rendered.

**Impact:** The visual explanation is part of the argument, especially in
Chapter 5. A green publishing job is insufficient evidence that readers
received it.
**Correction and acceptance:** Required diagram failures must fail publication.
Add an existing-build-compatible negative control and expected-figure check;
ensure PDF and EPUB cannot pass by retaining Mermaid source or silently
omitting a diagram. This does not assert that the currently deployed HTML is
broken: the live Chapter 5 rendered all seven diagrams and had no detected
KaTeX errors in the inspected page.

### BR-M02 — Auxiliary publishing descriptions have not all adopted the corrected reader contract

**Severity:** Medium. **Classification:** Current production-copy drift.
**Evidence:** `docs/index.md:20`, `springer/book-information-form.md:58,75-80`,
and `jose/paper.md:53-61,119-124` retain universal "every formula"/"every result"
execution claims. The JOSE packet also describes 15 labs and a Jekyll site
(`jose/paper.md:59-61,135-150`), versus ten numbered labs and the current MyST
site. The Springer form answers "Previously published electronically: No"
(`springer/book-information-form.md:95`) despite the public site, releases and
Zenodo archive. `manuscript/Leanpub.yml:31` labels the library documentation
URL as the book's web version.

**Impact:** A correct capstone qualification is ineffective if outreach,
landing pages or a proposal restore the old claim. Electronic availability
and publication by an academic publisher are not the same question.
**Correction and acceptance:** Use one canonical description of deliverables,
inventory and software boundaries. Update material intended for reuse, or
explicitly archive obsolete packets. Obtain the owner's accurate electronic
publication disclosure; do not infer publisher status, permissions or personal
credentials. `docs/` and `jose/` are excluded by the current MyST configuration:
these findings concern reusable auxiliary material, not a claim that its
Jekyll template is the active website.

### BR-M03 — Publication automation does not enforce the full existing evidence boundary

**Severity:** Medium. **Classification:** Regression-protection gap.
**Evidence:** `.github/workflows/build.yml` builds PDFs and runs `make lab-check`
but does not run `make verify-data`, `make pipeline-check`, or the six
`code/ch*.fsx` companions as an explicit suite. The release workflow builds and
packages without a semantic verification job. The MyST workflow deploys on
main, with no pull-request build in that workflow.

**Impact:** The project already has independent checks designed to catch shared
bad inputs and capstone/output drift, but a routine successful publishing run
does not establish that they ran.
**Correction and acceptance:** Wire the bounded oracle, package-parity,
companion and output-isolation checks into the relevant CI/release dependency
chain. Keep expensive chemistry regeneration a separately bounded,
reproducible stage. Release artifacts must identify the source commit and
corresponding evidence; adding more unconnected green jobs is not sufficient.

### BR-M04 — Updating a data figure does not invalidate the cached book outputs

**Severity:** Medium. **Classification:** Reproduced build-dependency gap.
**Evidence:** `Makefile:73-90` lists chapters, filters, preambles and manifests
as PDF/EPUB prerequisites, but not source figures. After producing a sample,
`make -n -W manuscript/figures/h2o_bond_angle.png sample` with that existing
sample's output path reported "Nothing to be done".

**Impact:** A regenerated plot can be correct on disk while the locally rebuilt
book still contains the previous plot.
**Correction and acceptance:** Declare figure dependencies for every affected
output. A figure-only change must rebuild those outputs; an unchanged source
tree should still avoid unnecessary work.

### BR-L01 — The repository's agent handoff describes a different book

**Severity:** Low. **Classification:** Production-governance defect.
**Evidence:** `.github/copilot-instructions.md` describes *What Quantum Computers
Are Actually For*, its eight units and a nonexistent local `SPEC.md`.
`.github/instructions/book-review-bootstrap.instructions.md` still contains
unfilled title, reader, artifact and audit-path placeholders.
**Correction and acceptance:** When implementation is authorised, populate the
encodings-book contract and authoritative paths, preserving explicitly
unresolved policy decisions. Do not invent a local PDF-archive policy.

### BR-L02 — Sample contents and descriptive counts are stale

**Severity:** Low. **Classification:** Reader-navigation/inventory friction.
**Evidence:** `manuscript/sample-filter.lua:1-3` promises a full table of contents,
but `manuscript/Sample.txt` omits `appendix-theory.md`; the current sample lists
Appendix A and References but not Appendix B. `README.md:67,76-77` retains
approximately 45,960 words and 175 pages. The current `make word-count` reports
46,313 whitespace-delimited source words, while the July accepted full PDF was
176 pages. Neither source-word counts nor historical page counts should be
presented as a new edition's measured pagination.
**Correction and acceptance:** Derive sample contents from the canonical
manifest, then select which chapters have full text. Generate or explicitly
date edition statistics. This does not reopen the repaired full-book
Appendix B omission: `Book.txt` and `myst.yml` include both appendices.

## Encodings-series findings and book crosswalk

**Reviewed series:** `johnazariah/quantum-workbooks`, existing *Encodings series*
session, branch `johnazariah-encodings-blog-series`, clean commit
`7e613cde70f5cf86890bb4e7817d8a86b31d0fe2`. The report identifies exactly five
branch-diff files: four draft posts and `social/encodings-hooks.md`. The series
branch is not merged into `origin/main`. Draft dates are placeholders, not
permission to publish.

In the following findings, post paths are relative to
`quantum-workbooks/docs/blog/posts/`, not to this book repository.

| Post | Draft path | Book crosswalk | Present judgement |
|---|---|---|---|
| 1 | `2026-10-06-the-antisymmetry-problem.md` | Chapters 3, 5, 22; Appendix B | Sound introductory argument after its previous revisions; favourable review is not formal author approval |
| 2 | `2026-10-13-fenwick-trees-and-bravyi-kitaev.md` | Chapters 5, 7; Appendix B | Reconstruct the indexing and occupation sets before further editorial approval |
| 3 | `2026-10-20-three-constructions-in-a-trenchcoat.md` | Chapter 7, constructor distinctions | Reframe as our proposed generalisation, not a defect in canonical BK/SRL |
| 4 | `2026-10-27-the-star-tree-theorem.md` | Chapters 7-8; remaining CA-M010 gate | Require a precisely scoped theorem and auditable evidence; no blanket framework conclusion |

### BS-H01 — Post 2's occupation set is incorrect

**Severity:** High. **Classification:** Confirmed existing draft blocker.
**Evidence:** Post 2, lines 165-173, defines `Occ(j)` as a contiguous
responsibility interval and gives `Occ(3)={0,1,2,3}`. The reviewed canonical
Fenwick construction instead gives `Occ(3)={1,2,3}` and
`Occ(7)={3,5,6,7}`. The wrong set feeds `R(j)=P(j)\setminus Occ(j)`
(lines 183-191), so this changes the constructed Majoranas rather than merely
the explanation. The book separates canonical BK's corrected `j+1` Fenwick
construction from generic custom trees (`07-six-encodings.md:173-175`;
`appendix-theory.md:75-78`).
**Correction and acceptance:** Regenerate the n=4/n=8 sets, strings and CAR
results from one named canonical implementation. Teach prefix queries and
point updates as their distinct least-significant-bit operations, with an
explicit conversion between zero-based mode indices and one-based Fenwick
indices. Correct tables, derivation and hooks together.

### BS-H02 — Posts 3-4 misattribute our proposed construction to established literature

**Severity:** High. **Classification:** Previously identified attribution and
scope blocker; still present in the frozen drafts.
**Evidence:** Post 3, lines 32,44-58,76, assigns "choose any labelled rooted
tree", its descendant-based sets and a conflation of recipes to SRL. Post 4,
lines 48,211-213, carries that attribution into its theorem and critique.
The current book instead restricts the claim to FockMap's custom
tree-to-index-set construction and treats canonical JW/BK/Parity separately
(`07-six-encodings.md:166-184`).

The existing series session's author-decision record, corroborated by local
session history on 20 August, explicitly says that **we** proposed the
arbitrary-rooted-tree/all-descendants generalisation with the
symmetric-difference ansatz; canonical BK/SRL are correct.
**Correction and acceptance:** Apply that existing decision, not a new
editorial invention: eventually retitle Post 3 *We Put Three Constructions in
a Trenchcoat* and Post 4 *One Grandchild Breaks Our Recipe*. State the exact
object being tested before the counterexample or theorem. No indictment of
SRL, canonical BK, or the entire literature follows from failure of our
extension.

### BS-H03 — The draft's path-based alternative is falsely universal

**Severity:** High. **Classification:** Current scope overclaim.
**Evidence:** Post 3, lines 231-237, says its alternative accepts any rooted
tree and produces all known encodings. Post 4, lines 52,204-215,255-261,
extends this to "no other encodings", necessary literature migration and a
settled framework question. The book explicitly limits its path-based API
to validated labelled trees with at most three children per node
(`07-six-encodings.md:189-203`; `08-building-vlasov.md:13-16,90,242`).
**Correction and acceptance:** State the actual domain and distinguish
literature results from tested implementation contracts. Remove universal
path, historical migration and framework-settled claims unless new primary
evidence supports precisely those statements.

### BS-M01 — Posts 2-4 lack primary references and executable provenance for their counts

**Severity:** Medium. **Classification:** Evidence/completeness gap.
**Evidence:** Unlike Post 1's reference section, Posts 2-4 have no outbound
references or References heading in the reviewed branch. Claims about
120 CAR checks, a 701-tree census, citation counts and a migration of the
field have no runnable series artifact. This series contains no notebooks,
executable companions or dependency pins.
**Correction and acceptance:** Cite Fenwick, original BK, SRL and only the
later tree constructions actually used; link immutable research artifacts
for the exact set/string generation and census. Either supply the evidence
for numerical and historical claims or remove them. Do not promote finite
census evidence into an unrestricted theorem.

There is **no identified supplied theorem blueprint**. The August decision
mentions using one later, but the series reviewer could not identify a
worktree artifact or supplied proof that fulfils that instruction. Treat it
as missing evidence, not as an approved report supposedly waiting elsewhere.

### BS-M02 — Post 2 converts operator weight into circuit feasibility

**Severity:** Medium. **Classification:** Misleading scaling interpretation.
**Evidence:** Post 2, lines 239,249, treats `256 -> 9` as the difference between
a feasible and impossible circuit. Its Fenwick/competitive-programming history
also needs the more precise cumulative-frequency-table provenance already
used in Post 1. The book explicitly fences the census as operator-level
evidence (`22-scaling.md:49-51,109-115`).
**Correction and acceptance:** Retain a measured worst-case Majorana/ladder
weight statement, not a molecular CNOT, runtime or feasibility conclusion.
State the effects not measured: Hamiltonian term distribution, cancellations,
tapering, compilation, connectivity and the algorithm itself.

### BS-M03 — The social mirror and complete publication payloads disagree

**Severity:** Medium. **Classification:** Draft integration defect.
**Evidence:** The series reviewer parsed both front matter and
`social/encodings-hooks.md`. Post 1 matches; Posts 2-4 each differ by a newline
in the LinkedIn text after YAML parsing. With the canonical URL appended,
the simple hook-plus-URL lengths are 288, 318, 329 and 297 characters.
The pipeline reviewer reports the actual emitted Post 2/3 payloads as 319/330
once its separator is included. Posts 2-3 exceed 300 under the actual
inline-URL delivery either way; the delivered string is the acceptance target.
**Correction and acceptance:** Choose one canonical hook source and generate
the mirror; compare parsed values, not raw YAML formatting. Validate the
actual delivery payload, including URL if sent inline. Regenerate titles,
slugs, next-post links and hooks together after the authorised narrative
repairs. A link-card-only policy is an alternative only if the publisher
actually implements it.

### Series approachability and boundaries

Post 1 supplies a useful standalone chain from fermionic antisymmetry through
CAR to parity-aware encoding. It now distinguishes the 16-state full H2 Fock
space from the six-state two-electron sector and keeps JW scaling at the
operator level. Do not flatten this successful introduction into a compressed
book abstract.

Post 2 asks a cold reader to reconcile zero/one indexing, `U/P/Occ/R`,
Majoranas and strings at once. Rebuild it around one labelled n=8 example:
query, update, recover occupation, then construct the operators. The code
artifact should establish the table, not be written later to agree with it.

Posts 3-4 currently offer a confident critique before identifying the limited
object being criticised. Their repaired sequence should introduce our
proposed extension, show a depth-two counterexample, state the exact rule and
its hypotheses, and only then explain what a theorem or census establishes.
Own the unsuccessful experiment; do not make the literature carry it.

The author has already required Post 1 approval, then corrected Post 2
approval, before Posts 3-4 are edited. This review preserves that order and
does not change any approval or scheduled date.

## Secondary website and publishing-pipeline review

The existing *Blog publishing pipeline* session supplied a separate review,
without editing or publishing. Its workflow evidence is tied to the clean
Encodings worktree at `7e613cde70f5cf86890bb4e7817d8a86b31d0fe2`; the reported
Quantum production `origin/main` was
`416f64c5229b666fd7c015bf7d0cb74c69981b30`. The series branch was seven commits
ahead and two behind that production baseline, with no PR.

The live Quantum root and sitemap were healthy. No Encodings post route or
landing page was yet published; the planned October URLs correctly returned
404. These are draft-state facts, not broken-live-post findings. The main
Jekyll site's home layout links generically to `/quantum/`
(`_layouts/home.html:51-55`); it does not automatically synchronise book
source into the four posts.

### BP-M01 — The publisher has no explicit author-approval gate

**Severity:** Medium. **Evidence:** In `quantum-workbooks`,
`.github/workflows/social-syndicate.yml:27-29,61-73,89-111` selects eligible
posts by date after a successful deployment or manual dispatch. It does not
test an author-approved/draft/status field.
**Impact:** Once drafts are integrated and become date-eligible, this mechanism
cannot enforce the owner's separate editorial approvals. The unmerged
October drafts are not claimed to have been published.
**Acceptance:** Explicit author approval, actual deployment readiness and
date eligibility must all be required for distribution, including
announcements. Preserve the existing URL/readiness logic.

### BP-M02 — A normal strict build does not exercise the future-dated series

**Severity:** Medium. **Evidence:** `mkdocs.yml:43-48` sets
`draft_if_future_date: true`; `.github/workflows/deploy.yml:31-35` runs ordinary
`mkdocs build --strict`. The review's successful strict run produced no output
for any of the four October posts.
**Acceptance:** Use a future-inclusive preview validation path that checks
these exact drafts, their links, maths, metadata and assets without publishing
them. A strict production build that excludes them cannot count as their
editorial/output acceptance.

### BP-M03 — Publication must validate the actual emitted social payload

**Severity:** Medium; overlaps BS-M03 rather than a separate prose defect.
**Evidence:** `.github/workflows/social-syndicate.yml:373-392` constructs
`hook + "\n\n" + url` for posts and submits it without the length check
used for announcements at lines 457-472. Post 2 is `231+2+86=319`
characters and Post 3 is `238+2+90=330`.
**Acceptance:** Validate the exact payload, not the hook in isolation or a
mirror joined with a different separator. Keep one executable hook source
(currently post front matter) and a generated review mirror.

The pipeline report additionally recommends binding syndication to the
deployed commit, serialising competing runs, retaining per-platform durable
IDs and visible partial-failure state, and using a stable series identifier
rather than `categories[0]` for threading. These are secondary hardening
recommendations for a separately scoped pipeline change, not newly proven
mathematical defects in the book.

The adjacent Circuit Bench 08 explicitly identifies itself as a reduced
two-qubit demonstration, not a full chemistry pipeline
(`docs/circuit-bench/08-vqe-h2/README.md:3-5`). No book-synchronisation rewrite
of that correctly labelled material is justified by this review.

**Harmonisation rule:** Correct the book first where its explanations are
wrong; derive both book and blog Fenwick material from the canonical
construction rather than making two erroneous explanations agree. Only
after approved titles and slugs are final should the series landing page,
navigation, mirrors and publication payloads be generated. No review verdict
in this file grants editorial approval or authorises publication.

---

# Apress expansion and definition-order review — 2026-09-07

**Author direction, later the same day:** Plan for an Apress book in the
approximately 300-page range; review opportunities to explain concepts more
fully, define material before use and avoid sacrificing clarity for brevity.
**Mode:** Additional review and planning only. No manuscript expansion,
chapter reordering, publisher submission or revised edition has been
implemented.
**Baseline:** Same 23-chapter source at `c5cf9df`, approximately 46,313
whitespace-delimited source words. The previous full-book correctness and
consistency findings remain open.

## Expansion findings: where the explanation needs more room

The additional pass covered the foreword, all 23 chapters and both
appendices, including code, diagrams, summaries and exercises. The specialist
had a long-line display limitation; the coordinator's earlier wrapped read
of all 76 paragraphs over 350 characters covers those same unchanged source
passages. Targeted searches and direct rereads confirmed the key late/missing
definitions below. This pass did not rerun code or verify additional external
theorems.

**Principal finding:** The book often explains the input and displays the
output, but leaves the operation between them to a helper. The extra space
is best spent turning that pattern into:
**define the objects; predict one result; work one instance; call the helper;
compare; transfer the method to a second instance.**

These are pedagogical priorities, not twelve additional claims of wrong
physics. Related correctness findings retain their original IDs and must
be resolved before the corresponding explanation is expanded.

| Priority | First substantive use / present gap | Expansion that earns its space | Observable acceptance |
|---|---|---|---|
| **XP01 — Chemical objects and units** | Ch 1 uses coordinates/pair sums at `01-electronic-structure.md:43-57`, Bohr/Ha at line 83, overlap normalisation at 129-133 and integral values at 187-200 before a sufficient units/matrix-element foundation | One symbol-and-unit card; one distance-to-nuclear-energy calculation; one orbital normalisation; atomic orbital to molecular orbital to spin-orbital to determinant; distinguish an integral from an HF orbital eigenvalue | Reader can identify and reproduce the units and meaning of every Chapter 1 input rather than recognise only its name |
| **XP02 — Fermionic action before operator-order arguments** | Ch 1 explicitly previews second quantisation, but Ch 2 requires operator ordering and CAR to reason at `02-notation.md:89-103,135-145`; full action/vacuum construction is missing or deferred | A minimal dagger/rightmost-first/CAR inset before Ch 2's reasoning; complete vacuum, occupation-state and signed ladder-action construction in Ch 5 | Reader calculates a permitted creation, a forbidden creation, an annihilation and a signed product without guessing |
| **XP03 — Actual code literacy** | First factory at `02-notation.md:203-220` uses pipelines, parsing, options, maps and pattern matching; line 175's "read let as define" does not prepare the reader | Trace a real two-index and four-index key through every intermediate value; explain missing versus zero; label excerpt dependencies and give a complete entry point | An F# novice predicts the function's output and runs the example without supplying unexplained variables |
| **XP04 — Pauli and representation foundations** | Pauli products, tensors and weight enter Ch 4 before the closing glossary; matrices are in Ch 9, multiplication in Appendix B, general JW in Appendix B | Move matrix/product/tensor definitions forward; work one labelled operator action; define support/weight; use a small label/integer/vector-row conversion card | Reader multiplies Pauli factors, calculates weight and locates the state in the declared basis before seeing a cost or matrix claim |
| **XP05 — Complete coupling calculation** | `06-building-hamiltonian.md:18-29` starts with density matrices; at 114-149 the decisive Pauli collection is asserted rather than shown | Start with a two-configuration pure-state energy; compare its mixture; introduce trace/density notation; expand one monomial and collect all four contributions; diagonalise the closed-shell block | Reader reconstructs a positive and negative coupling sign and the energy lowering, not just the library call |
| **XP06 — A working Fenwick lesson** | `05-visual-encodings.md:193-249` needs XOR, prefix queries, storage intervals, update ancestry, recovery and complexity together; BM-M04 identifies wrong steps | Use one eight-mode occupation vector throughout; teach XOR/lowbit, zero/one indexing, stored bits, query, update and inverse recovery as different operations | Reader computes the entire stored vector, answers a prefix query, updates one occupation and recovers occupations; diagram and code agree |
| **XP07 — Tree to Majoranas to ladders** | Majorana bounds at `05-visual-encodings.md:281-283` precede any operational Majorana definition; `08-building-vlasov.md:90-100` delegates construction to the helper | Define Majoranas and inverse pairing before a quantitative bound; enumerate one tree's qubit nodes, labelled terminal paths, selected strings, pairing and phase convention; complete the CAR calculation | Reader derives one encoded ladder without calling the encoder and accounts for every qubit, mode and selected path |
| **XP08 — Uninterrupted molecular tapering** | Chs 10-12 explain deletion after a sign is chosen, but `12-clifford-tapering.md:153-166` labels literal signs as derived without showing the derivation | Carry one H2 number/spin-parity ledger through generator action, binary row reduction, independent commuting selection, signed Clifford conjugation and sector matrix comparison | Reader justifies each sign without choosing the lowest sector energy and states what the parities leave undetermined |
| **XP09 — Quantities, units and meaning of error bounds** | Local/global errors and a commutator norm precede an operator-norm definition; the rotation exponential appears after earlier angle calculations | Define the norm and atomic time; work a two-term refinement example; derive local-to-global scaling and the Rz factor of two; separate model, pruning, simulation, sampling and phase-resolution budgets | Reader knows what each tolerance bounds, in what units, and what it cannot establish |
| **XP10 — VQE before optimisation jargon** | `20-algorithms.md:40-57` uses ansatz; the explicit gloss is at line 260. Lines 78-94 use variance/allocation without elementary statistics | One normalised, sector-appropriate trial family and energy curve; a small record of measurement outcomes; mean/variance/standard error; independent allocation and a grouped covariance counterexample | Reader calculates an estimate and uncertainty and separates ansatz bias, optimiser stopping and sampling error |
| **XP11 — Phase decoding and actual import** | QPE uses controlled powers/inverse QFT at `20-algorithms.md:166-207`; Ch 18 exports before Ch 21's interchange explanation | One-control phase kickback; a two-bit exactly representable QFT example; shifted H2 energy-to-phase-to-integer-to-energy decode; one named/versioned import round trip with a negative control | Reader explains the phase sign/branch and distinguishes parsing, circuit equality, simulation error and energy validation |
| **XP12 — Fulfil the teaching route** | Missing exercise sets, delayed definitions in appendices, and capstone claims that exceed the deliberately untapered/reference split | Input/output checkpoints; bounded exercises and selected worked answers; retrieval-oriented appendices; accurate objectives and final inventory | Reader can locate the provenance and scope of every reported energy/circuit/result and complete the chapter's stated learning task |

## Define-before-use register

**Reading key:** **Keep** means the introduction already does useful work;
**preview** is a legitimate destination, not an illicit use; **bridge** means
a definition exists but needs an operational example or local prerequisite;
**move** means useful material arrives too late; **missing** means no adequate
operational definition was located. An assumed-QM or linear-algebra fact may
need a short recap, not a new foundations chapter.

Locations below are relative to `manuscript/`. The proposed home is where
the definition should support the first operation, not where a glossary
could list the word.

### Chemistry, states and units

| Concept | First substantive use | Current explanation | Treatment / proposed home |
|---|---|---|---|
| Coordinates, particle counts and pair sums | `01-electronic-structure.md:43-57` | Interaction table, incomplete symbol legend | **Bridge / Ch 1:** distinguish nuclear/electronic indices, positions, masses and sums over unordered pairs |
| Atomic units, Bohr and hartree | `01-electronic-structure.md:83` | Atomic units named later at `03-spin-orbitals.md:155`; hbar=1 at `14-time-evolution.md:45` | **Missing operational convention / Ch 1 before calculation:** identify length/energy units and Coulomb convention, then work the conversion |
| Atomic time and dimensionless exponent | `15-trotter-formulas.md:46-64` | hbar=1 earlier; atomic-time description later in Ch 20 | **Bridge / Ch 14:** time unit is hbar divided by hartree; energy-times-time divided by hbar is dimensionless |
| Basis functions and STO-3G | `01-electronic-structure.md:95-117` | Defined there | **Keep:** add AO/MO labels, not another general basis-set essay |
| Molecular orbital and overlap | `01-electronic-structure.md:127-135` | Overlap S named at line 133 | **Bridge / Ch 1:** normalise one sum explicitly, stating normalisation of constituent orbitals |
| Spin-orbital | `01-electronic-structure.md:137-145`; spin integration in Ch 3 | Early table; fuller definition `03-spin-orbitals.md:15-36` | **Keep and bridge / Ch 3:** display spatial-times-spin function before integrating spin overlaps |
| Configuration and occupation vector | `01-electronic-structure.md:150-159` | Defined with concrete table | **Keep:** identify the antisymmetric many-electron state represented by the label |
| Slater determinant | Needed by configuration/HF discussion; explicit determinant language in `06-building-hamiltonian.md:33` | Single-determinant gloss later in `19-bond-angle.md:202`; no construction found | **Missing / Ch 1:** one normalised two-electron determinant, with determinant algebra only as far as needed |
| HF, RHF and FCI | `01-electronic-structure.md:154-173`; RHF provenance `03-spin-orbitals.md:162` | HF single-configuration gloss and finite-basis FCI scope early; restricted HF explanation at `18-complete-pipeline.md:173-175` | **Bridge / Ch 1 and before Ch 3 provenance:** distinguish orbital optimisation from configuration coefficients; define closed-shell restriction |
| One-body integral | `01-electronic-structure.md:187-200` | Numeric physical labels, no adequate one-electron matrix-element definition found | **Missing / Ch 1:** write the one-electron operator and its matrix element; distinguish orbital energy from integral |
| Chemist/physicist two-electron integrals | `02-notation.md:25-49` | Definitions and coordinate grouping there | **Keep:** add electron separation and local spatial-index legend |
| Raw, weighted and antisymmetrised coefficients | `02-notation.md:135-145,187-201` | Raw/weighted API contract is present; restricted-sum explanation is wrong | **Correct then bridge / Ch 2:** three equivalent representations of one nonzero-exchange example |
| Interleaved/blocked spin indices | `03-spin-orbitals.md:65-80` | Explicit tables and convention warning | **Keep:** work one odd-index floor/remainder conversion |
| Delta and spin selection | `03-spin-orbitals.md:88-94,111-126` | Same-spin explanation and allowed blocks | **Bridge / Ch 3:** define the two delta cases and derive the spin overlap |
| Slots, allowed/nonzero entries and unique values | `03-spin-orbitals.md:36,59,116-128,241-247` | Counts conflated in places | **Correct then bridge / Ch 3:** separate four counts and raw tensor entries from surviving operator monomials |
| Total spin, spin projection, singlet/triplet | Labels at `01-electronic-structure.md:155,158`; conservation at `03-spin-orbitals.md:46` | No sufficient sector-oriented introduction located | **Bridge / Chs 1/3:** spin projection from alpha/beta counts; equal counts do not alone imply a singlet |
| Correlation energy | `01-electronic-structure.md:161-163` | Definition early; improved account in Ch 6 | **Keep and work / Ch 6:** two-configuration energy and a consistent electronic/total reference |
| Active space and frozen core | Exercise `07-six-encodings.md:252` | Explanation later at `19-bond-angle.md:64-69` | **Late dependency:** supply transformed inputs and constant offset first, or move the exercise to a later optional project |

### Fermionic/Pauli algebra, representations and data structures

| Concept | First substantive use | Current explanation | Treatment / proposed home |
|---|---|---|---|
| Second-quantised Hamiltonian | `01-electronic-structure.md:171-179` | Explicitly a preview | **Preview — keep:** do not turn Chapter 1 into a full operator-algebra derivation |
| Dagger and rightmost-first action | `02-notation.md:89-103` | Creation/destruction gloss in Ch 1 | **Prerequisite recap / Ch 2 before ordering:** adjoint, reversed product under adjoint, one action calculation |
| CAR and anticommutator braces | Operator-order reasoning `02-notation.md:97-145`; displayed CAR `05-visual-encodings.md:62` | Commutation glossary late in Ch 4; consequences in Ch 5 | **Bridge / Ch 2 inset, full Ch 5:** define braces and work equal/distinct-index cases |
| Fock space and vacuum | Sector/ladder reasoning in Ch 5; vacuum first named in `09-verification.md:187` exercise | Occupation summary in Appendix B | **Missing operational construction / Ch 5, minimal Ch 2 inset:** sectors of different particle number; vacuum is not the zero vector |
| Full signed ladder action | Needed for Ch 2 ordering and `06-building-hamiltonian.md:86-109` | Particular JW action in Ch 5; general formula in `appendix-theory.md:58-68` | **Move/derive in Ch 5:** both signs and forbidden-occupation zero cases before Hamiltonian expansion |
| Pauli matrices and multiplication phases | `04-qubits-gates-circuits.md:49` | Actions there; matrices `09-verification.md:33`; phase rules in Appendix B | **Move / Ch 4:** matrix/action link and a short multiplication table before products |
| Tensor action and general states | Bell construction `04-qubits-gates-circuits.md:86` | Closing box at line 166, with BM-M03 error | **Move and repair / Ch 4:** product basis versus general superposition; one two-qubit action |
| Commutator brackets | Pauli/encoding reasoning; later explicit binary tests | Closing Ch 4 glossary provides definitions | **Keep fact, move support earlier:** one XY/YX calculation before algebraic reliance on commutation |
| Pauli string, support and weight | `04-qubits-gates-circuits.md:123-149` cost inference | String glossary at 166; operational weight in `05-visual-encodings.md:153-162` | **Move / Ch 4 before cost:** count nonidentity positions; distinguish a sum of strings from one string |
| Displayed label, occupation integer and matrix order | Occupation labels in Ch 1; tensors in Ch 4 | Full convention box `05-visual-encodings.md:15-28`; matrix rule in Ch 9 | **Keep contract; stage it:** small worked card in Ch 4, brief reminders in Chs 5/9/21 |
| Number operator | `05-visual-encodings.md:340-346` | Definition and JW image there | **Keep; finish derivation:** reproduce cancellation using already-taught ladder algebra |
| Big-O, Theta, worst/average cases | Ch 1 motivational complexity; calculations in Ch 5 | Verbal descriptions without a compact data-structure primer | **Preview early, bridge in Ch 5:** identify what operation is counted, logarithm depth and constant/finite-size distinctions |
| XOR and prefix parity | `05-visual-encodings.md:212-239` | XOR name and storage picture | **Bridge / Ch 5:** truth table and one occupation parity before tree queries |
| Fenwick storage/query/update/recovery | `05-visual-encodings.md:239-249` | Qualitative three-question table; j+1 reminder later | **Rebuild / Ch 5:** lowbit and zero/one indexing, different traces for each operation |
| Cumulative-parity encoding | API selection `07-six-encodings.md:103-110`; physical use in Ch 10 | Short Appendix B gloss | **Bridge / Ch 7:** encode and invert one occupation vector; state where total parity is stored |
| Unitary equivalence/conjugation | `07-six-encodings.md:78-88` | Described as basis change | **Prerequisite recap / Ch 7:** transform states as well as operators; spectra do not determine the change of basis |
| Majoranas and inverse pairing | Bound `05-visual-encodings.md:281-283` | Named but not operationally defined in Ch 8/Appendix B | **Missing:** define briefly before bound or defer bound; complete construction at start of Ch 8 |
| Root, terminal path, node, leaf, depth and breadth-first order | Ch 5 tree preview; `08-building-vlasov.md:35-81` | Pictures and child-index formula | **Bridge / Ch 8:** distinguish qubit nodes, path endpoints, Majoranas and modes; enumerate one tree |
| Generic custom index sets versus BK versus path maps | `07-six-encodings.md:133-205` | Substantially correct scope notes | **Keep; exemplify:** finite census, enforced input restriction and general theorem are different evidence |

### Symmetry, algorithms, statistics and extensions

| Concept | First substantive use | Current explanation | Treatment / proposed home |
|---|---|---|---|
| Z2 generator | `10-why-tapering.md:35-43` | Operational square-to-identity definition later in Ch 12 | **Move / Ch 10:** binary eigenvalues, commutation and conservation before sector inference |
| Parity sector versus charge/spin sector | `11-diagonal-z2.md:93-110` | Sector list and warnings | **Bridge / Chs 10-11:** enumerate surviving charges/spins and derive the actual molecular signs |
| Diagonal substitution | `11-diagonal-z2.md:118-147` | Rule and term table | **Keep:** add the original-to-remaining qubit map and matching sector spectrum |
| GF(2), pivots, rank/nullity and null space | `12-clifford-tapering.md:70-86` | Binary Pauli table and XOR row-reduction analogy | **Bridge / Ch 12:** one complete binary elimination, free variables and phase information kept separately |
| Centralizer and independent commuting subgroup | `12-clifford-tapering.md:70-76` | Correct distinction | **Keep; demonstrate:** two candidates commute with H but not with one another |
| Clifford conjugation and S gate | Preview Ch 10; synthesis `12-clifford-tapering.md:92-129` | Conjugation table/example in Ch 12 | **Bridge before synthesis:** signed-Pauli preservation, S matrix and a Y-phase example |
| Expectation, coherence, mixture, density matrix and trace | `06-building-hamiltonian.md:18-29` | Formula supplied immediately | **Bridge / Ch 6:** pure-state expectation first, matched-population mixture second, compact density notation third |
| Coefficient 1-norm | `06-building-hamiltonian.md:252-256` | Explicit coefficient sum | **Keep:** do not silently reuse it as an operator or measurement norm |
| Operator norm, local/global unitary error | Ch 14 formulas; `15-trotter-formulas.md:164-196` bound | Good distinctions, no operational norm definition found | **Bridge / Ch 14:** state the quantity, domain, units and permissible inference before a numerical bound |
| Rotation-angle convention | Coefficient/angle calculation in Ch 15; staircase Ch 16 | Explicit exponential at `18-complete-pipeline.md:118-122`, again Ch 21 | **Move / Ch 4 rotations and Ch 15 reminder:** derive rather than memorise the factor of two |
| Global/relative phase and controlled identity | `15-trotter-formulas.md:40-44` | Useful warning; ordinary phase action earlier | **Bridge / Ch 14-15:** show how controlling a global phase makes it relative between control branches |
| Ansatz and variational principle | `20-algorithms.md:40-57` | Explicit ansatz gloss at line 260 | **Move/work / start of Ch 20 VQE:** one normalised state family, objective and expressivity limit |
| Shots, mean, variance, standard error and confidence | `20-algorithms.md:78-94` | Shots defined; variance assumed | **Bridge / before allocation:** a binary random variable and a short measurement record; uncertainty is not bias or a stopping condition |
| Group covariance | Grouping at `20-algorithms.md:61-73`; estimator at 80-94 | No adequate definition found | **Missing / Ch 20:** group estimator and covariance before reusing an independent-shot formula |
| QWC versus general commutation | `20-algorithms.md:61-71` | Definition and direct five-basis partition | **Keep:** one globally commuting but non-QWC pair clarifies the boundary |
| Controlled evolution and phase kickback | Preview Ch 14; operational Ch 20 | Shifted eigenphase supplied | **Bridge / Ch 20:** derive one-control action on a superposition before controlled powers |
| QFT/inverse QFT | `20-algorithms.md:180` | Purpose named, transform not defined | **Missing / Ch 20:** normalised transform and a two-bit example before the phase-decoding algorithm |
| Eigenstate overlap | Ch 9 amplitude; QPE Ch 20 | Squared amplitude stated | **Keep/bridge:** identify the inner product and distinguish overlap success from resolution/confidence |
| Native gates, routing and depth | Ch 4 cost caveats/SWAP inference | More operational account late in Ch 21 | **Preview/bridge:** small logical/physical map when used; detailed routing example in Ch 21, not a platform catalogue |
| QASM 2/3, Q# ownership and JSON | Export calls in Ch 18 | Interchange examples in Ch 21 | **Preview in Ch 18; operational in Ch 21:** versioned importer, schema, caller allocation, angle and phase contract |
| Bosonic cutoff and CCR boundary | `23-whats-next.md:70-78`; Appendix B | d has conflicting meanings; finite boundary omitted | **Optional topic, mandatory local definitions if retained:** levels, ladder factors, unused states and cutoff error |
| Normal ordering and mixed species | Appendix A APIs and Appendix B final section | Labels and algorithm instruction only | **Optional bridge:** one CAR/CCR reorder; distinguish different particle species from symmetry sectors |

### Code prerequisites and forward references

| Dependency | Current first use | Required treatment |
|---|---|---|
| Coefficient factory; strings/tuples/arrays; pipelines and function application | `02-notation.md:203-220` | Define the mathematical lookup first; trace a concrete key through parsing, permutation and lookup; state the output type |
| Map lookup, Some/None and zero | Same factory | Explain absence versus present zero; do not imply every missing or malformed key is a physical zero |
| Package/module/script setup | Ch 2 package note; `03-spin-orbitals.md:193-204` | Explain #r, #load, open, working directory and escaped module name before the first entry point uses them |
| Unsigned indices, 4u and conversions | `03-spin-orbitals.md:193-204` and later | Briefly identify type versus physical quantity; demonstrate safe index conversion once |
| The `encoders` value | Loop in `07-six-encodings.md:43-51`, before its list is supplied | Move its definition before the first runnable use or identify the snippet as a contextual excerpt with a named dependency |
| Tree recursion, options and records | `08-building-vlasov.md:35-81` | Show the base case and one recursive step beside a tree; explain record fields without assuming FP expertise |
| Tapering record updates and literal sector | `12-clifford-tapering.md:153-166` | Teach the record operation locally, but derive the physical sector before putting its signs into a record |
| Python/PySCF geometry-to-result snippet | `19-bond-angle.md:75-101` | A small code/object bridge when introduced; identify what `mol`, RHF orbitals and FCI result represent, rather than assuming F# literacy transfers to an unexplained Python API |

### Definition and notation stress cases

The register is a substantial first-use audit, not a claim that a keyword
list certifies every symbol in a future edition. During implementation,
attach a local notation contract to each new derivation. In particular:

- Overlap S, total spin S and the S gate must not be inferred to be the same
  object because the letter is reused.
- Spin labels alpha/beta and state amplitudes alpha/beta need local context.
- Electron count, number of spin-orbitals, number of Pauli terms and number
  of product-formula steps must have stated roles/ranges rather than an
  unannounced reuse of N or n.
- An index on a spatial integral is not automatically a spin-orbital index.
  An output qubit after tapering is not automatically the original orbital.
- A coefficient-list norm, operator norm, energy uncertainty and unitary
  error have different meanings and units; define the bounded quantity
  before substituting a number.
- "Exact" must identify the object and scope: symbolic Pauli phases,
  finite-basis FCI, a sector restriction and approximate time evolution are
  not one kind of exactness.

## The expanded reader contract

The extra space should complete the explanation, not multiply introductory
claims, repeat warnings or turn API tables into prose. The book should let
the intended reader answer, before being asked to manipulate a new object:

1. What is it, in the problem we are solving?
2. What do its symbols, indices, units and conventions mean?
3. What is one small instance, and how does it behave?
4. Which operation are we about to perform on it, and why is that operation
   valid under the stated assumptions?

This is not a demand to re-teach all prerequisite linear algebra or
introductory quantum mechanics. It is a demand not to smuggle in chemistry,
second quantisation, Pauli algebra, unfamiliar programming constructs,
statistics or data structures as though those were already prerequisites.

**Define-before-use means before substantive use.** A hook may name VQE,
correlation energy or a tree encoding as a destination, provided it gives
enough ordinary-language context and does not ask the reader to reason with
its undefined machinery. The full operational definition belongs at the
first calculation, code call, algorithmic claim or exercise that needs it.
A late glossary entry is not a substitute. Conversely, spelling out every
future definition in Chapter 1 would destroy the problem-first progression.

For a reader returning to a later chapter, provide a short local reminder
and a precise cross-reference to the canonical definition. Keep one meaning
and one convention, not repeated competing introductions.

## Indicative 300-page allocation, not a pagination prediction

The author's target is not represented as an Apress requirement or accepted
production specification. The 176-page comparison is the existing full-book
build, not an Apress layout. The current Makefile uses 11-point type and
one-inch margins with locally chosen fonts; no Apress trim size, page design,
code style or treatment of figures has been established here.

Multiplying current words by `300/176` would yield about 78,943 words, but
that is only a same-density arithmetic extrapolation. It is **not** the
recommended way to determine what the book needs. Source counts include
code, tables and markup; explanatory diagrams, displayed equations, worked
solutions and code listings have very different page costs.

The following is a **content-space budget** for discussion. Chapter numbers
are retained. Each chapter allocation includes its opening, figures, code,
worked examples, summaries and exercises; do not add those again. No extra
chapter-opening or part-divider allowance is hidden outside the total.

| Part | Chapters | Allocated pages |
|---|---|---:|
| I — The Molecule | 1-3 | 36 |
| II — The Machine | 4 | 12 |
| III — Encoding | 5-9 | 62 |
| IV — Tapering | 10-13 | 34 |
| V — Circuits | 14-17 | 38 |
| VI — The Pipeline | 18-21 | 48 |
| VII — Horizons | 22-23 | 12 |
| **Chapter content subtotal** | **23 chapters** | **242** |
| Front matter | Rights, contents, preface, how to use the book | 10 |
| Code-reading bridge | Proposed unnumbered material before substantive code use | 8 |
| Appendix A | Selected practical API/entry-point reference | 8 |
| Appendix B | Mathematical/convention summary, symbol and term lookup | 10 |
| Selected worked solutions | Proposed reader-facing back matter | 12 |
| References | Consolidated cited works | 6 |
| Index | Proposed production allowance | 4 |
| **Supporting matter subtotal** | | **58** |
| **Indicative total** | | **300** |

The code bridge and solutions/index are recommended components, not approved
new manuscript files or changes to the chapter count. Short explanations
must still appear where needed in chapters; eight pages of introductory
code material cannot define every later API in advance. A separate
instructor solution set can be longer than the printed selected answers,
but no essential prerequisite explanation should be outsourced to it.

### Chapter-level allocation and intended learning gain

Current word counts are source measurements, not estimates of current
typeset chapter pages. Target pages are allocations for the **whole revised
chapter**, not pages to add.

| Ch | Current source words | Target pages | Learning gain that earns the space |
|---|---:|---:|---|
| 1 | 3,129 | 12 | Basis functions to molecular orbitals, determinants and declared energy/model conventions |
| 2 | 2,702 | 14 | Coordinate/index conversion and raw, antisymmetrised and weighted coefficient conventions with checked examples |
| 3 | 2,596 | 10 | Explicit spin-index maps, selection rules and storage/allowed/unique-entry counting |
| 4 | 2,208 | 12 | State vectors, coherence, gates and tensor-basis order without false foundational shortcuts |
| 5 | 3,449 | 16 | Occupation, parity queries and updates on one JW/BK example, with correct diagrams |
| 6 | 1,811 | 14 | A complete coupling expansion, cancellation and assembly of the canonical H2 operator |
| 7 | 2,097 | 8 | Like-for-like encoding comparison and clear representation/CAR assumptions |
| 8 | 1,865 | 14 | Tree nodes, paths, Majoranas and one fully checked ladder operator |
| 9 | 1,818 | 10 | Matrix, labelled-state, sector and spectrum evidence, with diagnostic counterexamples |
| 10 | 1,487 | 6 | Conserved quantities and exactly what parity does and does not fix |
| 11 | 1,523 | 8 | An explicit sector calculation from the original operator to a checked reduction |
| 12 | 2,200 | 14 | Physical molecular sector, binary linear algebra and Clifford conjugation in one complete path |
| 13 | 1,232 | 6 | Interpretation of scoped generated comparisons, rather than unsupported benchmark expansion |
| 14 | 1,727 | 10 | Separate measurement, preparation and simulation, then motivate noncommuting evolution |
| 15 | 1,611 | 12 | Local/global product-formula error, units, norm definitions and a stated resource budget |
| 16 | 955 | 8 | Trace basis changes, parity accumulation, phase rotation and uncomputation |
| 17 | 727 | 8 | A reproducible logical-cost comparison and its explicit limits |
| 18 | 1,912 | 12 | Trace the implemented pipeline, data provenance and the boundaries of each stage |
| 19 | 2,017 | 10 | Interpret fixed-coordinate reference chemistry, sampled minima and basis limitations |
| 20 | 2,372 | 16 | A small VQE estimator with its assumptions and a worked shifted-QPE phase decode |
| 21 | 1,633 | 10 | One named format/importer route with order, phase and caller-ownership conventions |
| 22 | 895 | 6 | Read resource assumptions and compare cost categories without inventing a crossover threshold |
| 23 | 1,618 | 6 | Scoped extensions that build on the taught concepts without promising another textbook |
| **Total** | **43,584** | **242** | |

The intentionally larger allocations go to the transitions that currently
hide reasoning: conventions, alternative encodings, coupling expansion,
physical-sector tapering and algorithmic measurement. The short chapters
should not all be inflated equally. Some may need better examples rather
than much more prose.

### Editorial prescription for every chapter

This complements the page allocation: retain successful explanations,
complete the specific missing operation and move or defer what interrupts
the prerequisite chain. None of these recommendations changes the manuscript
structure without implementation approval.

| Source | Keep | Add or work through | Move, defer or narrow |
|---|---|---|---|
| Foreword | Personal framing, audience and problem-first intent | Reading/running routes and an honest two-track deliverable | Do not assume chemistry, FP, statistics or data-structure fluency; promise only delivered exercises and computations |
| Ch 1 | Born-Oppenheimer, finite-basis exactness, occupation table | Units, symbols, orbital normalisation, determinant/HF/FCI comparison and one-body matrix element | Leave second quantisation a scoped preview; do not survey or implement every classical chemistry method |
| Ch 2 | Coordinate grouping and index-shuffle motivation | Minimal operator-action inset; corrected prefactor example; traced lookup function | Place code bridge before factory; defer builder execution to Ch 6; put migration history in a compatibility note |
| Ch 3 | Interleaved table and concrete cross-spin lookup | Product spin functions, delta derivation, allowed/forbidden entry and count ledger | Move the Pauli-output exercise after encoding is taught; keep previews recognisable as previews |
| Ch 4 | Probabilities, CNOT action and Bell preparation | Pauli matrices/products, general tensor states, order card, phase and rotation conventions | Move the closing prerequisite glossary forward; leave staircase derivation to Ch 16 |
| Ch 5 | Fermion-sign example and three bookkeeping questions | Full ladder/JW action; XOR, lowbit, stored bits/query/update/recovery; light asymptotic primer | Define Majoranas briefly before a bound or defer that bound to Ch 8; trim duplicate decision tables |
| Ch 6 | Corrected coefficient table and correlation interpretation | Pure-state to mixture/trace bridge; full coupling collection and closed-shell block | Remove repair-history prose; put the all-six comparison exercise after its interface is introduced |
| Ch 7 | Interface unification and distinct implementation scopes | Explicit basis-map and cumulative-parity example; named function contract | Define `encoders` before using it; defer custom-tree and frozen-core tasks until their inputs/methods exist |
| Ch 8 | Concrete tree-shape comparison and breadth-first child rule | Tree vocabulary, enumerated paths/Majoranas/pairing, one ladder and complete CAR check | Keep literature correspondence, empirical census and API support separate; avoid an unprovided all-size theorem |
| Ch 9 | Independent reference, sectors and separate electronic/total energies | Direct occupation matrix, permutation counterexample, labelled-state checks and HF block | Narrow spectrum-only guarantees; do not teach vacuum for the first time in an exercise |
| Ch 10 | Motivation and exact-within-sector explanation | Z2 before use, conserved generator action, parity/charge/spin counterexample | Leave binary elimination to Ch 12; point-group theory can be an optional preview |
| Ch 11 | Term-by-term substitution and labelled exploratory helper | Derived physical signs, remaining-qubit label map, matching sector spectrum and exercises | Put sector selection before molecular application; do not let arbitrary toy signs imply a physical selection rule |
| Ch 12 | Binary Pauli table, centralizer warning and Heisenberg example | Row reduction, rank/nullity, signed conjugation and one H2 sector reduction | Put the literal sector record after its derivation; defer stabilizer-code implementation |
| Ch 13 | Scoped toy comparisons and metadata requirements | One fully specified before/after record and interpretation exercises | Replace removed-table history with current scope; no invented molecule benchmarks |
| Ch 14 | Energy-to-unitary progression | Measurement/preparation/evolution comparison, noncommuting example and meaning of error | Keep qubitization a defined, scoped alternative rather than an unearned complexity table or new full pipeline |
| Ch 15 | Canonical logical count and dimensionless/energy distinction | Operator norm, atomic time, explicit rotation list, refinement and error ledger | Higher nested-commutator theory optional; mark supplied hypothetical exercises honestly |
| Ch 16 | ZZ then XXYY progression | Basis-state trace, bit XOR versus eigenvalue product, phase accumulation, uncomputation and exercises | Replace extra weight tables with an explanation of why the circuit works |
| Ch 17 | Logical-only comparison and provenance requirements | Same-width Hamiltonians with different weight distributions; full per-step/total cost ledger | Remove stale release-withholding prose; no blanket inference from ladder maxima |
| Ch 18 | Two-track implementation and safe untapered baseline | Artifact checkpoints, symbol-to-code types, direct/skeleton comparison and an optional validated taper branch | State the actual scope; export here is a preview until Ch 21; no promise that every earlier concept is integrated |
| Ch 19 | Fixed bond, named PySCF backend and basis limitations | Geometry diagram, AO/MO/PySCF bridge, sampled versus fitted minimum and constrained curvature | Full normal-mode analysis optional; remove the unrelated skeleton-speed claim |
| Ch 20 | Direct QWC partition, identity sampling rule and shifted phase relation | One ansatz/energy curve, outcome statistics/covariance, phase kickback, small QFT and complete energy decode | Move ansatz definition forward; defer optimisation and mitigation surveys |
| Ch 21 | Order boundary and final verification checklist | Named import round trip, Y/rotation/order negative control, phase policy, allocation and small routing example | Replace universal portability with actual versioned compatibility; avoid a vendor catalogue |
| Ch 22 | Operator/molecule distinction and restrained FeMo active-space context | Resource-category diagram, determinant/dense-memory example and acronym glosses | Do not infer molecular totals or teach full classical solvers merely to reach a page allocation |
| Ch 23 | Translation layer is necessary but not the whole computation | Short accurate extension cards; local bosonic definitions if quantitative content remains | Defer full ADAPT, mitigation, vibronics and QEC implementations; inventory only what has actually been built |
| Appendix A | Selected signatures and raw/weighted distinction | Argument/return contracts, taught Trotter/export entry points and complete-example destinations | Drop exhaustive-API claims; do not reproduce the entire library documentation |
| Appendix B | Phase closure and convention reminders | Retrieval tables for operators, Majoranas, sectors, norms and cutoff; explicit external destinations | Teach essential definitions in their chapters first; do not relabel summaries as proofs |

### Required local moves, not a wholesale restructure

Retain the problem-first progression. Chapters 1-3 establish the chemical
object, with a short operator-action/CAR inset before Chapter 2's ordering
reasoning and the code bridge before its factory. Chapter 4 establishes
Pauli matrices, tensors and angle conventions. Chapter 5 supplies the full
fermionic action/JW construction and practical Fenwick arithmetic. Chapter 8
then derives tree encodings from Majoranas rather than introducing the word
only inside an unexplained helper.

Carry one H2 generator/sign ledger across Chapters 10-12. Carry one
operator/angle/error ledger across Chapters 14-17. Chapters 18-21 retain
their current order but explicitly distinguish construction, classical
reference, algorithm and import evidence. A short algorithm-orientation box
in Chapter 14 is enough; moving all of Chapter 20 earlier is not necessary
for these improvements and would require a separate structural decision.

### Exercise dependencies and selected answers

The longer edition should use exercises to consolidate taught operations,
not make the learner discover an absent prerequisite. Preserve legitimate
conditional examples: a supplied hypothetical term count/weight in Chapters
4 or 15 is not an unsupported molecular benchmark if it is labelled as an
assumption. Similarly, arbitrary-sign diagonal toys, the deliberately
one-generator Heisenberg example and the fixed-bond water calculation are
valid within their stated scopes.

The actual dependency failures need action:

| Location | Required repair before assigning it |
|---|---|
| Ch 2 conversion/prefactor exercises | Correct the symmetry hint and supply distinguishable data for error diagnosis |
| Ch 3 Pauli-output question | Move after Ch 6 or explicitly defer it; correct spin-allowed count answers |
| Ch 6 sign and six-encoding exercises | First show the general JW expansion; fix term indices; supply the encoder list or move the comparison to Ch 7 |
| Ch 7 custom/frozen-core tasks | Supply a complete CAR harness and transformed frozen-core data/offset, or make them later optional projects |
| Ch 8 topology/comparison tasks | State a complete topology, mode/pairing convention and stopping range; specify what equality is being tested |
| Ch 9 vacuum/HF/expectation questions | Define vacuum earlier; resolve spatial/spin notation; supply the state or prior two-by-two derivation |
| Ch 10 point-group extension | Label as an optional extension with enough source material, not an unstated chemistry prerequisite |
| Ch 15 hypothetical resource question | State time units and which quantity determines the step count; do not infer it from a coefficient norm alone |
| Ch 17 benchmark-design question | Supply a metadata schema and define the comparison metric, rather than asking whether an encoding "beats" another in the abstract |

For the eleven currently missing exercise sets, prefer a prediction or
interpretation and a trace/calculation of the new operation. Add an optional
project only where useful; identical three-question templates are not a goal.
Selected answers should prioritise integral conversion, spin counting,
fermion signs, Pauli collection, Fenwick operations, Majorana pairing,
molecular sector signs, Trotter units/bounds, staircase phase, VQE covariance,
QPE unwrapping and export-order failures. Show intermediate states and
conventions rather than only final numbers or "run the script".

### Visuals with an instructional job

| Visual | Home | Operation made visible |
|---|---|---|
| AO to MO to spin-orbital to determinant | Ch 1 | Distinguish functions, orbitals and many-electron states |
| Electron-coordinate colouring of four indices | Ch 2 | Perform the convention shuffle |
| Four spin blocks and count ledger | Ch 3 | Separate storage from allowed entries |
| Label/integer/row card and matched-population states | Chs 4/6, reused 9/21 | Resolve order and distinguish coherence from a mixture |
| Separate Fenwick storage, query, update and recovery panels | Ch 5 | Execute different operations without conflating tree paths |
| Coupling collection table | Ch 6 | Account for phase signs and cancellations |
| Nodes to terminal strings to Majorana pairs | Ch 8 | Explain every tree count and the resulting ladder |
| Sector decomposition and Clifford label map | Chs 10-12 | Identify which states are retained and how labels change |
| Measurement versus evolution of the same Pauli string | Chs 14/20 | Use the correct circuit primitive |
| Compute-rotate-uncompute state trace | Ch 16 | Derive the factor of two and parity action |
| Two-track artifact/provenance diagram | Chs 18/19 | Separate constructed circuits from calculated reference energies |
| Phase-kickback circuit and phase-unwrapping scale | Ch 20 | Decode a signed energy |
| Logical/physical map across a SWAP | Ch 21 | Interpret routing rather than merely count added gates |
| Truncated bosonic ladder with top boundary | Optional Ch 23/B | Understand what truncation changes |

These are candidate teaching figures, not a requirement to add fourteen
unrelated illustrations. Combine or reuse panels when they serve the same
operation. They are included in the page budgets, not an extra page tranche.

### Core expansion versus optional scope

The core additions are the unit/state/code foundations, ladder and Pauli
action, correct Fenwick arithmetic, one actual tree/Majorana construction,
full coupling collection, physical-sector tapering, an error ledger, small
VQE/QPE demonstrations, a named import route and fair exercises/answers.
Those additions explain the book already promised.

Full PREPARE/SELECT/qubitization implementation, an all-size tree theorem,
general QEC/syndrome circuits, ADAPT pool design, mitigation algorithms,
large active-space/frozen-core implementations, full vibrational analysis
and mixed-species simulation are optional expansions of the remit. Do not
silently commit to them to make the book longer. If a short extension retains
a quantitative formula, its local definitions and hypotheses are still
mandatory.

Do not expand repair history, repeated "pipeline complete" claims,
near-identical weight tables, unsupported feasibility figures or the entire
public API. More copy about why a missing step matters is not a replacement
for showing the step.

### How to calibrate this budget

Before treating any word target as contractual, typeset representative
revised material using the actual publisher layout: a prose/chemistry section,
an equation-heavy derivation, a code-and-diagram section, and exercises with
selected solutions. Measure their actual space and adjust the allocations.
If clarity requires more than an allocation, first move optional depth,
duplicate reference material or code output—not definitions or indispensable
derivation steps. If it requires less, do not pad it.

## Clarity-first editorial acceptance rules

- Before an equation is used, name the objects and the meaning, domain,
  index ranges and units of newly introduced symbols. Explain a convention
  change at the point of change; a symbol glossary alone cannot do that job.
- Before a nontrivial algebraic step, supply the identity, hypothesis or
  earlier worked step that licenses it. "Clearly", "simply" or an API name
  cannot replace the missing reasoning.
- Before an algorithmic example, identify inputs, output, invariant and
  failure conditions. Separate a theorem, an implementation guarantee and
  an observed result.
- Before a code call, explain what object it consumes and returns, its
  convention and the prerequisite definitions. A runnable opaque call is
  not an explanation; a beautiful derivation with unrunnable code is not a
  completed computational example.
- Before an exercise, ensure all necessary concepts and inputs have appeared.
  Give the reader an answer, checkable intermediate result or rubric at the
  appropriate level. Exercises may extend an idea, but must not conceal the
  first explanation of an essential prerequisite.
- Prefer one connected worked example and a small counterexample over
  repeated summaries. Explain why the counterexample fails and what the
  successful construction preserves.
- Retain useful plain-language motivation and the author's voice. Do not
  mistake shorter sentences, fewer equations or fewer words for a lower
  cognitive burden.

The acceptance target is a reader who can explain and carry out the next
step, not a manuscript that reaches a particular page count or a keyword
scan that finds a definition somewhere.

---

# Implementation verification checkpoint — 2026-09-07

**Authority:** The author subsequently authorised full book implementation.
**Integrated initial repair:** `4137857` (child commit `9293687`).
**Status:** Partial progress; **EC-M01, EC-M02 and RG-01 remain open** pending
the follow-up below. No full-book completion or regeneration acceptance is
claimed.

The initial repair replaces moment-only spectrum acceptance with
complex-Hermitian realification/Jacobi diagonalisation and makes the oracle
verifier read-only by default. Its targeted cases pass: a split degeneracy
that the old moment gate accepted is rejected; a Pauli-Y imaginary-coupling
case is handled; non-Hermitian, non-finite and wrong-dimension cases are
rejected; corrupted coefficient/spectrum/order/metadata examples do not
silently rewrite the oracle.

Independent code review then found two additional false-acceptance paths:

1. **Mixed-scale eigenvalue accuracy:** `labs/PauliMatrix.fsx:132-136` uses
   scale-based rotation/stopping cutoffs unrelated to the requested absolute
   tolerance. With diagonal entry `1e10` and a separate imaginary off-diagonal
   pair `-9e-7 i`, `+9e-7 i`, the true spectrum is
   `[-9e-7, 9e-7, 1e10]`. The gate at `1e-7` instead accepts
   `[0, 0, 1e10]`. Solver convergence/residual criteria must respect the
   requested accuracy or fail closed when it cannot be established.
2. **Fixed metadata is not a computed energy:** the recursive JSON
   comparison in `code/numerical_integrity.py:42-45` applies the numerical
   energy tolerance to threshold metadata too. A changed
   `input_threshold=-1e-10` or `combined_pauli_threshold=0` is accepted.
   Fixed thresholds, counts and ordering indices require exact value/type
   comparison; tolerance belongs only to the derived numerical fields.

Both findings have been assigned to the executable-integrity owner with
explicit regression cases. Passing the original negative controls is not
sufficient closure, and no guard is relaxed on the strength of the initial
commit.

**Independent structural integration work:** `dca1dce` adds
`scripts/check-book-contract.py` and 15 standard-library unit tests. The
checker reports the expected pre-expansion gaps (two missing support
sections and eleven absent exercise sections). It checks assembly, fenced
code and relative resources, not mathematical truth or define-before-use
comprehension. These failures must be resolved by the chapter/support
implementation, not by weakening the contract.

# Targeted chapters 01–13 verification — 2026-09-19

## Reviewed scope

On-disk numbered chapters 01–13, identified by the caller as HEAD `b605610`,
against the September correction/expansion plan and September audit, not
the July history. Every chapter line range was requested through EOF.
Long-line searches recovered most text truncated by the file reader.
Eight lines still have hidden tails in both available views:
`01-electronic-structure.md:21,23,25,140,408`,
`05-visual-encodings.md:211`, `09-verification.md:177`, and
`12-clifford-tapering.md:57`. This is therefore not an unqualified
full-text-reading certificate.

The mathematical checks covered integral conventions, real/complex ERI
symmetries, spin counts, state and matrix ordering, Fenwick operations,
Majorana normalisation/path pairing, H2 assembly, and signed physical-sector
tapering. Narrow companion reads covered the Chapter 7 weight loop,
Chapter 8 CAR check, and Chapter 12 physical taper, including the spectrum
assertion it calls. No code was run. No CLI was exposed, the pinned external
tree-helper source was inaccessible to the workspace reader, and the
reported HEAD was not independently resolved. Chapters 14–23, support
material, broad library correctness, builds and publishing remain with
their other reviewers. Only this authoritative audit addendum was written;
no manuscript/code edits, delegation, commits or pushes were performed.

## Result

The central high-priority mathematical repairs survive the inspected pass.
No remaining high-priority defect was found in that material. Two concrete
consistency residuals and one low-priority categorical cost claim remain:

1. **Medium — BE-M03, mixed-index HF exercise remains.**
   `manuscript/09-verification.md:362` still writes
   `h00 + h11 + [00|00] + Vnn` without identifying the first two indices
   as spin-orbital and the bracket as spatial. Using the spatial table
   gives approximately `-0.3385183695` Ha, not RHF `-1.1167593074` Ha.
   Minimal repair: use `2 h00(spatial) + [00|00](spatial) + Vnn`, or
   explicitly distinguish spin and spatial superscripts. The corresponding
   derivations in Chapters 3 and 6 are correct. This is the previously
   identified exercise boundary, not a defect in the canonical tensor.
2. **Medium — stated spectral tolerance exceeds the named gate.**
   `manuscript/13-tapering-benchmarks.md:72,89-92` describes a direct
   eigenvalue comparison at `1e-10` Ha and points to
   `code/ch12-h2-physical-taper.fsx`. That script's lines 37 and 44 pass
   `1e-9` to `assertSpectrumMatrix`; `labs/PauliMatrix.fsx:175-184`
   uses that argument as the maximum sorted-eigenvalue error. Its
   `1e-10` entrywise block checks are different tests, not a direct
   `1e-10` eigenvalue gate. For example, a diagonal spectrum shifted
   by `5e-10` Ha passes the stated function at `1e-9` but not `1e-10`.
   Minimal repair: have the executable owner enforce and rerun the
   claimed tolerance, or report the actual `1e-9` spectral tolerance
   separately from the `1e-10` matrix tolerance. No numerical error in
   the displayed canonical eigenvalues is alleged.
3. **Low — BM-M06, categorical resource summaries remain.**
   `manuscript/10-why-tapering.md:274` promises reduced downstream cost;
   `manuscript/13-tapering-benchmarks.md:245-246` says tapering reduces
   both term count and weight. The latter chapter's own mixed example
   keeps four terms and maximum weight two. More generally, fixing
   `Z0=+1` in `H=ZII+IXX` gives `II+XX`: two terms and two staircase
   CNOTs both before and after. Minimal repair: say these quantities
   *can* decrease and retain the existing model-specific counts.

## Closed items

Within the inspected chapter scope, BM-H01/H02/H03 are repaired:
raw half / antisymmetrised quarter / restricted antisymmetrised unit
prefactors agree; the nonzero-exchange example gives 0.5 Ha rather than
0.7 Ha; canonical inputs agree; q0-left labels, integer rows and reversed
dense factors agree; spectra are no longer offered as label certificates.
BM-M01/M02/M03/M04/M17 are also repaired in the inspected passages.

The checked H2 ledger retains HF row 3 at `-1.8318636465` Ha,
electronic FCI `-1.8523881736` Ha, 15 terms including identity,
14 nonidentity rotations and 36 untapered staircase CNOTs.
The alpha/beta parity signs are derived as `(-1,-1)`.
Chapters 10–13 consistently map reduced rows to original
`[12,9,6,3]`, retain the full four-state `N=2, Ms=0` block at two
qubits, and distinguish the optional closed-shell pair `+1` block.
Their signed coefficient ledger, reduced matrices and analytic spectra
agree. The reported `4→2→1` resource arithmetic is sound.

XP01/02/04/05/06/08 and the corresponding early BE-M02 mathematical
bridges are supplied and check out in this scope. The Majorana
normalisation, two-node construction and full CAR derivation in XP07
are mathematically consistent.

## Remaining items

BM-H05's Chapter 1 backend promise is repaired in the visible passage;
its repeated claims outside this scope are not closed here.
BM-M05/M16 correctly distinguish ideal versus midpoint-tree weight,
Clifford versus arbitrary term-count invariance, and finite census versus
runtime support/theorem claims. Their actual pinned-helper source
correspondence was not independently verified in this tool environment.
BM-M10 is repaired in Chapters 10–12; its appendix boundary is outside
this pass. BM-M06 and BE-M03 remain partial for the residuals above.
XP03, remaining BE/XP teaching/support requirements, executable gates
and book-wide closure belong to the other owners.

## New risks

The spectrum-tolerance reporting mismatch is the only new verification
contract issue established here. Hidden long-line tails and unavailable
helper execution/source are explicit evidence limits, not presumed bugs.

## Focused closure of the three residuals — 2026-09-19

**Reviewed scope:** Only the three findings above and the named spectrum
gate. **Result:** All three are closed in the current working-tree text.

- `13-tapering-benchmarks.md:72-73` now distinguishes `1e-9` Ha spectrum
  tolerance from `1e-10` Ha entrywise matrix tolerance, matching
  `code/ch12-h2-physical-taper.fsx:35,37,43-44`.
- `09-verification.md:370-377` explicitly uses spatial integrals and
  `2 h00 + Coulomb + Vnn`, recovering the correct total HF energy.
- `10-why-tapering.md:274-277` and
  `13-tapering-benchmarks.md:247-249` make cost/count/weight reductions
  conditional, while preserving sector exactness and width reduction.

**Remaining items:** None from these three findings.
The parent reports having read all eight previously truncated tails via
wrapped CLI output; that closes the reading gap at the coordinated-review
level, not by a new independent read here.
**New risks:** None identified in this focused pass. No tests were rerun.

---

# Final implementation acceptance — 2026-09-19

**Accepted implementation tree:** `c688ce0e3d79296a76a65c009e112d48a5a932a3`,
with the documentation-only closure in this addendum following it.
**Mode:** Author-authorised book implementation and local proof generation.
No publisher submission, public book release, licence change or blog
publication was performed.
**Outcome:** The bounded correctness, expansion, integration and local-output
tasks are complete. This is a substantially expanded manuscript candidate
for the Apress sequence reported by the author, not a claim of publisher
acceptance of this particular draft or an external scientific peer review.

## What was integrated

The five isolated workstreams were combined on
`johnazariah-book-and-blog-review`. All 23 numbered chapters were corrected
and expanded. The book now includes the code-reading bridge before Chapter 2,
complete worked coupling/Fenwick/Majorana/physical-taper/VQE/QPE examples,
exercises in every chapter, two expanded selected-reference appendices,
25 selected solution sections and 53 central reference entries.

The current source inventory is **83,235 whitespace-delimited words**,
including code, tables and markup, versus 46,313 at review baseline.
That is not a claim of 83,235 prose words. The full local PDF measures
**291 pages** with the unchanged 11-point, one-inch-margin layout. No font,
margin or blank-page manipulation was used to achieve the approximately
300-page planning target.

Significant integration commits include:

- `e4cc7a8`, `d84d877`, `08a7398`: the three chapter workstreams;
- `66cb16b`, `0919720`, `919e28b`, `b605610`: support matter and production
  tooling;
- `63d55fd`, `5616b9d`, `8ed7e87`, `7998cc9`: numerical and executable
  integrity beyond the initial partial repair;
- `f390e9e`: final chapter-boundary corrections, MathML compatibility and
  print-layout repairs;
- `622efb4`: convergence provenance and accurately labelled figures;
- `89e386a`: checkout-local devcontainer tools;
- `3d77efc`: the upstream HTML hydration repair via an exact publisher pin;
- `c688ce0`: accumulated-roundoff/global-Hermiticity accuracy guards.

## Independent manuscript and pedagogy closure

The final verification was divided into the full Chapters 1-13, the full
Chapters 14-23, and the cross-book learning sequence, support matter and
exercise/selected-answer alignment. These were verification passes on the
written expansion, not acceptance of the workstream authors' summaries.

The residuals found during integration were corrected:

- Chapter 15 now reports 15/30 **stored factors including identity** versus
  14/28 **emitted nonidentity rotations**, retaining 36/72 logical CNOTs.
- Chapter 4 defines S, S-dagger and the `Sdg` code name through matrices,
  amplitude action and the Y-basis measurement sequence before later use.
- Chapter 9's HF exercise explicitly uses spatial integrals.
- Chapter 13 distinguishes the actual `1e-9` eigenvalue tolerance from
  `1e-10` entrywise matrix comparison.
- Tapering summaries distinguish guaranteed width reduction from
  conditional gate/term/weight savings.

Focused independent rereads accepted these repairs. The pedagogy reviewer
closed XP01-12 within source-level verification and found no remaining
blocking or medium reader failures in that pass. All 23 exercise sets were
compared with the selected solutions; selected coverage is intentionally
not a complete instructor key.

The Chapters 1-13 reviewer had eight long-paragraph display gaps. The
coordinator read all eight wrapped source passages and found no additional
residual. This closes the coordinated reading gap without pretending the
specialist's tool itself exposed those tails.

| Finding group | Final disposition |
|---|---|
| BM-H01-H05 | Closed in the revised teaching text; central equations, canonical inputs, ordering, measurement/simulation split and water scope reconciled |
| BM-M01-M04, BM-M06-M15, BM-M17, BM-L01 | Closed within the mathematical/source verification and the corrected local residuals above |
| BM-M05/M16; CA-M010 | Closed at the retained narrow scope: actual helper shape, finite tests, documented custom-constructor warning and separate path-tree validation; no unsupported all-size theorem or SRL indictment |
| BE-M01-M06; XP01-XP12 | Implemented and source-verified: prerequisites, complete worked bridges, exercises/selected answers, current-reader prose, honest appendices and purposeful figures |
| EC-M01/EC-M02 | Closed for the implemented small-matrix/reference gates after the additional independent-review fixes and negative controls below |
| EC-M03-M06, EC-L01/L02 | Convergence/finiteness, complete data contracts, provenance/grids, portable versus archival reproduction, lab scope and executable entry points repaired and exercised |
| CA-M011 | Pinned `groupCommutingTerms` output verified as greedy QWC, not general commutation; full accounting, compatibility and identity treatment checked |
| CA-H008 | Closed for the book's stated scope: actual QASM 2/3, JSON and Q# round trips, controlled identity phase and honestly labelled resource-helper semantics; no full fault-tolerant QPE budget is claimed |
| BR-M01-M04, BR-L01/L02 | Rendering fails closed, figures invalidate outputs, evidence gates precede publication jobs, auxiliary drafts marked historical, correct agent contract/manifests and sample inventory |
| BR-H01 | Local source/artifact inconsistency repaired: stale tracked PDF removed, local proofs rebuilt, old public downloads labelled earlier releases. Replacing public editions is a separate author/publisher action |
| BR-H02 | Remains an external owner/rights-record task; the implementation does not alter or claim to repair Zenodo's historical mixed-rights record |

## Numerical verification: failures were repaired, not waived

The independent numerical reviewer first reproduced false acceptance from
mixed scales, then accumulated Jacobi roundoff, then a globally significant
Hermiticity defect whose individual entries were small. Each was repaired
and added as a negative control rather than hidden by relaxing tolerances.
The final focused re-review reported no significant issues in the changes.

The gate now accounts for dimension/rotation-dependent floating-point
roundoff using the matrix norm, checks the **global** Hermiticity defect,
explicitly symmetrises only an input within the allocated perturbation
budget, and includes solver allowance in the final comparison. It fails
closed when the requested absolute accuracy is not supportable. This is
the book's practical small-matrix numerical acceptance routine, not a new
formal interval-arithmetic theorem for arbitrary matrices.

The accepted negative controls cover the old split-degeneracy moment
counterexample; imaginary Pauli-Y information; large unrelated diagonal
scales; real and complex rank-one roundoff; accumulated non-Hermitian
blocks; nonfinite/dimension errors; corrupted oracle coefficients, spectra,
ordering and metadata; negative/zero thresholds and fractional counts; wrong
state labels; and accidental raw/weighted double adaptation.
Successful and failed default oracle verification remain read-only.

## RG-01 and fresh executable evidence

The chosen immutable public pin remains **FockMap 0.9.0**, source
`96320a56786393269fd681c67c66df88058a8b8f`, DLL SHA-256
`0ba8ae967ea65d4945a336c1217f8939e41feb63b6f04af617b66537717d25c3`.
The fixture remains the byte-identical research artifact at merged
`66ebdfe255c0cc6ba25a6d1b76b58401aee3ab06`, SHA-256
`6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812`.
The committed direct oracle is unchanged by the provenance promotion:
SHA-256 `a6747218407232d28c00864f98ccbe16f1ecb2656209d0cd64f0395aabd2db20`.

Fresh integrated acceptance passed:

| Check | Result |
|---|---|
| `make import-check` with isolated pinned importers | Complete bounded numerical, provenance, physical-sector, algorithm and actual import gate passed |
| Immutable H2 contract | 4+32 raw inputs, complete JW matrix, literal N=0 through 4 eigenvalue lists/multiplicities, labelled HF row 3, immutable DLL and adapter negative control passed |
| Six encoding comparisons | Actual complex-Hermitian sorted eigenvalue differences at rounding scale; moments retained only as diagnostics |
| Physical taper | Explicit alpha/beta sector `(-1,-1)`, row map `[12,9,6,3]`, four-state sector spectrum and separately named closed-shell block passed |
| CAR evidence | All six built-ins for n=2 through 6; selected custom-chain/star contrasts for n=3 through 6 passed as finite tests, not an exhaustive all-shape or all-size theorem |
| QWC / resource helper | Five measurement bases; identity needs no shots; `qpeResources` documented and checked as an uncontrolled-step proxy, not a compiled QPE resource estimate |
| VQE / QPE illustration | Canonical variational minimum and shifted 12-bit phase decode reproduced with overlap/resolution/confidence distinctions retained |
| All ten numbered labs | Passed on the integrated tree |
| Existing six F# companions and parameterised H2 example | Passed |
| Three complete printed support examples | Passed |
| Chemistry portable and archival reproduction | Portable numerical parity passed before promotion; strict isolated archival comparison passed twice in the data workstream and again after integration |
| Generation failure controls | Nonconvergence, late water failure, partial-output protection and caught-I/O rollback passed |
| Structural and publication tooling | 15 assembly unit tests and 12 bounded publication tests passed; all 23 exercise sections and six support sources present |

The promoted data add 146 converged solver records without changing any
pre-existing tensor/energy values. CSVs, oracle and immutable physicist
fixture/provenance bytes remain unchanged. Figure/provenance changes are
deliberate: the H2 plot separates the finite-STO-3G two-atom limit from
the complete-basis reference; the water plot names PySCF, basis, fixed bond
length and lowest sampled angle. Byte reproduction is explicitly a
recorded-environment contract, not a portability promise for every allowed
BLAS/platform/font combination.

Actual import acceptance used the pinned Qiskit/QASM importer and Q# runtime,
not a replacement handwritten parser:

- QASM 2/3, strict JSON-to-Qiskit and transpiled labelled-column errors:
  at most `4.601e-15`.
- Compiled/simulated Q# on all 16 labelled inputs: `2.735e-15`.
- The restored-identity controlled 32-by-32 product, after compilation:
  `2.267e-14`.
- Product-formula versus exact evolution discrepancy: approximately
  `1.430e-3`, explicitly separate from import error.

A compiled **one-step** controlled circuit is measured; it is not a
fault-tolerant full-QPE hardware budget. No ground energy is inferred from
a circuit count or an identity coefficient.

## Actual output acceptance

| Artifact | Measured result |
|---|---|
| Full PDF | 291 pages; 36 image placements; no text spans beyond page boundaries in the automated inspection |
| Sample PDF | 61 pages; 9 image placements; full printed contents including omitted appendices/solutions with dash page entries |
| EPUB | 31 XHTML documents; 36 PNGs; 3,235 MathML expressions; no raw-TeX mathematical fallback |
| HTML | 29 pages inspected in a real browser, all HTTP 200, 3,041 rendered KaTeX expressions across the inspected pages, no page/console errors, no detected KaTeX errors or broken article images |

Initial output inspection found clipped spectrum rows, a long coefficient
equation, a hash and a selected-solution equation. They were reformatted
without removing values. Initial EPUB conversion exposed unsupported legacy
font declarations; the notation was normalised and a fail-closed MathML
gate added with a deliberate undefined-command negative control.

Initial HTML built with Jupyter Book 2.1.5 rendered content but raised React
hydration errors. The official upstream exporter injected its index redirect
before React-owned head content. Official **2.1.6** contains the upstream
repair, inserting the redirect at the end of the head. The project now pins
2.1.6 and rejects an incompatible ambient publisher before deleting existing
output. A clean isolated rebuild and all-29-page integrated browser pass
verified the fix. JavaScript was not disabled and errors were not suppressed.

The PDF SHA-256 is
`bfbc051839d8e138a41aa605d2efb4b9b3189e1a759a7b93e846c8e5b3df2741`;
sample PDF
`7e1c48f7fc61cf5ade4a0788a8d967cf8147df19caf075b37e7a854b144ef988`;
EPUB
`95cd08aabb46d94679b1aefb28699e8cfa03fed67de6e03d340d41b51bdb7a2e`.
These identify this generated proof set; PDF creation metadata may change
on a later rebuild. Detailed command logs and artifact identities are kept
in the session's proof/evidence bundle, not added as parallel review files.

## What remains outside implementation completion

John reports that Apress are interested and happy to pick up the book after
QCSE (*Quantum Bottleneck*). That is now the publication direction, not
self-publication. Publisher contract/schedule/layout, external reader
feedback and editorial acceptance remain separate decisions. The 291-page
local layout does not predict Apress's final pagination.

Historical GitHub/Zenodo editions were not replaced; Zenodo rights metadata
was not changed. Earlier Springer/JOSE drafts are explicitly historical
rather than silently reused as current submissions. The blog's P1-then-P2
approval sequence and later-post freeze remain in force. Its reviewed
harmonisation contract can use the corrected book commit, but no blog
article, hook, date or approval was changed under book implementation
authority.
