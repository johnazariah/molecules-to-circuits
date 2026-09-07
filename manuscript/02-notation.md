# Chapter 2: The Notation Minefield

_Two communities, two integral conventions, one factor-of-two error that will derail your entire Hamiltonian if you get it wrong. This chapter exists to make sure you don't._

## In This Chapter

- **What you'll learn:** The difference between chemist's (Mulliken) and physicist's (Dirac) two-electron integral notation, how to convert between them, and the three most common errors that produce plausible-but-wrong Hamiltonians.
- **Why this matters:** Every sign, every coefficient, every eigenvalue in the rest of this book depends on getting the integral convention right *here*. The errors are insidious: wrong-convention Hamiltonians have the right structure, the right symmetry, and the right number of terms. Only the numbers are wrong.
- **Prerequisites:** Chapter 1 (you know what $h_{pq}$ and the two-body integrals represent physically).

---

## The Problem

There are two standard notations for two-electron repulsion integrals. They are both in widespread use. They are both perfectly valid. And they index the same physical quantity with the indices in **different positions**.

If you take integrals from a chemistry code (which typically outputs chemist's notation) and plug them into a physics formula (which uses physicist's notation) without converting, you will get the wrong Hamiltonian. You will not get an error message. Your code will run. Your Pauli strings will have the right structure. Your eigenvalues will be wrong — silently, plausibly, maddeningly wrong.

This chapter names the two conventions, shows you exactly how they differ, gives you the conversion rule, and then catalogues the three errors that trap essentially every student who encounters this material for the first time. If you remember nothing else from this chapter, remember the boxed equation at the centre.

---

## Convention 1: Chemist's Notation (Mulliken)

For the coordinate formulae here, $p,q,r,s$ label spatial orbitals
$\phi_p$, and $r_{12}=|\mathbf r_1-\mathbf r_2|$ is the electron
separation. Lengths and energies use Chapter 1's atomic units. In the
second-quantised Hamiltonian below, the same notation will label
**spin-orbitals** instead; Chapter 3 supplies the spin overlaps that make
that transition. Index conventions and spin expansion are separate jobs.

Chemist's notation — also called Mulliken notation or charge-density notation — groups indices by **spatial coordinate**. The integral is written with square brackets:

$$[pq \mid rs] = \iint \phi_p^*(\mathbf{r}_1)\,\phi_q(\mathbf{r}_1)\;\frac{1}{r_{12}}\;\phi_r^*(\mathbf{r}_2)\,\phi_s(\mathbf{r}_2)\;d\mathbf{r}_1\,d\mathbf{r}_2$$

Read it as: "the charge density $\phi_p^* \phi_q$ at position $\mathbf{r}_1$ interacts via Coulomb repulsion with the charge density $\phi_r^* \phi_s$ at position $\mathbf{r}_2$."

