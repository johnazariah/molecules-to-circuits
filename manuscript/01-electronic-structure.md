# Chapter 1: The Electronic Structure Problem

_We have a molecule. We want a quantum circuit. This chapter is about the first question: what, exactly, are we trying to compute?_

## In This Chapter

- **What you'll learn:** How a molecule becomes a finite mathematical problem — from the Schrödinger equation, through the Born–Oppenheimer approximation, to a small set of numbers called integrals.
- **Why this matters:** Every step in the rest of this book — encoding, tapering, Trotterization, circuit compilation — operates on the output of this chapter. If you don't understand what the integrals represent, you can't understand what the quantum computer is computing.
- **Prerequisites:** Linear algebra (matrices, eigenvalues). Introductory quantum mechanics (wavefunctions, bra-ket notation). No prior knowledge of molecular orbital theory or second quantization is assumed.

---

## The Question

Here is a question that a first-year chemistry student can state, and whose accurate answer can occupy a computational chemist for years:

> **Given a molecule — its atoms and their positions — what is its ground-state energy?**

Electronic ground-state energies are essential inputs to predictions of bond strengths, molecular shapes and reaction energies. They are not, by themselves, predictions of whether a reaction proceeds or where a liquid boils. Reaction rates also depend on barriers and dynamics; phase equilibrium depends on free energies at a specified temperature and pressure. An isolated molecule's electronic energy does not contain the entropy of a beaker of water.

Even the narrower electronic problem is hard. Electron–electron repulsion couples the electrons' coordinates, so the familiar hydrogen-atom separation of variables no longer solves it. Helium has no corresponding elementary closed-form solution, though highly accurate numerical calculations are possible. Perturbation theory can improve an approximate answer, but successive orders do not guarantee successive digits; a series can converge slowly or fail, particularly when several electronic configurations compete.

Classical computational chemistry has developed an extraordinary arsenal of approximation methods — Hartree–Fock, density functional theory, coupled cluster, configuration interaction — each trading accuracy for tractability in a different way. We will define Hartree–Fock and full configuration interaction below; the others are alternatives, not prerequisites for this chapter. For example, CCSD(T), a widely used coupled-cluster approximation, has conventional cost scaling as the seventh power of basis size and is often excellent near a single-determinant reference. It is not generally reliable when several determinants are equally important. Keeping *all* configurations avoids that particular truncation, but their number grows combinatorially with the number of orbitals and electrons.

This is where quantum simulation enters the picture. An $n$-qubit register can represent amplitudes over $2^n$ occupation states without storing those amplitudes one by one in classical memory. That compact representation is necessary, but it is not an algorithmic guarantee: preparing a useful molecular state and estimating its ground-state energy can still be hard. Quantum algorithms may offer better scaling for structured chemistry problems when the Hamiltonian can be implemented efficiently, the trial state has adequate overlap with the target, and the required precision and fault-tolerant resources are available (Kempe, Kitaev & Regev, 2006; Reiher et al., 2017). Translating the molecular problem into that form requires a specific sequence of mathematical transformations, each with its own conventions, sign choices, and opportunities for error.

This chapter covers the first transformation: turning the continuous, infinite-dimensional molecular problem into a finite-dimensional matrix problem. The result will be a set of numbers — the **molecular integrals** — that encode everything we need to know about the molecule.

We will do this for the hydrogen molecule, H₂. Its smallest basis model is trivial for a laptop, which is precisely why it is useful here: we can see every step, check every number, and build intuition for what happens at larger scale. Solving that small model is not the same as solving the continuous molecular problem exactly. Later, when we work with H₂O, the same kinds of objects will be larger.

---

## A Molecule Is a Collection of Charges

Strip away the language of orbitals and bonds, and the interactions are recognisable: positively charged nuclei and negatively charged electrons interacting via Coulomb's law. The particles also have kinetic energy, and their state is quantum mechanical. Electrostatics supplies the interactions, not the whole answer.

