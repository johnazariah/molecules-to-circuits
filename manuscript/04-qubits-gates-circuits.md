# Chapter 4: The Quantum Computer's Vocabulary

_We've spent three chapters learning what the molecule wants to say. Now we need to learn what the quantum computer can hear._

## In This Chapter

- **What you'll learn:** How states, Pauli matrices and tensor products fit together; how phase differs from probability; how CNOT can create entanglement; and which gates are universal.
- **Why this matters:** The encoding (next chapter) translates fermionic operators into qubit operations. If you don't understand the target language, you can't evaluate the translation.
- **Prerequisites:** Chapters 1–3 (you have the molecular Hamiltonian). Basic familiarity with 2×2 matrices is assumed.

---

## Two Worlds, One Problem

At this point in the story, we have two things that don't talk to each other.

On the **chemistry side**: a Hamiltonian written as a polynomial in creation and annihilation operators, $\hat{H} = \sum h_{pq} a_p^\dagger a_q + \ldots$, parameterised by molecular integrals. Its ground state lives in a space of electron configurations whose dimension grows combinatorially.

On the **computing side**: a quantum processor made of qubits — two-level quantum systems that can be in superposition and can be entangled with each other.

The promise of quantum simulation is that the second can solve the first. But to see *how*, we need to understand what a quantum computer can actually do — not in the abstract "it tries all possibilities at once" sense (which is wrong), but concretely: what are the elementary operations, what do they cost, and are they enough?

---

## Qubits: The Power of Superposition

A classical bit is 0 or 1. End of story.

A **qubit** can be in any superposition of $\lvert 0\rangle$ and $\lvert 1\rangle$:

$$\lvert\psi\rangle = \alpha\lvert 0\rangle + \beta\lvert 1\rangle$$

where $\alpha$ and $\beta$ are complex amplitudes with $\lvert\alpha\rvert^2 + \lvert\beta\rvert^2 = 1$. When you measure the qubit, you get 0 with probability $\lvert\alpha\rvert^2$ and 1 with probability $\lvert\beta\rvert^2$. The superposition collapses — you can't read out both amplitudes.

This seems limiting: if one measurement cannot reveal the amplitudes, what
good are they? The useful distinction from a probability distribution is
**interference**. Amplitudes add before we square their magnitudes, so
different contributions can reinforce or cancel. Designing those
contributions is an algorithmic task; having a superposition does not
automatically amplify the answer we want.

For our purposes, the key fact is that an arbitrary pure $n$-qubit state
has $2^n$ complex amplitudes, subject to normalisation and an irrelevant
overall phase. Four qubits provide 16 basis states, containing H₂'s six
two-electron configurations. For 100 qubits, an explicit general state
vector would have roughly $10^{30}$ entries. Some structured states admit
compact classical descriptions, and not every such quantum state is
efficiently preparable. The large state space is a resource, not a
speedup certificate.

### Phase is information that a single basis cannot see

The states

$$|+\rangle=\frac{|0\rangle+|1\rangle}{\sqrt2},
\qquad |-\rangle=\frac{|0\rangle-|1\rangle}{\sqrt2}$$

both give equally likely 0 and 1 outcomes in the computational basis.
Their **relative phase** differs: one coefficient has changed sign
relative to the other. They are orthogonal states, not two notations for
the same state.

By contrast, multiplying *both* coefficients by $e^{i\varphi}$ is a
**global phase**. For any measurement amplitude,
$|\langle x|e^{i\varphi}\psi\rangle|^2
=|\langle x|\psi\rangle|^2$, so it cannot change a measurement
probability. We will use the Hadamard gate below to convert the relative
sign in $|+\rangle$ or $|-\rangle$ into a definite 0 or 1 outcome.

---

## Several Qubits: Products, Sums and Labels

A **tensor product** combines state spaces. For two qubits, the four
products $|0\rangle|0\rangle$, $|1\rangle|0\rangle$,
$|0\rangle|1\rangle$, $|1\rangle|1\rangle$ form a basis.
We abbreviate $|q_0\rangle|q_1\rangle$ by $|q_0q_1\rangle$,
with qubit 0 displayed on the left.

If each qubit has its own state, the joint state is a **product state**.
For example,

