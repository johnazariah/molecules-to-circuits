# Appendix B: Notation and Convention Reference

Use this appendix to retrieve a definition or check a boundary
convention. The chapters teach these ideas where they are first
needed; this is not a second, hidden prerequisite course.
External sources are named in **References**, not by unexplained
"Theory chapter" numbers.

## Indices, units and energies

| Symbol | Meaning here | Check before using it |
|:---|:---|:---|
| $N_e$ | Electron count | Not the number of qubits or molecular orbitals |
| $n$ | Number of spin-orbitals, hence untapered qubits for the full encodings here | A spatial basis of size $m$ gives $n=2m$ spin-orbitals |
| $p,q,r,s$ | Spin-orbital indices in an operator Hamiltonian | Spatial integrals use separately declared indices |
| $h_{pq}$ | One-electron integral | Not generally an HF orbital eigenvalue |
| $g_{pqrs}$ | Raw single-bar physicist integral $\langle pq\mid rs\rangle$ | No prefactor folded in |
| $V_{nn}$ | Nuclear repulsion at the stated geometry | Added separately to electronic energy |
| $E_\mathrm{el}$, $E_\mathrm{tot}$ | Electronic and total Born-Oppenheimer energies | $E_\mathrm{tot}=E_\mathrm{el}+V_{nn}$, with any other offsets declared |
| $t,\Delta t$ | Total evolution time and step size | Not optimisation iterations |
| $r$ in a product-formula cost | Number of steps | Its local meaning differs from an orbital index |
| $M$ | Number of Pauli terms when explicitly declared | State whether identity and pruned terms are included |

Atomic units set $\hbar=m_e=e=4\pi\epsilon_0=1$.
Energy is measured in hartree (Ha); length in bohr unless an
angstrom input is explicitly stated. One bohr is approximately
0.529177 Å. The atomic time unit is $\hbar/E_h$, approximately
$2.418884\times10^{-17}$ seconds. Thus a coefficient in hartree
multiplied by a time in atomic units gives the dimensionless phase
used in the exponent.

Do not compare an electronic energy with a quoted total molecular
energy. Adding a constant shifts every energy and changes percentages
computed relative to an energy zero; it does not change a gap within
the same geometry. A fixed-bond angular minimum is not an optimised
geometry. FCI is exact only within the supplied finite one-particle
basis and other declared modelling choices.

## Occupation labels, qubit labels and matrix rows

The book displays
$\lvert n_0n_1\ldots n_{n-1}\rangle$ and Pauli signatures
$P_0P_1\ldots P_{n-1}$ with **index 0 on the left**.
The dense matrix basis is indexed by the occupation integer

$$
k=\sum_{j=0}^{n-1}n_j2^j.
$$

Consequently the displayed occupation `1100` has integer
$1+2=3$, not 12. A conventional four-bit binary printout of 3 is
`0011`; it prints the high bit first.

For a displayed Pauli signature $P_0P_1\ldots P_{n-1}$, build the
dense matrix as

$$
P_{n-1}\otimes\cdots\otimes P_1\otimes P_0.
$$

For example, displayed `ZI` is $I\otimes Z$, with diagonal
$(1,-1,1,-1)$, in rows 0, 1, 2, 3. Displayed `IZ` is
$Z\otimes I$, with diagonal $(1,1,-1,-1)$.
The number operator on mode 0 has diagonal $(0,1,0,1)$;
this is a useful labelled-state check that a spectrum alone cannot
replace.

The occupation-to-qubit identification in this paragraph is literal
for Jordan-Wigner. Other encodings transform states as well as
operators. Their computational bitstrings need not be occupation
strings with a different caption. Cumulative parity, for example,
stores running occupation parities rather than individual occupations.

Qiskit-style Pauli labels put qubit 0 on the right. Reverse a book
signature at that boundary, but do not then reverse an already
converted matrix a second time. An export should state separately
its string convention, qubit indices and numerical basis order.

## Fermionic ladders and normal ordering

The vacuum $\lvert\Omega\rangle$ is a normalised state with no
particles. It is not the zero vector. We define

