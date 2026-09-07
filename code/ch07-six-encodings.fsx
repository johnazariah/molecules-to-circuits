// ══════════════════════════════════════════════════════════════
// Chapter 7 Companion: Six Encodings, One Interface
// ══════════════════════════════════════════════════════════════
// Run with: dotnet fsi code/ch07-six-encodings.fsx
// Prereq:   .NET 10 SDK; FSI restores pinned FockMap from NuGet.

#r "nuget: FockMap, 0.9.0"
#load "ch03-spin-orbitals.fsx"

open System.Numerics
open Encodings
open Encodings.BravyiKitaev
open Encodings.JordanWigner
open Encodings.MajoranaEncoding
open Encodings.TreeEncoding

printfn ""
printfn "Chapter 7: Six Encodings, One Interface"
printfn "========================================="

let encoders = [
    ("Jordan-Wigner",         jordanWignerTerms)
    ("Bravyi-Kitaev",         bravyiKitaevTerms)
    ("Parity",                parityTerms)
    ("Balanced Binary Tree",  balancedBinaryTreeTerms)
    ("Balanced Ternary Tree", ternaryTreeTerms)
    ("Vlasov Tree",           vlasovTreeTerms)
]

// ── Weight scaling comparison ──
printfn ""
printfn "Pauli weight scaling (worst-case weight of a†_j):"
printfn ""
printfn "  n     JW    BK    TT    JW/TT"
printfn "  ──    ──    ──    ──    ─────"

for n in [| 4u; 8u; 16u; 32u; 64u |] do
    let maxWeight (encoder : LadderOperatorUnit -> uint32 -> uint32 -> PauliRegisterSequence) =
        [| for j in 0u .. n - 1u ->
               let terms = encoder Raise j n
               terms.SummandTerms
               |> Array.map (fun t ->
                   t.Signature |> Seq.filter (fun c -> c <> 'I') |> Seq.length)
               |> Array.max |]
        |> Array.max

    let jw = maxWeight jordanWignerTerms
    let bk = maxWeight bravyiKitaevTerms
    let tt = maxWeight ternaryTreeTerms
    printfn "  %2d    %2d    %2d    %2d    %.1f×" n jw bk tt (float jw / float tt)

// ── CNOT cost comparison ──
printfn ""
printfn "CNOT cost per Pauli rotation [2(w-1)]:"
printfn ""
for w in [| 1; 2; 4; 5; 12; 100 |] do
    let cnots = 2 * (w - 1) |> max 0
    printfn "  weight %3d → %3d CNOTs" w cnots