For H₂, this means:
- **Two protons** (charge $+e$ each), separated by a distance $R$
- **Two electrons** (charge $-e$ each), somewhere in the space around them

Before writing the Hamiltonian, we need to name its symbols and choose units.

| Symbol | Meaning |
|:---|:---|
| $M$, $A,B$ | Number of nuclei and indices labelling nuclei |
| $N$, $i,j$ | Number of electrons and indices labelling electron coordinates |
| $\mathbf R_A$, $\mathbf r_i$ | Three-dimensional nuclear and electronic positions |
| $Z_A$, $M_A$ | Nuclear charge number and nuclear mass |
| $r_{ij}=\lvert\mathbf r_i-\mathbf r_j\rvert$ | Distance between two electrons |
| $\nabla_i^2$ | Laplacian: sum of second derivatives in electron $i$'s three coordinates |
| $\sum_{A<B}$, $\sum_{i<j}$ | Sums over distinct unordered pairs; each pair appears once |

We use **atomic units** from this point onwards. One length unit is the
Bohr radius $a_0\approx0.5291772109$ Å; one energy unit is the hartree,
$E_h\approx27.2114$ eV. The abbreviations Ha and hartree mean the same energy
unit. In these units the numerical values of $\hbar$, the electron mass
$m_e$, the elementary charge magnitude $e$, and $4\pi\epsilon_0$ are one.
Nuclear masses below are therefore measured in electron masses.

This is a unit convention, not a deletion of physical constants. In SI,
the repulsion between charges $+e$ is $e^2/(4\pi\epsilon_0 r)$.
Atomic units absorb that factor into the length and energy scales:
for a distance whose numerical value in Bohr is $r$, the repulsion has
numerical value $1/r$ in hartree. We must not put a distance in Å directly
into $1/r$ and call the result hartree.

The total energy contains five contributions:

| Interaction | Formula | Sign | Strength |
|:---|:---|:---:|:---|
| Nuclear kinetic energy | $-\frac{1}{2M_A}\nabla_A^2$ | — | Small inverse-mass coefficient; not identically zero |
| Electron kinetic energy | $-\frac12\nabla_i^2$ | — | Significant |
| Nuclear repulsion | $\frac{Z_A Z_B}{\lvert\mathbf{R}_A - \mathbf{R}_B\rvert}$ | $+$ | Repulsive |
| Electron–nuclear attraction | $-\frac{Z_A}{\lvert\mathbf{r}_i - \mathbf{R}_A\rvert}$ | $-$ | Attractive |
| Electron–electron repulsion | $\frac{1}{\lvert\mathbf{r}_i - \mathbf{r}_j\rvert}$ | $+$ | Repulsive, and couples electronic coordinates |

The full Hamiltonian is the sum of all five:

$$
\hat{H} = \underbrace{-\sum_{A=1}^{M} \frac{1}{2M_A} \nabla_A^2}_{\text{nuclear KE}}
         \underbrace{-\frac12\sum_{i=1}^{N} \nabla_i^2}_{\text{electronic KE}}
         + \underbrace{\sum_{A<B} \frac{Z_A Z_B}{\lvert\mathbf{R}_A - \mathbf{R}_B\rvert}}_{\text{nuclear repulsion}}
         \underbrace{- \sum_{i,A} \frac{Z_A}{\lvert\mathbf{r}_i - \mathbf{R}_A\rvert}}_{\text{electron-nuclear attraction}}
         + \underbrace{\sum_{i<j} \frac{1}{\lvert\mathbf{r}_i - \mathbf{r}_j\rvert}}_{\text{electron repulsion}}
$$

This is exact within the non-relativistic, point-charge Coulomb model. Relativistic, quantum-electrodynamic, finite-nuclear-size, and other corrections are outside that model. If we could solve this equation, we would have its exact molecular energy.

There is no general closed-form solution of this molecular equation. Numerical
answers are possible, but we must state which parts of the model and state
space we approximate.

