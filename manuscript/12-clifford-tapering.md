# Chapter 12: General Clifford Tapering

_When no single qubit is diagonal, multi-qubit Z₂ symmetries may still exist. A Clifford rotation reveals them._

## In This Chapter

- **What you'll learn:** Binary Pauli commutation, an actual null-space
  calculation, phase-correct Clifford conjugation, and the complete H₂
  reduction begun in Chapter 10.
- **Why this matters:** A single-qubit Z scan can miss conserved
  multi-qubit products. Exposing them requires both a basis change and
  the correct physical signs.
- **Prerequisites:** Chapters 10–11 (diagonal tapering concepts and mechanics).

---

## The Limitation of Diagonal Tapering

Diagonal tapering requires a qubit where *every* Hamiltonian term is I or Z. But consider the 2-qubit Heisenberg model:

$$\hat{H} = J(X_0 X_1 + Y_0 Y_1 + Z_0 Z_1)$$

No single qubit is diagonal — both qubits have X, Y, and Z terms. Diagonal tapering finds nothing.

But the operator $Z_0 Z_1$ **commutes** with every term: $[Z_0 Z_1, X_0 X_1] = [Z_0 Z_1, Y_0 Y_1] = [Z_0 Z_1, Z_0 Z_1] = 0$. It is a valid Z₂ symmetry generator — it just involves *two* qubits instead of one.

If we could rotate $Z_0 Z_1$ onto a single-qubit $Z$ — say, $Z_0 I_1$ — then qubit 0 would become diagonally taperable. The Clifford rotation that achieves this is a simple CNOT.

---

## The Binary Pauli Representation

To find all Z₂ generators systematically, we need a way to represent Pauli strings that makes commutativity easy to check. The trick is to encode each Pauli operator as **two binary bits**:

| Pauli | X bit | Z bit | Think of it as... |
|:---:|:---:|:---:|:---|
| I | 0 | 0 | No flip, no phase |
| X | 1 | 0 | Bit-flip only |
| Y | 1 | 1 | Both flip and phase |
| Z | 0 | 1 | Phase-flip only |

An $n$-qubit Pauli string becomes a binary vector of length $2n$ — the X-bits followed by the Z-bits:

$$\sigma \;\leftrightarrow\; (\underbrace{x_0, x_1, \ldots, x_{n-1}}_{\text{X bits}} \mid \underbrace{z_0, z_1, \ldots, z_{n-1}}_{\text{Z bits}})$$

The API listings are contextual excerpts with FockMap 0.9.0 and
`System.Numerics`, `Encodings` and `Encodings.Tapering` open, as in
`code/ch12-clifford-tapering.fsx`. They illustrate individual operations;
the separate H₂ companion below supplies the complete physical calculation.

```fsharp
let sv = toSymplectic (PauliRegister("XYZ", Complex.One))
// sv.X = [| true; true; false |]   — X has x=1, Y has x=1, Z has x=0
// sv.Z = [| false; true; true |]   — X has z=0, Y has z=1, Z has z=1
```

> **On the name "symplectic":** The quantum computing literature calls this the *symplectic representation*, and the commutativity check below a *symplectic inner product*. The word "symplectic" comes from Greek for "intertwined" — it refers to the fact that commutativity depends on a *crosswise* pairing between the X-bits of one string and the Z-bits of the other (and vice versa). This crosswise structure is what mathematicians call a symplectic form. We'll use the name "binary Pauli representation" when the intuition matters and "symplectic" when referencing the literature.

### Why this representation?

The payoff: two Pauli strings commute if and only if their **crosswise dot product** is zero (mod 2):

$$\text{commute?} \quad \sum_{i=0}^{n-1} (a_{x_i} \cdot b_{z_i} + a_{z_i} \cdot b_{x_i}) \stackrel{?}{=} 0 \pmod{2}$$

We pair the X-bits of $a$ with the Z-bits of $b$, and vice versa.
If the sum is even, they commute; if odd, they anticommute.
At each qubit the contribution counts whether the two local Paulis
introduce a minus sign on exchange. Multiplying those local signs
explains why only the parity of the count matters.

```fsharp
let a = toSymplectic (PauliRegister("XX", Complex.One))
let b = toSymplectic (PauliRegister("ZZ", Complex.One))
commutes a b  // true — XX and ZZ commute
```

---

