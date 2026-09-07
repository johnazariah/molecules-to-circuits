// dotnet fsi code/ch07-fenwick.fsx
// Canonical BK index sets, not a Fenwick tree fed to treeEncodingScheme.
#r "nuget: FockMap, 0.9.0"
open Encodings
open Encodings.FenwickTree
open Encodings.MajoranaEncoding
open Encodings.BravyiKitaev

let n = 8
let occupations = [|1;0;1;1;0;0;1;0|]
let tree = ofArray (^^^) 0 occupations
for j in 0 .. n-1 do
    let prefix = occupations |> Array.take j |> Array.fold (^^^) 0
    let fromSet indices = indices |> Set.fold (fun acc i -> acc ^^^ tree.Data[i+1]) 0
    if fromSet (paritySet j) <> prefix || fromSet (occupationSet j) <> occupations[j] then
        failwith "Fenwick parity/occupation reconstruction failed"
    let changed = update tree j 1
    let changedIndices = [for i in 0 .. n-1 do if changed.Data[i+1] <> tree.Data[i+1] then yield i] |> Set.ofList
    if changedIndices <> Set.add j (updateSet j n) then failwith "Fenwick update-set mismatch"
    let operators = bravyiKitaevTerms Raise (uint32 j) (uint32 n)
    printfn "j=%d U=%A P=%A Occ=%A: %s" j (updateSet j n) (paritySet j) (occupationSet j) (operators.ToString())
if updateSet 3 8 <> set [7] || paritySet 3 <> set [1;2] || occupationSet 3 <> set [1;2;3] then
    failwith "Canonical j=3,n=8 index-set anchor changed"
printfn "Majorana assignments c3=%A; d3=%A" (cMajorana bravyiKitaevScheme 3 8) (dMajorana bravyiKitaevScheme 3 8)