$$
(a|0\rangle+b|1\rangle)_0(c|0\rangle+d|1\rangle)_1
=ac|00\rangle+bc|10\rangle+ad|01\rangle+bd|11\rangle.
$$

A **general** pure state is instead

$$|\Psi\rangle=\sum_{q_0,\ldots,q_{n-1}\in\{0,1\}}
c_{q_0\cdots q_{n-1}}|q_0\cdots q_{n-1}\rangle,
\qquad \sum_q|c_q|^2=1.$$

It need not factor into one vector for each qubit. A pure state that
cannot factor across a proposed split of the register is **entangled**
across that split. For two qubits, arrange the four coefficients in
$\begin{pmatrix}c_{00}&c_{01}\\c_{10}&c_{11}\end{pmatrix}$.
A nonzero product state has rank one, hence
$c_{00}c_{11}-c_{01}c_{10}=0$. This provides a small, useful
entanglement test for pure states, not for arbitrary statistical mixtures.

### The label-to-row card

A displayed label and an integer array index are not the same notation.
Our state-vector row is

$$b=\sum_{j=0}^{n-1}q_j2^j.$$

Qubit 0 is therefore the least significant bit of the integer, although
it is the leftmost character of our displayed label.

| Displayed label | Occupied/set indices | Integer row $b$ |
|:---:|:---|:---:|
| $\lvert00\rangle$ | None | 0 |
| $\lvert10\rangle$ | 0 | 1 |
| $\lvert01\rangle$ | 1 | 2 |
| $\lvert11\rangle$ | 0 and 1 | 3 |
| $\lvert1100\rangle$ | 0 and 1 in a four-qubit register | 3 |

For the last row, the 16-component vector has a 1 at row 3, not row 12.
The ordinary binary numeral for the integer 3 is `0011`; it is not our
displayed occupation label. A dimension count or an eigenvalue list
cannot detect a consistently reversed labelling convention.

---

## Operations on a Single Qubit

In the ordered single-qubit basis $(|0\rangle,|1\rangle)$, these basis
vectors are $(1,0)^T$ and $(0,1)^T$. A closed-system gate is a
$2\times2$ **unitary** matrix $U$, satisfying $U^\dagger U=I$ so that
it preserves state norm. Measurements and noise are not, in general,
unitary gates.

An observable such as energy is represented by a **Hermitian** matrix
$A=A^\dagger$, whose eigenvalues are real. Hermitian and unitary are
different properties; the Pauli matrices happen to have both.

**The Pauli operators** — you'll soon see them from the chemistry side (after encoding, the Hamiltonian lives in Pauli space — Chapter 5). Now meet them as gates:

- **$X$** (bit-flip): swaps $\lvert 0\rangle$ and $\lvert 1\rangle$. The quantum analogue of a NOT gate.
- **$Z$** (phase-flip): leaves $\lvert 0\rangle$ alone, applies a minus sign to $\lvert 1\rangle$. This is a purely quantum operation — it changes the *phase* of the superposition without changing the computational-basis measurement probabilities.
- **$Y$** = $iXZ$: both a bit-flip and a phase-flip.

Here are the matrices, including the identity:

$$
I=\begin{pmatrix}1&0\\0&1\end{pmatrix},\quad
X=\begin{pmatrix}0&1\\1&0\end{pmatrix},\quad
Y=\begin{pmatrix}0&-i\\i&0\end{pmatrix},\quad
Z=\begin{pmatrix}1&0\\0&-1\end{pmatrix}.
$$

Their columns tell us their actions. In particular,
$Y|0\rangle=i|1\rangle$ and $Y|1\rangle=-i|0\rangle$.
Those factors of $i$ are not decoration: multiplying several ladder
operators in Chapter 6 depends on them.

### Multiplication, including the signs

All three nonidentity Paulis square to $I$. The other products follow
from matrix multiplication; in this table the row factor acts on the
**left** of the column factor:

| $\times$ | $I$ | $X$ | $Y$ | $Z$ |
|:---:|:---:|:---:|:---:|:---:|
| $I$ | $I$ | $X$ | $Y$ | $Z$ |
| $X$ | $X$ | $I$ | $iZ$ | $-iY$ |
| $Y$ | $Y$ | $-iZ$ | $I$ | $iX$ |
| $Z$ | $Z$ | $iY$ | $-iX$ | $I$ |

