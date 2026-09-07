// ══════════════════════════════════════════════════════════════
// Chapter 12 Companion: General Clifford Tapering
// ══════════════════════════════════════════════════════════════
// Run with: dotnet fsi code/ch12-clifford-tapering.fsx
// Prereq:   .NET 10 SDK; FSI restores pinned FockMap from NuGet.

#r "nuget: FockMap, 0.9.0"

open System.Numerics
open Encodings
open Encodings.Tapering

printfn ""
printfn "Chapter 12: General Clifford Tapering"
printfn "======================================"

// ── Symplectic representation ──
printfn ""
printfn "Symplectic representation:"
let sv = toSymplectic (PauliRegister("XYZ", Complex.One))
printfn "  XYZ → X bits = %A, Z bits = %A" sv.X sv.Z

// ── Commutativity check ──
printfn ""
printfn "Commutativity:"
let a = toSymplectic (PauliRegister("XX", Complex.One))
let b = toSymplectic (PauliRegister("ZZ", Complex.One))
printfn "  XX and ZZ commute? %b" (commutes a b)

let c = toSymplectic (PauliRegister("XZ", Complex.One))
let d = toSymplectic (PauliRegister("YI", Complex.One))
printfn "  XZ and YI commute? %b" (commutes c d)

// ── Heisenberg model: Clifford finds symmetries diagonal can't ──
printfn ""
printfn "Heisenberg model (XX + YY + ZZ + ZI + IZ):"

let heis =
    [| PauliRegister("XX", Complex(0.5, 0.0))
       PauliRegister("YY", Complex(0.5, 0.0))
       PauliRegister("ZZ", Complex(-0.3, 0.0))
       PauliRegister("ZI", Complex(0.2, 0.0))
       PauliRegister("IZ", Complex(0.2, 0.0)) |]
    |> PauliRegisterSequence

let diagCount = (diagonalZ2SymmetryQubits heis).Length
let fullCount = z2SymmetryCount heis
printfn "  Diagonal Z₂ qubits: %d" diagCount
printfn "  General Z₂ symmetries: %d" fullCount
printfn ""

// ── Full tapering remains sector-explicit ──
printfn "  FullClifford application is withheld here."
printfn "  Select generator eigenvalues from the target physical sector,"
printfn "  then compare all tapered sector spectra with the untapered matrix."
