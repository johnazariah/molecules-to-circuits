# Appendix A: Selected FockMap API Reference

This is a working reference for the operations used in the book, not an
inventory of every public function. It targets **FockMap 0.9.0**, whose
NuGet package records source commit
`96320a56786393269fd681c67c66df88058a8b8f`. The
[pinned source](https://github.com/johnazariah/encodings/tree/96320a56786393269fd681c67c66df88058a8b8f/src/Encodings)
is the precise external destination for the API contracts below. The
[online library documentation](https://johnazariah.github.io/encodings/)
may describe a later version.

Each section states the inputs, the result and what the result does
*not* establish. For the molecular derivations, return to the named
chapters. For F# syntax, use **Reading and Running the Code**.

## Values at the boundary

| Value | Meaning in these examples |
|:---|:---|
| `Complex` | `System.Numerics.Complex`: floating-point real and imaginary parts |
| `Pauli` | One of `I`, `X`, `Y`, `Z` |
| `LadderOperatorUnit` | In particular, `Raise` or `Lower` for a fermionic ladder |
| `PauliRegister` | A fixed-width Pauli string with a complex coefficient |
| `PauliRegisterSequence` | A sum of such strings, possibly with an outer coefficient |
| `EncoderFn` | `LadderOperatorUnit -> uint32 -> uint32 -> PauliRegisterSequence` |
| Coefficient factory | `string -> Complex option`; a key lookup, not an integral solver |
| `HamiltonianSkeleton` | Precomputed operator structure for a particular width and encoder |
| `TrotterStep` | An ordered array of symbolic Pauli rotations and a time-step value |
| `Gate[]` | An ordered logical gate list, not a compiled hardware schedule |

`Complex` coefficients are numerical even though Pauli multiplication
tracks its discrete phases algebraically. "Symbolic" does not mean
that arbitrary floating-point integrals have become exact real numbers.

For inspection, use `ham.DistributeCoefficient.SummandTerms`: this
pushes the outer coefficient into the individual terms. Inspecting only
an inner coefficient while ignoring an outer factor can give a wrong
table. `term.Signature` puts qubit 0 **on the left**; it is not a
Qiskit-style Pauli label.

## Choosing an encoder

All six entry points below accept the ladder operation, zero-based mode
index, and total mode count, in that order. The mode index must be less
than the count. The output is an encoded ladder, usually a sum of strings,
not a state vector and not a circuit.

| Module to open after `open Encodings` | Function |
|:---|:---|
| `Encodings.JordanWigner` | `jordanWignerTerms` |
| `Encodings.BravyiKitaev` | `bravyiKitaevTerms` |
| `Encodings.MajoranaEncoding` | `parityTerms` |
| `Encodings.TreeEncoding` | `balancedBinaryTreeTerms` |
| `Encodings.TreeEncoding` | `ternaryTreeTerms` |
| `Encodings.TreeEncoding` | `vlasovTreeTerms` |

For example, `jordanWignerTerms Raise 2u 4u` gives the two four-qubit
strings `ZZXI` and `ZZYI` with coefficients `0.5` and `-0.5i`.
`Lower` changes the sign of the imaginary coefficient.
The encoding preserves an operator algebra; changing encoder does not
choose an electron count or prepare the corresponding state.

The custom interfaces have different domains. The generic
`treeEncodingScheme` helper has a documented star-tree support policy;
it does not enforce that policy by rejecting every non-star input.
The separate canonical BK implementation uses Fenwick-specific formulas.
The path-based Majorana construction is different again:
`computeLinks` checks that a node has at most three children.
Do not infer an all-tree validity theorem from a function accepting a
record. *Six Encodings, One Interface* and *Building a Tree Encoding*
explain the supported construction, finite tests and required CAR checks.

## Raw and weighted Hamiltonian contracts

Open `Encodings.Hamiltonian`. The primary API consumes a raw
**spin-orbital**, single-bar physicist factory:

| Query | Returned value | Operator contribution |
|:---|:---|:---|
| `"p,q"` | $h_{pq}$ | $h_{pq}a_p^\dagger a_q$ |
| `"p,q,r,s"` | $g_{pqrs}=\langle pq\mid rs\rangle$ | $\tfrac12 g_{pqrs}a_p^\dagger a_q^\dagger a_s a_r$ |

The builder applies the half and annihilator order. The raw factory
does neither. A chemist-order source must first undergo the appropriate
index conversion, and a spatial source must undergo spin expansion.
The factory contract is not permission to skip those steps.

The legacy weighted API uses a different key:
`"p,q,k,l"` returns the **entire coefficient** of
$a_p^\dagger a_q^\dagger a_k a_l$. It applies that coefficient
verbatim. Thus raw key `(p,q,r,s)` maps to weighted key `(p,q,s,r)`
with value $g_{pqrs}/2$. The type is identical in F#, so the compiler
cannot distinguish the conventions for you.

| Entry point | Arguments, left to right | Result |
|:---|:---|:---|
| `computeHamiltonian` | raw factory, `uint32` count | JW Hamiltonian |
| `computeHamiltonianWith` | encoder, raw factory, count | Chosen encoding |
| `computeHamiltonianWithParallel` | encoder, raw factory, count | Same raw contract, parallel construction |
| `computeHamiltonianCached` | encoder, raw factory, count | Same raw contract, cached operator work |
| `computeHamiltonianFromWeighted` | weighted factory, count | Legacy weighted JW Hamiltonian |
| `computeHamiltonianFromWeightedWith` | encoder, weighted factory, count | Legacy weighted chosen encoding |
| `weightedToRawFactory` | weighted factory | Factory suitable for a primary builder |
| `antisymmetrizedToRawFactory` | full double-bar factory | Operator-equivalent input for a primary builder |

The last adapter halves the double-bar entries so that the primary
builder's half produces the unrestricted quarter-prefactor Hamiltonian.
It does not reconstruct the original single-bar electron-repulsion
tensor from antisymmetrised data. It preserves the summed fermionic
operator under the stated complete-tensor convention.

`None` tells a builder to skip a contribution. Validate the source
before interpreting an absent entry as zero. Nuclear repulsion and
frozen-core constants are not added by these constructors.

### Complete script: `raw-weighted.fsx`

This two-mode example has
$H=-n_0-0.5n_1+0.6n_0n_1$, in arbitrary but consistent energy units.
It is not H₂. The raw entries and their weighted counterparts are all
given, so there is no hidden input file.

```fsharp
#r "nuget: FockMap, 0.9.0"
open System.Numerics
open Encodings
open Encodings.Hamiltonian
open Encodings.JordanWigner

let real x = Complex(x, 0.0)
let raw = Map.ofList [
    "0,0", real -1.0
    "1,1", real -0.5
    "0,1,0,1", real 0.6
    "1,0,1,0", real 0.6
]
let weighted = Map.ofList [
    "0,0", real -1.0
    "1,1", real -0.5
    "0,1,1,0", real 0.3
    "1,0,0,1", real 0.3
]
let rawFactory key = Map.tryFind key raw
let weightedFactory key = Map.tryFind key weighted
let direct = computeHamiltonian rawFactory 2u
let legacy = computeHamiltonianFromWeighted weightedFactory 2u
let migrated = computeHamiltonian (weightedToRawFactory weightedFactory) 2u
let skeleton = computeHamiltonianSkeleton jordanWignerTerms 2u
let dressed = applyCoefficients skeleton rawFactory

let expected = Map.ofList [
    "II", real -0.60; "ZI", real 0.35
    "IZ", real 0.10; "ZZ", real 0.15
]
let coefficients (ham: PauliRegisterSequence) =
    ham.DistributeCoefficient.SummandTerms
    |> Array.map (fun term -> term.Signature, term.Coefficient)
    |> Map.ofArray

for name, ham in [
    "raw", direct; "weighted", legacy
    "migrated", migrated; "skeleton", dressed
] do
    let actual = coefficients ham
    if actual.Count <> expected.Count then
        failwithf "%s: unexpected term inventory" name
    for KeyValue(signature, value) in expected do
        match Map.tryFind signature actual with
        | Some found when Complex.Abs(found - value) < 1e-12 -> ()
        | _ -> failwithf "%s: wrong coefficient for %s" name signature
    printfn "%s: all four coefficients agree" name
```

The four printed success lines certify the stated coefficient table,
not a molecular result. As a useful negative control, replace the
primary builder's `rawFactory` by `weightedFactory`: the expected
table must no longer pass. Keeping both names in a program is a little
verbose; it is cheaper than losing the convention at the boundary.

### Skeletons across geometries

`computeHamiltonianSkeleton encoder n` constructs the full structural
inventory for that width and encoder. `applyCoefficients skeleton raw`
then supplies new raw coefficients. Its weighted counterpart is
`applyCoefficientsFromWeighted skeleton weighted`.

The sparse variant `computeHamiltonianSkeletonFor encoder raw n`
discovers active keys from factory **presence**, not merely from the
magnitude of the first geometry's coefficients. Reuse requires every
later nonzero key to belong to that structural inventory. If a scan
breaks a symmetry or introduces previously omitted entries, build a
union-support skeleton or the full skeleton. Reusing an incomplete
skeleton is not an optimisation of the same Hamiltonian.

The obsolete names `computeHamiltonianFromPhysicist` and
`computeHamiltonianFromPhysicistWith` are identity aliases for the
raw-primary builders. In contrast, the obsolete
`rawPhysicistToWeightedFactory` really performs the half-and-swap
adaptation. Do not put that adapter in front of a raw-primary builder.

## Tapering is a sector operation

Open `Encodings.Tapering`. `diagonalZ2SymmetryQubits ham` returns
qubit indices whose individual Z operators commute with the Hamiltonian;
it is not the general multi-qubit symmetry finder.
`taperDiagonalZ2 sector ham` receives an explicit list of
`(qubitIndex, eigenvalue)` pairs. Each eigenvalue is `+1` or `-1`.
The returned `Z2TaperingResult` contains the reduced Hamiltonian and the
mapping information needed to interpret it.

`findCommutingGenerators ham` gives binary candidates commuting with
the Hamiltonian. Candidates need not mutually commute. The unified
`taper options ham` pipeline selects and transforms a commuting set
according to its options and returns a `TaperingResult`.
Neither a function name nor an all-positive default establishes that
the physical molecular state lies in the selected sector.

Derive signs from the intended state or conserved quantum numbers
before substitution. Compare the reduced matrix to that sector of the
original matrix, not to whichever sector happens to have the smallest
energy. For a complete molecular sign ledger, see *General Clifford
Tapering*. Appendix B distinguishes parity, charge and spin sectors.

## From a Hamiltonian to a gate list

Open `Encodings.Trotterization`.

| Function | Input | Output |
|:---|:---|:---|
| `firstOrderTrotter` | time step, Hermitian Hamiltonian | `TrotterStep` |
| `secondOrderTrotter` | time step, Hermitian Hamiltonian | Symmetric unmerged `TrotterStep` |
| `trotterize First` / `trotterize Second` | time step, Hamiltonian | The corresponding step |
| `decomposeRotation` | `PauliRotation` | `Gate[]` |
| `decomposeTrotterStep` | `TrotterStep` | Concatenated `Gate[]` |
| `trotterCnotCount` | `TrotterStep` | Logical staircase CNOT count |
| `trotterStepStats` | `TrotterStep` | Rotation, logical gate and weight statistics |

A `PauliRotation` has fields `Operator` and `Angle`.
Its convention is $\exp(-i\theta P)$, not $R_z(\theta)$.
`Gate.Rz(q, angle)` instead means
$\exp(-i\,\mathrm{angle}\,Z_q/2)$. Decomposition therefore emits
an Rz angle of $2\theta$.

The builders reject materially imaginary Pauli coefficients
(`abs Im > 1e-9` in this pin). Callers must still establish finite inputs
and a scientifically appropriate Hermiticity tolerance.
`secondOrderTrotter` retains a forward and reverse half-step array;
its rotation count is not a claim that adjacent equal rotations have
been merged.

Identity factors occur in the symbolic rotations but emit no gates.
Consequently `RotationCount` need not equal the number of emitted Rz
gates. Omitting an identity phase is permissible for an ordinary
uncontrolled evolution up to global phase; it is not automatically
permissible when that evolution is controlled for phase estimation.

### Complete script: `one-rotation-export.fsx`

This complete example exports one nonidentity term
$H=0.25X_0Z_1$ for a step of $0.2$, so
$\theta=0.05$ and the Rz angle is $0.1$. Run it in an empty
working directory if you want to keep its generated files separate.

```fsharp
#r "nuget: FockMap, 0.9.0"
open System
open System.Globalization
open System.IO
open System.Numerics
open Encodings
open Encodings.Trotterization
open Encodings.CircuitOutput

CultureInfo.CurrentCulture <- CultureInfo.InvariantCulture
let hamiltonian =
    PauliRegisterSequence [| PauliRegister("XZ", Complex(0.25, 0.0)) |]
let step = firstOrderTrotter 0.2 hamiltonian
let gates = decomposeTrotterStep step
let expected = [|
    Gate.H 0; Gate.CNOT(0, 1); Gate.Rz(1, 0.1)
    Gate.CNOT(0, 1); Gate.H 0
|]
if gates <> expected then failwithf "Unexpected gate sequence: %A" gates
let stats = trotterStepStats step
if stats.CnotCount <> 2 || stats.TotalGates <> 5 then
    failwith "Unexpected logical gate count"

let qasm3 = { defaultOpenQasmOptions with Precision = 12 }
let qasm2 = { defaultOpenQasm2Options with Precision = 12 }
let qsharp = {
    defaultQSharpOptions with
        OperationName = "OneRotation"
        Precision = 12
}
File.WriteAllText("one-rotation.qasm", toOpenQasm qasm3 2 gates)
File.WriteAllText("one-rotation-v2.qasm", toOpenQasm qasm2 2 gates)
File.WriteAllText("one-rotation.qs", toQSharp qsharp 2 gates)
File.WriteAllText("one-rotation.json",
    toCircuitJson 2 (Map.ofList [("phasePolicy", "no identity term")]) gates)
printfn "One rotation: %d CNOTs, %d total logical gates" stats.CnotCount stats.TotalGates
```

The invariant culture is deliberate: this pin formats textual angles
with the current culture. A locale decimal comma must not become part
of a QASM or Q# numeric literal. Precision is a formatting choice,
not an error bound for the Hamiltonian simulation.

## Export contracts and ownership

Open `Encodings.CircuitOutput`. The low-level renderers accept an
explicit width, which preserves idle qubits:

| Function | Arguments | Result |
|:---|:---|:---|
| `toOpenQasm` | options, `int` width, `Gate[]` | QASM 2 or 3 text |
| `toQSharp` | options, `int` width, `Gate[]` | Q# operation text |
| `toCircuitJson` | width, `Map<string,string>` metadata, gates | FockMap JSON text |

The QASM defaults choose version 3; `defaultOpenQasm2Options` chooses
version 2. The generated Q# operation takes a caller-supplied `Qubit[]`;
it does not allocate, reset or release that array. Its width argument
does not enforce the caller's array length. The caller must supply
enough qubits and respect their state and lifetime.

The convenience `trotterStepToOpenQasm` helper infers width from the
highest referenced emitted gate. It can lose trailing idle qubits,
and an identity-only step has no emitted gates from which to infer
the intended width. Prefer the explicit-width functions at a format
boundary.

FockMap JSON records `numQubits`, `gateCount`, `metadata` and `gates`;
it is a library schema, not an industry interchange standard.
Rendering text establishes neither successful import by a particular
tool version nor circuit equivalence. *Speaking the Hardware's
Language* treats import, ordered-unitary comparison, phase policy
and energy estimation as separate obligations.

## Where the complete molecular inputs live

The repository's Chapter 3 companion loads the canonical H₂ fixture.
The Chapter 6 and 7 companions construct Hamiltonians; Chapter 9's
Python program supplies an independent read-only reference check.
The Chapter 18 companion constructs the untapered logical circuit and
separates its derived output from canonical data. Its data scan is not
the water FCI program.

Use the exact commands in the repository README and the entry-point
headers. Do not transplant a molecular number into one of this
appendix's deliberately hypothetical examples and call the result
a new geometry calculation.