> **Common Mistake #1:** Counting particles alone misses the state-space problem. The electronic *wavefunction* $\Psi(\mathbf{r}_1, \mathbf{r}_2, \ldots, \mathbf{r}_N)$ depends on $3N$ spatial coordinates, as well as spin. For 50 electrons that is 150 continuous spatial variables. A naive direct-product grid is hopelessly large; useful classical methods exploit structure rather than storing that grid.

---

## The Born–Oppenheimer Approximation: Freezing the Nuclei

Protons are about 1836 times heavier than electrons. This mass separation
motivates the Born–Oppenheimer approximation: solve an electronic problem at
fixed nuclear positions, then use the resulting energy surface to describe
nuclear motion. Here we perform the first, **clamped-nuclei** step. We treat
the positions $\{\mathbf{R}_A\}$ as parameters, not electronic variables.
The approximation can fail where electronic states approach or cross and
nuclear motion couples them strongly; heavy nuclei alone are not a universal
guarantee.

The result is the **electronic Hamiltonian**:

$$
\hat{H}_\text{el} = -\frac12\sum_{i=1}^{N} \nabla_i^2
                     - \sum_{i,A} \frac{Z_A}{\lvert\mathbf{r}_i - \mathbf{R}_A\rvert}
                     + \sum_{i<j} \frac{1}{\lvert\mathbf{r}_i - \mathbf{r}_j\rvert}
$$

The nuclear repulsion energy becomes a constant for a given geometry:

$$V_{nn} = \sum_{A<B}\frac{Z_A Z_B}{\lvert\mathbf R_A-\mathbf R_B\rvert}
\quad\text{and, for H₂,}\quad V_{nn}=\frac1R.$$

Our canonical H₂ input is $R=0.74$ Å, near equilibrium, not a claim that
this is the optimised bond length of every model. The conversion is

$$R/a_0=\frac{0.74}{0.5291772109}\approx1.3983973,
\qquad V_{nn}/E_h=\frac{1}{1.3983973}\approx0.7151043.$$

The committed calculation supplies $V_{nn}=0.7151043391$ Ha. We retain its
precision in the data and round only for display. Two electrons do not double
this nuclear term: there is exactly one nuclear pair.

What remains is a problem in the electronic coordinates alone. Solve it for one
geometry and obtain $E_\text{el}(R)$. Add $V_{nn}(R)$ to obtain the
Born–Oppenheimer **potential energy surface**, a function of nuclear geometry.
For a diatomic bond coordinate this is a curve. Its minimum gives a model
equilibrium separation; its curvature, together with nuclear masses, supports
a harmonic vibrational estimate. For a polyatomic molecule one needs the
appropriate multidimensional derivatives and mass weighting, not just any
one-dimensional slice.

> **Why this matters for quantum simulation:** Most conventional electronic-structure calculations, classical or quantum, start with this fixed-geometry problem. A quantum energy algorithm could be repeated at several geometries. Our demonstrated H₂O angular scan instead uses **classical PySCF RHF/FCI energies**, with O–H length fixed at 0.9584 Å. FockMap constructs operators and circuits; that is not the same task as preparing a state and estimating its energy. The water scan is a reference calculation, not a completed quantum geometry optimisation.

---

## Basis Sets: Making the Infinite Finite

The electronic Hamiltonian acts on wavefunctions $\Psi(\mathbf{r}_1, \mathbf{r}_2)$ — functions of continuous 3D coordinates. A finite computer cannot represent an arbitrary such function exactly. We need a finite representation.

The standard approach: expand each molecular orbital as a **linear combination of known functions**, called basis functions. This is the same idea as representing a vector in a finite basis, except the "vectors" are functions and the "basis" is a set of atomic orbital shapes.

An **orbital** is a one-electron wavefunction, not a little orbit traced by
an electron. We call an atom-centred basis function an **atomic orbital**
(AO) here, even when it is an approximate Gaussian function rather than an
exact isolated-atom eigenfunction. A **molecular orbital** (MO) is a
one-electron function assembled from the chosen AOs:

