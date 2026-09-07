# Selected Worked Solutions

These are worked answers to selected exercises, not a complete answer
key. Each identifies the chapter and exercise by name so that the
reasoning remains findable if exercises are reordered. Use the
question's stated model and inputs: a hypothetical coefficient table
is not the canonical H₂ fixture.

## The Quantum Computer's Vocabulary: Bell state

Starting in $\lvert00\rangle$, the Hadamard on qubit 0 gives
$(\lvert00\rangle+\lvert10\rangle)/\sqrt2$ in the book's
q0-leftmost display. The controlled-NOT with control 0 and target 1
therefore gives

$$
\lvert\Phi^+\rangle
=\frac{\lvert00\rangle+\lvert11\rangle}{\sqrt2}.
$$

If this were a product
$(a\lvert0\rangle+b\lvert1\rangle)
\otimes(c\lvert0\rangle+d\lvert1\rangle)$,
we would need $ac=bd=1/\sqrt2$ and $ad=bc=0$.
The nonzero values of $ac$ and $bd$ make all four factors
nonzero, contradicting the two zero cross terms. The state is
therefore not a product.

Perfectly correlated Z outcomes alone do not prove that result:
the equal classical mixture of `00` and `11` has those same
outcome probabilities. The coherent Bell state also has
$\langle XX\rangle=1$; that mixture has
$\langle XX\rangle=0$.

## The Quantum Computer's Vocabulary: CNOT cost

The hypothetical Hamiltonian has 100 nonidentity terms. In JW,
20 have weight 50 and 80 have weight 2; in the ternary example,
those same 20 heavy terms have weight 5. Using the unoptimised
logical staircase cost $2(w-1)$ per term gives

$$
C_\mathrm{JW}=20[2(50-1)]+80[2(2-1)]=2120,
$$

$$
C_\mathrm{tree}=20[2(5-1)]+80[2(2-1)]=320.
$$

The difference is 1800 CNOTs per first-order step under this model,
about an 84.9% reduction. Nothing in this arithmetic establishes
a molecular error tolerance, routing overhead or a hardware
runtime advantage. It is a comparison of the supplied weight
distributions under one declared compilation rule.

## Checking Our Answer: Sector analysis

The question uses the normal-ordered electronic Hamiltonian,
without nuclear repulsion. Every one-body monomial
$a_p^\dagger a_q$ kills the vacuum because its rightmost
annihilator does. Every two-body monomial
$a_p^\dagger a_q^\dagger a_s a_r$ does so for the same reason.
Thus

$$
H_\mathrm{el}\lvert0000\rangle=0.
$$

The zero-electron sector contains only this one state, so its
electronic spectrum is the single eigenvalue 0. Adding $V_{nn}I$
would change that eigenvalue to $V_{nn}$. The vacuum argument
does not say that the full molecular Hamiltonian, including its
nuclear constant, has a zero-energy vacuum.

The Pauli expansion can contain an identity coefficient even
though this eigenvalue is zero. Other diagonal Pauli terms
cancel that coefficient on the vacuum. Treating the identity
coefficient alone as the vacuum energy misses those terms.

## Six Encodings, One Interface: Cumulative parity

For occupation `1011`, cumulative parity stores
$q_j=n_0\oplus\cdots\oplus n_j$. The successive values are
$1$, $1\oplus0=1$, $1\oplus0\oplus1=0$, and
$1\oplus0\oplus1\oplus1=1$, giving encoded label `1101`.

The inverse calculation is $n_0=q_0$ and
$n_j=q_{j-1}\oplus q_j$ for $j>0$:
`1101` gives $1$, $1\oplus1=0$, $1\oplus0=1$,
$0\oplus1=1$, recovering `1011`.
The original occupation integer is $1+4+8=13$; the encoded
computational integer is $1+2+8=11$. Both labels use q0-leftmost
display, but they label different computational rows. A matrix
comparison must transform the state convention too.

## Building a Tree Encoding: A missing imaginary unit