## Finding All Symmetry Generators

A Pauli string $g$ is a Z₂ symmetry of $\hat{H}$ if it commutes with every term and squares to the identity. Since every phase-free Pauli string (a tensor product of $\{I, X, Y, Z\}$, which is how this book represents them) squares to $I$, the squaring condition is automatic.

The commutation condition $\text{crosswise dot product} = 0$ for all terms
$t_k$ is a system of binary linear equations. Its null space is the Pauli
**centralizer** of the Hamiltonian: every solution commutes with every
Hamiltonian term. An arbitrary basis of that null space need not commute
pairwise. Tapering therefore needs a second step: select an independent
mutually commuting (Abelian) subgroup and retain the Pauli phases
(Bravyi et al., 2017).

```fsharp
let centralizer = findCommutingGenerators hamiltonian
// SymplecticVector[] representing the centralizer candidates.

// A valid tapering implementation must then select independent candidates
// that also commute with one another and track their phases.
```

### GF(2), rank and free variables

$\mathrm{GF}(2)$ is arithmetic with only the values zero and one.
Addition and subtraction are both XOR: $1+1=0$.
Multiplication has the ordinary binary truth table. A **null space**
is the set of vectors $v$ satisfying $Av=0$; here its elements encode
Paulis commuting with every Hamiltonian term.

For an unknown generator $v=(x\mid z)$ and Hamiltonian row
$(x_t\mid z_t)$, put $(z_t\mid x_t)$ into the check matrix $A$.
Then ordinary binary matrix multiplication $Av=0$ is exactly the
crosswise commutation condition. The swapped halves matter.
Coefficients do not enter this matrix, except that zero terms must first
be removed.

Consider $H=XX+YY+ZZ$. With column order
$(x_0,x_1,z_0,z_1)$, its check matrix is

$$A=
\begin{pmatrix}
0&0&1&1\\
1&1&1&1\\
1&1&0&0
\end{pmatrix}.$$

Swap the first and third rows. Use the new first row to clear the
leading one of the second row by XOR:

$$
\begin{pmatrix}
1&1&0&0\\
1&1&1&1\\
0&0&1&1
\end{pmatrix}
\longrightarrow
\begin{pmatrix}
1&1&0&0\\
0&0&1&1\\
0&0&1&1
\end{pmatrix}
\longrightarrow
\begin{pmatrix}
1&1&0&0\\
0&0&1&1\\
0&0&0&0
\end{pmatrix}.
$$

There are two **pivots**, the first nonzero entries in the two independent
rows. The **rank** is therefore two. Of four unknown bits, two are
free: choose $x_1=s$ and $z_1=t$. The equations give
$x_0=s$, $z_0=t$, so

$$v=s(1,1,0,0)+t(0,0,1,1).$$

The null-space dimension is $4-2=2$, with Pauli representatives $XX$
and $ZZ$. Both commute with the Hamiltonian and with one another.
Their product is **$-YY$**, not $YY$; binary addition alone has forgotten
that minus sign. A sector with $XX=\lambda_x$ and $ZZ=\lambda_z$
has $YY=-\lambda_x\lambda_z$. The phase-free binary vectors answer
commutation questions, not all coefficient or sector questions.

### A centralizer basis need not be jointly fixable

Take $H=ZI$ on two qubits. Its only condition is $x_0=0$.
A null-space basis is $ZI,IX,IZ$. All three commute with $H$,
but $IX$ and $IZ$ anticommute with each other. No state can have
definite eigenvalues of both. Linear independence alone does not
make them a valid three-generator tapering set, and a two-qubit
register certainly cannot lose three qubits.

There is a constructive elimination step for this second problem too.
Write the binary commutation form as $s(a,b)$, equal to zero for
commutation and one for anticommutation. If two independent candidates
$g,h$ satisfy $s(g,h)=1$, replace each remaining basis vector $r$ by

$$r'=r+s(r,h)g+s(r,g)h.$$

Then $s(r',g)=s(r',h)=0$: the added contributions cancel the old
ones modulo two. Keep $g$ as a sector generator, discard $h$ from the
selected set, and repeat on the transformed remaining span. A vector
commuting with that entire span can be retained directly. Remove
dependent vectors as usual. This produces an independent commuting
selection rather than mistaking the full nullity for the removable
qubit count. Binary vector additions correspond to Pauli products;
track their signs separately when translating the resulting basis
and its sector labels back to operators.