$$\phi_p(\mathbf r)=\sum_{\mu=0}^{K-1}C_{\mu p}\chi_\mu(\mathbf r).$$

The $\chi_\mu$ are the $K$ AO basis functions; column $p$ of $C$ contains
the coefficients of MO $p$. Choosing a finite basis restricts the space of
functions we can represent. Changing $C$ rotates within that space; it
does not enlarge the basis.

### The Hydrogen Atom: Exact Solutions We Can't Use Directly

For a single hydrogen atom, the Schrödinger equation has exact solutions: the familiar $1s$, $2s$, $2p$, $3d$, … orbitals. These have the form

$$\phi_{1s}(r) \propto e^{-\zeta r}$$

where $\zeta$ determines the radial decay. Slater-type functions reproduce the
hydrogenic cusp and exponential tail, but multicentre molecular integrals
are substantially less convenient than their Gaussian counterparts.

### Gaussians: The Practical Compromise

The solution, introduced by S. F. Boys in 1950, is to replace Slater-type orbitals with sums of Gaussian functions:

$$g(r) \propto e^{-\alpha r^2}$$

The product of two Gaussians centred at different points is another Gaussian centred at a third point — a property that makes all the necessary integrals analytically tractable. The price: Gaussians decay too quickly at large $r$ and have the wrong cusp at $r = 0$. The fix: use several Gaussians to approximate each Slater orbital.

### STO-3G: The Smallest Meaningful Basis

The "Slater-Type Orbital approximated by 3 Gaussians" (STO-3G) basis set fits three Gaussians to each atomic orbital. For hydrogen, STO-3G provides one basis function per atom: an approximation to the $1s$ orbital.

Is STO-3G a good basis set? No — it is the smallest possible choice, and it captures only the crudest features of the electronic structure. Serious computational chemistry uses much larger basis sets (cc-pVDZ, cc-pVTZ, aug-cc-pVQZ, …). But STO-3G is perfect for *learning*, because it keeps the numbers small enough to track by hand while still exhibiting all the essential structure.

> **Common Mistake #2:** Confusing "basis set" with "basis states." The basis set (STO-3G) determines which *orbitals* we use. The basis states (the 6 configurations in the table below) are the many-electron states built from those orbitals. A bigger basis set gives more orbitals, which gives exponentially more configurations — and this is where the computational hardness lives.

> **What "exact" means in this book:** Full configuration interaction gives the
> exact eigenvalue of the finite-basis Hamiltonian in the specified electron
> sector, up to numerical solver tolerance. That is not complete-basis
> accuracy, nor an exact treatment of nuclear motion or relativity. An
> encoding can represent this finite operator exactly; a later simulation
> or energy-estimation algorithm can still introduce additional error.

---

## Molecular Orbitals for H₂

With one STO-3G basis function on each hydrogen atom ($1s_A$ and $1s_B$), the Linear Combination of Atomic Orbitals (LCAO) procedure gives two molecular orbitals:

$$\sigma_g = \frac{1s_A + 1s_B}{\sqrt{2(1+S)}} \qquad \text{(bonding)}$$

$$\sigma_u = \frac{1s_A - 1s_B}{\sqrt{2(1-S)}} \qquad \text{(antibonding)}$$

where $S = \langle 1s_A \mid 1s_B \rangle$ is the overlap integral between the two atomic orbitals.

The denominators earn their place. Write the normalised, real AOs as $A$
and $B$, with $\langle A|A\rangle=\langle B|B\rangle=1$ and
$\langle A|B\rangle=\langle B|A\rangle=S$. Then

$$\langle A+B|A+B\rangle=1+S+S+1=2(1+S).$$

Dividing by its square root normalises the bonding combination. Replacing
both plus signs by minus signs gives $2(1-S)$ for the antibonding
combination. Also
$\langle A+B|A-B\rangle=1-S+S-1=0$, so the two MOs are orthogonal.
The overlap is dimensionless: it measures how much two normalised functions
coincide, not an energy.

