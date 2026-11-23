namespace Encodings

/// A purely functional Fenwick tree (binary indexed tree) parameterized by
/// an associative combining operation.
///
/// The tree stores partial aggregates over an array of n values, supporting:
///   - point update   : O(log n)
///   - prefix query   : O(log n)
///   - range query    : via prefix difference when the combine has an inverse
///
/// The index sets used by the Bravyi-Kitaev encoding (update, parity,
/// occupation, remainder) all fall out naturally from the Fenwick tree
/// structure, making this a good foundation for the BK transform.
module FenwickTree =

    /// A Fenwick tree over values of type 'a.
    ///
    /// 'combine' must be associative:  combine (combine a b) c = combine a (combine b c)
    /// 'identity' must be a left/right identity for combine.
    type FenwickTree<'a> =
        { /// The underlying storage (1-indexed internally; slot 0 unused).
          Data     : 'a array
          /// Associative combining operation (e.g. (+), (^^^), Set.union).
          Combine  : 'a -> 'a -> 'a
          /// Identity element for the combine operation.
          Identity : 'a }

    // ─────────────────────────────────────────────
    //  Bit-twiddling helpers
    // ─────────────────────────────────────────────

    /// Lowest set bit of k (e.g. lsb 6 = 2, lsb 8 = 8).
    let inline lsb k = k &&& -k

    /// Walk from k toward the root by adding the lowest set bit.
    /// Returns the sequence of 1-based ancestor indices (excluding k itself).
    let ancestors n k =
        k + lsb k
        |> Seq.unfold (fun i ->
            if i > n then None
            else Some (i, i + lsb i))

    /// Walk from k toward the leaves by clearing the lowest set bit.
    /// Returns the sequence of 1-based descendant indices (excluding k itself),
    /// stopping at the "left wall" of k's subtree.
    let descendants k =
        let wall = k - lsb k          // parent with k's bit cleared
        k - 1
        |> Seq.unfold (fun i ->
            if i <= wall then None
            else Some (i, i &&& (i - 1)))

    /// Walk by clearing the lowest set bit starting from k itself.
    /// Returns the 1-based prefix-contributing indices.
    let prefixIndices k =
        k
        |> Seq.unfold (fun i ->
            if i <= 0 then None
            else Some (i, i &&& (i - 1)))

    // ─────────────────────────────────────────────
    //  Construction
    // ─────────────────────────────────────────────

    /// Build a Fenwick tree of size n from a point-value function f,
    /// where f returns the raw value at 0-based index i.
    let build (combine : 'a -> 'a -> 'a) (identity : 'a) (n : int) (f : int -> 'a) : FenwickTree<'a> =
        // First fill raw values (1-indexed), then propagate to parents.
        let data = Array.init (n + 1) (fun i -> if i = 0 then identity else f (i - 1))
        for i in 1 .. n do
            let p = i + lsb i
            if p <= n then
                data.[p] <- combine data.[p] data.[i]
        { Data = data; Combine = combine; Identity = identity }

    /// Build a Fenwick tree from an array of values.
    let ofArray (combine : 'a -> 'a -> 'a) (identity : 'a) (values : 'a array) : FenwickTree<'a> =
        build combine identity values.Length (fun i -> values.[i])

    /// Create an empty (all-identity) Fenwick tree of size n.
    let empty (combine : 'a -> 'a -> 'a) (identity : 'a) (n : int) : FenwickTree<'a> =
        build combine identity n (fun _ -> identity)

    /// The number of elements in the tree.
    let size (tree : FenwickTree<'a>) = tree.Data.Length - 1

    // ─────────────────────────────────────────────
    //  Queries
    // ─────────────────────────────────────────────

    /// Compute the prefix aggregate of elements 0 .. j (0-based, inclusive).
    let prefixQuery (tree : FenwickTree<'a>) (j : int) =
        prefixIndices (j + 1)
        |> Seq.fold (fun acc i -> tree.Combine acc tree.Data.[i]) tree.Identity

    /// Retrieve the single-element value at 0-based index j.
    /// Requires that 'combine' has a notion of "undo" or that
    /// we reconstruct from the tree structure.
    ///
    /// For a XOR / symmetric-difference tree this is exact.
    /// For a general tree, this walks the Fenwick descendants.
    let pointQuery (tree : FenwickTree<'a>) (j : int) =
        let k = j + 1
        descendants k
        |> Seq.fold (fun acc i -> tree.Combine acc tree.Data.[i]) tree.Data.[k]

    // ─────────────────────────────────────────────
    //  Update
    // ─────────────────────────────────────────────

    /// Return a new Fenwick tree with the value at 0-based index j
    /// combined with delta:  tree[j] <- combine tree[j] delta
    let update (tree : FenwickTree<'a>) (j : int) (delta : 'a) =
        let n    = size tree
        let data = Array.copy tree.Data
        let k    = j + 1
        data.[k] <- tree.Combine data.[k] delta
        for ancestor in ancestors n k do
            data.[ancestor] <- tree.Combine data.[ancestor] delta
        { tree with Data = data }

    // ─────────────────────────────────────────────
    //  Index-set extraction (for BK encoding)
    // ─────────────────────────────────────────────

    /// Update set U(j): the set of ancestor indices that store partial
    /// aggregates including element j.  These are the nodes that need
    /// updating when element j changes.
    /// Returns 0-based index set, excluding j itself.
    let updateSet (j : int) (n : int) : Set<int> =
        ancestors n (j + 1)
        |> Seq.map (fun i -> i - 1)
        |> Set.ofSeq

    /// Parity set P(j): the set of tree indices whose stored values
    /// contribute to the prefix aggregate of elements 0 .. (j-1).
    /// Returns 0-based indices.
    let paritySet (j : int) : Set<int> =
        prefixIndices j
        |> Seq.map (fun i -> i - 1)
        |> Set.ofSeq

    /// Occupation set Occ(j): the set of indices in j's subtree that
    /// together determine element j's value.
    /// { j } ∪ { descendants of j+1 in 1-based Fenwick }
    /// Returns 0-based indices.
    let occupationSet (j : int) : Set<int> =
        let k = j + 1
        descendants k
        |> Seq.map (fun i -> i - 1)
        |> Set.ofSeq
        |> Set.add j

    /// Remainder set R(j) = P(j) \ Occ(j).
    let remainderSet (j : int) : Set<int> =
        Set.difference (paritySet j) (occupationSet j)

    /// Symmetric difference of two sets.
    let symmetricDifference (a : Set<'b>) (b : Set<'b>) : Set<'b> =
        (a - b) + (b - a)