For $L$ terms and $2n$ columns, straightforward dense binary elimination
costs $O(Ln^2)$ bit operations before bit-packing improvements.
The cost of choosing and transforming generators depends on their
number, support and representation. It is not valid to infer that this
stage is "never the bottleneck" from a formal $O(n^4)$ integral count;
sparse inputs and implementation overhead can change the balance.

---

## Clifford Rotation: Making Generators Diagonal

Once we have independent **mutually commuting** generators, we need
a Clifford circuit $U$ such that:

$$U g_i U^\dagger = Z_{q_i} \quad \text{for each generator } g_i$$

This rotates each multi-qubit generator onto a single-qubit $Z$, making the system diagonally taperable.

> **Algorithm: Clifford Tapering**
>
> **Input:** A Pauli Hamiltonian $\hat{H} = \sum_k c_k P_k$ on $n$ qubits.
>
> **Output:** A Hamiltonian on $n-m$ qubits for the specified sector of
> the $m$ selected independent commuting generators.
>
> 1. **Represent** each term $P_k$ as a $2n$-bit symplectic vector.
> 2. **Build** the $L \times 2n$ commutation check matrix (one row per term).
> 3. **Compute** its null space via binary Gaussian elimination to obtain the centralizer candidates.
> 4. **Select** an independent mutually commuting subgroup $g_1,\ldots,g_m$, including each generator's phase.
> 5. **Synthesize** a Clifford that maps the generators to distinct single-qubit $Z_{q_i}$ operators while preserving generators already reduced. Local H/S choices and every CNOT update must track the Pauli phase.
> 6. **Conjugate** every term $P_k$ by the collected Clifford gates.
> 7. **Fix** each target qubit $q_i$ to the eigenvalue implied by the physical sector and remove it.
>
> **Inputs still needed:** the physical signs of the chosen generators.
> Neither a null space nor Clifford synthesis discovers those signs
> from the target molecule's name.

FockMap's synthesis uses three elementary gate types:

| Gate | Symbol | Effect on Pauli |
|:---|:---:|:---|
| Hadamard | $H_j$ | $X\to Z,\ Z\to X,\ Y\to-Y$ |
| Phase gate | $S_j$ | $X\to Y,\ Y\to-X,\ Z\to Z$ |
| CNOT | $\text{CNOT}_{c,t}$ | Propagates X from control to target; propagates Z from target to control |

The synthesis algorithm must choose target qubits and local Clifford gates
consistently across the whole commuting set. Mapping a Y support may introduce
a minus sign, and CNOT conjugation can also change Pauli phases. Dropping those
signs changes Hamiltonian coefficients and can corrupt the tapered spectrum.

```fsharp
let (gates, targets) = synthesizeTaperingClifford independentGens
// gates : CliffordGate list — the rotation circuit
// targets : int[] — which qubit each generator maps to
```

This is a contextual API signature, not a proof that an arbitrary array
called `independentGens` meets the required pairwise-commuting contract.
If synthesis produces $Ug_iU^\dagger=-Z_{q_i}$ rather than
$+Z_{q_i}$, an original eigenvalue $\lambda_i$ becomes target
$Z_{q_i}=-\lambda_i$. Changing a generator basis by multiplying rows
similarly multiplies its sector signs, including any Pauli-product phase.
The sign ledger must follow the generators actually used.

---

## Applying the Clifford to the Hamiltonian

The Clifford circuit is applied **symbolically** — no matrices, no state vectors. Each Pauli term is conjugated: $\sigma_\alpha \to U \sigma_\alpha U^\dagger$. This is a sequence of substitution rules applied to the symplectic vector:

```fsharp
let rotatedH = applyClifford gates hamiltonian
// Every term is now conjugated — generators have become single-qubit Zs
```

After rotation, inspect the target columns and apply the single-qubit
substitution from Chapter 11. Every term must be I/Z on each target.
This is a useful postcondition even when synthesis claims to have
completed successfully.

---

## The Unified Pipeline

FockMap exposes a unified `taper` function, but we must separate its
convenience from the mathematical acceptance conditions.
In the pinned 0.9.0 source, `FullClifford` obtains centralizer candidates
and calls `independentGenerators`, which performs binary linear
independence selection. That operation alone is not the mutually
commuting-subgroup selection just described.
Consequently an arbitrary Hamiltonian cannot be certified by the mere
presence of a `FullClifford` result.