$$
\lvert n_0\ldots n_{n-1}\rangle
=(a_0^\dagger)^{n_0}\cdots(a_{n-1}^\dagger)^{n_{n-1}}
\lvert\Omega\rangle.
$$

The rightmost operator acts first. Let
$\pi_j=\sum_{k<j}n_k$. Creation and annihilation have the actions

$$
a_j^\dagger\lvert\mathbf n\rangle
=(1-n_j)(-1)^{\pi_j}\lvert\mathbf n+\mathbf e_j\rangle,
\qquad
a_j\lvert\mathbf n\rangle
=n_j(-1)^{\pi_j}\lvert\mathbf n-\mathbf e_j\rangle.
$$

Read each formula only on its allowed occupation branch: creation
on an occupied mode and annihilation on an empty mode give the zero
vector. They do not produce a state with occupation 2 or -1.

The anticommutator is $\{A,B\}=AB+BA$, and the commutator is
$[A,B]=AB-BA$. The canonical anticommutation relations (CAR) are

$$
\{a_p,a_q^\dagger\}=\delta_{pq}I,\qquad
\{a_p,a_q\}=0,\qquad
\{a_p^\dagger,a_q^\dagger\}=0.
$$

Here $\delta_{pq}$ is 1 for equal indices and 0 otherwise.
Normal ordering puts creation operators before annihilation operators,
using those relations. For example
$a_p a_q^\dagger=\delta_{pq}I-a_q^\dagger a_p$.
The contraction term must not be discarded when the indices coincide.
Taking an adjoint reverses a product:
$(AB)^\dagger=B^\dagger A^\dagger$.

The raw Hamiltonian convention is

$$
H_\mathrm{el}
=\sum_{pq}h_{pq}a_p^\dagger a_q
+\frac12\sum_{pqrs}g_{pqrs}
a_p^\dagger a_q^\dagger a_s a_r.
$$

With a complete unrestricted double-bar tensor
$\bar g_{pqrs}=g_{pqrs}-g_{pqsr}$, the equivalent two-body sum
has prefactor $1/4$. Restricting to $p<q$ and $r<s$ requires
the appropriate antisymmetrised coefficients and removes that
quarter. Restricting a raw tensor and retaining its old prefactor
is not the same transformation. *The Notation Minefield* works
the nonzero-exchange case explicitly.

## Pauli and Majorana normalisations

The single-qubit matrices are

$$
I=\begin{pmatrix}1&0\\0&1\end{pmatrix},\quad
X=\begin{pmatrix}0&1\\1&0\end{pmatrix},\quad
Y=\begin{pmatrix}0&-i\\i&0\end{pmatrix},\quad
Z=\begin{pmatrix}1&0\\0&-1\end{pmatrix}.
$$

Each nonidentity Pauli squares to $I$.
$XY=iZ$, $YZ=iX$, $ZX=iY$; reversing either distinct
nonidentity pair changes the sign. Operators acting on distinct
qubits commute. Tensor products multiply component by component
with their phases retained.

A phase-free Pauli string is one tensor product of $I,X,Y,Z$.
Its **support** is the set of nonidentity positions; its **weight**
is the size of that set. The identity string has weight zero,
not one. A sum of strings is not itself a single Pauli string.
The phase-free strings form an operator basis; including
$\{\pm1,\pm i\}$ gives the Pauli group closed under multiplication.

For the convention $\lvert1\rangle=$ occupied,

$$
a_j^\dagger=\left(\prod_{k<j}Z_k\right)\frac{X_j-iY_j}{2},
\qquad
a_j=\left(\prod_{k<j}Z_k\right)\frac{X_j+iY_j}{2},
\qquad
n_j=a_j^\dagger a_j=\frac{I-Z_j}{2}.
$$

Define Hermitian Majoranas by

$$
\gamma_{2j}=a_j+a_j^\dagger,\qquad
\gamma_{2j+1}=-i(a_j-a_j^\dagger).
$$

Their normalisation in this book is
$\{\gamma_u,\gamma_v\}=2\delta_{uv}I$, so every
$\gamma_u^2=I$. Inverting the pair gives

