// ══════════════════════════════════════════════════════════════
// Chapter 18 Companion: Verified Hamiltonian to Circuit + Cost Scan
// ══════════════════════════════════════════════════════════════
// Run with: dotnet fsi code/ch18-pipeline.fsx
// Prereq:   .NET 10 SDK; FSI restores pinned FockMap from NuGet.
//
// This script demonstrates the validated untapered path from Chapter 18:
//   1. Build Hamiltonian → 2. Trotterize → 3. Export
//
// It then applies the skeleton API across the H₂ bond-length grid
// to report Hamiltonian and circuit costs. Reference HF/FCI energies
// are generated independently by ch18-dissociation-scan.py.
//
// Output: _build/data/h2_circuit_costs.csv

#r "nuget: FockMap, 0.9.0"
#load "ch03-spin-orbitals.fsx"
#load "../labs/PauliMatrix.fsx"

open System
open System.IO
open System.Numerics
open Encodings
open Encodings.BravyiKitaev
open Encodings.CircuitOutput
open Encodings.Hamiltonian
open Encodings.JordanWigner
open Encodings.MajoranaEncoding
open Encodings.TreeEncoding
open Encodings.Trotterization

// ══════════════════════════════════════════════════════════════
//  Part 1: Verified Hamiltonian to Circuit (single geometry)
// ══════════════════════════════════════════════════════════════

printfn ""
printfn "Chapter 18: Verified Hamiltonian to Circuit"
printfn "============================================"
printfn ""

let encoders = [
    ("Jordan-Wigner",  jordanWignerTerms)
    ("Bravyi-Kitaev",  bravyiKitaevTerms)
    ("Parity",         parityTerms)
    ("Binary Tree",    balancedBinaryTreeTerms)
    ("Ternary Tree",   ternaryTreeTerms)
    ("Vlasov Tree",    vlasovTreeTerms)
]

let pruneZeroTerms (hamiltonian: PauliRegisterSequence) =
    hamiltonian.DistributeCoefficient.SummandTerms
    |> Array.filter (fun term -> Complex.Abs term.Coefficient > 1e-12)
    |> PauliRegisterSequence

let canonicalJw =
    computeHamiltonian
        ``Ch03-spin-orbitals``.h2RawPhysicistFactory
        4u
    |> pruneZeroTerms
let canonicalJwTerms =
    canonicalJw.DistributeCoefficient.SummandTerms
    |> Array.filter (fun term -> Complex.Abs term.Coefficient > 1e-12)

PauliMatrix.assertH2JwMatrix "Canonical JW" 1e-9 canonicalJw
PauliMatrix.assertH2Spectrum "Canonical JW" 1e-9 canonicalJw
let totalWeight = canonicalJwTerms |> Array.sumBy (fun term -> term.Signature |> Seq.filter ((<>) 'I') |> Seq.length)
let coefficientNorm = canonicalJwTerms |> Array.sumBy (fun term -> Complex.Abs term.Coefficient)
if totalWeight <> 32 || abs (coefficientNorm - 2.699277824145158) > 1e-12 then
    failwith "Canonical JW weight/norm mismatch"

let requireCanonicalCoefficient signature expected =
    let actual =
        canonicalJwTerms
        |> Array.tryFind (fun term -> term.Signature = signature)
        |> Option.map (fun term -> term.Coefficient.Real)
        |> Option.defaultWith (fun () -> failwithf "Missing Pauli term %s" signature)

    if abs (actual - expected) > 1e-9 then
        failwithf
            "FockMap Hamiltonian convention mismatch for %s: expected %.10f, got %.10f. Refusing to emit circuit-cost output."
            signature
            expected
            actual

if canonicalJwTerms.Length <> 15 then
    failwithf "Expected 15 nonzero JW terms, got %d" canonicalJwTerms.Length

requireCanonicalCoefficient "IIII" -0.8121706072
requireCanonicalCoefficient "XXYY" -0.0453026155

printfn "  %-15s  %6s  %5s  %10s  %11s"
    "Encoding" "Qubits" "Terms" "CNOTs/step" "Total gates"
printfn "  %-15s  %6s  %5s  %10s  %11s"
    "───────────────" "──────" "─────" "──────────" "───────────"