Its `Sector` entries refer to **target qubit indices after the
synthesised Clifford**, not directly to particle numbers or a user's
preferred original generators. An empty sector selects positive signs
on those targets. That default is not a molecular ground-sector selector.
To use automatic synthesis responsibly, inspect its actual generators,
their mutual commutation, the signed images under its Clifford, and
the correspondence between those images and the physical signs.
Then compare the resulting sector spectrum.

For the molecular example below we instead use an explicit, fully
derived two-CNOT circuit and signed diagonal substitution. This uses
the existing public operations without claiming that automatic
generator selection has solved the physics.

For a result whose generator and sector checks have passed, these fields
allow the transformation to be inspected. This is a result-schema excerpt,
not another executable calculation:

```fsharp
result.OriginalQubitCount  // before tapering
result.TaperedQubitCount   // after tapering
result.RemovedQubits       // which qubits were removed
result.Generators          // inspect these and their pairwise commutation
result.CliffordGates       // the rotation circuit applied
result.TargetQubits        // which qubits the generators mapped to
result.Hamiltonian         // the tapered PauliRegisterSequence
```

---

## Worked Example: Heisenberg Model

$$\hat{H} = X_0X_1 + Y_0Y_1 + Z_0Z_1$$

This is a 2-qubit Hamiltonian where every qubit has X, Y, and Z terms. Diagonal tapering sees nothing at all. But there *is* a hidden symmetry — let's find it and use it.

**Step 1: Check for diagonal Z₂ qubits.**

Qubit 0 appears as: X, Y, Z across the three terms. Not diagonal (has X and Y). Qubit 1: same story. Diagonal tapering finds zero candidates.

**Step 2: Find general Z₂ generators.**

We need Pauli strings that commute with every term. Let's check $Z_0 Z_1$:

- Does $Z_0 Z_1$ commute with $X_0 X_1$? Use the crosswise dot product: X-bits of $ZZ$ are $(0,0)$, Z-bits are $(1,1)$. X-bits of $XX$ are $(1,1)$, Z-bits are $(0,0)$. Crosswise sum: $(0 \cdot 0 + 1 \cdot 1) + (0 \cdot 0 + 1 \cdot 1) = 2$, which is even → **commute** ✓
- Does $Z_0 Z_1$ commute with $Y_0 Y_1$? X-bits of $YY$ are $(1,1)$, Z-bits are $(1,1)$. Crosswise sum: $(0 \cdot 1 + 1 \cdot 1) + (0 \cdot 1 + 1 \cdot 1) = 2$ → **commute** ✓
- Does $Z_0 Z_1$ commute with itself? Always yes ✓

$Z_0 Z_1$ is a Z₂ generator. The Heisenberg Hamiltonian has additional
commuting structure, but we intentionally use this one generator to show a
single-qubit reduction and both of its sectors.

**Step 3: Clifford synthesis — rotate $Z_0 Z_1$ onto a single qubit.**

We want a circuit $U$ such that $U (Z_0 Z_1) U^\dagger = Z_0 I_1$ — the generator acts on only one qubit after rotation.

The key gate: **CNOT(1 → 0)** (qubit 1 is control, qubit 0 is target). Recall from Chapter 4 that CNOT propagates Z from target to control: $Z_\text{target} \to Z_\text{control} \cdot Z_\text{target}$, while $Z_\text{control}$ is unchanged. So:

$$\text{CNOT}_{1,0}:\quad Z_0 \to Z_0 Z_1, \quad Z_1 \to Z_1$$

Under this gate, $Z_0 Z_1 \to (Z_0 Z_1) \cdot Z_1 = Z_0 Z_1^2 = Z_0$ — we've isolated a single $Z_0$, which is good. But let's try the other direction for comparison: **CNOT(0 → 1)** (qubit 0 is control, qubit 1 is target):

$$\text{CNOT}_{0,1}:\quad Z_0 \to Z_0, \quad Z_1 \to Z_0 Z_1$$

Under this gate, $Z_0 Z_1 \to Z_0 \cdot (Z_0 Z_1) = Z_0^2 Z_1 = Z_1$ — we've isolated a single $Z_1$.

