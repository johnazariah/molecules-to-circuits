// ══════════════════════════════════════════════════════════════
// Chapter 6 Companion: Building the Qubit Hamiltonian
// ══════════════════════════════════════════════════════════════
// Run with: dotnet fsi code/ch06-building-hamiltonian.fsx
// Prereq:   .NET 10 SDK; FSI restores pinned FockMap from NuGet.

#r "nuget: FockMap, 0.9.0"
#load "ch03-spin-orbitals.fsx"

open System.Numerics
open Encodings
open Encodings.BravyiKitaev
open Encodings.Hamiltonian
open Encodings.JordanWigner
open Encodings.MajoranaEncoding
open Encodings.TreeEncoding

printfn ""
printfn "Chapter 6: Building the Qubit Hamiltonian"
printfn "=========================================="

// Build the JW Hamiltonian on 4 qubits
let hamiltonian =
    computeHamiltonian
        ``Ch03-spin-orbitals``.h2RawPhysicistFactory
        4u
let terms =
    hamiltonian.DistributeCoefficient.SummandTerms
    |> Array.filter (fun term -> Complex.Abs term.Coefficient > 1e-12)

let requireCoefficient signature expected =
    let actual =
        terms
        |> Array.tryFind (fun term -> term.Signature = signature)
        |> Option.map (fun term -> term.Coefficient.Real)
        |> Option.defaultWith (fun () -> failwithf "Missing Pauli term %s" signature)

    if abs (actual - expected) > 1e-9 then
        failwithf
            "FockMap Hamiltonian convention mismatch for %s: expected %.10f, got %.10f. Run make verify-data and reconcile the coefficient factory before using this output."
            signature
            expected
            actual

if terms.Length <> 15 then
    failwithf "Expected 15 nonzero JW terms, got %d" terms.Length

requireCoefficient "IIII" -0.8121706072
requireCoefficient "XXYY" -0.0453026155

printfn ""
printfn "The complete 15-term H₂ Hamiltonian (Jordan-Wigner):"
printfn ""
let mutable i = 0
for t in terms do
    i <- i + 1
    let character =
        if t.Signature |> Seq.forall (fun c -> c = 'I' || c = 'Z') then
            "diagonal"
        else
            "configuration coupling"
    printfn "  %2d. %+.4f  %s    [%s]" i t.Coefficient.Real t.Signature character

printfn ""
let diagCount = terms |> Array.filter (fun t -> t.Signature |> Seq.forall (fun c -> c = 'I' || c = 'Z')) |> Array.length
let offDiagCount = terms.Length - diagCount
printfn "  Diagonal terms: %d" diagCount
printfn "  Configuration-coupling terms: %d" offDiagCount
printfn ""
printfn "  Coupling enables determinant mixing; correlation energy is the"
printfn "  optimized expectation difference of the full Hamiltonian."

// Try other encodings
printfn ""
printfn "Same Hamiltonian, different encodings:"
let encoders = [
    ("Jordan-Wigner",         jordanWignerTerms)
    ("Bravyi-Kitaev",         bravyiKitaevTerms)
    ("Parity",                parityTerms)
    ("Balanced Binary Tree",  balancedBinaryTreeTerms)
    ("Balanced Ternary Tree", ternaryTreeTerms)
    ("Vlasov Tree",           vlasovTreeTerms)
]

for (name, encoder) in encoders do
    let ham =
        computeHamiltonianWith
            encoder
            ``Ch03-spin-orbitals``.h2RawPhysicistFactory
            4u
    let terms =
        ham.DistributeCoefficient.SummandTerms
        |> Array.filter (fun term -> Complex.Abs term.Coefficient > 1e-12)
    printfn "  %-25s  %d terms" name terms.Length