The **bonding** orbital $\sigma_g$ has enhanced density between the nuclei.
The **antibonding** orbital $\sigma_u$ has a nodal plane at the midpoint,
where its amplitude and density vanish. Occupying it tends to weaken this
bond; there is no density *at the node* pushing nuclei apart.
The labels $g$ and $u$ name even and odd behaviour under inversion through
the molecular midpoint. That symmetry will explain a zero integral below.

Each spatial orbital can hold one electron of each spin ($\alpha$ = spin-up, $\beta$ = spin-down), giving us 2 spatial orbitals × 2 spins = **4 spin-orbitals**:

| Index $p$ | Spatial orbital | Spin |
|:---:|:---:|:---:|
| 0 | $\sigma_g$ | $\alpha$ |
| 1 | $\sigma_g$ | $\beta$ |
| 2 | $\sigma_u$ | $\alpha$ |
| 3 | $\sigma_u$ | $\beta$ |

---

## The Six Configurations of H₂

A **spin-orbital** includes both spatial and spin information:
$\psi_p(x)=\phi_\mu(\mathbf r)\omega_\sigma(s)$, with
$x=(\mathbf r,s)$ and spin function $\omega_\sigma$ equal to $\alpha$ or
$\beta$. These spin functions are orthonormal. Chapter 3 will use that
fact to integrate out spin.

Two occupied spin-orbitals do not give an ordinary product of distinguishable
electron wavefunctions. For orthonormal $\psi_p,\psi_q$, the normalised
two-electron **Slater determinant** is

$$\Phi_{pq}(x_1,x_2)=\frac{1}{\sqrt2}
\left[\psi_p(x_1)\psi_q(x_2)-\psi_q(x_1)\psi_p(x_2)\right],
\qquad p<q.$$

Exchanging the two electron coordinates changes its sign. Trying $p=q$
gives zero, which is the exclusion principle in the formula. For $p\ne q$
the two product terms are orthogonal and each has norm one, explaining
$1/\sqrt2$. The electron coordinate labels are not persistent identities:
the determinant does not say which electron owns which orbital.

An **occupation configuration** specifies the occupied spin-orbitals and,
with an ordering convention, denotes this antisymmetric state. For two
electrons in four spin-orbitals there are
$\binom42=4!/(2!2!)=6$ choices. We write each as
$\lvert n_0 n_1 n_2 n_3\rangle$, with $n_p\in\{0,1\}$.
Index 0 is displayed on the left; these strings are labels, not
ordinary left-to-right binary numerals.

Spin projection is $M_S=(N_\alpha-N_\beta)/2$ in units of $\hbar$.
Total spin $S$ is a different quantum number: $S=0$ is a **singlet** and
$S=1$ a **triplet**, with respectively one or three possible projections.
Having one electron of each spin fixes $M_S=0$, not necessarily $S=0$.
Two open-shell opposite-spin determinants can combine into a singlet or
the $M_S=0$ member of a triplet.

| Configuration | Occupation | Description |
|:---:|:---:|:---|
| $\lvert 1100\rangle$ | $\sigma_{g\alpha}\, \sigma_{g\beta}$ | Doubly occupied bonding orbital; closed-shell singlet |
| $\lvert 1010\rangle$ | $\sigma_{g\alpha}\, \sigma_{u\alpha}$ | One in each orbital, same spin (triplet) |
| $\lvert 1001\rangle$ | $\sigma_{g\alpha}\, \sigma_{u\beta}$ | One in each, opposite spin |
| $\lvert 0110\rangle$ | $\sigma_{g\beta}\, \sigma_{u\alpha}$ | One in each, opposite spin |
| $\lvert 0101\rangle$ | $\sigma_{g\beta}\, \sigma_{u\beta}$ | One in each, same spin (triplet) |
| $\lvert 0011\rangle$ | $\sigma_{u\alpha}\, \sigma_{u\beta}$ | Doubly occupied antibonding orbital; closed-shell singlet |

