// Read-only parameter extension: dotnet fsi code/ch06-h2-at-bond.fsx 1.40
#r "nuget: FockMap, 0.9.0"
#load "H2Data.fsx"
#load "../labs/PauliMatrix.fsx"
open System
open System.Globalization
open System.IO
open Encodings.Hamiltonian

let args = fsi.CommandLineArgs |> Array.skip 1
if args.Length <> 1 then failwith "Usage: dotnet fsi code/ch06-h2-at-bond.fsx BOND_LENGTH"
let bond = Double.Parse(args[0], CultureInfo.InvariantCulture)
let _, records = H2Data.loadScan (Path.Combine(__SOURCE_DIRECTORY__, "h2_dissociation_integrals.json"))
let _, vnn, integrals =
    records |> Array.tryFind (fun (r, _, _) -> r = bond)
    |> Option.defaultWith (fun () -> failwithf "Bond length must be in the committed grid: %A" H2Data.expectedGrid)
let h = computeHamiltonian (fun key -> Map.tryFind key integrals) 4u
let eigenvalues = h |> PauliMatrix.hamiltonianMatrix |> PauliMatrix.hermitianEigenvalues
printfn "R=%.2f Angstrom; Vnn=%.10f Ha (separate); full electronic spectrum=%A" bond vnn eigenvalues
printfn "%s" (h.ToString())