$$
a_j=\frac{\gamma_{2j}+i\gamma_{2j+1}}2,\qquad
a_j^\dagger=\frac{\gamma_{2j}-i\gamma_{2j+1}}2,\qquad
n_j=\frac{I+i\gamma_{2j}\gamma_{2j+1}}2.
$$

A source normalising Majoranas to square $I/2$ uses different
factors. Importing its ladder formula without changing the
normalisation fails the CAR. Likewise, the relative $i$ in
the pairing is essential, not a harmless phase convention for
the whole operator.

An exact encoding of the **full** Fock space for $n$ fermionic
modes must accommodate its $2^n$ states and hence needs at least
$n$ qubits. A fixed-charge or symmetry-sector encoding has a
smaller domain and can need fewer. This dimension argument does
not say that every mode must live on its own identifiable qubit.

For tree-based constructions distinguish qubit nodes, labelled
terminal paths, selected Majorana strings and paired fermionic
modes. They are different counts. Jiang et al.'s optimal ternary
construction gives a mathematical weight bound; an API helper's
particular tree shape must be measured or derived separately.
Neither a "balanced" name nor a small successful CAR census
proves the helper attains the ideal bound for every size.

## Sector vocabulary

| Term | What is fixed | What need not be fixed |
|:---|:---|:---|
| Number sector | $N_e$, an eigenvalue of $\sum_j n_j$ | Total spin |
| Number-parity sector | $(-1)^{N_e}$ | The number itself |
| Spin-projection sector | $M_S=(N_\alpha-N_\beta)/2$, in units of $\hbar$ | Total spin $S$ |
| Singlet / triplet | Total spin $S=0$ / $S=1$ | A triplet's $M_S$ can be $-1,0,1$ |
| Z₂ symmetry sector | Eigenvalues $s_j=\pm1$ of a commuting independent generator set | Every other charge or spin label |
| Fermionic / bosonic species | The particle algebra used for a mode | A molecular symmetry eigenvalue |

In interleaved spin order, mode $2p$ is $p\alpha$ and mode
$2p+1$ is $p\beta$. Recover the spatial index using integer
division by 2, not rounding; the remainder identifies the spin.
Equal alpha and beta counts imply $M_S=0$, not necessarily a singlet.

A Pauli symmetry generator $G$ is Hermitian, satisfies $G^2=I$,
and commutes with the Hamiltonian. A simultaneous sector requires
the chosen generators to commute with one another as well.
The **centralizer** is the set commuting with the Hamiltonian;
two of its elements can anticommute with each other.

For diagonal tapering, replace the selected $Z_q$ by its
physical eigenvalue $s_q$ and delete the corresponding position.
For Clifford tapering, first transform the Hamiltonian and its
sector consistently. Conjugation $H'=UHU^\dagger$ also maps
states to $U\lvert\psi\rangle$; signs of transformed generators
must be retained. Keep a map from old indices to surviving qubits.

Binary symplectic representations use arithmetic over
$\mathrm{GF}(2)$: addition is XOR, so $1+1=0$.
They encode Pauli support and commutation, but discarded scalar
phases must be tracked separately during signed conjugation.
Rank counts independent constraints; duplicating a generator
does not remove another qubit.

## Which norm, which error?

For a state vector, $\|v\|_2=\sqrt{\sum_j|v_j|^2}$.
For an operator, the spectral/operator norm is

$$
\|A\|=\sup_{\|v\|_2=1}\|Av\|_2.
$$

For Hermitian $A$, it is the largest absolute eigenvalue.
The Frobenius norm $\|A\|_F=\sqrt{\mathrm{Tr}(A^\dagger A)}$
is a different quantity. A coefficient 1-norm for
$H=\sum_j c_jP_j$ is

$$
\lambda=\sum_j|c_j|,\qquad \|H\|\leq\lambda.
$$

The bound uses $\|P_j\|=1$. Equality need not hold.
A measurement budget often omits the known identity coefficient;
declare that convention rather than silently reusing the same
$\lambda$. Hamiltonian norms carry energy units; an error
$\|U-\widetilde U\|$ between unitaries is dimensionless.

