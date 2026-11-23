namespace Encodings

/// Bravyi-Kitaev encoding for fermion-to-qubit mappings.
///
/// The BK transform uses a Fenwick tree structure to achieve O(log n) Pauli
/// weight per ladder operator, compared to O(n) for the Jordan-Wigner transform.
///
/// This module delegates to MajoranaEncoding with Fenwick-tree index sets.
///
/// Reference:
///   Seeley, Richard, Love — "The Bravyi-Kitaev transformation for quantum
///   computation of electronic structure" (arXiv:1208.5986)
module BravyiKitaev =
    open Encodings.MajoranaEncoding

    /// Compute the Bravyi-Kitaev encoding of a single ladder operator
    /// at mode index j within a register of n qubits.
    let bravyiKitaevTerms (op : LadderOperatorUnit) (j : uint32) (n : uint32) =
        encodeOperator bravyiKitaevScheme op j n