The proposed operator is $B=(XZ-YI)/2$. Let $P=XZ$ and $Q=YI$.
Both are Hermitian, square to $I$, and anticommute. Therefore
$B^\dagger=B$ and

$$
B^2=\frac14(P^2-PQ-QP+Q^2)=\frac I2.
$$

It follows that $\{B,B^\dagger\}=2B^2=I$, which passes the
mixed creation-annihilation test. But $\{B,B\}=2B^2=I$,
where the same-type CAR require zero. A single mixed
anticommutator is therefore an inadequate validation.

With the intended relative imaginary unit,
$A=(P-iQ)/2$, the same-type square is
$(P^2-i\{P,Q\}-Q^2)/4=0$, while
$\{A,A^\dagger\}=I$. The missing $i$ changed the algebra;
it was not merely an overall phase on an otherwise valid ladder.

## Why Tapering?: Follow the ledger

The circuit applies CNOT(0→2) and CNOT(1→3). Its computational
action replaces the target bits by
$n_2\oplus n_0$ and $n_3\oplus n_1$.
For the intended H₂ sector, each spin species has odd parity,
so the transformed target bits are both 1 and the corresponding
Z eigenvalues are both $-1$.

Keep qubits 0 and 1 and reconstruct the discarded original
occupations from those target bits:
$n_2=1\oplus n_0$, $n_3=1\oplus n_1$.
The explicit ledger is

| Reduced row | Kept label | Original occupation label | Original row |
|:---:|:---:|:---:|:---:|
| 0 | `00` | `0011` | 12 |
| 1 | `10` | `1001` | 9 |
| 2 | `01` | `0110` | 6 |
| 3 | `11` | `1100` | 3 |

The full-space total parity is the product of the two spin
parities and is therefore $+1$. It is dependent, not a third
independent bit that can be removed. This derivation identifies
the sector without inspecting which block has the lowest energy.

## Diagonal Z₂ Symmetries: Collision and cancellation

For $H=0.7IX+0.7ZX+0.2ZI$, select $Z_0=-1$ and remove qubit 0.
The terms map to $0.7X$, $-0.7X$, and $-0.2I$.
The first two collide and cancel, leaving $H_\mathrm{red}=-0.2I$.
Both remaining eigenvalues are $-0.2$ in the supplied energy units.

The reduction is not obtained by just deleting every `Z` and
keeping each original coefficient. It requires substitution of
the sector signs, deletion of the selected position, and collection
of equal remaining signatures. Counting terms before collection
would also give the wrong reduced inventory.

## General Clifford Tapering: Complete H₂ matrix

The exercise supplies

$$
H=CII+B(ZI+IZ)+DZZ+FYY,
$$

with $C=-1.0534210769165218$, $B=0.3948443633559027$,
$D=0.0112461571508209$, and $F=-0.1812104620151967$ Ha.
In the reduced integer-row order `00`, `10`, `01`, `11`,
the matrix is

$$
\begin{pmatrix}
C+2B+D&0&0&-F\\
0&C-D&F&0\\
0&F&C-D&0\\
-F&0&0&C-2B+D
\end{pmatrix}.
$$

The sign difference between the two off-diagonal blocks comes
from $YY\lvert00\rangle=-\lvert11\rangle$ but
$YY\lvert10\rangle=\lvert01\rangle$.
The even block has roots
$C+D\pm\sqrt{4B^2+F^2}$; the odd block has
roots $C-D\pm|F|$.
Sorting gives the electronic energies

$$
-1.8523881736,\quad -1.2458776961,\quad
-0.8834567721,\quad -0.2319616660\ \mathrm{Ha}.
$$

These are the four states in the selected one-alpha/one-beta
sector, not all six states in the two-electron sector.
Selecting the additional reduced $ZZ=+1$ block would retain
only the paired, closed-shell configurations. That can be a
useful further restriction, but must not be labelled the entire
$N_e=2$, $M_S=0$ space.

For the companion exercise **Generator with a minus sign**, if
$UgU^\dagger=-Z_2$ and the original eigenvalue is $g=-1$,
then $-Z_2=-1$ on the transformed state, hence $Z_2=+1$.
Discarding the minus sign selects the opposite sector.