> **What just happened:** The CNOT "absorbed" the $Z_0$ factor by multiplying it with another $Z_0$, leaving only $Z_1$. This is the Clifford rotation — it's not a physical rotation in space, but an algebraic simplification achieved by conjugation with a gate.

**Step 4: Apply the CNOT to every Hamiltonian term.**

Under CNOT(0,1) conjugation, recall the full propagation rules: $Z_0 \to Z_0$, $Z_1 \to Z_0 Z_1$ (Z propagates from target to control), $X_0 \to X_0 X_1$ (X propagates from control to target), $X_1 \to X_1$. Applying these to each Heisenberg term:

| Original | Propagation | Result |
|:---:|:---|:---:|
| $X_0 X_1$ | $X_0 \to X_0 X_1$, $X_1 \to X_1$ | $(X_0 X_1)(X_1) = X_0 I_1 = X_0$ |
| $Y_0 Y_1$ | Conjugate the full Pauli product, including phase | $-X_0 Z_1$ |
| $Z_0 Z_1$ | $Z_0 \to Z_0$, $Z_1 \to Z_0 Z_1$ | $Z_0 (Z_0 Z_1) = Z_1$ |

After the CNOT rotation,

$$H'=X_0-X_0Z_1+Z_1.$$

Qubit 1 is now diagonal, but its sector still matters:

- $Z_1=+1$: $H'_+=I$, giving eigenvalues $\{1,1\}$.
- $Z_1=-1$: $H'_-=2X_0-I$, giving eigenvalues $\{-3,1\}$.

Together the sectors give $\{-3,1,1,1\}$, exactly the spectrum of
$XX+YY+ZZ$. The ground state is in the $-1$ sector. Replacing the result by
the one-qubit Hamiltonian $X_0$ would lose both the $-3$ eigenvalue and the
sector structure.

A verified one-generator reduction turns each two-dimensional symmetry sector
into a one-qubit problem. Both sectors are needed to recover the full spectrum.
Direct $4\times4$ matrix conjugation provides an independent check of every
sign in the symbolic derivation.

## H₂: Finish the Physical Reduction

Return to the generators chosen in Chapter 10,
$g_\alpha=ZIZI$ and $g_\beta=IZIZ$, both with eigenvalue $-1$.
They commute and are independent. The full binary search also explains
where they sit among the candidates.

Every single-qubit Z occurs with a nonzero coefficient in the canonical
H₂ Hamiltonian. Commuting with those four terms forces
$x_0=x_1=x_2=x_3=0$ in an unknown generator.
Each coupling term has X support on all four qubits, so the remaining
condition is

$$z_0+z_1+z_2+z_3=0\pmod2.$$

There are four independent X constraints and one Z constraint:
rank five in eight binary columns, nullity three.
All candidates are even-weight Z products, so in this case they
*do* commute pairwise. One basis is
$Z_0Z_2,\ Z_1Z_3,\ Z_0Z_1$.
We use the first two to keep the full $N=2,M_s=0$ block.
The third will later split that block; it is not implied by the two
spin parities.

### Every transformed term, including its sign

Set $U=\mathrm{CNOT}(0,2)\mathrm{CNOT}(1,3)$.
For the first gate,
$X_0\to X_0X_2$, $Z_2\to Z_0Z_2$, with $Z_0$ and $X_2$
unchanged. The second gate has the analogous rules on 1 and 3.
Extend the rules multiplicatively, preserving Pauli phases.
For instance,

$$U(Y_0Y_1X_2X_3)U^\dagger
=(Y_0X_2)(Y_1X_3)X_2X_3=Y_0Y_1.$$

This gives `YYXX` $\to$ `YYII`. The original coefficient is $-g$,
so the final contribution after tapering is $-g\,YY$.
The complete ledger below uses the original coefficient in its first
column; the last column is the *signed operator* remaining after
$Z_2=Z_3=-1$:

