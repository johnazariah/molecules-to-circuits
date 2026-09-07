// dotnet fsi code/ch08-car-census.fsx [MAX_MODES=4]
// Finite evidence, not a theorem about all sizes or arbitrary tree labellings.
#r "nuget: FockMap, 0.9.0"
#load "../labs/PauliMatrix.fsx"
open System
open System.Numerics
open Encodings
open Encodings.JordanWigner
open Encodings.BravyiKitaev
open Encodings.MajoranaEncoding
open Encodings.TreeEncoding

let maxModes =
    match fsi.CommandLineArgs |> Array.skip 1 with
    | [||] -> 4
    | [|value|] -> Int32.Parse value
    | _ -> failwith "Usage: dotnet fsi code/ch08-car-census.fsx [MAX_MODES]"
if maxModes < 3 || maxModes > 6 then failwith "Finite dense census supports MAX_MODES 3..6"
let carError encode n =
    let dimension = pown 2 n
    let create = Array.init n (fun j -> encode Raise (uint32 j) (uint32 n) |> PauliMatrix.hamiltonianMatrix)
    let annihilate = Array.init n (fun j -> encode Lower (uint32 j) (uint32 n) |> PauliMatrix.hamiltonianMatrix)
    let mutable error = 0.0
    for p in 0 .. n-1 do
        for i in 0 .. dimension-1 do
            for j in 0 .. dimension-1 do
                error <- max error (Complex.Abs(create[p][i,j] - Complex.Conjugate(annihilate[p][j,i])))
        for q in 0 .. n-1 do
            for a,b,target in [annihilate[p], annihilate[q], 0.0; create[p], create[q], 0.0; annihilate[p], create[q], (if p=q then 1.0 else 0.0)] do
                let ab, ba = PauliMatrix.multiply a b, PauliMatrix.multiply b a
                for i in 0 .. dimension-1 do
                    for j in 0 .. dimension-1 do
                        let expected = if i=j then Complex(target,0.0) else Complex.Zero
                        error <- max error (Complex.Abs(ab[i,j] + ba[i,j] - expected))
    error
let encoders = ["JW", jordanWignerTerms; "BK", bravyiKitaevTerms; "Parity", parityTerms;
                "Binary", balancedBinaryTreeTerms; "Ternary", ternaryTreeTerms; "Vlasov", vlasovTreeTerms]
for n in 2 .. maxModes do
    for name, encode in encoders do
        let error = carError encode n
        if error > 1e-12 then failwithf "%s n=%d fails full CAR/adjoint checks: %.3e" name n error
        printfn "CAR/adjoints %-8s n=%d max error %.3e" name n error

let treeFromParents (parents: int option[]) =
    let rec node i : TreeNode =
        { Index=i; Parent=parents[i]
          Children=[for j in 0 .. parents.Length-1 do if parents[j] = Some i then yield node j] }
    let root = node 0
    let rec collect (n: TreeNode) = seq { yield n.Index,n; for child in n.Children do yield! collect child }
    { Root=root; Nodes=collect root |> Map.ofSeq; Size=parents.Length }
for n in 3 .. maxModes do
    let chain = treeFromParents (Array.init n (fun i -> if i=0 then None else Some(i-1)))
    let star = treeFromParents (Array.init n (fun i -> if i=0 then None else Some 0))
    // The generic index-set scheme is distinct from path/Majorana encoders.
    let chainError = carError (encodeOperator (treeEncodingScheme chain)) n
    let starError = carError (encodeOperator (treeEncodingScheme star)) n
    if chainError < 1e-3 || starError > 1e-12 then failwith "Generic chain/star finite regression changed"
    printfn "Generic index-set n=%d: chain CAR error %.1f; star %.1f (constructor itself does not enforce this)" n chainError starError