## From Hamiltonian to Time Evolution: Two uses of XXYY

The input state is already prepared; its preparation cost is
outside both counts. To measure $X_0X_1Y_2Y_3$, apply H on
qubits 0 and 1, and S† followed by H on qubits 2 and 3, then
measure all four in Z. Multiply their four signs classically.
In the stated H/S/S†/CNOT/Rz alphabet this uses six local gates,
no CNOTs and four readouts.

To implement $e^{-i\theta X_0X_1Y_2Y_3}$, use those basis
changes, compute parity with three CNOTs, apply an Rz of
$2\theta$, undo the three CNOTs, and undo the basis changes.
That uses twelve basis-change gates, one Rz and six CNOTs,
with no measurement. The parity must be uncomputed because
the output is still a quantum state to be used later.

The two circuits manipulate the same Pauli label for different
purposes. Assigning the evolution's six CNOTs to every
measurement shot invents a cost the measurement circuit
does not have.

## From Hamiltonian to Time Evolution: A local error

With $A=0.3X$ Ha and $B=0.4Z$ Ha,
$XZ=-iY$ and $ZX=iY$, so
$[A,B]=-0.24iY$ Ha².
Expanding the ordered approximation gives

$$
e^{-iA\delta}e^{-iB\delta}
-e^{-i(A+B)\delta}
=-\frac{\delta^2}{2}[A,B]+O(\delta^3).
$$

For $\delta=0.1$ atomic time units, the leading term is
$0.0012iY$. Since $\|Y\|=1$, its norm is 0.0012,
dimensionless. This is the leading small-step contribution,
not the exact finite-step error and not a guarantee for a
long sequence of repeated steps.

## The CNOT Staircase: A sparse support

For `XIZY`, the nonidentity qubits are 0, 2 and 3. Qubit 1
is not part of the parity computation. The forward staircase
is CNOT(0→2), CNOT(2→3); the reverse is
CNOT(2→3), CNOT(0→2). Thus the logical count is four CNOTs.

There is one X position and one Y position. In the stated
alphabet, X needs H before and after; Y needs S† then H before,
H then S after. Together with the one Rz this gives
$1+2n_X+4n_Y=1+2+4=7$ single-qubit gates.
The Z position needs no basis change.

These logical CNOTs need not be native edges of a hardware
coupling graph. Routing is a separate cost; an identity at
qubit 1 does not by itself authorise or forbid a physical
interaction between 0 and 2.

## Cost Analysis Across Encodings: Same maximum, different cost

The supplied Hamiltonians are
$H_A=ZIII+IZII+IIZI+XXXX$ and
$H_B=ZZZZ+XXXX+YYYY+ZXXZ$, with all coefficients 1 Ha.
Both have four qubits, four terms and maximum weight four.

For $H_A$, three weight-one terms cost no CNOTs and one
weight-four term costs six: $C_A=6$.
For $H_B$, all four terms have weight four: $C_B=24$.
The maximum alone concealed a factor of four in the
first-order staircase cost.

Neither the maximum nor even the complete weight histogram
determines every single-qubit gate count. In this alphabet,
X and Y positions have different basis-change costs. Retain
the actual Pauli strings when reporting full gate totals.

## The Question We Can Now Answer: An energy ledger

The exercise gives electronic FCI energy
$-1.8523881736$ Ha and nuclear repulsion
$+0.7151043391$ Ha, so

$$
E_\mathrm{tot}=-1.1372838345\ \mathrm{Ha}.
$$

With electronic HF energy $-1.8318636465$ Ha, the correlation
energy is

$$
E_\mathrm{corr}
=E_\mathrm{FCI,el}-E_\mathrm{HF,el}
=-0.0205245271\ \mathrm{Ha}.
$$

The nuclear term cancels in that difference because the same
geometry is used on both sides. In a four-qubit Pauli expansion,
the identity coefficient is $\mathrm{Tr}(H)/16$, the average
of the 16 full-space eigenvalues. It is not the ground-state
energy or an energy produced by the exported circuit.