| Coefficient (Ha) | Original | $UPU^\dagger$ | After fixing targets |
|---:|:---:|:---:|:---:|
| $-0.8121706072487134$ | IIII | IIII | $II$ |
| $-0.2234315369081336$ | IIIZ | IZIZ | $-IZ$ |
| $-0.2234315369081336$ | IIZI | ZIZI | $-ZI$ |
| $0.1744128761226154$ | IIZZ | ZZZZ | $ZZ$ |
| $0.1714128264477690$ | IZII | IZII | $IZ$ |
| $0.1206252348339041$ | IZIZ | IIIZ | $-II$ |
| $0.1659278503377033$ | IZZI | ZZZI | $-ZZ$ |
| $-0.0453026155037992$ | XXYY | YYZZ | $YY$ |
| $0.0453026155037992$ | XYYX | YYZI | $-YY$ |
| $0.0453026155037992$ | YXXY | YYIZ | $-YY$ |
| $-0.0453026155037992$ | YYXX | YYII | $YY$ |
| $0.1714128264477691$ | ZIII | ZIII | $ZI$ |
| $0.1659278503377034$ | ZIIZ | ZZIZ | $-ZZ$ |
| $0.1206252348339041$ | ZIZI | IIZI | $-II$ |
| $0.1686889817036122$ | ZZII | ZZII | $ZZ$ |

All coefficients are electronic. Nuclear repulsion remains separate.
The two surviving qubits are old qubits 0 and 1, in that order.
Combining terms gives

$$H_2=C\,II+B(ZI+IZ)+D\,ZZ+F\,YY,$$

with

$$
\begin{aligned}
C&=-1.0534210769165218,\\
B&=\phantom{-}0.3948443633559027,\\
D&=\phantom{-}0.0112461571508209,\\
F&=-0.1812104620151967,
\end{aligned}
\qquad\text{in Ha}.
$$

For example, $C$ is the old identity coefficient minus the two
spin-parity coefficients. $D$ is
$0.1686889817036122+0.1744128761226154
-2(0.1659278503377033)$.
The paired $B$ coefficients differ only at floating-point round-off;
the source fixture, rather than the rounded display, supplies the
calculation.

### The actual reduced matrix

In reduced integer order, the displayed labels are
$|00\rangle,|10\rangle,|01\rangle,|11\rangle$.
They map to original occupation rows $[12,9,6,3]$.
Since $YY|00\rangle=-|11\rangle$ and
$YY|10\rangle=|01\rangle$, the matrix is

$$
H_2=
\begin{pmatrix}
C+2B+D&0&0&-F\\
0&C-D&F&0\\
0&F&C-D&0\\
-F&0&0&C-2B+D
\end{pmatrix}
$$

$$
\approx
\begin{pmatrix}
-0.252486193&0&0&0.181210462\\
0&-1.064667234&-0.181210462&0\\
0&-0.181210462&-1.064667234&0\\
0.181210462&0&0&-1.831863646
\end{pmatrix}\ {\mathrm{Ha}}.
$$

The lower-right entry is the HF energy. It is not the upper-left entry:
the HF determinant reduced to `11`. The middle two rows describe the
different open-shell determinants, even though their diagonal entries
are equal.

The two eigenvalues in the even retained-bit block are
$C+D\pm\sqrt{(2B)^2+F^2}$.
Those in the odd block are $C-D\pm|F|$. Sorting all four gives

$$
-1.8523881736,\quad -1.2458776961,\quad
-0.8834567721,\quad -0.2319616660\quad{\mathrm{Ha}}.
$$

These are exactly the four eigenvalues of the original $N=2,M_s=0$
block. They are not all six eigenvalues of $N=2$: the two other
$M_s$ components lie outside our selected sector.

### A public-API calculation with no guessed sector

The complete companion is `code/ch12-h2-physical-taper.fsx`.
Its essential operations, after constructing the canonical `jwHam`
and opening `Encodings.Tapering`, are:

```fsharp
let physicalGates = [ CNOT(0, 2); CNOT(1, 3) ]
let rotated = applyClifford physicalGates jwHam
let physical =
    taperDiagonalZ2 [(2, -1); (3, -1)] rotated
let reduced = physical.Hamiltonian
```

These are existing FockMap 0.9.0 APIs. The derivation fixes both gate
directions and both sector signs before the calls. The companion
compares the explicit occupation block and the full reduced spectrum;
a successful default-positive automatic reduction would not establish
the same result.

### Optional third removal: a smaller, named block

The additional generator $g_{\mathrm{pair}}=Z_0Z_1$ commutes with this H₂
Hamiltonian. Its $+1$ sector contains the two closed-shell determinants
`1100` and `0011`; its $-1$ sector contains the two open-shell
$M_s=0$ determinants. Neither $N=2$ nor $M_s=0$ alone fixes this sign.
We may choose $+1$ when studying the closed-shell invariant block,
and the independent canonical spectrum confirms that its lower root
is the molecular ground energy here. This is a block-specific statement,
not permission to assign every unfamiliar symmetry the HF sign.

