# Chapter 15: Trotterization in Practice

_Chapter 14 explained why we need to break the time-evolution operator into small, implementable pieces. This chapter actually does it — and reveals something satisfying: the structure of the Hamiltonian we've been studying since Chapter 6 determines the structure of the circuit._

## In This Chapter

- **What you'll learn:** How to apply first-order and second-order Trotter decomposition to the H₂ Hamiltonian, what the rotation list looks like, how to choose a time step, and how to estimate circuit cost before generating a single gate.
- **Why this matters:** The rotation list is the bridge between the symbolic Hamiltonian (the physicist's object) and the physical circuit (the engineer's object). This is where the two worlds meet.
- **Prerequisites:** Chapter 14 (you understand the Trotter–Suzuki formula and why it's needed).

---

## What Happens When Theory Meets H₂

In Chapter 14, we developed the Trotter formula in general terms: break $e^{-i\hat{H}t}$ into a product of single-term rotations. The formula looked clean and abstract. Now let's apply it to the actual 15-term Hamiltonian we've been carrying since Chapter 6 — the one we built integral by integral, verified against eigenvalues, and (optionally) tapered.

What comes out the other end is a **rotation list** — an ordered sequence of Pauli rotations, each with:
- a **Pauli string** (the "axis" of rotation: $XXYY$, $IIZZ$, etc.)
- a **rotation angle** (the coefficient times the time step)

We need two angle names. An entry $(P,\theta)$ in the rotation list means
$e^{-i\theta P}$, with **exponent angle** $\theta=c\Delta t$. A gate
$R_z(\gamma)$ means

$$R_z(\gamma)=e^{-i\gamma Z/2}
=\begin{pmatrix}e^{-i\gamma/2}&0\\0&e^{i\gamma/2}\end{pmatrix}.$$

Consequently, after reducing a Pauli string to a Z parity, the gate angle
is $\gamma=2\theta=2c\Delta t$. It is quite possible to use the correct
coefficient and the wrong angle convention. The result still looks like
a circuit; it simulates the wrong amount of time.

This list is the intermediate representation between the symbolic world (where the Hamiltonian lives) and the gate world (where the quantum computer lives). Chapter 16 will decompose each rotation into elementary gates; here, we focus on producing the list and understanding its structure.

Chapter 6 separated diagonal configuration energies from off-diagonal
configuration couplings. In the JW rotation list, weight-1/2 Z terms use fewer
standard-staircase CNOTs than the weight-4 coupling terms. This is a
representation-specific logical cost statement.

---

## The H₂ Hamiltonian, One Last Time

Here are our 15 terms, grouped by character:

| Group | Pauli Strings | Coefficients (Ha) | Weight | Character |
|:---:|:---|:---|:---:|:---|
| 1 | $IIII$ | $-0.8121706072$ | 0 | Energy shift |
| 2–5 | $IIIZ$, $IIZI$, $IZII$, $ZIII$ | $-0.2234315369$ to $+0.1714128264$ | 1 | Diagonal |
| 6–11 | $IIZZ$, $IZIZ$, $IZZI$, $ZIIZ$, $ZIZI$, $ZZII$ | $+0.1206252348$ to $+0.1744128761$ | 2 | Diagonal |
| 12–15 | $XXYY$, $XYYX$, $YXXY$, $YYXX$ | $\pm 0.0453026155$ | 4 | Configuration coupling |

Write $H=c_I I+H'$. The identity contributes the global phase
$e^{-ic_It}=e^{+i0.8121706072t}$ in *uncontrolled* evolution.
The emitted circuit represents $e^{-iH't}$ and has 14 non-identity
rotations, although the Hamiltonian has 15 terms.

Controlling the evolution changes the situation. On a control in $|+\rangle$,

$$\operatorname{ctrl}(e^{-ic_It}I)|+\rangle|\psi\rangle
=\frac{|0\rangle+e^{-ic_It}|1\rangle}{\sqrt2}|\psi\rangle.$$

What was a global phase is now relative between the control branches.
Either include the controlled offset phase, or declare that QPE measures
$H'$ and restore $c_I$ during energy decoding. Dropping it silently
changes the answer.

### First-order Trotter with $\Delta t = 0.1$

Each term produces one Pauli rotation. The angle is $\theta_k = c_k \times \Delta t$:

This is a contextual excerpt using the Hamiltonian from Chapter 6; the
complete construction runs with `dotnet fsi code/ch18-pipeline.fsx`.

```fsharp
open Encodings
open Encodings.Trotterization

let step = firstOrderTrotter 0.1 hamiltonian

printfn "Stored factors including identity: %d" step.Rotations.Length
for r in step.Rotations do
    printfn "  angle=%+.6f  Pauli=%s  weight=%d"
        r.Angle
        r.Operator.Signature
        (r.Operator.Signature |> Seq.filter (fun c -> c <> 'I') |> Seq.length)
```

The array contains 15 factors, including the identity phase. Circuit
emission skips the identity and produces 14 nonidentity rotations.
The following table lists only those 14 nonidentity factors in
**signature order for reading**, not a promise about the
library's iteration order. Preserve the actual returned order when building
the reference matrix for an exported circuit.

| Signature | $c_k$ (Ha) | $\theta_k=c_k(0.1)$ | Rz parameter $2\theta_k$ |
|:---|---:|---:|---:|
| IIIZ | -0.2234315369 | -0.0223431537 | -0.0446863074 |
| IIZI | -0.2234315369 | -0.0223431537 | -0.0446863074 |
| IIZZ | 0.1744128761 | 0.0174412876 | 0.0348825752 |
| IZII | 0.1714128264 | 0.0171412826 | 0.0342825653 |
| IZIZ | 0.1206252348 | 0.0120625235 | 0.0241250470 |
| IZZI | 0.1659278503 | 0.0165927850 | 0.0331855701 |
| XXYY | -0.0453026155 | -0.0045302616 | -0.0090605231 |
| XYYX | 0.0453026155 | 0.0045302616 | 0.0090605231 |
| YXXY | 0.0453026155 | 0.0045302616 | 0.0090605231 |
| YYXX | -0.0453026155 | -0.0045302616 | -0.0090605231 |
| ZIII | 0.1714128264 | 0.0171412826 | 0.0342825653 |
| ZIIZ | 0.1659278503 | 0.0165927850 | 0.0331855701 |
| ZIZI | 0.1206252348 | 0.0120625235 | 0.0241250470 |
| ZZII | 0.1686889817 | 0.0168688982 | 0.0337377963 |

Coefficients have energy units; both angle columns are dimensionless.
For example, the negative XXYY coefficient requires a negative Rz
parameter. Changing all four coupling signs is not a harmless display
choice unless states and the operator are transformed consistently.

The omitted identity factor has $\theta_I=-0.0812170607$ at this
time step. It is not a fifteenth Rz on an arbitrarily chosen qubit:
$e^{-i\theta_I I}$ acts equally on every basis state, whereas an Rz
gives opposite phases to the two Z eigenvalues.

### What the rotation list tells us

Each entry specifies the unitary $e^{-i\theta P}$; its two Pauli
eigenspaces receive phases $e^{-i\theta}$ and $e^{i\theta}$. The weight-1 and weight-2 Z rotations need fewer
entangling gates than the weight-4 coupling rotations under the standard
staircase decomposition.

---

## Second-Order Trotter: Symmetry for Accuracy

First-order Trotter applies the rotations in one direction:

$$e^{-ic_1 P_1 \Delta t} \cdot e^{-ic_2 P_2 \Delta t} \cdot \ldots \cdot e^{-ic_L P_L \Delta t}$$

Second-order Trotter applies them forward at half-angle, then backward at half-angle:

$$\underbrace{e^{-ic_1 P_1 \Delta t/2} \cdots e^{-ic_L P_L \Delta t/2}}_{\text{forward, half angle}} \cdot \underbrace{e^{-ic_L P_L \Delta t/2} \cdots e^{-ic_1 P_1 \Delta t/2}}_{\text{reverse, half angle}}$$

```fsharp
let step2 = secondOrderTrotter 0.1 hamiltonian

printfn "Stored factors including identity: %d (vs %d for first-order)"
    step2.Rotations.Length
    step.Rotations.Length
// → 30 stored factors (2 × 15); emission gives 28 nonidentity rotations
```

The symmetry cancels the second-order term in the *local operator
error*. This is not a scalar overestimate cancelled by an underestimate:
the errors are operators with signs and order. If $S_2(\delta)$ is
the symmetric step, then $S_2(-\delta)=S_2(\delta)^{-1}$.
Near zero, its logarithm therefore has only odd powers of $\delta$.
Its linear term is $-iH\delta$, leaving a cubic leading discrepancy.

| Property | First-order | Second-order |
|:---|:---:|:---:|
| Stored factors including identity | 15 for H₂ | 30 for H₂ |
| Emitted nonidentity rotations per step | $L$ (14 for H₂) | $2L$ (28 for H₂) |
| Rotation angles | $c_k \Delta t$ | $c_k \Delta t / 2$ |
| Error per step | $O(\Delta t^2)$ | $O(\Delta t^3)$ |
| Total error for $N$ steps | $O(t^2/N)$ | $O(t^3/N^2)$ |

Second order improves the asymptotic dependence on step count, but its prefactor
depends on the ordered nested commutators. Whether it wins at a particular
accuracy must be checked for the chosen Hamiltonian, ordering, and compiler.

The central pair of identical half-step factors can be merged into one
full-step factor. Adjacent repeated symmetric steps can also have
mergeable boundary rotations. The raw FockMap list contains both half-step
factors, including two identity factors. After identity omission, the
28-rotation, 72-CNOT figures are deliberately *before* adjacent-rotation
merging.

---

## Choosing the Time Step

The time step $\Delta t$ is the key knob in Trotterization. Too large → bad approximation. Too small → too many steps → too deep a circuit.

A useful dimensionless scale is
$\Delta t\sum_{k\ne I}|c_k|$, but requiring it to be below one is a heuristic,
not an accuracy theorem. The identity coefficient is excluded because it
commutes with every term and contributes no product-formula error.

For the canonical H₂ Hamiltonian,

$$\sum_{k\ne I}|c_k|=1.8871072169\ \text{Ha}.$$

Thus $\Delta t=0.1$ gives a scale of $0.1887$. Accuracy still has to be
established from commutators or direct unitary comparison.

Three quantities that have all been called a "norm" need separate names:

| Quantity | Definition | Canonical H₂ value | Use |
|:---|:---|:---|:---|
| Operator norm | $\max_{\|\psi\|=1}\|H\psi\|$ | Bounded above by $\lambda_{\mathrm{coeff}}$ | Worst-case action of the operator |
| Coefficient 1-norm | $\lambda_{\mathrm{coeff}}=\sum_k\lvert c_k\rvert$ | 2.6992778241 Ha | Pauli-expansion normalisation |
| Measurement coefficient norm | $\lambda_{\mathrm{meas}}=\sum_{k\ne I}\lvert c_k\rvert$ | 1.8871072169 Ha | Independent-term sampling bound |
| Pair-commutator sum | $\Lambda_{\mathrm{comm}}=\sum_{j<k}\|[c_jP_j,c_kP_k]\|$ | 0.2861997180 Ha² | First-order simulation bound |

There is also a matrix induced 1-norm, the largest absolute column sum.
It is not the Pauli coefficient 1-norm. We use explicit names instead
of writing an ambiguous $\|H\|_1$ and expecting the subscript to do
all the explaining.

---

## Quick CNOT Estimate (Before Gate Decomposition)

We can estimate the CNOT cost without actually building the gate circuit, using the formula from Chapter 4:

$$\text{CNOTs per Trotter step} = \sum_{k=1}^{L} 2(w_k - 1)$$

```fsharp
let cnots = trotterCnotCount step
printfn "Estimated CNOTs per first-order step: %d" cnots
```

For H₂ (14 non-identity terms):

| Term type | Count | Weight | CNOTs each | Subtotal |
|:---:|:---:|:---:|:---:|:---:|
| Single-Z ($IIIZ$, etc.) | 4 | 1 | 0 | 0 |
| Double-Z ($IIZZ$, etc.) | 6 | 2 | 2 | 12 |
| Coupling ($XXYY$, etc.) | 4 | 4 | 6 | 24 |
| **Total** | **14** | — | — | **36** |

36 CNOTs per first-order Trotter step. Second-order: 72. For a 100-step simulation: 3,600 (first-order) or 7,200 (second-order) CNOTs total.

These are unoptimised logical counts before routing, native-gate lowering,
state preparation, repetition, and error handling. They describe this H₂
product formula, not hardware feasibility or a larger molecule's cost.

---

## How Many Trotter Steps Do I Need?

The Trotter approximation introduces operator error. How many steps keep
it below a dimensionless target $\eta$?

### From one step to the whole simulation

Let $V=e^{-iH\delta}$ be the exact step and $S$ the approximate
unitary step. Suppose $\|S-V\|\leq K\delta^{p+1}$. A first-order
formula has $p=1$; a symmetric second-order formula has $p=2$.
The constant $K$ contains coefficients and commutators, so it is not
dimensionless in energy-time notation.

The telescoping identity

$$S^N-V^N=\sum_{\ell=0}^{N-1}S^{N-1-\ell}(S-V)V^\ell$$

contains $N$ copies of the one-step error, each surrounded by unitaries
of norm one. Hence

$$\|S^N-V^N\|\leq N\|S-V\|
\leq K\frac{|t|^{p+1}}{N^p},\qquad \delta=t/N.$$

This is the local-to-global calculation. Halving the step at fixed total
time doubles the number of steps; it does not keep the global error
improvement equal to the local one. First order improves by about two,
not four; second order by about four, not eight, once the leading term
dominates.

For the toy $H=0.3X+0.4Z$ Ha, the pair-commutator norm is 0.24 Ha².
At $t=1$, the first-order bound is $0.12/N$: 10, 20 and 40 steps
give bounds 0.012, 0.006 and 0.003 respectively. These are rigorous
upper bounds, not measured errors. Commuting terms would make this
particular first-order error vanish at every step count.

### A rigorous first-order bound

For first-order Trotter with $N$ steps of size $\Delta t = t/N$, the total error is bounded by:

$$\|S_1(t/N)^N-e^{-iHt}\|
\leq \frac{t^2}{2N} \sum_{j < k} \lVert [c_j P_j,\; c_k P_k] \rVert.$$

This is the pair-commutator form used here; tighter ordering-dependent results
and higher-order bounds are developed by Childs et al. (2021).

Define the first-order commutator sum

$$\Lambda_{\mathrm{comm}}=\sum_{j<k}\lVert[c_jP_j,c_kP_k]\rVert.$$

It bounds a source of ordering error. Two Pauli strings either commute, contributing
zero, or anticommute, contributing $2|c_jc_k|$. For the table above, 16 pairs
anticommute and

$$\Lambda_{\mathrm{comm}}=0.2861997180\ \text{Ha}^2.$$

For $t=1$ and a target unitary-operator error
$\eta=1.6\times10^{-3}$, the bound gives

$$N\geq\frac{t^2\Lambda_{\mathrm{comm}}}{2\eta}=89.44,$$

so 90 first-order steps are sufficient under this norm bound. This $\eta$ is
a dimensionless simulation error, not automatically an energy error of
1.6 mHa; a QPE or observable-estimation error budget must connect the two.

Do not confuse $\Lambda_{\mathrm{comm}}$, which has units Ha$^2$, with the
coefficient 1-norm
$\lambda_{\mathrm{coeff}}=\sum_k|c_k|=2.6992778241$ Ha or the
non-identity measurement norm $1.8871072169$ Ha.

Second-order bounds involve ordered nested commutators and their constants
depend on the exact symmetric formula. We therefore do not infer a numerical
second-order step count from $\lVert H\rVert_1$ alone. Compute the applicable
nested-commutator bound or compare the product-formula unitary directly with
$e^{-iHt}$.

### A resource budget with its units attached

The 90-step certificate at $t=1$ uses $90\times14=1260$ emitted
Pauli rotations and $90\times36=3240$ unoptimised logical CNOTs.
It says nothing yet about preparing the input state, repeating a
measurement, synthesising arbitrary angles in a fault-tolerant gate set,
or adding controls for QPE.

Keep a separate error ledger:

| Source | Example quantity | What it bounds |
|:---|:---|:---|
| Finite basis or active-space choice | Change in a target energy, Ha | Difference between chemical models |
| Coefficient pruning | $\sum_{\mathrm{removed}}\lvert c_k\rvert$, Ha | Upper bound on operator change |
| Product formula | $\eta=\|U_{\mathrm{PF}}-U_{\mathrm{exact}}\|$ | Dimensionless evolution discrepancy |
| Angle serialisation/synthesis | Unitary discrepancy | Extra implementation error |
| Sampling | Standard error in Ha | Statistical uncertainty for an estimator |
| Phase grid | $2\pi/(2^m t_0)$, Ha | QPE energy-bin spacing |

For example, deleting terms with total absolute coefficient $\rho$ changes
$H$ by an operator of norm at most $\rho$. The corresponding evolution
changes by at most $|t|\rho$ in atomic units. This gives a legitimate
conversion between one energy-valued bound and one dimensionless bound.
Simply reusing the decimal number 0.0016 for both is not a conversion.

The ledger also explains why a tighter Trotter bound does not repair a
poor basis set. The simulated model can be implemented extremely accurately
and still be an inaccurate model of the molecule.

---

## Key Takeaways

- A Trotter step converts a Hamiltonian into a list of **Pauli rotations** — each with a Pauli string and an angle.
- First-order: $L$ rotations. Second-order: $2L$ rotations at half angle, with quadratically better error scaling.
- $\Delta t\lambda_{\mathrm{meas}}$ is a scale, not an accuracy guarantee; commutator bounds or direct unitary checks set the step count.
- CNOT cost is estimable from Pauli weights alone: $\sum_k 2(w_k - 1)$.
- The canonical untapered H₂ step uses 36 CNOTs. A simulation cost additionally needs the total time, accuracy, compiler and surrounding algorithm.

## Common Mistakes

1. **Losing the identity offset.** Omit an ordinary identity-phase gate for
   uncontrolled observables, but retain the coefficient in the energy
   ledger. Controlled evolution makes this phase relative.

2. **Assuming second order always wins.** It has better asymptotic error scaling, but ordering, commutator prefactors, and compiler cancellations determine the actual crossover.

3. **Treating a norm heuristic as a proof.** A small
   $\Delta t\lambda_{\mathrm{meas}}$ is useful intuition, but only an applicable
   bound or direct comparison certifies the approximation.

## Exercises

1. **Rotation count.** A hypothetical molecular Hamiltonian has 600
   non-identity terms. How many rotations are emitted by the unmerged
   symmetric second-order construction? This is an assumed count, not
   an H₂O benchmark.

2. **Time step.** A hypothetical Hamiltonian has
   $\lambda_{\mathrm{meas}}=30$ Ha. For $\Delta t=0.01$ and $t=1$
   atomic time units, calculate the number of steps and the dimensionless
   norm scale. Does either establish an accuracy certificate?

3. **CNOT scaling.** Assume a Hamiltonian has 200 non-identity terms,
   each of weight 5. Calculate its raw first-order staircase CNOT count.
   Explain why this assumption cannot be inferred from a creation-operator
   maximum for an encoding.

## Further Reading

- Childs, A. M., Su, Y., Tran, M. C., Wiebe, N., and Zhu, S. "Theory of Trotter Error with Commutator Scaling." *Phys. Rev. X* 11, 011020 (2021). DOI: 10.1103/PhysRevX.11.011020.
- Suzuki, M. "General theory of fractal path integrals with applications to many-body theories and statistical physics." *J. Math. Phys.* 32, 400–407 (1991). DOI: 10.1063/1.529425.

---

**Previous:** [Chapter 14 — From Hamiltonian to Time Evolution](14-time-evolution.html)

**Next:** [Chapter 16 — The CNOT Staircase](16-cnot-staircase.html)