For example,
$XY=\begin{pmatrix}i&0\\0&-i\end{pmatrix}=iZ$,
whereas $YX=-iZ$. Reversing factors is not harmless.
The **commutator** $[A,B]=AB-BA$ tests whether they commute;
the **anticommutator** $\{A,B\}=AB+BA$ was introduced in Chapter 2.
Here $[X,Y]=2iZ$ and $\{X,Y\}=0$.

Why are the Paulis special? Because they form a **basis** for all $2 \times 2$ Hermitian matrices. Any single-qubit observable — any Hamiltonian term — can be written as a real-linear combination of $I$, $X$, $Y$, $Z$. (With complex coefficients, they span all $2 \times 2$ matrices.) This is why the encoded Hamiltonian is a sum of Pauli strings: it's the natural basis for qubit operators, just as $\{\lvert 0\rangle, \lvert 1\rangle\}$ is the natural basis for qubit states.

**The Hadamard gate** ($H$) creates superposition from a definite state:

$$H\lvert 0\rangle = \frac{\lvert 0\rangle + \lvert 1\rangle}{\sqrt{2}}, \qquad H\lvert 1\rangle = \frac{\lvert 0\rangle - \lvert 1\rangle}{\sqrt{2}}$$

Its matrix is
$H=2^{-1/2}\begin{pmatrix}1&1\\1&-1\end{pmatrix}$,
and $H^2=I$. Thus $H|+\rangle=|0\rangle$ and
$H|-\rangle=|1\rangle$: the relative phase that a computational-basis
measurement missed becomes a certain measurement outcome. A classical
50/50 mixture of 0 and 1 would remain 50/50 after applying $H$ to
each prepared state. Interference uses amplitudes, not merely ignorance
about which basis state was prepared.

### Rotation angles have a convention

For a Pauli $P$, we define

$$R_P(\theta)=e^{-i\theta P/2}
=\cos(\theta/2)I-i\sin(\theta/2)P.$$

The last equality follows by grouping even and odd powers in the
exponential series, using $P^2=I$. In particular,

$$R_z(\theta)=
\begin{pmatrix}e^{-i\theta/2}&0\\0&e^{i\theta/2}\end{pmatrix}.$$

This changes the relative phase by $\theta$, while leaving computational
basis probabilities unchanged. It gives
$R_z(\pi)|+\rangle=-i|-\rangle$; the leading $-i$ is global.
The matrix $R_x$ uses the same convention with $X$ in place of $Z$.

A Hamiltonian term $cP$ evolving for physical time $t$ contributes
$e^{-ictP/\hbar}=R_P(2ct/\hbar)$.
The energy-time product divided by $\hbar$ is dimensionless. When we
later use atomic units for time as well as energy, the time unit is
$\hbar/E_h$ and the gate angle is $2ct$. A coefficient in hartree
alone is **not** a rotation angle; a time and a sign convention are
still required.

---

## Pauli Strings: The Operator Basis for a Register

A **Pauli string** assigns one of $I,X,Y,Z$ to each qubit.
`XZI` means $X_0Z_1I_2$ in our displayed ordering. Its **support** is
the set of nonidentity positions, here $\{0,1\}$; its **weight** is
the size of that set, here 2. The identity string has weight zero.
A weighted sum such as $0.3\,XZI-0.2\,IIZ$ is not a single Pauli
string and does not have one support size without specifying what
summary we mean.

Operators on different factors commute: $X_0Z_1=Z_1X_0$. Operators
on the *same* factor obey the multiplication table above. For example,

$$ (X_0Z_1)(Y_0X_1)
=(X_0Y_0)(Z_1X_1)
=(iZ_0)(iY_1)=-Z_0Y_1.$$

This factor-by-factor multiplication is the symbolic work the encoding
library performs.

### From a string to a matrix, without reversing the physics

The **Kronecker product** of matrices makes their tensor action explicit:
if $A=(a_{ij})$, then $A\otimes B$ is the block matrix with blocks
$a_{ij}B$. It satisfies
$(A\otimes B)(u\otimes v)=(Au)\otimes(Bv)$.

