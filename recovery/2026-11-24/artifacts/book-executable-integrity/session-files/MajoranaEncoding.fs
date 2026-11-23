namespace Encodings

/// Generic fermion-to-qubit encoding via Majorana decomposition.
///
/// Every encoding in this family maps a single fermionic ladder operator
/// to a pair of Majorana operators (c, d) built from three index sets:
///
///   U(j, n)  — update set:     qubits that flip when occupation of mode j changes
///   P(j)     — parity set:     qubits encoding the parity n₀ ⊕ … ⊕ n_{j−1}
///   Occ(j)   — occupation set: qubits encoding whether mode j is occupied
///
/// The Majorana operators are:
///   c_j = X_{U(j)∪{j}} · Z_{P(j)}
///   d_j = Y_j · X_{U(j)} · Z_{(P(j)⊕Occ(j))∖{j}}
///
/// And the ladder operators follow:
///   a†_j = ½(c_j − i·d_j)     a_j = ½(c_j + i·d_j)
///
/// Different choices of index-set functions yield different encodings:
///   Jordan-Wigner :  U = ∅,              P = {0…j−1},     Occ = {j}
///   Parity        :  U = {j+1…n−1},      P = {j−1}?,      Occ = {j−1,j}?
///   Bravyi-Kitaev :  U, P, Occ from Fenwick tree structure
module MajoranaEncoding =
    open System.Numerics
    open Encodings.FenwickTree

    /// Defines the three index-set functions that characterise an encoding.
    type EncodingScheme =
        { /// Update set: qubits (besides j) that need an X flip when n_j changes.
          Update     : int -> int -> Set<int>
          /// Parity set: qubits whose parity encodes n₀ ⊕ … ⊕ n_{j−1}.
          Parity     : int -> Set<int>
          /// Occupation set: qubits whose parity encodes n_j.
          Occupation : int -> Set<int> }

    // ─────────────────────────────────────────────
    //  Pauli-string construction
    // ─────────────────────────────────────────────

    /// Build a PauliRegister of width n from a sparse assignment list.
    /// Qubits not mentioned default to I.
    let pauliOfAssignments (n : int) (assignments : (int * Pauli) list) (coeff : Complex) =
        List.init n (fun i ->
            assignments
            |> List.tryFind (fun (k, _) -> k = i)
            |> Option.map snd
            |> Option.defaultValue Pauli.I)
        |> fun ops -> PauliRegister (ops, coeff)

    // ─────────────────────────────────────────────
    //  Majorana operators from index sets
    // ─────────────────────────────────────────────

    let private setAssign (pauli : Pauli) (indices : Set<int>) =
        indices |> Set.toList |> List.map (fun k -> (k, pauli))

    /// c Majorana:  X on {j} ∪ U(j),  Z on P(j).
    let cMajorana (scheme : EncodingScheme) (j : int) (n : int) =
        let u = scheme.Update j n
        let p = scheme.Parity j
        (j, Pauli.X) :: setAssign Pauli.X u @ setAssign Pauli.Z p

    /// d Majorana:  Y on j,  X on U(j),  Z on (P(j) ⊕ Occ(j)) \ {j}.
    let dMajorana (scheme : EncodingScheme) (j : int) (n : int) =
        let u   = scheme.Update j n
        let p   = scheme.Parity j
        let occ = scheme.Occupation j
        let dZ  = symmetricDifference p occ |> Set.remove j
        (j, Pauli.Y) :: setAssign Pauli.X u @ setAssign Pauli.Z dZ

    // ─────────────────────────────────────────────
    //  Generic encoding entry point
    // ─────────────────────────────────────────────

    /// Encode a single ladder operator using the given scheme.
    ///
    ///   a†_j = ½ c_j − ½i d_j
    ///   a_j  = ½ c_j + ½i d_j
    let encodeOperator (scheme : EncodingScheme) (op : LadderOperatorUnit) (j : uint32) (n : uint32) : PauliRegisterSequence =
        match op with
        | Identity -> PauliRegisterSequence ()
        | _ when j >= n -> PauliRegisterSequence ()
        | _ ->
            let ji = int j
            let ni = int n
            let cReg = pauliOfAssignments ni (cMajorana scheme ji ni) (Complex (0.5, 0.0))
            let dCoeff = match op with Raise -> Complex (0.0, -0.5) | _ -> Complex (0.0, 0.5)
            let dReg = pauliOfAssignments ni (dMajorana scheme ji ni) dCoeff
            PauliRegisterSequence [| cReg; dReg |]

    // ═════════════════════════════════════════════
    //  Concrete encoding schemes
    // ═════════════════════════════════════════════

    /// Jordan-Wigner:  U = ∅,  P = {0 … j−1},  Occ = {j}.
    let jordanWignerScheme : EncodingScheme =
        { Update     = fun _ _ -> Set.empty
          Parity     = fun j   -> set [ 0 .. j - 1 ]
          Occupation = fun j   -> Set.singleton j }

    /// Bravyi-Kitaev:  index sets from the Fenwick tree.
    let bravyiKitaevScheme : EncodingScheme =
        { Update     = updateSet
          Parity     = paritySet
          Occupation = occupationSet }

    /// Parity:  U = {j+1 … n−1},  P = {j−1} if j>0 else ∅,  Occ = {j−1, j} if j>0 else {j}.
    let parityScheme : EncodingScheme =
        { Update     = fun j n -> set [ j + 1 .. n - 1 ]
          Parity     = fun j   -> if j > 0 then Set.singleton (j - 1) else Set.empty
          Occupation = fun j   -> if j > 0 then set [ j - 1; j ] else Set.singleton j }

    // ─────────────────────────────────────────────
    //  Convenience wrappers
    // ─────────────────────────────────────────────

    /// Encode a ladder operator using the Parity encoding.
    let parityTerms (op : LadderOperatorUnit) (j : uint32) (n : uint32) =
        encodeOperator parityScheme op j n
