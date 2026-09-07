(**
# H2O Fixed-Bond Angle Scan

This lab analyzes the committed PySCF outputs used by Chapter 19. It is a
faithful data-analysis companion, not a FockMap energy calculation.

The Python generator fixes the O-H distance at the experimental value
0.9584 Angstrom and scans only the H-O-H angle:

```bash
python3 code/ch19-bond-angle-scan.py
dotnet fsi labs/08-h2o-workshop.fsx
```

The resulting minimum is conditional on that fixed bond length. A full
equilibrium geometry calculation would optimize both bond lengths and angle.
*)

open System
open System.Globalization
open System.IO
open System.Text.Json
open System.Security.Cryptography

type ScanPoint =
    { AngleDegrees: float
      NuclearRepulsionHa: float
      HartreeFockHa: float
      FullCiHa: float }

let parseFloat (value: string) =
    let result = Double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture)
    if not (Double.IsFinite result) then failwith "Nonfinite water scan value"
    result

let loadScan path =
    if not (File.Exists(path)) then
        failwithf
            "Missing %s. Run: python3 code/ch19-bond-angle-scan.py"
            path

    let lines = File.ReadAllLines(path)
    let expectedHeader = "angle_degrees,Vnn_Ha,E_HF_Ha,E_FCI_Ha"

    if lines.Length < 2 || lines.[0].Trim() <> expectedHeader then
        failwithf "Unexpected CSV schema in %s" path

    lines
    |> Array.skip 1
    |> Array.filter (String.IsNullOrWhiteSpace >> not)
    |> Array.map (fun line ->
        match line.Split(',') with
        | [| angle; vnn; hf; fci |] ->
            { AngleDegrees = parseFloat angle
              NuclearRepulsionHa = parseFloat vnn
              HartreeFockHa = parseFloat hf
              FullCiHa = parseFloat fci }
        | fields ->
            failwithf
                "Expected 4 CSV fields in %s, found %d"
                path
                fields.Length)

let codeDir = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "code"))
let coarsePath = Path.Combine(codeDir, "h2o_bond_angle_coarse.csv")
let finePath = Path.Combine(codeDir, "h2o_bond_angle_fine.csv")
let metadataPath = Path.Combine(codeDir, "h2o_bond_angle_metadata.json")

if not (File.Exists(metadataPath)) then
    failwith
        "Missing H2O provenance metadata. Run: python3 code/ch19-bond-angle-scan.py"

let metadataDocument = JsonDocument.Parse(File.ReadAllText(metadataPath))
let metadata = metadataDocument.RootElement
let recordedBondLength =
    metadata.GetProperty("fixed_OH_bond_length_angstrom").GetDouble()
let pyscfVersion = metadata.GetProperty("pyscf_version").GetString()

if not (Double.IsFinite recordedBondLength) || abs (recordedBondLength - 0.9584) > 1e-12 then
    failwithf "Unexpected recorded O-H bond length: %.10f" recordedBondLength

let coarse = loadScan coarsePath
let fine = loadScan finePath

let expectedCoarse = [|60.0 .. 5.0 .. 180.0|]
let expectedFine = [|95.0 .. 115.0|]
for field, expected, data in ["coarse_grid_degrees", expectedCoarse, coarse; "fine_grid_degrees", expectedFine, fine] do
    let declared = metadata.GetProperty(field).EnumerateArray() |> Seq.map _.GetDouble() |> Seq.toArray
    if declared <> expected || Array.map _.AngleDegrees data <> expected then
        failwithf "Unexpected, duplicate or missing water grid point: %s" field
let repositoryRoot = Directory.GetParent(codeDir).FullName
let hashes = metadata.GetProperty("generated_files_sha256")
let expectedFiles = set ["code/h2o_bond_angle_coarse.csv"; "code/h2o_bond_angle_fine.csv"; "manuscript/figures/h2o_bond_angle.png"]
if (hashes.EnumerateObject() |> Seq.map _.Name |> Set.ofSeq) <> expectedFiles then
    failwith "Unexpected water provenance file set"