### What HF optimises, and what FCI adds

**Hartree–Fock (HF)** minimises the energy over single Slater determinants
by optimising their orbitals. It includes electron–electron interaction,
including exchange from antisymmetry; it does not mean switching repulsion
off. In **restricted Hartree–Fock (RHF)** for a closed shell, each occupied
spatial orbital is used by an $\alpha$ electron and a $\beta$ electron.
The two spin partners share the same spatial function.

For the canonical H₂ calculation, RHF selects the doubly occupied
$\sigma_g$ orbital. Once those MOs define our table, the RHF state is the
single configuration $\lvert1100\rangle$. **Full configuration interaction
(FCI)** instead varies the coefficients of *all* determinants in the chosen
electron sector:

$$|\Psi\rangle=\sum_{D=1}^{6}c_D|D\rangle,\qquad
\sum_D|c_D|^2=1.$$

The variational principle says a normalised trial state's energy cannot be
below the lowest eigenvalue in that sector. FCI includes the RHF state as
one candidate, so its minimum cannot be higher. FCI does not need a second
orbital optimisation to be complete in this finite space: a unitary
rotation of the full orbital basis changes the determinant coordinates,
not the space they span.

At 0.74 Å the electronic RHF and FCI energies are respectively
$-1.8318636465$ and $-1.8523881736$ Ha. Their difference,

$$E_{\mathrm{corr}}=E_{\mathrm{FCI}}-E_{\mathrm{RHF}}
\approx-0.0205245271\ \text{Ha},$$

is the **correlation energy** relative to this RHF reference in this basis.
Adding the same nuclear constant to both energies leaves the difference
unchanged. A percentage of an energy, in contrast, changes when its
reference offset changes; the difference in hartree is the useful quantity.

That correction is small compared with the magnitude of the electronic
energy, but chemical predictions depend on *differences* between large
energies. Unequal correlation corrections can change an isomer ordering
or reaction-energy estimate. During bond breaking, the single-determinant
description may become qualitatively poor. This is one reason to seek
methods that treat competing configurations without explicitly storing
every coefficient classically.

> **A hint of what's coming:** Those occupation vectors $\lvert n_0 n_1 n_2 n_3\rangle$ look exactly like qubit computational basis states $\lvert q_0 q_1 q_2 q_3\rangle$. Four spin-orbitals → four qubits. Six configurations → a 16-dimensional Hilbert space (of which 6 states have two electrons). A quantum computer can represent superpositions of these configurations natively.
>
> The mapping is less straightforward than it appears, though — we'll see why in Chapter 5.

---

## Second Quantization: A Preview

At this point we could write down the $6 \times 6$ Hamiltonian matrix in the configuration basis and diagonalise it. For H₂, that works fine, and water's $\binom{14}{10}=1001$ determinants are also manageable. It is the combinatorial growth towards larger orbital and electron counts that defeats this direct approach.

There is a more compact way to write the Hamiltonian — using **creation and annihilation operators** rather than wavefunctions. We will develop this formalism properly in Chapter 5, where we'll need it to understand encoding. For now, the key result is that the electronic Hamiltonian can be written as:

$$\hat{H} = \sum_{pq} h_{pq}\, a_p^\dagger a_q + \frac{1}{2}\sum_{pqrs} \langle pq \mid rs\rangle\, a_p^\dagger a_q^\dagger a_s a_r + V_{nn}$$

where $h_{pq}$ are **one-body integrals**, $\langle pq \mid rs\rangle$ are **two-body integrals**, and $V_{nn}$ is the nuclear repulsion constant. The operators $a_p^\dagger$ and $a_p$ create and destroy electrons in specific spin-orbitals, and they obey algebraic rules (the canonical anti-commutation relations) that automatically enforce the Pauli exclusion principle.

This is the object that the rest of the book operates on. Everything that follows — encoding, tapering, Trotterization — takes this Hamiltonian and transforms it into something a quantum computer can execute.