To respect our integer-row convention, a displayed
$P_0P_1\cdots P_{n-1}$ has dense matrix
$P_{n-1}\otimes\cdots\otimes P_0$.
The column vector for a displayed product state is correspondingly
$|q_{n-1}\rangle\otimes\cdots\otimes|q_0\rangle$.
One may choose the opposite dense ordering, but then rows must change
too. We choose once and keep it.

For a concrete two-qubit action,

$$X_0Z_1|01\rangle=-|11\rangle.$$

Qubit 0 flips from 0 to 1; qubit 1 stays at 1 and supplies the minus
sign. In row order $(|00\rangle,|10\rangle,|01\rangle,|11\rangle)$,

$$X_0Z_1\ \longleftrightarrow\ Z\otimes X
=\begin{pmatrix}
0&1&0&0\\
1&0&0&0\\
0&0&0&-1\\
0&0&-1&0
\end{pmatrix}.$$

Column 2 has its nonzero entry $-1$ at row 3, exactly as the labelled
action requires. This one-column check is often more revealing than
diagonalising the whole matrix.

The $4^n$ Pauli strings span all operators on $n$ qubits. Because they
are Hermitian, a Hermitian Hamiltonian has real coefficients in its
fully collected Pauli expansion. Intermediate fermionic products may
have complex coefficients that cancel when the Hermitian terms are
assembled. That distinction will matter in Chapter 6.

Logical cost models often count two-qubit operations more heavily than
single-qubit rotations. Physical duration, fidelity, native gate, and
connectivity are backend- and calibration-specific, so this book does not treat
one timing/error figure as a platform constant.

---

## The Hard Part: Entanglement

Single-qubit gates cannot turn a product input into an entangled state:
$(U_0\otimes U_1)(|\psi_0\rangle\otimes|\psi_1\rangle)$
is still a product. Such circuits can perform local rotations and
interference, but their pure state can be tracked using $n$ small
vectors rather than one general $2^n$-component vector.

Entanglement allows states that this product description cannot represent.
It is important for general multiqubit computation, though entanglement
alone is no guarantee of a useful computational advantage.

Consider the Bell state
$|\Phi^+\rangle=(|00\rangle+|11\rangle)/\sqrt2$.
Computational-basis measurement gives 00 or 11 with equal probabilities.
Those outcomes alone are easy to reproduce classically: toss a coin
and prepare either 00 or 11. Perfect agreement in one basis is not
evidence against shared classical randomness.

The difference is coherence. Using
$|0\rangle=(|+\rangle+|-\rangle)/\sqrt2$ and
$|1\rangle=(|+\rangle-|-\rangle)/\sqrt2$ gives

$$|\Phi^+\rangle=\frac{|++\rangle+|--\rangle}{\sqrt2}.$$

The Bell state is also perfectly correlated when both qubits are
measured in the $X$ basis $\{|+\rangle,|-\rangle\}$.
The coin-prepared mixture instead gives all four $X$-basis outcome
pairs with probability $1/4$:

| Preparation | Computational-basis outcomes | $X$-basis outcomes |
|:---|:---|:---|
| Bell superposition | 00, 11: $1/2$ each | ++, --: $1/2$ each |
| Coin-selected 00 or 11 | 00, 11: $1/2$ each | ++, +-, -+, --: $1/4$ each |

This comparison distinguishes these two preparations. Bell's theorem
is the stronger statement about the impossibility of reproducing
certain correlations across suitable measurement settings with a
local hidden-variable model; the single $Z$-basis correlation does
not establish it.

For our molecule, the practical distinction is between a coherent
superposition of determinants and a random selection of determinants.
Chapter 6 calculates their different energy expectations before
introducing density matrices as notation for both.

### The CNOT gate: the entanglement maker

The **controlled-NOT** (CNOT) gate is the simplest operation that creates entanglement. It acts on two qubits — a *control* and a *target* — and flips the target if the control is $\lvert 1\rangle$:

$$\text{CNOT}\lvert 00\rangle = \lvert 00\rangle, \quad \text{CNOT}\lvert 01\rangle = \lvert 01\rangle, \quad \text{CNOT}\lvert 10\rangle = \lvert 11\rangle, \quad \text{CNOT}\lvert 11\rangle = \lvert 10\rangle$$

On its own, CNOT doesn't look quantum — it's just a conditional flip. But apply it after a Hadamard:

