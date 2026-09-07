(**
# Comparing Encodings Side-by-Side

This tutorial demonstrates all six fermion-to-qubit encodings available
in the library and compares their Pauli weight scaling.

## The Six Encodings

| Encoding | Pauli Weight | Function |
|----------|--------------|----------|
| Jordan-Wigner | O(n) | `jordanWignerTerms` |
| Bravyi-Kitaev | O(log n) | `bravyiKitaevTerms` |
| Parity | O(n) | `parityTerms` |
| Binary Tree | Θ(log n) | `balancedBinaryTreeTerms` |
| Ternary Tree | Θ(log n) | `ternaryTreeTerms` |
| Vlasov Tree | Θ(log n) | `vlasovTreeTerms` |

## Setup
*)

#r "nuget: FockMap, 0.9.0"
#load "../code/ch03-spin-orbitals.fsx"
#load "PauliMatrix.fsx"
open Encodings
open Encodings.BravyiKitaev
open Encodings.Hamiltonian
open Encodings.JordanWigner
open Encodings.MajoranaEncoding
open Encodings.TreeEncoding

(**
## Test Configuration

We'll encode the creation operator a†₂ on a system with 8 fermionic modes:
*)

let modeIndex = 2u
let totalModes = 8u

let encodings = [
    ("Jordan-Wigner", jordanWignerTerms)
    ("Bravyi-Kitaev", bravyiKitaevTerms)
    ("Parity",        parityTerms)
    ("Binary Tree",   balancedBinaryTreeTerms)
    ("Ternary Tree",  ternaryTreeTerms)
    ("Vlasov Tree",   vlasovTreeTerms)
]

(**
## Helper: Calculate Pauli Weight

Pauli weight is the number of non-identity operators in a Pauli string.
Lower weight means shorter quantum circuits.
*)

let pauliWeight (reg: PauliRegister) =
    reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

let maxWeight (prs: PauliRegisterSequence) =
    prs.SummandTerms |> Array.map pauliWeight |> Array.max

(**
## Encoding Results

Let's see how each encoding represents a†₂:
*)

printfn ""
printfn "═══════════════════════════════════════════════════════════════"
printfn " Comparing Fermion-to-Qubit Encodings"
printfn " Operator: a†₂ (creation on mode 2) with %d total modes" totalModes
printfn "═══════════════════════════════════════════════════════════════"

for (name, encode) in encodings do
    let result = encode Raise modeIndex totalModes
    let weight = maxWeight result

    printfn ""
    printfn "▶ %s (max weight = %d)" name weight
    printfn "  Pauli strings:"
    for term in result.SummandTerms do
        printfn "    %s" term.Signature

(**
## Weight Comparison Table

Now let's create a summary table comparing the encodings:
*)

printfn ""
printfn "═══════════════════════════════════════════════════════════════"
printfn " Weight Scaling Comparison"
printfn "═══════════════════════════════════════════════════════════════"
printfn ""
printfn "  Encoding         Max Weight   Scaling"
printfn "  ────────────────────────────────────────"

for (name, encode) in encodings do
    let result = encode Raise modeIndex totalModes
    let weight = maxWeight result
    let scaling =
        match name with
        | "Jordan-Wigner" | "Parity" -> "O(n)"
        | "Bravyi-Kitaev" | "Binary Tree" -> "Θ(log n)"
        | _ -> "Θ(log n)"
    printfn "  %-16s  %5d        %s" name weight scaling

(**
## When to Use Each Encoding

### Jordan-Wigner
- **Best for**: Small systems, simple implementations, educational purposes
- **Pros**: Easy to understand, well-documented
- **Cons**: O(n) depth limits scalability

### Bravyi-Kitaev
- **Best for**: Medium-sized systems needing balance
- **Pros**: O(log n) weight, well-studied
- **Cons**: More complex implementation

### Parity
- **Best for**: Systems where final qubit encodes total parity
- **Pros**: Simple structure, useful for symmetry exploitation
- **Cons**: Same O(n) scaling as Jordan-Wigner

### Binary Tree
- **Best for**: Large systems with power-of-2 mode counts
- **Pros**: Clean Θ(log n) structure
- **Cons**: Less literature than Bravyi-Kitaev

### Ternary Tree
- **Best for**: Maximum efficiency on NISQ hardware
- **Pros**: Θ(log n) worst-case weight with a smaller tree-depth constant
- **Cons**: Newest approach, fewer reference implementations

## Key Insight
*)

printfn ""
printfn "═══════════════════════════════════════════════════════════════"
printfn " Key Insight"
printfn "═══════════════════════════════════════════════════════════════"
printfn ""
printfn " Tree-based mappings reduce worst-case ladder-operator weight"
printfn " from linear to logarithmic growth."
printfn ""
printfn " The generated n=64 maxima are JW=64, BK=7, ternary=6."
printfn ""

(**
## Full H2 Spectrum Comparison

Term counts and mutual agreement are not enough. We construct each 16x16 Pauli
matrix and diagonalise the complete complex-Hermitian operator. Sorted
eigenvalues are compared directly at the stated absolute tolerance in Hartrees.
Power sums remain diagnostics only: small moment errors do not bound eigenvalue
errors at the same tolerance.
*)

let h2Lookup = ``Ch03-spin-orbitals``.h2RawPhysicistFactory

printfn " Full H2 spectrum comparison:"
for (name, encode) in encodings do
    let hamiltonian =
        computeHamiltonianWith encode h2Lookup 4u
    if name = "Jordan-Wigner" then
        PauliMatrix.assertH2JwMatrix name 1e-9 hamiltonian
    PauliMatrix.assertH2Spectrum name 1e-7 hamiltonian