---

## The Numbers: H₂ Integrals in STO-3G

First define the one-electron operator at fixed nuclear geometry:

$$\hat h=-\frac12\nabla^2-\sum_A\frac{Z_A}{|\mathbf r-\mathbf R_A|}.$$

Its **matrix element** between two spatial MOs is

$$h_{pq}=\langle\phi_p|\hat h|\phi_q\rangle
=\int\phi_p^*(\mathbf r)
\left[-\frac12\nabla^2-\sum_A\frac{Z_A}{|\mathbf r-\mathbf R_A|}\right]
\phi_q(\mathbf r)\,d^3\mathbf r.$$

Read the operator rightmost first: differentiate or multiply $\phi_q$,
multiply the result by $\phi_p^*$, then integrate. A diagonal entry
$h_{pp}$ is the kinetic-plus-nuclear-attraction expectation in one MO.
An off-diagonal entry measures a coupling between MOs. Neither includes
electron–electron repulsion.

This also connects the AO calculation to the MO table. If $h^{\mathrm{AO}}$
is the matrix evaluated in the atom-centred functions, substitution of
$\phi_p=\sum_\mu C_{\mu p}\chi_\mu$ gives

$$h^{\mathrm{MO}}_{pq}
=\sum_{\mu\nu}C_{\mu p}^*h^{\mathrm{AO}}_{\mu\nu}C_{\nu q},
\qquad h^{\mathrm{MO}}=C^\dagger h^{\mathrm{AO}}C.$$

The dagger means conjugate transpose. AOs on different atoms usually
overlap, so MO orthonormality is $C^\dagger S_{\mathrm{AO}}C=I$, where
$(S_{\mathrm{AO}})_{\mu\nu}=\langle\chi_\mu|\chi_\nu\rangle$.
The molecular integrals are therefore not just a relabelled AO array:
they have been transformed into the specified orbital basis.

For H₂/STO-3G at **0.74 Å in the canonical RHF MO basis**, the non-zero
spatial one-body integrals are:

| Integral | Value (Hartree) | Physical meaning |
|:---:|:---:|:---|
| $h_{00}$ | $-1.2533097866$ | $\langle\sigma_g\mid\hat h\mid\sigma_g\rangle$ |
| $h_{11}$ | $-0.4750688488$ | $\langle\sigma_u\mid\hat h\mid\sigma_u\rangle$ |

These are **not HF orbital eigenvalues**. The HF effective one-electron
operator, called the Fock operator, also includes the mean-field Coulomb
and exchange contributions from the occupied orbitals. Its eigenvalues
are conventionally written $\epsilon_p$, not $h_{pp}$.

The off-diagonal elements $h_{01}=h_{10}=0$ by inversion symmetry:
$\hat h$ is even, $\sigma_g$ is even and $\sigma_u$ is odd, making the
integrand odd. Orthogonality alone would not suffice. For example, the
orthogonal coordinate vectors $(1,0)^T$ and $(0,1)^T$ have off-diagonal
matrix element 1 under $\begin{pmatrix}0&1\\1&0\end{pmatrix}$.
An overlap matrix and a Hamiltonian matrix answer different questions.

The two-body integrals are a $2 \times 2 \times 2 \times 2$ tensor — 16 elements, of which only a few are distinct by symmetry. We will develop these fully in Chapter 3, after sorting out the notation conventions in Chapter 2.

The nuclear repulsion constant is $V_{nn}=0.7151043391$ Ha.

These numbers — the integrals and the nuclear repulsion — are the *output* of this chapter and the *input* to everything that follows. A classical electronic structure code (PySCF, Gaussian, ORCA) computes them from the molecular geometry and basis set. We will treat them as given.

> **What's ahead in Chapters 2–3:** The next two chapters are the notation and bookkeeping layer — getting the integral conventions right, expanding from spatial orbitals to spin-orbitals, and building the complete numerical tables that the encoding pipeline will consume. They are detail-heavy by necessity: get a sign or index wrong here, and every Hamiltonian coefficient downstream is wrong, with no error message to tell you. If you find yourself impatient, remember: these are the numbers the quantum computer will actually process. Chapters 4 and 5 are where the quantum hardware and encoding ideas arrive.

