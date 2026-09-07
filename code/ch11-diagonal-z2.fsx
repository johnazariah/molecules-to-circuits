// ══════════════════════════════════════════════════════════════
// Chapter 11 Companion: Diagonal Z₂ Symmetries
// ══════════════════════════════════════════════════════════════
// Run with: dotnet fsi code/ch11-diagonal-z2.fsx
// Prereq:   .NET 10 SDK; FSI restores pinned FockMap from NuGet.

#r "nuget: FockMap, 0.9.0"

open System.Numerics
open Encodings
open Encodings.Tapering

printfn ""
printfn "Chapter 11: Diagonal Z₂ Symmetries"
printfn "===================================="

// ── Example: a fully diagonal Hamiltonian ──
let h =
    [| PauliRegister("ZIZI", Complex(0.8, 0.0))
       PauliRegister("ZZII", Complex(-0.4, 0.0))
       PauliRegister("IIZZ", Complex(0.3, 0.0))
       PauliRegister("IZIZ", Complex(0.2, 0.0)) |]
    |> PauliRegisterSequence

printfn "Original Hamiltonian (4 qubits):"
printfn "  %s" (h.ToString())
printfn ""

let symQubits = diagonalZ2SymmetryQubits h
printfn "Diagonal Z₂ symmetric qubits: %A" symQubits
printfn ""

// ── Taper qubits 1 and 3 in different sectors ──
let sectorPlus = [ (1, 1); (3, 1) ]
let sectorMinus = [ (1, -1); (3, 1) ]

let taperedPlus = taperDiagonalZ2 sectorPlus h
let taperedMinus = taperDiagonalZ2 sectorMinus h
let chapterSector = taperDiagonalZ2 [(1, 1); (3, -1)] h
let chapterTerms =
    chapterSector.Hamiltonian.DistributeCoefficient.SummandTerms
    |> Array.map (fun term -> term.Signature, term.Coefficient)
    |> Map.ofArray
let expectedChapterTerms = Map.ofList ["ZZ", Complex(0.8,0.0); "ZI", Complex(-0.4,0.0); "IZ", Complex(-0.3,0.0); "II", Complex(-0.2,0.0)]
if chapterTerms <> expectedChapterTerms then failwithf "Chapter (+1,-1) toy sector mismatch: %A" chapterTerms
printfn "Chapter (+1,-1) sector: %s" (chapterSector.Hamiltonian.ToString())

printfn "(+1,+1) sector:"
printfn "  %d → %d qubits" taperedPlus.OriginalQubitCount taperedPlus.TaperedQubitCount
printfn "  Removed: %A" taperedPlus.RemovedQubits
printfn "  Result: %s" (taperedPlus.Hamiltonian.ToString())
printfn ""

printfn "(-1,+1) sector:"
printfn "  %d → %d qubits" taperedMinus.OriginalQubitCount taperedMinus.TaperedQubitCount
printfn "  Removed: %A" taperedMinus.RemovedQubits
printfn "  Result: %s" (taperedMinus.Hamiltonian.ToString())
printfn ""

// ── Convenience: taper all in +1 sector ──
let auto = taperAllDiagonalZ2WithPositiveSector h
printfn "Auto (+1 sector for all): %d → %d qubits" auto.OriginalQubitCount auto.TaperedQubitCount
printfn "  Result: %s" (auto.Hamiltonian.ToString())
