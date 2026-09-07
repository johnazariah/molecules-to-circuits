(**
# Encoding the H₂ Molecule

This tutorial walks through encoding the electronic Hamiltonian of molecular
hydrogen (H₂) using the Jordan-Wigner transformation. This is the canonical
example for quantum chemistry on quantum computers.

## Physical Setup

We use the STO-3G minimal basis set, which gives:
- **2 spatial orbitals**: σg (bonding) and σu (antibonding)
- **4 spin-orbitals**: σg↑, σg↓, σu↑, σu↓
- **4 qubits** after encoding

The electronic Hamiltonian in second quantization is:

$$H = \sum_{pq} h_{pq} \, a^\dagger_p a_q + \frac{1}{2} \sum_{pqrs} \langle pq | rs \rangle \, a^\dagger_p a^\dagger_q a_s a_r$$

where:
- $h_{pq}$ are one-body (kinetic + nuclear) integrals
- $\langle pq | rs \rangle$ are two-body (electron-electron) integrals

## Setup
*)

#load "../code/ch03-spin-orbitals.fsx"

open System.Numerics
open Encodings
open Encodings.Hamiltonian
open Encodings.JordanWigner

(**
## Molecular Integrals

These are loaded from the generated `code/h2_dissociation_integrals.json`
record for H₂/STO-3G at R = 0.74 Å. The lab does not maintain a second literal
copy.

### Spin-Orbital Indexing

| Index | Spin-Orbital |
|-------|--------------|
| 0     | σg↑          |
| 1     | σg↓          |
| 2     | σu↑          |
| 3     | σu↓          |

### One-Body Integrals h_{pq}

These include kinetic energy and electron-nuclear attraction.
In a spin-orbital basis with real spatial orbitals, only diagonal-in-spin
terms are nonzero:
*)

let nSpinOrbitals = 4u

let canonicalIntegrals = ``Ch03-spin-orbitals``.h2RawPhysicistIntegrals
let oneBodyIntegrals =
    canonicalIntegrals
    |> Map.filter (fun key _ -> key.Split(',').Length = 2)

(**
### Two-Body Integrals ⟨pq|rs⟩

These are the electron-electron repulsion integrals in physicist's notation.
Symmetries reduce the number of unique values significantly.
*)

let twoBodyIntegrals =
    canonicalIntegrals
    |> Map.filter (fun key _ -> key.Split(',').Length = 4)

(**
## Coefficient Lookup Function

The raw-physicist Hamiltonian function needs a way to look up integrals
for arbitrary index combinations. We provide the canonical raw factory:
*)

let rawPhysicistFactory = ``Ch03-spin-orbitals``.h2RawPhysicistFactory

(**
## Computing the Hamiltonian

Now we call the raw-primary `computeHamiltonian` with Jordan-Wigner encoding.
This function:
1. Iterates over all one-body index pairs (p,q)
2. Iterates over all two-body index quadruples (p,q,r,s)
3. Looks up coefficients using our factory
4. Encodes each term using Jordan-Wigner
5. Combines and simplifies the result
*)

printfn "Computing H₂ Hamiltonian with Jordan-Wigner encoding...\n"

let hamiltonian =
    computeHamiltonian rawPhysicistFactory nSpinOrbitals
let terms =
    hamiltonian.DistributeCoefficient.SummandTerms
    |> Array.filter (fun term -> Complex.Abs term.Coefficient > 1e-12)

let requireCoefficient signature expected =
    let actual =
        terms
        |> Array.tryFind (fun term -> term.Signature = signature)
        |> Option.map (fun term -> term.Coefficient.Real)
        |> Option.defaultWith (fun () -> failwithf "Missing Pauli term %s" signature)

    if abs (actual - expected) > 1e-9 then
        failwithf
            "FockMap Hamiltonian convention mismatch for %s: expected %.10f, got %.10f. Run make verify-data before trusting this lab."
            signature
            expected
            actual

if terms.Length <> 15 then
    failwithf "Expected 15 nonzero JW terms, got %d" terms.Length

requireCoefficient "IIII" -0.8121706072
requireCoefficient "XXYY" -0.0453026155

(**
## Examining the Results

Let's display the qubit Hamiltonian:
*)

printfn "════════════════════════════════════════════════════════════"
printfn " H₂ Qubit Hamiltonian (Jordan-Wigner Encoding)"
printfn "════════════════════════════════════════════════════════════\n"
printfn " Number of Pauli terms: %d\n" terms.Length

printfn " Pauli terms (coefficient × Pauli string):"
printfn " ───────────────────────────────────────────"

for term in terms do
    let c = term.Coefficient
    let sign = if c.Real >= 0.0 then "+" else ""
    printfn "   %s%.6f  %s" sign c.Real term.Signature

(**
## Understanding the Output

The H₂ Hamiltonian typically produces 15 unique Pauli terms:

| Pattern | Physical Origin |
|---------|-----------------|
| `IIII`  | Electronic energy offset |
| `ZZII`, `IZZI`, etc. | Diagonal one- and two-body contributions |
| `XXYY`, `XYYX`, `YXXY`, `YYXX` | Double-excitation configuration coupling |

## Why This Matters

The qubit Hamiltonian H can be measured on a quantum computer:
1. Each Pauli string is a tensor product of single-qubit observables
2. Measure each term separately (or use grouping strategies)
3. Combine measurements weighted by coefficients

This is the foundation for the Variational Quantum Eigensolver (VQE)
algorithm, which finds the ground state energy of molecules.

## Next Steps

Try modifying this tutorial to:
- Run `dotnet fsi code/ch06-h2-at-bond.fsx 1.40` to inspect a different
  committed scan geometry without editing the equilibrium fixture or loader
- Use `computeHamiltonianWith` with different encodings
- Compare the number of terms across encodings
*)

printfn "\n════════════════════════════════════════════════════════════"
printfn " Tutorial complete! H₂ encoded into %d Pauli terms." terms.Length
printfn "════════════════════════════════════════════════════════════"