let encodingCsv = ResizeArray<string>()
encodingCsv.Add("encoding,qubits,hamiltonian_factors,nonidentity_rotations,cnots_per_step,total_gates")
for (name, encoder) in encoders do
    let ham =
        computeHamiltonianWith
            encoder
            ``Ch03-spin-orbitals``.h2RawPhysicistFactory
            4u
        |> pruneZeroTerms
    PauliMatrix.assertH2Spectrum name 1e-9 ham
    let step = firstOrderTrotter 0.1 ham
    let stats = trotterStepStats step
    let nonzeroTerms =
        ham.DistributeCoefficient.SummandTerms
        |> Array.filter (fun term -> Complex.Abs term.Coefficient > 1e-12)
    let rotations = nonzeroTerms |> Array.filter (fun t -> t.Signature |> Seq.exists ((<>) 'I')) |> Array.length
    if nonzeroTerms.Length <> 15 || rotations <> 14 then failwith "Unexpected H2 factor/rotation count"
    encodingCsv.Add(sprintf "%s,4,%d,%d,%d,%d" name nonzeroTerms.Length rotations stats.CnotCount stats.TotalGates)

    printfn "  %-15s  %6d  %5d  %10d  %11d"
        name
        4
        nonzeroTerms.Length
        stats.CnotCount
        stats.TotalGates

// Show the QASM for JW
printfn ""
printfn "OpenQASM 3.0 output (Jordan-Wigner, untapered):"
printfn "────────────────────────────────────────────────"
let jwHam =
    computeHamiltonian
        ``Ch03-spin-orbitals``.h2RawPhysicistFactory
        4u
    |> pruneZeroTerms
let jwStep = firstOrderTrotter 0.1 jwHam
let jwGates = decomposeTrotterStep jwStep
let qasm = toOpenQasm defaultOpenQasmOptions 4 jwGates
printfn "%s" qasm

// ══════════════════════════════════════════════════════════════
//  Part 2: H₂ Hamiltonian and Circuit Costs Across Geometries
// ══════════════════════════════════════════════════════════════

printfn ""
printfn "H₂ Circuit-Cost Scan"
printfn "===================="
printfn ""

// ── Load integrals from JSON (generated by ch18-generate-h2-integrals.py) ──

let scriptDir = __SOURCE_DIRECTORY__
let dissociationData = ``Ch03-spin-orbitals``.h2ScanRecords

// ── Precompute the skeleton once ──
let skeleton = computeHamiltonianSkeleton jordanWignerTerms 4u
printfn "  Skeleton precomputed (structure encoded once)."
printfn ""

// ── Scan ──
printfn "  %8s  %12s  %12s  %12s"
    "R (Å)" "Terms" "CNOTs/step" "Total gates"
printfn "  %8s  %12s  %12s  %12s"
    "────────" "────────────" "────────────" "────────────"

// Collect data for CSV output
let mutable csvLines = [ "R_angstrom,terms,cnots_per_step,total_gates" ]

for (bondLength, _vnn, integralMap) in dissociationData do
    let rawFactory key = integralMap |> Map.tryFind key
    let ham = applyCoefficients skeleton rawFactory |> pruneZeroTerms
    let direct = computeHamiltonian rawFactory 4u |> pruneZeroTerms
    let left = PauliMatrix.hamiltonianMatrix ham
    let right = PauliMatrix.hamiltonianMatrix direct
    PauliMatrix.hermitianEigenvalues left |> ignore
    let error = Array2D.init 16 16 (fun i j -> Complex.Abs(left[i,j] - right[i,j])) |> Seq.cast<float> |> Seq.max
    if error > 1e-10 then failwithf "R=%.2f direct/skeleton matrix mismatch %.3e" bondLength error
    let step = firstOrderTrotter 0.1 ham
    let stats = trotterStepStats step

    // This script constructs Hamiltonians and circuits. It does not infer
    // a ground-state energy from the identity coefficient.
    let terms =
        ham.DistributeCoefficient.SummandTerms
        |> Array.filter (fun term -> Complex.Abs term.Coefficient > 1e-12)
    let nTerms = terms.Length

    printfn "  %8.2f  %12d  %12d  %12d"
        bondLength nTerms stats.CnotCount stats.TotalGates

    csvLines <- csvLines @ [
        sprintf "%.2f,%d,%d,%d"
            bondLength nTerms stats.CnotCount stats.TotalGates
    ]

// ── Write derived output outside canonical source data ──
let repositoryRoot = Directory.GetParent(scriptDir).FullName
let buildDataDir = Path.Combine(repositoryRoot, "_build", "data")
Directory.CreateDirectory(buildDataDir) |> ignore
let csvPath = Path.Combine(buildDataDir, "h2_circuit_costs.csv")
File.WriteAllLines(csvPath, csvLines)
File.WriteAllLines(Path.Combine(buildDataDir, "h2_encoding_costs.csv"), encodingCsv)

printfn ""
printfn "  Data written to: %s" (Path.GetFileName(csvPath))
printfn "  Reference energies: h2_dissociation.csv (generated by PySCF)"