---

## Key Takeaways

- A molecule is a collection of charged particles interacting via Coulomb's law. The ground-state energy is the lowest eigenvalue of the molecular Hamiltonian.
- The **Born–Oppenheimer approximation** fixes the nuclear positions, reducing the problem to the electronic Hamiltonian at a specific geometry.
- A **basis set** (STO-3G) restricts the continuous orbital space to a finite span. For H₂, this gives 2 spatial orbitals → 4 spin-orbitals → 6 two-electron configurations.
- **Second quantization** rewrites the Hamiltonian in terms of creation and annihilation operators, encoding the Pauli exclusion principle through anti-commutation relations (CAR).
- The result is a Hamiltonian specified by one-body integrals $h_{pq}$, two-body integrals $\langle pq \mid rs\rangle$, and a nuclear repulsion constant $V_{nn}$. These numbers are the starting point for encoding.

## Common Mistakes

1. **Confusing basis set with basis states.** The basis set (STO-3G) determines the *orbitals*. The basis states ($\lvert 1100\rangle$, etc.) are many-electron configurations built from those orbitals. More orbitals → exponentially more configurations.

2. **Thinking the difficulty is the particle count.** The quantum difficulty is not that we have many particles — it's that the wavefunction lives in an exponentially large Hilbert space. 50 electrons in 100 spin-orbitals gives $\binom{100}{50} \approx 10^{29}$ configurations.

3. **Forgetting $V_{nn}$.** The nuclear repulsion energy is a constant, not an operator, but it must be added back to get the total energy. Many encoding tutorials drop it, leading to energies that are off by ~0.7 Ha for H₂.

## Exercises

1. **Configuration counting.** How many ten-electron determinants exist for H₂O in STO-3G with 7 spatial orbitals (14 spin-orbitals)? Count the whole ten-electron sector, without restricting spin projection. How does this compare with the six two-electron determinants of H₂?

2. **Born–Oppenheimer curve.** Sketch a qualitatively bound H₂ curve $E_\text{el}(R)+V_{nn}(R)$, marking the repulsive short-distance region, a minimum, and the dissociation limit. Explain why physical reasoning alone does not locate a numerical minimum for a specified basis. Would changing the fixed O–H length in a water angular scan define the same curve?

3. **Basis set scaling.** If we used the cc-pVDZ basis instead of STO-3G, hydrogen would have 5 basis functions per atom instead of 1. How many spatial orbitals, spin-orbitals, and two-electron configurations would H₂ have?

4. **Units before arithmetic.** Convert 0.74 Å to Bohr using the conversion above and recover the displayed nuclear repulsion to seven significant figures. What erroneous value results from treating 0.74 as a distance in Bohr?

5. **Normalisation and exclusion.** Take two normalised real AOs with overlap $S=0.2$. Find the normalisation constants for their sum and difference. Separately, set $p=q$ in $\Phi_{pq}$ and explain why the zero function cannot be normalised into a two-electron state.

6. **Energy ledger.** Add $V_{nn}$ to the canonical electronic RHF and FCI energies. Verify that the correlation energy is unchanged. Explain why a one-body integral, an RHF orbital eigenvalue and a total molecular energy should not share the label "orbital energy".

## Further Reading

- Szabo, A. and Ostlund, N. S. *Modern Quantum Chemistry: Introduction to Advanced Electronic Structure Theory.* Dover, 1996. Chapters 1–3 cover the material in this chapter at greater depth.
- Helgaker, T., Jørgensen, P., and Olsen, J. *Molecular Electronic-Structure Theory.* Wiley, 2000. The definitive reference for basis sets and integral evaluation.

---

**Next:** [Chapter 2 — The Notation Minefield](02-notation.html)
