(**
---
title: Trotter Cost Comparison
category: Tutorials
categoryindex: 3
index: 7
---
*)

(**
# Trotter Cost: From Pauli Weight to CNOT Count

In the [scaling analysis](06-scaling.fsx), we compared encodings by their
**Pauli weight** — the number of non-identity Pauli operators in an encoded
ladder operator. But Pauli weight is a proxy for what actually matters on
hardware: **CNOT gates**.

This lab computes the concrete CNOT cost of a single Trotter step for the
H₂ molecule under all six encodings, connecting the abstract notion of
Pauli weight to measurable circuit depth.

## The CNOT Staircase

To implement a single Pauli rotation $e^{-i\theta P}$ where $P$ is a
Pauli string of weight $w$, the standard decomposition uses:

1. A CNOT staircase (depth $w - 1$) to entangle the relevant qubits
2. A single $R_z(2\theta)$ rotation
3. A reverse CNOT staircase (depth $w - 1$) to unentangle

**Total: $2(w - 1)$ CNOT gates per Pauli rotation.**

A first-order Trotter step applies one such rotation for each term in the
Hamiltonian, so the total CNOT count per Trotter step is:

$$\text{CNOTs per step} = \sum_{k=1}^{L} 2(w_k - 1)$$

where $L$ is the number of Pauli terms and $w_k$ is the weight of term $k$.

## Setup
*)

#load "../code/ch03-spin-orbitals.fsx"
#load "PauliMatrix.fsx"

open Encodings
open Encodings.BravyiKitaev
open Encodings.Hamiltonian
open Encodings.JordanWigner
open Encodings.MajoranaEncoding
open Encodings.TreeEncoding
open System.Numerics

(**
## H₂ Integrals (STO-3G, R = 0.74 Å)

The complete generated tensor is shared with Chapter 3 and checked by
`make verify-data`; this lab does not maintain a second truncated map.
*)

let nModes = 4u
let lookup = ``Ch03-spin-orbitals``.h2RawPhysicistFactory

(**
## CNOT Cost Computation

For each encoding, we:
1. Encode the H₂ Hamiltonian
2. Combine like terms (DistributeCoefficient)
3. Compute CNOT cost per Pauli rotation: $2(w - 1)$
4. Sum over all non-identity terms for one Trotter step
*)

let pauliWeight (signature : string) =
    signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

let cnotsPerRotation w = 2 * (w - 1) |> max 0

let trotterCost (ham : PauliRegisterSequence) =
    let terms = ham.DistributeCoefficient.SummandTerms
    let nonIdentityTerms =
        terms
        |> Array.filter (fun term ->
            Complex.Abs term.Coefficient > 1e-12
            && pauliWeight term.Signature > 0)
    let totalCnots =
        nonIdentityTerms
        |> Array.sumBy (fun t -> t.Signature |> pauliWeight |> cnotsPerRotation)
    let maxWeight =
        nonIdentityTerms
        |> Array.map (fun t -> pauliWeight t.Signature)
        |> Array.max
    let avgWeight =
        nonIdentityTerms
        |> Array.averageBy (fun t -> pauliWeight t.Signature |> float)
    {| Terms = nonIdentityTerms.Length
       MaxWeight = maxWeight
       AvgWeight = avgWeight
       TotalCnots = totalCnots |}

(**
## Encoding H₂ with All Six Encodings
*)

let encoders : (string * (LadderOperatorUnit -> uint32 -> uint32 -> PauliRegisterSequence)) list =
    [ "Jordan-Wigner",  jordanWignerTerms
      "Parity",         parityTerms
      "Bravyi-Kitaev",  bravyiKitaevTerms
      "Binary Tree",    balancedBinaryTreeTerms
      "Ternary Tree",   ternaryTreeTerms
      "Vlasov Tree",    vlasovTreeTerms ]

(**
## Results
*)

printfn ""
printfn "╔══════════════════════════════════════════════════════════════════════════╗"
printfn "║       CNOT Cost per First-Order Trotter Step — H₂ (STO-3G, 4 qubits)  ║"
printfn "╠══════════════════════════════════════════════════════════════════════════╣"
printfn "║ Encoding        │ Terms │ Max w │ Avg w │ CNOTs/step │ vs JW           ║"
printfn "╠═════════════════╪═══════╪═══════╪═══════╪════════════╪═════════════════╣"

let mutable jwCnots = 0