On the two retained qubits, apply CNOT$(0,1)$:
$ZZ\to IZ$, $ZI\to ZI$, $IZ\to ZZ$, and $YY\to-XZ$.
Then fix target $Z_1=+1$. The one-qubit Hamiltonian is

$$
\begin{aligned}
H_1&=(C+D)I+2BZ-FX\\
&=-1.0421749197657006\,I\\
&\quad+0.7896887267118053\,Z
+0.1812104620151967\,X .
\end{aligned}
$$

Its basis is $|0\rangle\leftrightarrow|0011\rangle_{\mathrm{occ}}$,
$|1\rangle\leftrightarrow|1100\rangle_{\mathrm{occ}}$.
Its eigenvalues are $-1.8523881736$ and $-0.2319616660$ Ha.
The other two $M_s=0$ energies were deliberately excluded, not lost
through numerical approximation.

For a normalised lower eigenvector, choose the HF component positive.
The ratio of double-excitation to HF amplitudes follows from the first
row of $(H_1-EI)v=0$:

$$\frac{v_0}{v_1}
=-\frac{0.1812104620151967}{-0.252486193\ldots-E}.$$

At the lower eigenvalue, this is about $-0.1133$. Normalisation gives
HF probability $|v_1|^2=0.9873338735$, agreeing with Chapter 9.
To recover the full state, insert target bit 1 equal to zero, undo
the final CNOT, insert the two spin-parity target bits equal to one,
then undo the original two CNOTs. The relative minus sign between
the determinants survives. Tapering preserves that wavefunction
information within the chosen block; it does not turn the result
into a classical bit string.

---

## Key Takeaways

- The **binary Pauli representation** (two bits per qubit) turns commutativity checking into a crosswise binary dot product — fast and exact.
- The null space gives the Pauli centralizer; tapering needs an independent mutually commuting subgroup with phases.
- **Clifford synthesis** rotates multi-qubit generators onto single-qubit Zs using H, S, and CNOT — no matrices needed.
- **The unified `taper` function** handles both diagonal and Clifford tapering with one API.
- Signed Clifford conjugation and sector substitution are exact algebraic
  operations. Finite-precision coefficients and the numerical evidence
  used to check an implementation still require explicit tolerances.

## Exercises

1. **A centralizer is not a tapering set.** For $H=ZI$, construct the
   one-row binary check matrix, find its rank and nullity, and verify
   that $ZI,IX,IZ$ form a null-space basis. Select two independent
   commuting generators. Explain why all three cannot be fixed together.

2. **Heisenberg signs.** Use CNOT$(0,1)$ to derive the image of $YY$
   by writing $Y=iXZ$. Compute the reduced Hamiltonian for both signs
   of $Z_1$ and recover the full multiset $\{-3,1,1,1\}$.

3. **Complete the H₂ matrix.** Starting from $C,B,D,F$ above, derive
   both $2\times2$ blocks and their eigenvalues. Identify which two
   original determinants correspond to each block. Why is the
   four-state spectrum not the entire two-electron spectrum?

4. **A generator with a minus sign.** Suppose synthesis maps a
   physical generator $g$ to $-Z_2$, and the target state has
   $g=-1$. What value of $Z_2$ must be passed to diagonal tapering?
   Would passing $-1$ merely change a global phase?

5. **Restore the lower root.** Use the one-qubit matrix to calculate
   the ground-state amplitude ratio and HF probability. Restore the
   two original determinants and add $V_{nn}=0.7151043390810812$ Ha
   to the lower electronic eigenvalue. Compare with Chapter 9.

## Further Reading

- Bravyi, S. et al. "Tapering off qubits to simulate fermionic Hamiltonians." arXiv:1701.08213 (2017). The general Z₂ tapering framework.
- Aaronson, S. and Gottesman, D. "Improved simulation of stabilizer circuits." *Phys. Rev. A* 70, 052328 (2004). The tableau formalism underlying our Clifford implementation.

---

**Previous:** [Chapter 11 — Diagonal Z₂ Symmetries](11-diagonal-z2.html)

**Next:** [Chapter 13 — Tapering Benchmarks](13-tapering-benchmarks.html)