$$|00\rangle\xrightarrow{H_0}
\frac{|00\rangle+|10\rangle}{\sqrt2}
\xrightarrow{\mathrm{CNOT}_{0\to1}}
\frac{|00\rangle+|11\rangle}{\sqrt2}.$$

Two gates — one Hadamard, one CNOT — and we've created a maximally entangled state from a product state. No classical operation can do this.

The state is not a product: its coefficient test gives
$c_{00}c_{11}-c_{01}c_{10}=1/2$, not zero.
CNOT is *capable* of entangling; it does not entangle every input.
On $|00\rangle$ alone it does nothing. On a product input with
target $|+\rangle$, flipping that target leaves it unchanged.

### Why two-qubit operations dominate this logical model

The standard Pauli-rotation staircase uses CNOTs to compute and uncompute parity,
so its logical count grows with Pauli weight. A physical compiler may replace
CNOT with CZ, ECR, Mølmer–Sørensen, or another native entangler and may add
routing operations for limited connectivity. Consult a named backend's dated
calibration record for physical timing and error data.

> **If you don't have access to quantum hardware**, you can still run the
> logical examples in a simulator. Quokka, Qiskit Aer, Cirq, and other tools
> have different supported sizes and execution models; check the selected
> backend rather than assuming a fixed qubit limit.
>
> The staircase count remains useful as a pre-compilation comparison. It does
> not by itself predict physical fidelity or feasibility.

> **The bottom line:** Pauli weight determines the standard logical staircase
> count. Hardware cost requires compilation and calibration data.

---

## Is This Enough? Universality

A natural question: are Pauli gates and CNOT sufficient for *all* quantum computation? Or do we need exotic gates that we haven't mentioned?

**Pauli gates and CNOT alone are not sufficient.** Starting in a
computational-basis state, they can only permute its label and change
its phase; they cannot even prepare $|+\rangle$. Adding arbitrary
single-qubit rotations changes the answer.

Any unitary operation on $n$ qubits can be decomposed into a sequence of:
- **Single-qubit rotations** ($R_x(\theta)$, $R_y(\theta)$, $R_z(\theta)$ for arbitrary $\theta$)
- **CNOT gates**

This is **universality for unitary gates**. Arbitrary-angle $R_z$ and
$R_x$ rotations, together with CNOT, can synthesise any finite-register
unitary up to global phase. A discrete gate set such as
$\{H,T,\mathrm{CNOT}\}$, where
$T=\operatorname{diag}(1,e^{i\pi/4})$, instead approximates arbitrary
unitaries to a chosen precision. These are related but distinct
statements. Neither says every desired unitary has a short circuit.
Measurements and classical control complete the computational model.

For quantum simulation specifically, we need even less. The Hamiltonian is a sum of Pauli strings, and each Pauli string's time evolution $e^{-i\theta P}$ decomposes into:
- A few single-qubit gates (to rotate into the right basis)
- A chain of CNOTs (to entangle the relevant qubits)
- One $R_z$ rotation (to apply the phase)
- The reverse of the above (to undo the basis change and entanglement)

This decomposition — the **CNOT staircase** — implements a nonidentity
Pauli rotation using $2(w-1)$ CNOT gates, where $w$ is its weight.
We will develop it in full detail in the Circuits stage (Chapters 14–17).

For now, the key formula is:

$$\boxed{\text{CNOTs per Pauli rotation} = 2(w - 1)}$$

This formula applies to one nonidentity Pauli **evolution** with $w\ge1$
in the standard logical staircase, before cancellation or routing.
The identity gives a global phase for uncontrolled evolution and needs
no staircase; substituting $w=0$ into the formula is meaningless.
Ordinary measurement of a Pauli string is a different task: local
basis changes and single-qubit measurements suffice. A weight-four
string need not cost six CNOTs to *measure*. Chapters 14 and 20 keep
that distinction explicit.

---

## The Cost Table

To make this concrete:

| Pauli weight $w$ | CNOTs | Example |
|:---:|:---:|:---|
| 1 | 0 | Single $Z$ — just an $R_z$ |
| 2 | 2 | $ZZ$ — one CNOT, one $R_z$, one reverse CNOT |
| 4 | 6 | $XXYY$ — an H₂ coupling term |
| 5 | 8 | A logarithmic-weight example |
| 12 | 22 | A heavier logical Pauli rotation |
| 100 | 198 | A near-register-wide logical rotation |

