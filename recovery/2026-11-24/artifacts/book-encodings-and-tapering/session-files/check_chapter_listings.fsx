#r "nuget: FockMap, 0.9.0"
#load "/Users/johnaz/PhD/copilot-worktrees/encodings-book/johnazariah-fluffy-doodle/code/ch03-spin-orbitals.fsx"

open System.Numerics
open Encodings
open Encodings.Hamiltonian
open Encodings.JordanWigner
open Encodings.BravyiKitaev
open Encodings.MajoranaEncoding
open Encodings.TreeEncoding
open Encodings.Tapering

let encoders = [
    ("Jordan-Wigner",         jordanWignerTerms)
    ("Bravyi-Kitaev",         bravyiKitaevTerms)
    ("Parity",                parityTerms)
    ("Balanced Binary Tree",  balancedBinaryTreeTerms)
    ("Balanced Ternary Tree", ternaryTreeTerms)
    ("Vlasov Tree",           vlasovTreeTerms)
]

for (name, encoder) in encoders do
    let ham =
        computeHamiltonianWith
            encoder ``Ch03-spin-orbitals``.h2RawPhysicistFactory 4u
    let terms = ham.DistributeCoefficient.SummandTerms
    if terms.Length <> 15 then failwithf "%s term count %d" name terms.Length
    printfn "%-25s  %d terms" name terms.Length

let n = 9
let rec buildNode j parent =
    let childIndices =
        [3*j+1; 3*j+2; 3*j+3] |> List.filter (fun c -> c < n)
    let children =
        childIndices |> List.map (fun c -> buildNode c (Some j))
    { Index = j; Children = children; Parent = parent }

let root = buildNode 0 None
let rec nodePairs node =
    (node.Index, node) :: (node.Children |> List.collect nodePairs)

let tree =
    { Root = root; Nodes = nodePairs root |> Map.ofList; Size = n }
let terms = encodeWithTernaryTree tree Raise 0u (uint32 n)
printfn "%A" terms

let requireTerms (expected : (string * Complex) list) (actual : PauliRegisterSequence) =
    let terms = actual.DistributeCoefficient.SummandTerms
    if terms.Length <> expected.Length then failwith "Unexpected term count"
    for signature, coefficient in expected do
        let term = terms |> Array.find (fun p -> p.Signature = signature)
        if Complex.Abs(term.Coefficient - coefficient) > 1e-12 then
            failwithf "Coefficient error %s" signature

requireTerms ["XZ", Complex(0.5,0.0); "YI", Complex(0.0,-0.5)]
    (encodeWithTernaryTree (vlasovTree 2) Raise 0u 2u)
requireTerms ["XX", Complex(0.5,0.0); "XY", Complex(0.0,-0.5)]
    (encodeWithTernaryTree (vlasovTree 2) Raise 1u 2u)

let jwHam =
    computeHamiltonianWith
        jordanWignerTerms ``Ch03-spin-orbitals``.h2RawPhysicistFactory 4u
let physicalGates = [ CNOT(0, 2); CNOT(1, 3) ]
let rotated = applyClifford physicalGates jwHam
let physical =
    taperDiagonalZ2 [(2, -1); (3, -1)] rotated
let reduced = physical.Hamiltonian
requireTerms
    [ "II", Complex(-1.0534210769165218,0.0)
      "IZ", Complex(0.3948443633559027,0.0)
      "ZI", Complex(0.3948443633559027,0.0)
      "ZZ", Complex(0.0112461571508209,0.0)
      "YY", Complex(-0.1812104620151967,0.0) ] reduced

let h =
    [| PauliRegister("ZIZI", Complex(0.8, 0.0))
       PauliRegister("ZZII", Complex(-0.4, 0.0))
       PauliRegister("IIZZ", Complex(0.3, 0.0))
       PauliRegister("IZIZ", Complex(0.2, 0.0)) |]
    |> PauliRegisterSequence
let result = taperDiagonalZ2 [ (1, 1); (3, -1) ] h
requireTerms
    ["ZZ",Complex(0.8,0.0); "ZI",Complex(-0.4,0.0); "IZ",Complex(-0.3,0.0); "II",Complex(-0.2,0.0)]
    result.Hamiltonian
printfn "Chapter listings and explicit physical coefficients passed."