for (name, encode) in encoders do
    let ham = computeHamiltonianWith encode lookup nModes
    PauliMatrix.assertH2Spectrum name 1e-7 ham
    let stats = trotterCost ham
    if name = "Jordan-Wigner" then jwCnots <- stats.TotalCnots
    let comparison =
        if name = "Jordan-Wigner" then "baseline"
        elif stats.TotalCnots < jwCnots then
            sprintf "%.0f%% fewer" (100.0 * float (jwCnots - stats.TotalCnots) / float jwCnots)
        elif stats.TotalCnots > jwCnots then
            sprintf "%.0f%% more" (100.0 * float (stats.TotalCnots - jwCnots) / float jwCnots)
        else "same"
    printfn "║ %-15s │  %3d  │  %3d  │ %4.1f  │    %4d    │ %-15s ║"
        name stats.Terms stats.MaxWeight stats.AvgWeight stats.TotalCnots comparison

printfn "╚══════════════════════════════════════════════════════════════════════════╝"

(**
## JW Has the Lowest Count for This H2 Hamiltonian

For H₂ (4 qubits), Jordan–Wigner has the **lowest** CNOT count. Why? Because
JW's $O(n)$ weight only becomes a problem at larger $n$. At $n = 4$, the
maximum weight of any JW operator is just 4 — and most Hamiltonian terms
have even lower weight.

This does not establish a molecular-size crossover. Products of ladder
operators can cancel parity strings, and simplification changes term weights.

## An Operator-Level Scaling Illustration

We measure the **maximum Pauli-component weight** in an encoded ladder
operator at several sizes. We also display the illustrative $2(w-1)$ CNOT
count for exponentiating a Pauli string of that weight. A ladder operator
is not itself a Hermitian Hamiltonian term; this is not a measured molecular
step cost, a depth estimate, or evidence of a crossover.
*)

let maxWeightForEncoding (encode : LadderOperatorUnit -> uint32 -> uint32 -> PauliRegisterSequence) n =
    [ for j in 0u .. n - 1u ->
        let prs = encode Raise j n
        prs.SummandTerms
        |> Array.map (fun t -> pauliWeight t.Signature)
        |> Array.max ]
    |> List.max

let systemSizes = [ 4u; 8u; 16u; 32u; 64u ]

printfn ""
printfn "╔════════════════════════════════════════════════════════════════════════════════╗"
printfn "║       Maximum Operator Weight → CNOTs per Rotation: 2(w − 1)                ║"
printfn "╠════════════════════════════════════════════════════════════════════════════════╣"
printfn "║ Encoding        │  n=4 (CNOTs) │  n=8 (CNOTs) │ n=16 (CNOTs) │ n=32 (CNOTs) ║"
printfn "╠═════════════════╪══════════════╪══════════════╪══════════════╪══════════════╣"

for (name, encode) in encoders do
    let data =
        [ for n in [ 4u; 8u; 16u; 32u ] ->
            let w = maxWeightForEncoding encode n
            sprintf "%2d (%3d)" w (cnotsPerRotation w) ]
    printfn "║ %-15s │  %-11s │  %-11s │  %-11s │  %-11s ║"
        name data.[0] data.[1] data.[2] data.[3]

printfn "╚════════════════════════════════════════════════════════════════════════════════╝"

let jw64 = cnotsPerRotation (maxWeightForEncoding jordanWignerTerms 64u)
let ternary64 = cnotsPerRotation (maxWeightForEncoding ternaryTreeTerms 64u)
printfn "n=64 illustrative maximum-component staircase counts: JW=%d, ternary=%d (ratio %.2f)"
    jw64 ternary64 (float jw64 / float ternary64)
if jw64 <> 126 || ternary64 <> 10 then failwith "Pinned n=64 ladder-component cost changed"

(**
## The Scaling Story

The reproduced operator-level separation is visible:

- At $n = 32$: JW uses 62 standard staircase CNOTs for its worst-case
  rotation, while the tested ternary mapping uses 8.
- At $n = 64$: the corresponding values are 126 and 10, a 12.6x ratio.

These are maximum single-rotation costs. A molecular step needs the complete
generated distribution of terms and weights.

## The Formula in Context

The $2(w-1)$ count is the cost of the standard compute–rotate–uncompute
CNOT staircase on an all-to-all logical device without ancillae.
Connectivity-aware compilation, cancellations between adjacent rotations,
ancilla-assisted constructions, or different native entangling gates can
change the final count. Pauli weight is therefore a useful pre-compilation
cost signal, not a universal lower bound.

---

**Previous:** [Encoding Scaling Analysis](06-scaling.fsx)

**Back to:** [Repository quick start](../README.md#quick-start)
*)
