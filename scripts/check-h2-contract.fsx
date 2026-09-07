// Immutable pin, raw-primary convention, literal N=0..4 spectra and state order.
#load "../code/ch03-spin-orbitals.fsx"
#load "../labs/PauliMatrix.fsx"
open System
open System.Numerics
open Encodings
open Encodings.Hamiltonian

let dll = typeof<PauliRegister>.Assembly.Location
let digest = H2Data.fileHash dll
if digest <> "0ba8ae967ea65d4945a336c1217f8939e41feb63b6f04af617b66537717d25c3" then
    failwithf "FockMap 0.9.0 DLL hash mismatch: %s" digest
let factory = ``Ch03-spin-orbitals``.h2RawPhysicistFactory
let h = computeHamiltonian factory 4u
PauliMatrix.assertH2JwMatrix "Immutable raw-primary JW" 1e-9 h
let matrix = PauliMatrix.hamiltonianMatrix h
let expected = [
    [|0.0|]
    [|-1.2533097866459773; -1.2533097866459773; -0.4750688487721783; -0.4750688487721783|]
    [|-1.8523881735695826; -1.2458776960825393; -1.2458776960825393; -1.2458776960825390; -0.8834567720521458; -0.23196166596181889|]
    [|-1.1607201545632546; -1.1607201545632546; -0.3595836390134429; -0.3595836390134429|]
    [|0.20807484184145802|] ]
for n in 0 .. 4 do
    let rows = [|0..15|] |> Array.filter (fun row -> [0..3] |> List.sumBy (fun j -> (row >>> j) &&& 1) = n)
    let block = Array2D.init rows.Length rows.Length (fun i j -> matrix[rows[i],rows[j]])
    PauliMatrix.assertSpectrumMatrix (sprintf "Literal N=%d" n) 1e-9 expected[n] block
let rejects label action =
    let rejected = try action(); false with _ -> true
    if not rejected then failwithf "Negative control accepted: %s" label
    printfn "Rejected: %s" label
rejects "displayed ket treated as big-endian row" (fun () ->
    if abs (matrix[12,12].Real + 1.831863646477506) > 1e-9 then failwith "HF row must be 3, not 12")
// Deliberately wrong: this legacy adapter must NOT feed a raw-primary builder.
let doubleAdapted = computeHamiltonian (rawPhysicistToWeightedFactory factory) 4u
rejects "double raw/weighted adaptation" (fun () -> PauliMatrix.assertH2JwMatrix "double-adapted" 1e-9 doubleAdapted)
printfn "Immutable NuGet DLL SHA-256: %s" digest
