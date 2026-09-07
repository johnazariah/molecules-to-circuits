// dotnet fsi code/ch21-export-check.fsx
// Actual versioned export artifacts; identity phase is explicitly recorded.
#load "ch03-spin-orbitals.fsx"
#load "../labs/PauliMatrix.fsx"
open System
open System.Globalization
open System.IO
open System.Numerics
open System.Text.Json
open Encodings
open Encodings.Hamiltonian
open Encodings.Trotterization
open Encodings.CircuitOutput

// FockMap 0.9.0 angle formatting is culture-sensitive. Declare the export locale.
CultureInfo.CurrentCulture <- CultureInfo.InvariantCulture
let h = computeHamiltonian ``Ch03-spin-orbitals``.h2RawPhysicistFactory 4u
PauliMatrix.assertH2JwMatrix "Export input" 1e-9 h
let step = firstOrderTrotter 0.1 h
let gates = decomposeTrotterStep step
let rotations = gates |> Array.filter (function Gate.Rz _ -> true | _ -> false) |> Array.length
if step.Rotations.Length <> 15 || rotations <> 14 then failwith "Identity factor/emitted rotation mismatch"
let outputDir = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../_build/data/export"))
Directory.CreateDirectory outputDir |> ignore
let write name (contents: string) = File.WriteAllText(Path.Combine(outputDir,name),contents)
write "h2.qasm2" (toOpenQasm {defaultOpenQasm2Options with Precision=15} 4 gates)
write "h2.qasm3" (toOpenQasm {defaultOpenQasmOptions with Precision=15} 4 gates)
write "h2.qs" (toQSharp {defaultQSharpOptions with Precision=15} 4 gates)
write "h2.circuit.json" (toCircuitJson 4 (Map.ofList [
    "FockMap","0.9.0"; "identityPhase","omitted from uncontrolled exported circuit"
    "signatureOrder","q0-leftmost"; "RzConvention","exp(-i angle Z/2)"]) gates)
write "h2.contract.json" (JsonSerializer.Serialize(
    {| schema = "book-export-contract-v1"; fockmap = "0.9.0"; qubits = 4
       time_step = step.TimeStep; decimal_places = 15
       rotations = step.Rotations |> Array.map (fun rotation -> {| signature=rotation.Operator.Signature; angle=rotation.Angle |}) |},
    JsonSerializerOptions(WriteIndented=true)))
printfn "Exported QASM 2/3, Q#, JSON and ordered Pauli-product contract to %s" outputDir
printfn "FockMap register-width arguments are int; ladder modes/counts are uint32."
printfn "15 factors, 14 Rz, 36 CNOT; identity phase is not emitted."
