// ══════════════════════════════════════════════════════════════
// Chapter 3 Companion: From Spatial to Spin-Orbital Integrals
// ══════════════════════════════════════════════════════════════
// Run with: dotnet fsi code/ch03-spin-orbitals.fsx
// Source:   code/h2_dissociation_integrals.json, record "0.74"

#r "nuget: FockMap, 0.9.0"
#load "H2Data.fsx"

open System.IO
open System.Numerics
open System.Text.Json
open Encodings
open Encodings.Hamiltonian

// ── H₂/STO-3G integral tables (spin-orbital, physicist's convention) ──

let referencePath =
    Path.Combine(__SOURCE_DIRECTORY__, "h2_dissociation_integrals.json")

if not (File.Exists(referencePath)) then
    failwithf
        "Missing %s. Run: python3 code/ch18-generate-h2-integrals.py"
        referencePath

let referenceRoot, h2ScanRecords = H2Data.loadScan referencePath
let referenceMetadata = referenceRoot.GetProperty("_metadata")
let h2Record = referenceRoot.GetProperty("0.74")
let h2BondLength = h2Record.GetProperty("bond_length_angstrom").GetDouble()
let h2NuclearRepulsion = h2Record.GetProperty("Vnn").GetDouble()
let h2ReferenceHartreeFock = h2Record.GetProperty("E_HF").GetDouble()

let h2RawPhysicistIntegrals =
    h2ScanRecords |> Array.find (fun (r, _, _) -> r = 0.74) |> fun (_, _, map) -> map

let h2RawPhysicistFactory key =
    h2RawPhysicistIntegrals |> Map.tryFind key

printfn "Chapter 3: Spin-Orbital Integrals"
printfn "================================="
printfn "Source: %s" (Path.GetFileName(referencePath))
printfn "PySCF: %s" (referenceMetadata.GetProperty("pyscf_version").GetString())
printfn "R = %.2f Angstrom, Vnn = %.10f Ha, E_HF = %.10f Ha"
    h2BondLength
    h2NuclearRepulsion
    h2ReferenceHartreeFock
printfn "One-body integrals (spin-orbital):"
for p in 0..3 do
    let key = sprintf "%d,%d" p p
    match h2RawPhysicistFactory key with
    | Some v -> printfn "  h(%d,%d) = %.10f Ha" p p v.Real
    | None -> ()

printfn ""
printfn "Total two-body integrals in raw map: %d" (h2RawPhysicistIntegrals |> Map.filter (fun k _ -> k.Split(',').Length = 4) |> Map.count)
printfn "  Same-spin (αα + ββ): 16"
printfn "  Cross-spin (αβ + βα): 16"

printfn ""
printfn "Canonical raw tensor loaded; FockMap's primary builders apply the index order and 1/2."
printfn "Run make verify-data for coefficient and dense-matrix parity."