For a normalised input,
$\|(U-\widetilde U)\lvert\psi\rangle\|_2\leq
\|U-\widetilde U\|$. A bound on a few spectral moments is
not a bound of that form, nor does it automatically bound every
individual eigenvalue to the same tolerance. Hermitian eigenvalues
can be compared directly after sorting when the intended test is
a spectrum comparison. Matrix entries and labelled states require
their own comparison.

Model error, integral/pruning error, product-formula error, sampling
uncertainty and phase-register resolution are different entries in
an error budget. They are not all fixed by increasing Trotter steps.

## Rotations, statistics and phase

The single-qubit convention is $R_z(\phi)=e^{-i\phi Z/2}$.
A Pauli factor $e^{-icP\Delta t}$ therefore needs an Rz of
$2c\Delta t$ inside its parity-computation circuit.
An identity term $c_0I$ contributes $e^{-ic_0t}$.
That global phase does not change an uncontrolled state's
measurement probabilities. Controlling the operation turns it
into a relative phase between control branches, so phase
estimation must account for it.

A Pauli measurement outcome is a random variable $X\in\{-1,+1\}$.
Its mean is $\mu=\mathbb E[X]$ and its variance is
$\mathbb E[(X-\mu)^2]=1-\mu^2$.
For $M$ independent repetitions the sample mean has variance
$(1-\mu^2)/M$. A standard error estimates uncertainty in that
mean; it is not a guarantee of ansatz quality or optimiser convergence.

For outcomes observed together, covariance is
$\mathrm{Cov}(X,Y)=\mathbb E[(X-\mathbb E X)(Y-\mathbb E Y)]$.
The variance of a grouped weighted sum contains cross terms:

$$
\mathrm{Var}\!\left(\sum_j c_jX_j\right)
=\sum_j c_j^2\mathrm{Var}(X_j)
+2\sum_{j<k}c_jc_k\mathrm{Cov}(X_j,X_k).
$$

Commuting observables can be correlated. Group compatibility
does not make those covariances vanish.

## Bosonic cutoff conventions

If a bosonic mode is truncated to **$d$ levels**, the retained
occupations are $0,\ldots,d-1$. The truncated annihilation
operator is

$$
b_d=\sum_{m=1}^{d-1}\sqrt m\,\lvert m-1\rangle\langle m\rvert.
$$

Its adjoint cannot raise the top retained state. Accordingly

$$
[b_d,b_d^\dagger]=I_d-d\lvert d-1\rangle\langle d-1\rvert,
$$

not $I_d$. Taking the trace gives zero on both sides, as every
finite-dimensional commutator must. An exact finite matrix
representation of the infinite canonical commutation relation
does not exist.

Unary one-hot encoding uses $d$ qubits and a $d$-dimensional
valid subspace; neighbouring-level transitions have two-qubit
support within that subspace. Binary and Gray labellings use
$\lceil\log_2 d\rceil$ qubits, with unused computational states
when $d$ is not a power of two. Gray labels make neighbouring
labels differ in one bit, but the required projectors mean a
transition is not generally a weight-one Pauli operator.

Do not confuse a largest retained occupation $m_\max$ with the
number of levels: $d=m_\max+1$. Check population near the cutoff
and convergence under a larger $d$; a compact encoding does
not remove truncation error.

For independent fermionic and bosonic species, cross-species
operators commute. Within each species use its own algebra.
In particular, infinite-space bosonic normal ordering gives
$bb^\dagger=b^\dagger b+I$, whereas the truncated matrices
have the boundary correction above. Formal normal ordering
before truncation and numerical multiplication after truncation
must not be confused.

## Reused letters

The overlap $S$ between molecular orbitals, the total-spin
quantum number $S$, and the single-qubit S gate are unrelated
objects. Alpha/beta can denote spin functions or state amplitudes;
the local definition decides. A geometric bond length and a
Trotter repetition count sometimes share a letter but not a unit.
When returning to a formula, read its local symbol definitions,
not just the nearest familiar glyph.