## A Fixed-Bond Water Angle Scan: Resolve the minimum

The three supplied total energies are
$E(98^\circ)=-75.0140506756$,
$E(99^\circ)=-75.0140865447$, and
$E(100^\circ)=-75.0140335106$ Ha.
Relative to 99°, the neighbours are higher by
0.0000358691 and 0.0000530341 Ha, or
0.0358691 and 0.0530341 mHa.
Thus 99° is the lowest of these **sampled** angles.

For a quadratic fitted to equally spaced samples with spacing
$h=1^\circ$, the vertex displacement from the centre is

$$
\Delta v=
\frac{h}{2}
\frac{E(98^\circ)-E(100^\circ)}
{E(98^\circ)-2E(99^\circ)+E(100^\circ)}
\approx-0.0965^\circ.
$$

The interpolated vertex is approximately 98.9035°.
It is not a new FCI calculation at that angle, and it
inherits the assumption that a quadratic is adequate locally.
Both statements remain conditional on fixed O–H length and
the finite basis.

For **Curvature**, the central second difference is
$0.0000889032$ Ha/degree². Converting the angular coordinate
gives

$$
\frac{d^2E}{dv_\mathrm{rad}^2}
\approx0.0000889032\left(\frac{180}{\pi}\right)^2
=0.2918519895\ \mathrm{Ha/radian^2}.
$$

A vibrational frequency additionally needs the kinetic/mass
metric. A full normal-mode calculation uses a suitable stationary
geometry, a mass-weighted Hessian and removal of translational
and rotational modes. This one-dimensional fixed-bond curvature
does not supply all of that information.

## Algorithms: Measurement statistics and covariance

Seven $+1$ and three $-1$ outcomes give a sample mean
$\bar x=(7-3)/10=0.4$. The sum of squared deviations is
$7(1-0.4)^2+3(-1-0.4)^2=8.4$.
The unbiased sample variance is therefore
$s^2=8.4/9=0.9333333$, and the estimated standard error
of the mean is $\sqrt{s^2/10}=0.305505$.

The population identity $\mathrm{Var}(X)=1-\mu^2$
contains the unknown true mean. Substituting the sample mean
gives 0.84; the factor $10/9$ is what turns that plug-in value
into the unbiased sample variance here.

For Z measurements of the Bell state, each individual mean
is zero and variance is one, but the paired outcomes always
agree. Thus $\mathrm{Cov}(Z_0,Z_1)=1$ and

$$
\mathrm{Var}(Z_0+Z_1)=1+1+2=4,\qquad
\mathrm{Var}(Z_0-Z_1)=1+1-2=0.
$$

These are variances of per-shot grouped estimators; for the
average of $M$ independent grouped shots, divide by $M$.
Dropping covariance would incorrectly give 2 for both sums.
Grouping can help or hurt a weighted estimator's variance,
even though it reduces the number of measurement settings.

## Algorithms: Shifted QPE decoding

The supplied convention is
$U=e^{-i(H-E_\mathrm{shift}I)t_0}$, with
$E_\mathrm{shift}=0.5$ Ha, $t_0=1$ atomic time unit and
$E\in[-2,0.25]$ Ha. The corresponding phase
$(E_\mathrm{shift}-E)t_0/(2\pi)$ lies strictly between
0 and 1, so this interval fixes the modulo-$2\pi$ branch.

With measured integer $y=1534$ in a 12-bit register,

$$
\widetilde E
=0.5-\frac{2\pi}{1}\frac{1534}{4096}
\approx-1.8531265286\ \mathrm{Ha}.
$$

One phase bin corresponds to
$2\pi/(4096t_0)=0.0015339807879$ Ha. The decoded value
is an estimate on this grid, not an assertion that the exact
eigenvalue equals the centre of the observed bin.

More phase bits improve the resolution; they do not prepare
the desired eigenstate, repair product-formula error, or by
themselves provide a desired confidence level. The input's
overlap with the target eigenstate determines how often that
eigenphase can be obtained.