for relative in expectedFiles do
    let digest = SHA256.HashData(File.ReadAllBytes(Path.Combine(repositoryRoot, relative))) |> Convert.ToHexString |> fun s -> s.ToLowerInvariant()
    if digest <> hashes.GetProperty(relative).GetString() then failwithf "Water provenance hash mismatch: %s" relative
for finePoint in fine do
    match coarse |> Array.tryFind (fun point -> point.AngleDegrees = finePoint.AngleDegrees) with
    | None -> ()
    | Some coarsePoint ->
        for left, right in [finePoint.NuclearRepulsionHa, coarsePoint.NuclearRepulsionHa; finePoint.HartreeFockHa, coarsePoint.HartreeFockHa; finePoint.FullCiHa, coarsePoint.FullCiHa] do
            if abs (left-right) > 5e-9 then failwith "Coarse/fine overlap mismatch"
for field, expected in ["molecule", "H2O"; "basis", "sto-3g (PySCF built-in basis data)"; "method", "RHF canonical molecular orbitals followed by PySCF FCI"] do
    if metadata.GetProperty(field).GetString() <> expected then failwithf "Water metadata mismatch: %s" field
if metadata.GetProperty("charge").GetInt32() <> 0 || metadata.GetProperty("spin_2S").GetInt32() <> 0 then
    failwith "Unexpected water charge/spin"

if coarse.Length <> 25 then
    failwithf "Expected 25 coarse points, found %d" coarse.Length

if fine.Length <> 21 then
    failwithf "Expected 21 fine points, found %d" fine.Length

let hfMinimum = fine |> Array.minBy (fun point -> point.HartreeFockHa)
let fciMinimum = fine |> Array.minBy (fun point -> point.FullCiHa)
let linear = coarse |> Array.find (fun point -> point.AngleDegrees = 180.0)

let hfBendingEnergy = linear.HartreeFockHa - hfMinimum.HartreeFockHa
let fciBendingEnergy = linear.FullCiHa - fciMinimum.FullCiHa
let hfFraction = hfBendingEnergy / fciBendingEnergy

printfn "H2O angular scan at fixed r_OH = 0.9584 Angstrom"
printfn "================================================"
printfn "Recorded PySCF version: %s" pyscfVersion
printfn "Coarse grid: %d points (60-180 deg, 5 deg step)" coarse.Length
printfn "Fine grid:   %d points (95-115 deg, 1 deg step)" fine.Length
printfn ""
printfn "HF sampled minimum:  %.0f deg  %.10f Ha"
    hfMinimum.AngleDegrees
    hfMinimum.HartreeFockHa
printfn "FCI sampled minimum: %.0f deg  %.10f Ha"
    fciMinimum.AngleDegrees
    fciMinimum.FullCiHa
printfn "Experimental angle:  104.52 deg"
printfn ""
printfn "HF bending energy (180 deg -> HF minimum):   %.6f Ha" hfBendingEnergy
printfn "FCI bending energy (180 deg -> FCI minimum): %.6f Ha" fciBendingEnergy
printfn "HF fraction of stated FCI bending energy:    %.1f%%" (100.0 * hfFraction)
printfn ""

printfn "Fine-scan neighborhood around the FCI minimum:"
fine
|> Array.filter (fun point -> abs (point.AngleDegrees - fciMinimum.AngleDegrees) <= 2.0)
|> Array.iter (fun point ->
    printfn "  %5.0f deg  HF %.10f Ha  FCI %.10f Ha"
        point.AngleDegrees
        point.HartreeFockHa
        point.FullCiHa)

if hfMinimum.AngleDegrees <> 101.0 then
    failwithf "Expected the sampled HF minimum at 101 deg, found %.1f" hfMinimum.AngleDegrees

if fciMinimum.AngleDegrees <> 99.0 then
    failwithf "Expected the sampled FCI minimum at 99 deg, found %.1f" fciMinimum.AngleDegrees

if abs (hfFraction - 0.897) > 0.002 then
    failwithf "Unexpected HF bending-energy fraction: %.6f" hfFraction

printfn ""
printfn "Checks passed."
printfn "This lab validates the PySCF reference data only."
printfn "Encoding and circuit-cost parity require a separate generated artifact."