This table is for the standard all-to-all logical staircase. It does not predict
a molecule's total circuit or hardware feasibility; routing, cancellations,
native gates, and the distribution of Hamiltonian terms still matter.

---

## Key Takeaways

- A **qubit** stores a superposition of $\lvert 0\rangle$ and $\lvert 1\rangle$. An $n$-qubit register stores $2^n$ amplitudes — an exponentially large state space.
- **Pauli operators** ($I$, $X$, $Y$, $Z$) form a basis for single-qubit matrices. Tensor products of them give a basis for register operators, so the encoded Hamiltonian is a sum of Pauli strings.
- **CNOT** is the entangling gate used by the standard logical staircase. Its count is a useful pre-compilation metric, not a platform-independent physical cost.
- Arbitrary-angle $R_z$, $R_x$ and CNOT are **universal for unitaries**. Pauli gates plus CNOT alone are not.
- The **CNOT staircase** decomposes a Pauli rotation into $2(w-1)$ CNOTs, where $w$ is the Pauli weight. This is the conversion factor from encoding choice to circuit cost.

## Common Mistakes

1. **"A quantum computer tries all possibilities at once."** A superposition does not let us read all its amplitudes. A useful algorithm must exploit structure and produce informative measurement probabilities; a large state space alone proves no speedup.

2. **Treating single-qubit gates as free.** We emphasise CNOTs in a named logical cost model. Rotation synthesis, depth, measurements and physical errors can still dominate a complete computation.

3. **Forgetting connectivity.** A backend may require routing for a non-native interaction. A SWAP has a three-CNOT decomposition, but actual native gates and routing costs depend on the architecture and compiler.

## Exercises

1. **Bell state.** Starting from $\lvert 00\rangle$, apply $H$ to qubit 0, then CNOT(0→1). Write out the resulting state. Why is it entangled? (Hint: can you write it as a product $\lvert\psi_1\rangle \otimes \lvert\psi_2\rangle$?)

2. **CNOT cost.** Two hypothetical representations of a Hamiltonian have 100 nonidentity terms each. In representation A, 20 have weight 50 and 80 have weight 2. In B, the 20 heavy terms have weight 5 and the rest still have weight 2. Compute the logical CNOT totals for one staircase evolution per term, without routing or cancellation. Why is this arithmetic not a molecular benchmark or a measurement cost?

3. **Universality.** Prove that single-qubit gates preserve a product input's product form. Explain why this makes those pure-state circuits classically easy to track, without claiming that they cannot perform interference.

4. **One matrix column.** Construct the dense matrix for displayed `XZ` in the stated row convention. Apply it to $|01\rangle$. Then construct the unreversed Kronecker product and show that it acts differently on this same labelled input.

5. **Phase arithmetic.** Compute $(X_0Z_1)(Y_0X_1)$ and $(Y_0X_1)(X_0Z_1)$. Do the two strings commute even though their factors anticommute at each of two positions? Find their weights.

6. **Mixture versus coherence.** Derive the $X$-basis probabilities in the Bell/coin table. Why does agreement in the computational basis fail to identify the preparation?

7. **Rotation convention.** Apply $R_z(\pi)$ to $|+\rangle$, then apply $H$. What computational-basis outcome is certain? For a hypothetical $0.2\,Z$ Ha Hamiltonian and time $0.5\,\hbar/E_h$, what is the gate angle?

## Further Reading

- Nielsen, M. A. and Chuang, I. L. *Quantum Computation and Quantum Information.* Cambridge UP, 2000. The standard reference — we've barely scratched the surface.
- Preskill, J. "Quantum Computing in the NISQ Era and Beyond." *Quantum* 2, 79 (2018). Why gate count matters on near-term hardware.
- Mermin, N. D. *Quantum Computer Science: An Introduction.* Cambridge UP, 2007. A gentler introduction to the gate model, written for computer scientists.

---

**Previous:** [Chapter 3 — From Spatial to Spin-Orbital Integrals](03-spin-orbitals.html)

**Next:** [Chapter 5 — A Visual Guide to Encodings](05-visual-encodings.html)
