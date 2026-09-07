// dotnet fsi code/ch20-measurement-groups.fsx
#load "ch03-spin-orbitals.fsx"
open System
open System.IO
open System.Numerics
open System.Text.Json
open Encodings
open Encodings.Hamiltonian
open Encodings.VariationalCircuits

let h =
    computeHamiltonian ``Ch03-spin-orbitals``.h2RawPhysicistFactory 4u
    |> _.DistributeCoefficient.SummandTerms
    |> Array.filter (fun term -> Complex.Abs term.Coefficient > 1e-12)
    |> PauliRegisterSequence
let groups = groupCommutingTerms h
let qwc (left: string) (right: string) =
    Seq.forall2 (fun a b -> a='I' || b='I' || a=b) left right
if groups.GroupCount <> 5 || groups.TotalTerms <> 15 then failwith "Pinned H2 QWC grouping changed"
let signatures = groups.Bases |> Array.collect (fun basis -> basis.Terms |> Array.map _.Signature)
if Array.distinct signatures |> Array.length <> 15 then failwith "Grouping drops or duplicates a term"
for basis in groups.Bases do
    for a in basis.Terms do
        for b in basis.Terms do
            if not (qwc a.Signature b.Signature) then failwith "Returned group is not qubit-wise commuting"
    let label = basis.BasisRotation |> Array.map string |> String.concat ""
    printfn "%s: %s" label (basis.Terms |> Array.map _.Signature |> String.concat ", ")
let fullCommutes a b =
    Seq.zip a b |> Seq.filter (fun (x,y) -> x<>'I' && y<>'I' && x<>y) |> Seq.length |> fun n -> n%2=0
if not (fullCommutes "XXYY" "XYYX") || qwc "XXYY" "XYYX" then
    failwith "General-commutation/QWC negative control failed"
printfn "Pinned groupCommutingTerms is greedy QWC (not general commuting). Identity needs no shots."
printfn "Pinned qpeResources multiplies UNCONTROLLED step CNOT/gate counts; it is not a controlled-QPE resource estimate."
let output = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../_build/data/h2_measurement_groups.json"))
Directory.CreateDirectory(Path.GetDirectoryName output) |> ignore
File.WriteAllText(output, JsonSerializer.Serialize(
    groups.Bases |> Array.map (fun b ->
        {| basis = b.BasisRotation |> Array.map string |> String.concat ""
           terms = b.Terms |> Array.map _.Signature; coefficient_l1_Ha = b.Weight |}),
    JsonSerializerOptions(WriteIndented=true)))