The bracket $[pq\mid$ contains the two orbitals that share electron 1's coordinate. The bracket $\mid rs]$ contains the two orbitals that share electron 2's coordinate. Within each bracket: complex conjugate (bra) first, ket second.

**Why chemists like it:** The indices are grouped the way charge densities are: $\rho_1(\mathbf{r}_1) = \phi_p^*(\mathbf{r}_1)\phi_q(\mathbf{r}_1)$, and the integral is just the Coulomb interaction between two charge distributions. It makes the *physics* of repulsion visually clear.

> I first encountered this notation in Dr. Robert Ditchfield's Quantum Chemistry course (Chem 81) at Dartmouth in 1992. He would write the charge-density form on the blackboard and say, "This is the natural way to think about it." He was right — it *is* the natural way to think about it. It's just not the way the second-quantized Hamiltonian is written.

**Where you'll encounter it:** The PySCF electron-repulsion integrals used
by this book start in chemist's notation. Other chemistry and quantum
software interfaces may permute axes, pack symmetry-equivalent entries,
or absorb weights. Read the contract of the *particular array or export
function*, not just the name of its package. Never infer an array's
convention from the notation used elsewhere in its documentation.

---

## Convention 2: Physicist's Notation (Dirac)

Physicist's notation — sometimes called Dirac notation for integrals (not to be confused with Dirac bra-ket notation for states) — groups the two **bra indices** before the two **ket indices**:

$$\langle pq \mid rs\rangle = \iint \phi_p^*(\mathbf{r}_1)\,\phi_q^*(\mathbf{r}_2)\;\frac{1}{r_{12}}\;\phi_r(\mathbf{r}_1)\,\phi_s(\mathbf{r}_2)\;d\mathbf{r}_1\,d\mathbf{r}_2$$

Read it as a matrix element from ket orbitals $(r,s)$ to bra orbitals
$(p,q)$. The electron-1 coordinate connects $r$ to $p$; the electron-2
coordinate connects $s$ to $q$. These are integration coordinates, not
labels that make the electrons distinguishable.

Here $p$ and $r$ both belong to electron 1 (bra and ket respectively), while $q$ and $s$ both belong to electron 2. The angle brackets $\langle\,\rangle$ signal physicist's convention; the square brackets $[\,]$ signal chemist's.

**Why physicists like it:** The bra/ket grouping fits the operator
expression directly: create in the bra orbitals and remove from the ket
orbitals. We will write that expression after a short operator inset.

**Where you'll encounter it:** This book and FockMap's raw-integral
Hamiltonian interface use this convention. A symbol such as $g_{pqrs}$ in
another reference is not enough to identify it: inspect the defining
integral and the operator order.

---

## The Conversion: One Equation to Rule Them All

Comparing the two definitions term by term — matching which orbital sits at which coordinate — yields:

$$\boxed{\langle pq \mid rs\rangle = [pr \mid qs]}$$

That's it. Memorise this. Tattoo it if necessary.

The indices are **shuffled**, not just relabelled. The physicist's pair $(p, r)$ — the two orbitals for electron 1 — becomes the *left* bracket of the chemist's notation, with $p$ in the bra position and $r$ in the ket position. The physicist's pair $(q, s)$ — the two orbitals for electron 2 — becomes the *right* bracket.

### Worked example

Suppose you have the chemist's integral $[00 \mid 00] = 0.6748$ Ha (the Coulomb self-repulsion of two electrons both in the $\sigma_g$ orbital). What is the corresponding physicist's integral?

Use the conversion: $\langle pq \mid rs\rangle = [pr \mid qs]$. We need $[pr \mid qs] = [00 \mid 00]$, so $p = 0, r = 0, q = 0, s = 0$. Therefore:

$$\langle 00 \mid 00\rangle = [00 \mid 00] = 0.6748 \text{ Ha}$$

That one was trivial — all indices are the same. Now consider a more revealing example: $[00 \mid 11] = 0.6637$ Ha (the Coulomb repulsion between an electron in $\sigma_g$ and one in $\sigma_u$). We need $[pr \mid qs] = [00 \mid 11]$, so $p = 0, r = 0, q = 1, s = 1$. Therefore:

$$\langle 01 \mid 01\rangle = [00 \mid 11] = 0.6637 \text{ Ha}$$

Notice: $\langle 01 \mid 01\rangle \neq \langle 00 \mid 11\rangle$ in general. The integral $\langle 00 \mid 11\rangle = [01 \mid 01]$ is a pair-scattering integral in which both electrons change orbital. For H₂/STO-3G at 0.74 Å, $[01 \mid 01] = 0.1812$ Ha, distinct from $[00 \mid 11] = 0.6637$ Ha. For real orbitals, the within-bracket symmetry also gives $[01 \mid 01]=[01 \mid 10]$; these are not two independent values.

> **Common Mistake #1: Using the wrong convention.** If you take $[00 \mid 11]$ from PySCF and use it as $\langle 00 \mid 11\rangle$ in the physicist's Hamiltonian, you have substituted the Coulomb value for a pair-scattering value. The structure can still look plausible. Trace the coordinate pairs and check labelled matrix elements and reference energies; successful execution is not evidence that the convention matches.

---

## The Hamiltonian: Which Convention Goes Where?

### Just enough operator algebra to read the order

Chapter 1 previewed creation and annihilation. Before asking them to settle
a sign, we need four rules.

An operator acts on the state to its right; in $AB|\psi\rangle$, apply
$B$ first, then $A$. The **adjoint** $A^\dagger$ is its complex-conjugate
transpose in a matrix representation. It reverses products:
$(AB)^\dagger=B^\dagger A^\dagger$. Creation $a_p^\dagger$ is the adjoint
of annihilation $a_p$, not an inverse: creating in an already occupied
mode gives zero, and removing from an empty mode gives zero.

The **vacuum** $|\Omega\rangle$ has no occupied modes and norm one.
It is not the zero vector. In two ordered modes we choose
$|10\rangle=a_0^\dagger|\Omega\rangle$,
$|01\rangle=a_1^\dagger|\Omega\rangle$ and
$|11\rangle=a_0^\dagger a_1^\dagger|\Omega\rangle$.

The **anticommutator** is $\{A,B\}=AB+BA$. Fermionic operators satisfy
the canonical anticommutation relations (**CAR**):

$$\{a_p,a_q^\dagger\}=\delta_{pq}I,\qquad
\{a_p,a_q\}=0,\qquad
\{a_p^\dagger,a_q^\dagger\}=0.$$

Here $\delta_{pq}$ is 1 when $p=q$ and 0 otherwise; $I$ is the identity
operator. Thus different-mode ladders change sign when exchanged, while
the equal-mode identity reads $a_p a_p^\dagger=I-a_p^\dagger a_p$.
Setting $p=q$ in the last relation gives
$2(a_p^\dagger)^2=0$, enforcing exclusion.

For a concrete ordering check,

$$
\begin{aligned}
a_1a_0|11\rangle&=a_1|01\rangle=|\Omega\rangle,\\
a_0a_1|11\rangle&=-a_0|10\rangle=-|\Omega\rangle.
\end{aligned}
$$

The minus sign in the second line comes from passing $a_1$ across
$a_0^\dagger$. Consequently
$a_0^\dagger a_1^\dagger a_1a_0|11\rangle=|11\rangle$,
whereas reversing only the two annihilators gives $-|11\rangle$.
We have not invoked a qubit encoding to obtain this sign. It is already
part of the electronic problem. Chapter 5 develops the general signed
action on any occupation state.

### The unrestricted raw form

The standard second-quantized electronic Hamiltonian is written in **physicist's** notation:

$$\hat{H} = \sum_{pq} h_{pq}\, a_p^\dagger a_q + \frac{1}{2}\sum_{pqrs} \langle pq \mid rs\rangle\, a_p^\dagger a_q^\dagger a_s a_r + V_{nn}$$

Note three things:

1. **The one-body term** $h_{pq}\, a_p^\dagger a_q$ is convention-independent — there's only one standard for one-electron integrals.

2. **The two-body term** uses physicist's notation $\langle pq \mid rs\rangle$. If you have chemist's integrals, convert first: $\langle pq \mid rs\rangle = [pr \mid qs]$.

3. **The operator ordering is $a_p^\dagger a_q^\dagger a_s a_r$** — the annihilation operators are in *reverse* written order relative to the ket indices: remove $r$ first, then $s$. Reversing only these operators negates every surviving quartic monomial, including diagonal pair energies, not just off-diagonal couplings.

If you prefer to work entirely in chemist's notation, the Hamiltonian reads:

$$\hat{H} = \sum_{pq} h_{pq}\, a_p^\dagger a_q + \frac{1}{2}\sum_{pqrs} [pr \mid qs]\, a_p^\dagger a_q^\dagger a_s a_r + V_{nn}$$

This is the same equation — we just substituted the conversion rule. Use whichever form matches your integral source, but be consistent.

---

## The Symmetries That Save You (and Can Mislead You)

Two-electron integrals have symmetries that reduce the number of independent values:

### Chemist's notation symmetries

$$[pq \mid rs] = [qp \mid sr]^* = [rs \mid pq]$$

For **real** orbitals (which is the case for all our examples):

$$
\begin{aligned}
[pq\mid rs]&=[qp\mid rs]=[pq\mid sr]=[qp\mid sr]\\
&=[rs\mid pq]=[sr\mid pq]=[rs\mid qp]=[sr\mid qp].
\end{aligned}
$$

That's 8-fold symmetry — the number of independent integrals is roughly $n^4/8$ rather than $n^4$.

### Physicist's notation symmetries

$$\langle pq \mid rs\rangle = \langle qp \mid sr\rangle = \langle rs \mid pq\rangle^*$$

For real orbitals:

$$
\begin{aligned}
\langle pq\mid rs\rangle
&=\langle rq\mid ps\rangle
=\langle ps\mid rq\rangle
=\langle rs\mid pq\rangle\\
&=\langle qp\mid sr\rangle
=\langle sp\mid qr\rangle
=\langle qr\mid sp\rangle
=\langle sr\mid qp\rangle .
\end{aligned}
$$

There are still eight permutations: changing notation cannot destroy a
tensor's symmetry. To derive the second expression, for example, write
$\langle pq|rs\rangle=[pr|qs]$, exchange $p$ and $r$ in the left
chemist pair to obtain $[rp|qs]$, and convert back to
$\langle rq|ps\rangle$. This exchanges a bra index with a ket index,
not the two bra indices.

These eight real-orbital identities need not produce eight distinct array
positions when indices repeat. Nor may we use the real-only identities for
general complex orbitals: then conjugation matters and only the identities
under the preceding complex-orbital headings apply.

> **Common Mistake #2: Applying chemist's symmetries to physicist's integrals.** If you assume $\langle pq \mid rs\rangle = \langle qp \mid rs\rangle$ (swapping only the bra indices), you are applying a chemist's symmetry to a physicist's integral. In physicist's notation, the correct symmetry is $\langle pq \mid rs\rangle = \langle qp \mid sr\rangle$ — you must swap *both* bra indices and *both* ket indices together.

---

## The $\frac{1}{2}$ Prefactor

The factor of $\frac{1}{2}$ in front of the two-body term:

$$\frac{1}{2}\sum_{pqrs} \langle pq \mid rs\rangle\, a_p^\dagger a_q^\dagger a_s a_r$$

exists because the sum over all four indices counts every pair of electrons twice (once as $(p, q)$ and once as $(q, p)$). The $\frac{1}{2}$ corrects for this double-counting.

Some references absorb this factor into their stored coefficients. Another
choice is to use **antisymmetrised integrals**, denoted by a double bar:

$$\langle pq\Vert rs\rangle
=\langle pq\mid rs\rangle-\langle pq\mid sr\rangle.$$

The second term exchanges the two ket orbitals. It is an algebraic
subtraction, not a second physical interaction that we add by hand.
The following three forms of the two-body operator are equivalent:

$$
\boxed{
\begin{aligned}
H_2
&=\frac12\sum_{pqrs}\langle pq\mid rs\rangle
          a_p^\dagger a_q^\dagger a_s a_r\\
&=\frac14\sum_{pqrs}\langle pq\Vert rs\rangle
          a_p^\dagger a_q^\dagger a_s a_r\\
&=\sum_{\substack{p<q\\r<s}}\langle pq\Vert rs\rangle
          a_p^\dagger a_q^\dagger a_s a_r .
\end{aligned}}
$$

Restricting **both** pairs changes the coefficient as well as the
prefactor. Keeping raw single-bar integrals in the last line is wrong.

### Where the quarter comes from

Let $T_{pqrs}=a_p^\dagger a_q^\dagger a_s a_r$. Exchanging $r$ and $s$
negates $T$. Relabelling these dummy summation indices therefore gives

$$\sum_{pqrs}\langle pq|sr\rangle T_{pqrs}
=-\sum_{pqrs}\langle pq|rs\rangle T_{pqrs}.$$

Subtracting the exchanged-integral sum doubles the original sum; multiplying
by a quarter recovers its original half. Next, antisymmetrised integrals
change sign under exchanging either pair, just as $T$ does. Their product
does not. The unrestricted antisymmetrised sum therefore has four equal
contributions for each $p<q,r<s$ choice; the quarter removes them.
Terms with repeated creation or annihilation indices vanish by CAR.

### A nonzero-exchange example

Take a **hypothetical** pair of same-spin modes 0 and 1 with direct
integral $J=\langle01|01\rangle=0.7$ Ha and exchange
$K=\langle01|10\rangle=0.2$ Ha. These are teaching values, not H₂ data.
Let $T=a_0^\dagger a_1^\dagger a_1a_0$. The four relevant raw entries
contribute:

| Raw indices $(p,q,r,s)$ | Integral | Monomial relative to $T$ | Weighted contribution |
|:---:|:---:|:---:|:---:|
| $(0,1,0,1)$ | $J$ | $+T$ | $+JT/2$ |
| $(1,0,1,0)$ | $J$ | $+T$ | $+JT/2$ |
| $(0,1,1,0)$ | $K$ | $-T$ | $-KT/2$ |
| $(1,0,0,1)$ | $K$ | $-T$ | $-KT/2$ |

Their sum is $(J-K)T=0.5T$ Ha. The operator $T$ annihilates states
with fewer than two occupied modes and returns $|11\rangle$ unchanged.
Thus in the ordered basis $(|00\rangle,|10\rangle,|01\rangle,|11\rangle)$,

$$H_2=\operatorname{diag}(0,0,0,0.5)\ \text{Ha}.$$

The restricted antisymmetrised expression gives this matrix immediately:
there is one pair coefficient $J-K$. A restricted *raw* expression would
give 0.7 Ha on $|11\rangle$, losing exchange. Dropping the half from the
unrestricted raw expression would instead give 1.0 Ha. Distinguishable
errors need distinguishable tests.

> **Common Mistake #3: Moving the sum without moving the convention.**
> Raw unrestricted, antisymmetrised unrestricted, and antisymmetrised
> restricted coefficients need respectively the half, the quarter, and no
> prefactor. The size of a resulting energy error depends on the integrals
> and state; there is no diagnostic decimal place.

---

## A Complete Example: H₂ Two-Body Integrals

Here are representatives of all non-zero spatial two-body integrals for
canonical H₂/STO-3G at 0.74 Å in the RHF MO basis. The symmetry-related
positions are expanded in Chapter 3.

| Chemist's $[pq \mid rs]$ | Value (Ha) | Physicist's $\langle pq \mid rs\rangle$ | Conversion used |
|:---:|:---:|:---:|:---|
| $[00 \mid 00]$ | $0.6747559268$ | $\langle 00 \mid 00\rangle$ | $\langle 00 \mid 00\rangle = [00 \mid 00]$ |
| $[11 \mid 11]$ | $0.6976515045$ | $\langle 11 \mid 11\rangle$ | $\langle 11 \mid 11\rangle = [11 \mid 11]$ |
| $[00 \mid 11]$ | $0.6637114014$ | $\langle 01 \mid 01\rangle$ | $\langle 01 \mid 01\rangle = [00 \mid 11]$ |
| $[01 \mid 01]=[01 \mid 10]$ | $0.1812104620$ | $\langle 00 \mid 11\rangle=\langle 01 \mid 10\rangle$ | $\langle pq \mid rs\rangle=[pr \mid qs]$ |

Study this table. Make sure you can derive each entry in the rightmost column from the boxed conversion rule. If you can do that, you will never make error #1.

---

## The Companion Library: FockMap

Core transformations have executable checks: independent PySCF/direct-matrix
artifacts establish the chemistry reference, and FockMap supplies symbolic
operator implementations that must reproduce it. Companions fail closed when
the live package disagrees.

A few things worth knowing about it:

- **Its operator algebra is symbolic.** Pauli multiplication uses a discrete
  multiplication table and phase tracking rather than dense matrix products.
  Molecular integral coefficients are still numerical data with finite
  precision. Exact symbolic signs do not make the input decimals exact.

- **It is functional.** FockMap is written in F#, a functional-first language
  on .NET. [Reading and Running the Code](code-reading.html) introduces
  functions, maps, options, pipelines and script setup before the first
  factory. We will trace the physics-specific lookup below.

- **It runs anywhere.** FockMap targets .NET 10 and runs on Windows, macOS, and Linux. Install the API version used by this book with `dotnet add package FockMap --version 0.9.0`, or clone the repository and build from the accepted source.

- **Its package pin matters.** The companions use FockMap 0.9.0. The
  repository README records its source and DLL provenance; an API name
  without a version is not a reproducibility contract.

When you see code in this book, it serves one purpose: to show that the mathematics we just derived actually computes the right answer. The code is evidence, not the lesson.

---

## FockMap's Convention

FockMap 0.9 makes raw physicist integrals the primary contract.
`computeHamiltonianWith` accepts:

- One-body key `"p,q"` → $h_{pq}$
- Two-body key `"p,q,r,s"` → the raw single-bar integral
  $\langle pq \mid rs\rangle$

The library then applies the $\tfrac12$ prefactor and annihilator-index order required by
$\tfrac12\langle pq\mid rs\rangle a_p^\dagger a_q^\dagger a_s a_r$.
The explicitly named migration API `computeHamiltonianFromWeightedWith` instead
accepts the old full weighted coefficient of
$a_p^\dagger a_q^\dagger a_k a_l$ and applies it verbatim. Do not feed a
preweighted factory to a primary raw builder, or a raw factory to a
`...FromWeighted...` builder.

If the source provides chemist's notation, convert it to a raw physicist
factory. The following **contextual F# excerpt** shows the key permutation,
not a complete Hamiltonian-building script. `chemistIntegrals` must already
contain spin-orbital entries with a documented sparse-zero policy; Chapter 3
supplies the spatial-to-spin expansion. Its values must have the coefficient
type expected by the caller.

```fsharp
let physicistFactory chemistIntegrals (key : string) =
    let parts = key.Split(',') |> Array.map int
    match parts.Length with
    | 2 ->
        let p, q = parts.[0], parts.[1]
        chemistIntegrals |> Map.tryFind (sprintf "%d,%d" p q)
    | 4 ->
        let p, q, r, s = parts.[0], parts.[1], parts.[2], parts.[3]
        chemistIntegrals |> Map.tryFind (sprintf "%d,%d,%d,%d" p r q s)
    | _ -> invalidArg "key" "Expected two or four comma-separated indices."
```

Trace a one-body request `"0,0"`: `Split` makes two strings, `Array.map int`
turns them into integers 0 and 0, the two-entry branch binds $p=q=0$,
and `sprintf` reconstructs the unchanged lookup key. The result is
`Some value` if that entry exists, or `None` if it does not.

Now trace the four-index **spin-orbital** request `"0,3,0,3"`. The parsed
tuple is $(p,q,r,s)=(0,3,0,3)$; the lookup string is `"0,0,3,3"`.
Chapter 3's expansion assigns that entry the spatial Coulomb value
$[00|11]=0.6637114014$ Ha, since each coordinate preserves its spin.
No half is introduced here. That belongs to the raw Hamiltonian builder.

`Some 0.0` (or the caller's complex zero) says an entry exists with value
zero. `None` says it is absent. They are interchangeable *only after*
the data contract explicitly says omitted entries are known zeros.
A missing file, an incomplete tensor or a malformed key is not physical
evidence for a zero integral. A production loader must establish orbital
count, spin order, convention and completeness before this small lookup
function is trusted. The complete chapter companions are the executable
entry points; this excerpt explains their boundary rather than inventing
a second data source.

Getting this boundary right is necessary, not sufficient. Later operator,
labelled-state and matrix checks still matter. FockMap's primary FCIDUMP
adapters target the same raw physicist contract; do not add another
half-and-swap conversion to an already converted factory.

---

## Key Takeaways

- Two-electron integrals come in two flavours: chemist's $[pq \mid rs]$ groups factors by coordinate; physicist's $\langle pq \mid rs\rangle$ groups bra indices and ket indices.
- The conversion is $\langle pq \mid rs\rangle = [pr \mid qs]$ — indices are shuffled, not just relabelled.
- The second-quantized Hamiltonian uses physicist's notation. If your integrals come from a chemistry code, convert first.
- The three most common errors are: (1) using integrals in the wrong convention, (2) applying chemist's symmetries to physicist's integrals, and (3) mishandling the $\frac{1}{2}$ prefactor.
- Check integral entries, operator action and labelled matrix elements as well as known eigenvalues. A basis permutation can preserve every eigenvalue while assigning them to the wrong labels.

## Common Mistakes

1. **Wrong convention.** Using $[pq \mid rs]$ where $\langle pq \mid rs\rangle$ is expected (or vice versa). Produces plausible but wrong Hamiltonians.

2. **Wrong symmetries.** Both real-orbital tensors have eightfold symmetry, but under different permutations. Swapping only the two physicist bra indices is not one of the generic raw-integral equalities.

3. **The missing $\frac{1}{2}$.** It doubles the unrestricted raw two-body operator, not a state-independent energy constant. Restricting both index pairs requires antisymmetrisation instead.

## Exercises

1. **Conversion drill.** Given real canonical H₂ spatial orbitals and $[01|10]=0.1812104620$ Ha, find $\langle00|11\rangle$ and $\langle01|10\rangle$. Show the index conversion and the real-orbital symmetry used. Why would the supplied single number not generally suffice for complex orbitals?

2. **Symmetry check.** Starting from $\langle01|01\rangle=0.6637114014$ Ha, find $\langle10|10\rangle$ and verify the equality in chemist's notation. Then write the eight physicist permutations for four distinct symbolic indices; do not assume all eight positions differ when indices repeat.

3. **Prefactor diagnosis.** In the hypothetical two-mode example with $J=0.7$ Ha and $K=0.2$ Ha, three implementations return occupied-pair energies 0.5, 0.7 and 1.0 Ha. Match them to correct assembly, restricted raw assembly, and unrestricted raw assembly without the half. Explain why one unexplained molecular ground-energy discrepancy would not identify a unique bug.

4. **Rightmost first.** Apply $a_0^\dagger a_1^\dagger a_1a_0$ to each of the four two-mode basis states. Then reverse only the two annihilators. Which matrix entries change, and why is the result not merely an off-diagonal sign error?

5. **Lookup trace.** Trace `"1,1"` and `"0,1,2,3"` through the contextual factory. State the chemist lookup keys. What additional evidence would justify treating an absent result as a zero coefficient?

## Further Reading

- Szabo, A. and Ostlund, N. S. *Modern Quantum Chemistry.* Chapter 2, §2.3.2–2.3.3 gives both conventions and the conversion explicitly.
- Helgaker, T., Jørgensen, P., and Olsen, J. *Molecular Electronic-Structure Theory.* Chapter 10 is the definitive treatment of two-electron integrals, including symmetry properties.
- PySCF documentation on `mol.intor('int2e')` — returns integrals in chemist's notation.

---

**Previous:** [Chapter 1 — The Electronic Structure Problem](01-electronic-structure.html)

**Next:** [Chapter 3 — From Spatial to Spin-Orbital Integrals](03-spin-orbitals.html)
