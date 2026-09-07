# From Molecules to Quantum Circuits

**Subtitle:** A Computational Guide to Fermion-to-Qubit Encodings

**Author:** John S Azariah

**Repository:** https://github.com/johnazariah/molecules-to-circuits

This is the 23-chapter encodings book, not the eight-unit Quantum Bottleneck
project. Read `README.md`, `manuscript/Book.txt`, and the current September
sections of `.review/ACTION-PLAN.md` and `.review/CORRECTNESS-AUDIT.md`.
Older audit addenda are historical evidence, not present whole-book approval.

## Authorised work and boundaries

John authorised local book expansion and correction on 7 September 2026.
The approximate 300-page target is a content budget: 242 chapter pages and
58 supporting pages, not an agreed Apress trim/layout specification.
Do not change fonts, margins or blank-page policy merely to hit the target.
Public releases, publisher submissions, rights changes and blog approvals
require separate authorisation.

In coordinated sessions, respect the explicit file ownership assigned by
the coordinator. Do not edit peer-owned chapters, data or evidence scripts.
Do not create more agents, sessions or factories unless authorised.

Preserve the actual dedication, acknowledgements, biography, credentials
and permission statements. Use John's voice for authored prose without
inventing author facts or publication history.

## Reader and scientific contract

Assume linear algebra and introductory quantum mechanics. Introduce
chemistry, Pauli algebra, second quantisation, F# and measurement statistics
before substantive use. The code-reading bridge precedes Chapter 2;
later local definitions must not be outsourced to the appendices.

- H2/STO-3G at 0.74 angstrom supplies the checked integral-to-JW matrix
  and logical-circuit construction. Nuclear repulsion is separate.
- Water is a PySCF FCI angular scan at fixed experimental O-H length
  0.9584 angstrom. It is not a quantum-energy calculation or full
  geometry optimisation.
- Displayed labels put q0 leftmost. Occupation integer is sum(n_j * 2^j);
  dense tensor factors run in reverse display order.
- FockMap 0.9.0 primary builders consume raw physicist spin integrals;
  `FromWeighted` functions consume full operator coefficients.
- Choose tapering signs from the intended physical sector. A default
  all-positive sector or an energy-minimising sweep is not that derivation.
- Keep measurement circuits, Hamiltonian evolution and ansatz preparation
  distinct. Logical gate counts are not hardware runtime or advantage.
- Spectra alone do not establish labelled-state or matrix correctness.

## Immutable evidence

FockMap package: `0.9.0`; source:
`96320a56786393269fd681c67c66df88058a8b8f`.
Public net10.0 DLL SHA-256:
`0ba8ae967ea65d4945a336c1217f8939e41feb63b6f04af617b66537717d25c3`.
Research fixture source: `66ebdfe255c0cc6ba25a6d1b76b58401aee3ab06`;
fixture SHA-256:
`6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812`.
Do not version-bump or relax fail-closed guards to get a successful run.
Final format regeneration follows RG-01 in the current action plan.

## Assembly and evidence

`Book.txt` is the ordered source inventory. MyST's flattened TOC must
match it. `Sample.txt` selects bodies from that inventory, not a second
full chapter manifest. The sample keeps the full TOC without blank
omitted-chapter pages. PDF/EPUB come from Makefile; HTML from Jupyter Book 2.
Mermaid failures must fail the build; only validated caches may bypass rendering.

Run the narrow relevant commands in `Makefile` and preserve their scientific
meaning. Regeneration and verification are different operations. Do not
overwrite canonical data as a way of verifying it. Structural manuscript
checks do not certify mathematical correctness.

Maintain one authoritative audit and one action plan at the paths above.
Append later verification to the audit rather than creating review-file sprawl.
No standalone review markdown files unless the coordinator explicitly asks.
