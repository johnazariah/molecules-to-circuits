// dotnet fsi code/ch12-h2-physical-taper.fsx
// Physical spin parities are selected from HF, never by sweeping for a minimum.
#load "ch03-spin-orbitals.fsx"
#load "../labs/PauliMatrix.fsx"
open System
open System.IO
open System.Numerics
open System.Text.Json
open Encodings
open Encodings.Hamiltonian
open Encodings.Tapering
open Encodings.Trotterization

let h = computeHamiltonian ``Ch03-spin-orbitals``.h2RawPhysicistFactory 4u
PauliMatrix.assertH2JwMatrix "JW" 1e-9 h
let full = PauliMatrix.hamiltonianMatrix h
let parity row a b = if ((row >>> a) &&& 1) = ((row >>> b) &&& 1) then 1 else -1
let hfRow = 3
let signs = parity hfRow 0 2, parity hfRow 1 3
if signs <> (-1,-1) then failwith "HF spin-parity assignment failed"
let clifford = [CliffordGate.CNOT(0,2); CliffordGate.CNOT(1,3)]
let transformed = applyClifford clifford h
let reduced = (taperDiagonalZ2 [(2,-1); (3,-1)] transformed).Hamiltonian
let rows = [|12;9;6;3|]
let cnot row c t = if (row &&& (1 <<< c)) <> 0 then row ^^^ (1 <<< t) else row
for reducedRow in 0 .. 3 do
    let old = rows[reducedRow]
    if parity old 0 2 <> fst signs || parity old 1 3 <> snd signs then failwith "Wrong physical sector"
    if (old |> fun row -> cnot row 0 2 |> fun row -> cnot row 1 3) <> reducedRow + 12 then
        failwith "Wrong labelled reduction row order"
let block = Array2D.init 4 4 (fun i j -> full[rows[i],rows[j]])
let actual = PauliMatrix.hamiltonianMatrix reduced
for i in 0 .. 3 do
    for j in 0 .. 3 do
        if Complex.Abs(actual[i,j] - block[i,j]) > 1e-10 then failwith "Physical-sector matrix mismatch"
let spectrum = [|-1.8523881735695826; -1.2458776960825393; -0.8834567720521458; -0.23196166596181889|]
PauliMatrix.assertSpectrumMatrix "Physical Nalpha=Nbeta=1" 1e-9 spectrum actual
let pair = applyClifford [CliffordGate.CNOT(0,1)] reduced |> taperDiagonalZ2 [(1,1)] |> _.Hamiltonian
let pairMatrix = PauliMatrix.hamiltonianMatrix pair
let pairRows = [|12;3|]
for i in 0 .. 1 do
    for j in 0 .. 1 do
        if Complex.Abs(pairMatrix[i,j] - full[pairRows[i],pairRows[j]]) > 1e-10 then failwith "Closed-shell block mismatch"
PauliMatrix.assertSpectrumMatrix "Closed-shell pair (+1)" 1e-9 [|spectrum[0]; spectrum[3]|] pairMatrix
if abs (pairMatrix[1,1].Real + 1.831863646477506) > 1e-10 then failwith "Reduced HF is not |1>"
let wrong = (taperDiagonalZ2 [(2,1); (3,1)] transformed).Hamiltonian |> PauliMatrix.hamiltonianMatrix
if abs ((PauliMatrix.hermitianEigenvalues wrong)[0] - spectrum[0]) < 1e-3 then
    failwith "Wrong all-positive spin-parity sector unexpectedly matches physical ground state"

let metrics (h: PauliRegisterSequence) =
    let terms = h.DistributeCoefficient.SummandTerms |> Array.filter (fun t -> Complex.Abs t.Coefficient > 1e-12)
    let weights = terms |> Array.map (fun t -> t.Signature |> Seq.filter ((<>) 'I') |> Seq.length)
    terms.Length, Array.max weights, Array.sum weights, (firstOrderTrotter 0.1 h |> trotterStepStats).CnotCount
for label, operator, expected in ["4q", h, (15,4,32,36); "2q", reduced, (5,2,6,4); "1q", pair, (3,1,2,0)] do
    if metrics operator <> expected then failwithf "%s term/weight/CNOT mismatch" label
    printfn "%s: %s; metrics=%A" label (operator.ToString()) expected
printfn "Reduced rows [0;1;2;3] correspond to original rows %A. HF becomes row 3, then |1>." rows
printfn "The optional closed-shell pair block is not the whole two-electron sector."
let coefficients (operator: PauliRegisterSequence) =
    operator.DistributeCoefficient.SummandTerms
    |> Array.filter (fun t -> Complex.Abs t.Coefficient > 1e-12)
    |> Array.map (fun t -> t.Signature, t.Coefficient.Real) |> dict
let output = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../_build/data/h2_physical_taper.json"))
Directory.CreateDirectory(Path.GetDirectoryName output) |> ignore
File.WriteAllText(output, JsonSerializer.Serialize(
    {| original_rows_2q = rows; original_rows_1q = pairRows
       spin_parities = [|-1;-1|]; pair_parity = 1
       coefficients_2q_Ha = coefficients reduced; coefficients_1q_Ha = coefficients pair
       electronic_spectrum_2q_Ha = spectrum
       nuclear_repulsion_Ha = ``Ch03-spin-orbitals``.h2NuclearRepulsion |},
    JsonSerializerOptions(WriteIndented=true)))
