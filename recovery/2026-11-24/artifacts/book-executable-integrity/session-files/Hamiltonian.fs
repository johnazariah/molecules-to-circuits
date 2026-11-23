namespace Encodings

/// <summary>
/// Hamiltonian construction from one-body and two-body integrals.
/// </summary>
/// <remarks>
/// <para>
/// Assembles the second-quantized electronic Hamiltonian:
/// </para>
///
///   H = Σ_{pq} h_{pq} a†_p a_q  +  ½ Σ_{pqrs} ⟨pq|rs⟩ a†_p a†_q a_s a_r
///
/// <para>
/// and encodes it as a sum of Pauli strings using any provided encoding function.
/// The builders handle one-body terms, two-body terms, coefficient combination,
/// and zero-term dropping.
/// </para>
/// <para>
/// <b>BREAKING CHANGE (0.9.0).</b> The primary builders — <c>computeHamiltonian</c>,
/// <c>computeHamiltonianWith</c> and the parallel / cached / skeleton variants — now
/// consume <b>RAW single-bar physicist integrals</b> ⟨pq|rs⟩ keyed <c>"p,q,r,s"</c>
/// (no ½, no index swap). Internally each two-body key contributes
/// <c>½·⟨pq|rs⟩·a†_p a†_q a_s a_r</c> (the ½ of the two-body term and the r↔s
/// annihilator order are applied by the library). The <see cref="T:Encodings.Fcidump"/>
/// adapters likewise return raw physicist integrals, so FCIDUMP physics is unchanged.
/// </para>
/// <para>
/// The <b>previously released weighted</b> semantics (value = the full weighted
/// prefactor of <c>a†_i a†_j a_k a_l</c>, applied verbatim, two-body ½ pre-folded)
/// remain available behind the clearly named <c>computeHamiltonianFromWeighted…</c>
/// migration functions and the <c>weightedToRawFactory</c> adapter. See the migration
/// guide (<c>docs/guides/migration-0.9.md</c>) for the exact before/after mapping.
/// </para>
/// </remarks>
module Hamiltonian =
    open System
    open System.Numerics
    open Encodings.JordanWigner

    /// <summary>Mode indices 0 .. n-1, or an empty sequence when n = 0.</summary>
    /// <remarks>Guards against uint32 underflow of <c>n - 1u</c> when n = 0.</remarks>
    let private modeRange (n : uint32) : seq<uint32> =
        if n = 0u then Seq.empty else seq { 0u .. n - 1u }

    /// <summary>
    /// A function type that encodes a ladder operator into qubit Pauli strings.
    /// </summary>
    /// <remarks>
    /// Takes a ladder operator, the mode index, and the total number of qubits,
    /// returning the Pauli string representation. Different encodings (Jordan-Wigner,
    /// Bravyi-Kitaev, etc.) implement this signature with different Z-chain structures.
    /// </remarks>
    type EncoderFn = LadderOperatorUnit -> uint32 -> uint32 -> PauliRegisterSequence

    /// <summary>
    /// Structural Pauli terms for the two-body operator <c>a†_i a†_j a_k a_l</c>.
    /// </summary>
    /// <remarks>
    /// This helper contributes only the structural Pauli phases from the encoding
    /// algebra for <c>a†_i a†_j a_k a_l</c> (annihilators in the order <c>a_k</c> then
    /// <c>a_l</c>); the weighted core multiplies in the caller's coefficient. It is
    /// shared by every builder (sequential, parallel, cached, and both skeletons), so
    /// all surfaces agree. The raw-physicist primary builders reach it through the
    /// internal raw→weighted mapping (the ½ and the r↔s swap live there, not here).
    /// </remarks>
    let private twoBodyStructuralTerms (encode : EncoderFn) (i : uint32) (j : uint32) (k : uint32) (l : uint32) (n : uint32) : PauliRegister[] =
        let product =
            (encode Raise i n) * (encode Raise j n)
            * (encode Lower k n) * (encode Lower l n)
        product.DistributeCoefficient.SummandTerms

    /// <summary>Machine epsilon for IEEE-754 double precision (2^-52).</summary>
    let private machineEpsilon = 2.220446049250313e-16

    /// <summary>
    /// Cancellation-aware aggregation of assembled Pauli contributions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Groups contributions by Pauli signature and sums their coefficients, while
    /// tracking, per signature, the number of contributions and a magnitude
    /// <em>scale</em> (the sum of contribution magnitudes). A summed term is dropped
    /// only when it is an exact zero, or when it is a residual demonstrably produced
    /// by the cancellation of more than one contribution — i.e. the count is greater
    /// than one and the residual magnitude is at most a small multiple of machine
    /// epsilon times the scale.
    /// </para>
    /// <para>
    /// This is deliberately <b>not</b> an absolute threshold: a standalone term
    /// (count = 1) is never magnitude-pruned, so legitimate tiny coefficients
    /// (e.g. 1e-12, 1e-13, 1e-15) always survive. The core Pauli algebra remains
    /// exact; this reduction is applied only at Hamiltonian-assembly boundaries.
    /// </para>
    /// </remarks>
    let private reduceWithCancellation (contributions : PauliRegister[]) : PauliRegisterSequence =
        // K·eps·scale tolerance; K allows for accumulation over many contributions.
        let cancellationFactor = 32.0
        let reps   = System.Collections.Generic.Dictionary<string, PauliRegister>()
        let sums   = System.Collections.Generic.Dictionary<string, Complex>()
        let counts = System.Collections.Generic.Dictionary<string, int>()
        let scales = System.Collections.Generic.Dictionary<string, float>()
        for r in contributions do
            let key = r.Signature
            match sums.TryGetValue key with
            | true, existing ->
                sums.[key]   <- existing + r.Coefficient
                counts.[key] <- counts.[key] + 1
                scales.[key] <- scales.[key] + r.Coefficient.Magnitude
            | false, _ ->
                reps.[key]   <- r
                sums.[key]   <- r.Coefficient
                counts.[key] <- 1
                scales.[key] <- r.Coefficient.Magnitude
        [| for kvp in sums do
             let key   = kvp.Key
             let sum   = kvp.Value
             let count = counts.[key]
             let scale = scales.[key]
             let isCancellationResidue =
                 count > 1 && sum.Magnitude <= cancellationFactor * machineEpsilon * scale
             if (not sum.IsZero) && (not isCancellationResidue) then
                 yield reps.[key].ResetPhase sum |]
        |> PauliRegisterSequence

    /// <summary>
    /// Discriminated union representing a term in the Hamiltonian.
    /// </summary>
    type HamiltonianTerm =
    /// <summary>A one-body (overlap) term h_{ij} a†_i a_j.</summary>
    | Overlap  of OverlapTerm
    /// <summary>A two-body (exchange) term h_{ijkl} a†_i a†_j a_k a_l.</summary>
    | Exchange of ExchangeTerm

    /// <summary>
    /// A one-body overlap term with indices i and j, representing h_{ij} a†_i a_j.
    /// </summary>
    and OverlapTerm  = {i : uint32; j : uint32}
    with
        member private this.EncodedContributions (encode : EncoderFn) n coeff : PauliRegister[] =
            let product = (encode Raise this.i n) * (encode Lower this.j n)
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        /// Flat Pauli contributions for every one-body key the factory populates.
        static member internal ContributionsWith (encode : EncoderFn) coefficientFactory n : PauliRegister[] =
            [|
                for i in modeRange n do
                    for j in modeRange n do
                        let key = sprintf "%u,%u" i j
                        match coefficientFactory key with
                        | Some hij ->
                            let term = {OverlapTerm.i = i; OverlapTerm.j = j}
                            yield! term.EncodedContributions encode n hij
                        | _ -> ()
            |]

        static member internal Contributions coefficientFactory n =
            OverlapTerm.ContributionsWith jordanWignerTerms coefficientFactory n

    /// <summary>
    /// A two-body exchange term with indices i, j, k, l representing the operator
    /// <c>a†_i a†_j a_k a_l</c> (annihilators in the order k then l).
    /// </summary>
    /// <remarks>
    /// The coefficient supplied to this internal helper for key <c>"i,j,k,l"</c> is the
    /// FULL WEIGHTED prefactor of this operator string and is applied verbatim — the
    /// weighted core folds in the ½ of the two-body Hamiltonian term
    /// ½·Σ g_pqrs a†_p a†_q a_r a_s; no additional ½ is applied here. The raw-physicist
    /// primary builders reach this helper via the internal raw→weighted mapping (½ and
    /// r↔s swap), so a raw <c>⟨pq|rs⟩</c> factory is supplied to
    /// <c>computeHamiltonianWith</c> directly. The <see cref="T:Encodings.Fcidump"/>
    /// adapters produce a conforming raw factory.
    /// </remarks>
    and ExchangeTerm = {i : uint32; j : uint32; k : uint32; l : uint32}
    with
        member private this.EncodedContributions (encode : EncoderFn) n coeff : PauliRegister[] =
            twoBodyStructuralTerms encode this.i this.j this.k this.l n
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        /// Flat Pauli contributions for every two-body key the factory populates.
        static member internal ContributionsWith (encode : EncoderFn) coefficientFactory n : PauliRegister[] =
            [|
                for i in modeRange n do
                    for j in modeRange n do
                        for k in modeRange n do
                            for l in modeRange n do
                                let key = sprintf "%u,%u,%u,%u" i j k l
                                match coefficientFactory key with
                                | Some hijkl ->
                                    let term = {
                                        ExchangeTerm.i = i
                                        ExchangeTerm.j = j
                                        ExchangeTerm.k = k
                                        ExchangeTerm.l = l
                                    }
                                    yield! term.EncodedContributions encode n hijkl
                                | _ -> ()
            |]

        static member internal Contributions coefficientFactory n =
            ExchangeTerm.ContributionsWith jordanWignerTerms coefficientFactory n


    /// <summary>
    /// <b>[Legacy / migration]</b> Compute a qubit Hamiltonian from <b>weighted</b>
    /// integral coefficients using any encoding.
    /// </summary>
    /// <param name="encode">The encoding function to transform ladder operators to Pauli strings.</param>
    /// <param name="coefficientFactory">A function that returns Some(coefficient) for a given comma-separated index key (e.g., "0,1" for one-body, "0,1,2,3" for two-body), or None if the term should be skipped.</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// <para>
    /// This preserves the <b>previously released (≤ 0.8.0) weighted</b> contract: the
    /// factory returns the FULL WEIGHTED prefactor of the operator string, applied
    /// verbatim — "i,j" → coefficient of <c>a†_i a_j</c>; "i,j,k,l" → coefficient of
    /// <c>a†_i a†_j a_k a_l</c> <b>with the two-body ½ already folded in</b>. No ½ and
    /// no index swap are applied by the library.
    /// </para>
    /// <para>
    /// New code should prefer the raw-physicist <c>computeHamiltonianWith</c> (0.9.0+).
    /// To reuse an existing weighted factory with the raw builders, wrap it once with
    /// <c>weightedToRawFactory</c>. Numerically-zero residues from fermionic
    /// cancellation are removed (cancellation-aware); standalone tiny coefficients are
    /// preserved.
    /// </para>
    /// </remarks>
    let computeHamiltonianFromWeightedWith (encode : EncoderFn) coefficientFactory n =
        Array.append
            (OverlapTerm.ContributionsWith  encode coefficientFactory n)
            (ExchangeTerm.ContributionsWith encode coefficientFactory n)
        |> reduceWithCancellation

    /// <summary>
    /// <b>[Legacy / migration]</b> Compute a qubit Hamiltonian from <b>weighted</b>
    /// integral coefficients using Jordan-Wigner encoding.
    /// </summary>
    /// <param name="coefficientFactory">A function that returns Some(coefficient) for a given comma-separated index key (e.g., "0,1" for one-body, "0,1,2,3" for two-body), or None if the term should be skipped.</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// <para>
    /// Jordan-Wigner specialisation of <c>computeHamiltonianFromWeightedWith</c>. The
    /// factory returns the FULL WEIGHTED prefactor of the corresponding operator
    /// string, applied verbatim:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"i,j" → h_ij, the coefficient of <c>a†_i a_j</c>.</description></item>
    ///   <item><description>"i,j,k,l" → the coefficient of <c>a†_i a†_j a_k a_l</c>,
    ///   <b>with the ½ of the two-body term ½·Σ g_pqrs a†_p a†_q a_r a_s already folded in</b>.
    ///   No additional ½ or index swap is applied.</description></item>
    /// </list>
    /// <para>
    /// The nuclear/constant term is not added (callers may add E_nuc·I separately).
    /// New code should prefer the raw-physicist <c>computeHamiltonian</c> (0.9.0+).
    /// </para>
    /// </remarks>
    let computeHamiltonianFromWeighted coefficientFactory n =
        computeHamiltonianFromWeightedWith jordanWignerTerms coefficientFactory n


    // ── Parallel Hamiltonian construction ─────────────────────────────

    /// <summary>
    /// <b>[Legacy / migration]</b> Parallel version of
    /// <c>computeHamiltonianFromWeightedWith</c> (weighted contract).
    /// Distributes encoding work across available CPU cores using Array.Parallel.
    /// </summary>
    /// <param name="encode">The encoding function to transform ladder operators to Pauli strings.</param>
    /// <param name="coefficientFactory">A weighted coefficient lookup (see <c>computeHamiltonianFromWeightedWith</c>).</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// The n² one-body and n⁴ two-body index loops are parallelised.
    /// Coefficient lookups remain sequential (cheap), while the expensive
    /// encode-and-multiply steps run across all cores. Produces results
    /// identical to the sequential <c>computeHamiltonianFromWeightedWith</c>.
    /// </remarks>
    let computeHamiltonianFromWeightedWithParallel (encode : EncoderFn) coefficientFactory n =
        let encodeOneBody (i, j, coeff) : PauliRegister[] =
            let product = (encode Raise i n) * (encode Lower j n)
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        let encodeTwoBody (i, j, k, l, coeff) : PauliRegister[] =
            twoBodyStructuralTerms encode i j k l n
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        let oneBodyTerms =
            [| for i in modeRange n do
                   for j in modeRange n do
                       let key = sprintf "%u,%u" i j
                       match coefficientFactory key with
                       | Some hij -> yield (i, j, hij)
                       | _ -> () |]
            |> Array.Parallel.map encodeOneBody

        let twoBodyTerms =
            [| for i in modeRange n do
                   for j in modeRange n do
                       for k in modeRange n do
                           for l in modeRange n do
                               let key = sprintf "%u,%u,%u,%u" i j k l
                               match coefficientFactory key with
                               | Some hijkl -> yield (i, j, k, l, hijkl)
                               | _ -> () |]
            |> Array.Parallel.map encodeTwoBody

        Array.append (Array.concat oneBodyTerms) (Array.concat twoBodyTerms)
        |> reduceWithCancellation

    /// <summary>
    /// <b>[Legacy / migration]</b> Parallel version of
    /// <c>computeHamiltonianFromWeighted</c> (Jordan-Wigner, weighted contract).
    /// </summary>
    let computeHamiltonianFromWeightedParallel coefficientFactory n =
        computeHamiltonianFromWeightedWithParallel jordanWignerTerms coefficientFactory n


    // ── Cached Hamiltonian construction ──────────────────────────────

    /// <summary>
    /// <b>[Legacy / migration]</b> Optimised Hamiltonian construction with operator
    /// caching (weighted contract).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pre-computes and caches all 2n encoded ladder operators (n raises + n lowers)
    /// before assembling the Hamiltonian. This avoids redundant encoding computations
    /// when the same operator appears in many terms — each a†_i or a_j is computed
    /// once and reused across all one-body and two-body terms that reference it.
    /// </para>
    /// <para>
    /// For a system with n spin-orbitals and N_nz non-zero two-body integrals,
    /// this reduces encoding calls from 4·N_nz to 2n, with the multiplication
    /// cost remaining O(N_nz). Typical speedup is 5–20× for molecular systems
    /// where N_nz ≪ n⁴.
    /// </para>
    /// <para>
    /// <b>Coefficient contract (same as <c>computeHamiltonianFromWeightedWith</c>).</b>
    /// The factory returns the FULL WEIGHTED prefactor for each key, applied verbatim
    /// (two-body ½ pre-folded). Results are identical to the sequential builder,
    /// including the cancellation-aware removal of zero residues.
    /// </para>
    /// </remarks>
    /// <param name="encode">The encoding function.</param>
    /// <param name="coefficientFactory">Weighted coefficient lookup (see <c>computeHamiltonianFromWeightedWith</c>).</param>
    /// <param name="n">Number of spin-orbitals (qubits).</param>
    let computeHamiltonianFromWeightedCached (encode : EncoderFn) coefficientFactory (n : uint32) =
        // Pre-cache all encoded raise and lower operators
        let raiseOps = Array.init (int n) (fun i -> encode Raise (uint32 i) n)
        let lowerOps = Array.init (int n) (fun i -> encode Lower (uint32 i) n)

        let encodeOneBody (i : int, j : int, coeff) : PauliRegister[] =
            let product = raiseOps.[i] * lowerOps.[j]
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        let encodeTwoBody (i : int, j : int, k : int, l : int, coeff) : PauliRegister[] =
            // Verbatim a†_i a†_j a_k a_l (order k then l); factory coeff is the full
            // weighted prefactor (see twoBodyStructuralTerms).
            let product = raiseOps.[i] * raiseOps.[j] * lowerOps.[k] * lowerOps.[l]
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        let ni = int n

        let oneBodyTerms =
            [| for i in 0 .. ni - 1 do
                   for j in 0 .. ni - 1 do
                       let key = sprintf "%d,%d" i j
                       match coefficientFactory key with
                       | Some hij -> yield (i, j, hij)
                       | _ -> () |]
            |> Array.Parallel.map encodeOneBody

        let twoBodyTerms =
            [| for i in 0 .. ni - 1 do
                   for j in 0 .. ni - 1 do
                       for k in 0 .. ni - 1 do
                           for l in 0 .. ni - 1 do
                               let key = sprintf "%d,%d,%d,%d" i j k l
                               match coefficientFactory key with
                               | Some hijkl -> yield (i, j, k, l, hijkl)
                               | _ -> () |]
            |> Array.Parallel.map encodeTwoBody

        Array.append (Array.concat oneBodyTerms) (Array.concat twoBodyTerms)
        |> reduceWithCancellation


    // ── Pauli Skeleton: separate structure from coefficients ─────────

    /// <summary>
    /// A pre-computed Pauli term with its signature cached.
    /// </summary>
    /// <remarks>
    /// Caching the signature string and operator array avoids recomputing
    /// them during <c>applyCoefficients</c>, which is called once
    /// per geometry in a PES scan.
    /// </remarks>
    type SkeletonTerm =
        { /// <summary>Pauli signature string, e.g. "XYZII".</summary>
          Signature       : string
          /// <summary>Pauli operator array (shared, not copied).</summary>
          Operators       : Pauli[]
          /// <summary>Structural phase from the encoding algebra (no integral coefficient).</summary>
          StructuralCoeff : Complex }

    /// <summary>
    /// A single entry in a precomputed Hamiltonian skeleton.
    /// </summary>
    /// <remarks>
    /// Contains the index key and the pre-computed Pauli terms that result
    /// from encoding the corresponding operator product.  All phases are
    /// <em>structural</em> — they come from the encoding algebra, not
    /// from integral coefficients.
    /// </remarks>
    type SkeletonEntry =
        { /// <summary>Index key: "i,j" for one-body or "i,j,k,l" for two-body.</summary>
          Key   : string
          /// <summary>Pre-computed Pauli terms with cached signatures.</summary>
          Terms : SkeletonTerm[] }

    /// <summary>
    /// A precomputed encoding skeleton separating Pauli structure from
    /// integral coefficients.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Computing a qubit Hamiltonian has two independent parts:
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///     <b>Structure</b> — which Pauli strings appear for each operator product
    ///     (depends only on the encoding and system size).
    ///   </description></item>
    ///   <item><description>
    ///     <b>Coefficients</b> — the integral values that scale each term
    ///     (depends on the molecular geometry / parameters).
    ///   </description></item>
    /// </list>
    /// <para>
    /// The skeleton captures part (1) once, then <c>applyCoefficients</c>
    /// evaluates part (2) cheaply for any coefficient set.  This is ideal for
    /// potential energy surface scans where the basis set (and encoding
    /// structure) is fixed but integral values vary with geometry.
    /// </para>
    /// </remarks>
    type HamiltonianSkeleton =
        { /// <summary>One-body entries: a†_i a_j encoded as Pauli strings.</summary>
          OneBody   : SkeletonEntry[]
          /// <summary>Two-body entries: a†_i a†_j a_k a_l encoded as Pauli strings.</summary>
          TwoBody   : SkeletonEntry[]
          /// <summary>Number of qubits in the system.</summary>
          NumQubits : uint32 }

    /// <summary>
    /// Precompute the Pauli skeleton for a given encoding and system size.
    /// </summary>
    /// <param name="encode">The encoding function.</param>
    /// <param name="n">The number of qubits/modes.</param>
    /// <returns>
    /// A <see cref="T:Encodings.Hamiltonian.HamiltonianSkeleton"/> containing precomputed Pauli
    /// structures for all one-body and two-body operator products.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Uses <c>Array.Parallel.map</c> on the n⁴ two-body index space.
    /// Signatures and operator arrays are cached in <see cref="T:Encodings.Hamiltonian.SkeletonTerm"/>
    /// records so that <c>applyCoefficients</c> can accumulate
    /// coefficients without any Pauli algebra or string computation.
    /// </para>
    /// <para>
    /// The skeleton is <b>contract-agnostic</b> (pure Pauli structure for each
    /// operator product <c>a†_i a†_j a_k a_l</c>): it is consumed by both the raw
    /// <c>applyCoefficients</c> and the legacy <c>applyCoefficientsFromWeighted</c>,
    /// which differ only in how they map the supplied factory onto these structures.
    /// </para>
    /// </remarks>
    let computeHamiltonianSkeleton (encode : EncoderFn) (n : uint32) : HamiltonianSkeleton =
        let toSkeletonTerms (regs : PauliRegister[]) =
            regs |> Array.map (fun r ->
                { Signature       = r.Signature
                  Operators       = r.Operators
                  StructuralCoeff = r.Coefficient })

        let oneBody =
            [| for i in modeRange n do
                   for j in modeRange n -> (i, j) |]
            |> Array.Parallel.map (fun (i, j) ->
                let key = sprintf "%u,%u" i j
                let product = (encode Raise i n) * (encode Lower j n)
                let terms = product.DistributeCoefficient.SummandTerms |> toSkeletonTerms
                { Key = key; Terms = terms })
            |> Array.filter (fun e -> e.Terms.Length > 0)

        let twoBody =
            [| for i in modeRange n do
                   for j in modeRange n do
                       for k in modeRange n do
                           for l in modeRange n -> (i, j, k, l) |]
            |> Array.Parallel.map (fun (i, j, k, l) ->
                let key = sprintf "%u,%u,%u,%u" i j k l
                let terms = twoBodyStructuralTerms encode i j k l n |> toSkeletonTerms
                { Key = key; Terms = terms })
            |> Array.filter (fun e -> e.Terms.Length > 0)

        { OneBody = oneBody; TwoBody = twoBody; NumQubits = n }

    /// <summary>
    /// <b>[Legacy / migration]</b> Precompute a sparse Pauli skeleton using a
    /// <b>weighted</b> coefficient factory to discover active keys.
    /// </summary>
    /// <param name="encode">The encoding function.</param>
    /// <param name="coefficientFactory">A weighted factory that returns Some for keys to include. Only the presence/absence matters; coefficient values are ignored.</param>
    /// <param name="n">The number of qubits/modes.</param>
    /// <returns>
    /// A <see cref="T:Encodings.Hamiltonian.HamiltonianSkeleton"/> containing precomputed Pauli
    /// structures only for index combinations where the factory returns Some.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For molecules, typically only 5–10% of possible index combinations have
    /// non-zero integrals.  This variant precomputes only those entries,
    /// giving a proportional speedup over the full
    /// <c>computeHamiltonianSkeleton</c>.
    /// </para>
    /// <para>
    /// Use when all geometries in a scan share the same sparsity pattern
    /// (same basis set → same non-zero integral indices). The discovered keys are
    /// weighted structural keys "i,j,k,l" (operator <c>a†_i a†_j a_k a_l</c>) and must
    /// be applied with <c>applyCoefficientsFromWeighted</c>. For the raw-physicist
    /// contract use <c>computeHamiltonianSkeletonFor</c> instead.
    /// </para>
    /// </remarks>
    let computeHamiltonianSkeletonForFromWeighted (encode : EncoderFn) (coefficientFactory : string -> Complex option) (n : uint32) : HamiltonianSkeleton =
        let toSkeletonTerms (regs : PauliRegister[]) =
            regs |> Array.map (fun r ->
                { Signature       = r.Signature
                  Operators       = r.Operators
                  StructuralCoeff = r.Coefficient })

        let oneBodyKeys =
            [| for i in modeRange n do
                   for j in modeRange n do
                       let key = sprintf "%u,%u" i j
                       match coefficientFactory key with
                       | Some _ -> yield (i, j, key)
                       | None -> () |]

        let twoBodyKeys =
            [| for i in modeRange n do
                   for j in modeRange n do
                       for k in modeRange n do
                           for l in modeRange n do
                               let key = sprintf "%u,%u,%u,%u" i j k l
                               match coefficientFactory key with
                               | Some _ -> yield (i, j, k, l, key)
                               | None -> () |]

        let oneBody =
            oneBodyKeys
            |> Array.Parallel.map (fun (i, j, key) ->
                let product = (encode Raise i n) * (encode Lower j n)
                let terms = product.DistributeCoefficient.SummandTerms |> toSkeletonTerms
                { Key = key; Terms = terms })
            |> Array.filter (fun e -> e.Terms.Length > 0)

        let twoBody =
            twoBodyKeys
            |> Array.Parallel.map (fun (i, j, k, l, key) ->
                let terms = twoBodyStructuralTerms encode i j k l n |> toSkeletonTerms
                { Key = key; Terms = terms })
            |> Array.filter (fun e -> e.Terms.Length > 0)

        { OneBody = oneBody; TwoBody = twoBody; NumQubits = n }

    /// <summary>
    /// <b>[Legacy / migration]</b> Apply <b>weighted</b> integral coefficients to a
    /// precomputed skeleton.
    /// </summary>
    /// <param name="skeleton">The precomputed skeleton from <c>computeHamiltonianSkeleton</c> or <c>computeHamiltonianSkeletonForFromWeighted</c>.</param>
    /// <param name="coefficientFactory">A function returning Some(coefficient) for a given comma-separated index key, or None to skip. Same WEIGHTED contract as <c>computeHamiltonianFromWeightedWith</c> — the value is the full weighted prefactor (two-body ½ pre-folded), applied verbatim to <c>a†_i a†_j a_k a_l</c>. For the raw-physicist contract use <c>applyCoefficients</c>.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// <para>
    /// Accumulates coefficients directly into a dictionary keyed by
    /// pre-computed Pauli signatures.  No Pauli algebra, no intermediate
    /// <c>PauliRegister</c> construction, and no re-computation of
    /// signature strings.  Final <c>PauliRegister</c> objects are created
    /// only for the combined result (~tens of terms, not thousands).
    /// </para>
    /// <para>
    /// Zero residues are dropped cancellation-aware (exact zeros, or residues from
    /// more than one contribution below a small multiple of machine epsilon times the
    /// contribution scale), matching <c>computeHamiltonianFromWeightedWith</c>;
    /// standalone tiny coefficients are preserved.
    /// </para>
    /// <para>
    /// Typical runtime is under 10 ms for systems up to ~20 qubits,
    /// making PES scans over hundreds of geometries essentially free
    /// after the one-time skeleton build.
    /// </para>
    /// </remarks>
    let applyCoefficientsFromWeighted (skeleton : HamiltonianSkeleton) (coefficientFactory : string -> Complex option) : PauliRegisterSequence =
        // Accumulate coefficients by Pauli signature — no PauliRegister construction.
        // Track per-signature contribution count and magnitude scale so the final
        // reduction is cancellation-aware (see reduceWithCancellation).
        let cancellationFactor = 32.0
        let sums   = System.Collections.Generic.Dictionary<string, Complex>()
        let counts = System.Collections.Generic.Dictionary<string, int>()
        let scales = System.Collections.Generic.Dictionary<string, float>()
        let opsDict = System.Collections.Generic.Dictionary<string, Pauli[]>()

        let processEntries (entries : SkeletonEntry[]) =
            for entry in entries do
                match coefficientFactory entry.Key with
                | Some coeff ->
                    for term in entry.Terms do
                        let scaledCoeff = term.StructuralCoeff * coeff
                        match sums.TryGetValue term.Signature with
                        | true, existing ->
                            sums.[term.Signature]   <- existing + scaledCoeff
                            counts.[term.Signature] <- counts.[term.Signature] + 1
                            scales.[term.Signature] <- scales.[term.Signature] + scaledCoeff.Magnitude
                        | false, _ ->
                            sums.[term.Signature]    <- scaledCoeff
                            counts.[term.Signature]  <- 1
                            scales.[term.Signature]  <- scaledCoeff.Magnitude
                            opsDict.[term.Signature] <- term.Operators
                | None -> ()

        processEntries skeleton.OneBody
        processEntries skeleton.TwoBody

        // Construct PauliRegisters only for the final combined terms, dropping exact
        // zeros and cancellation residues (|sum| ≤ K·eps·scale when count > 1) while
        // preserving standalone tiny coefficients.
        [| for kvp in sums do
             let sign  = kvp.Key
             let sum   = kvp.Value
             let count = counts.[sign]
             let scale = scales.[sign]
             let isCancellationResidue =
                 count > 1 && sum.Magnitude <= cancellationFactor * machineEpsilon * scale
             if (not sum.IsZero) && (not isCancellationResidue) then
                 yield PauliRegister(opsDict.[sign], sum) |]
        |> PauliRegisterSequence


    // ── Factory adapters: raw ⟷ weighted, and antisymmetrised ────────

    /// <summary>
    /// (internal) Adapt a raw single-bar physicist factory to the legacy weighted
    /// contract: two-body key <c>"i,j,k,l"</c> ↦ <c>½·⟨ij|lk⟩</c> (i.e. raw entry
    /// <c>(p,q,r,s) ↦ ⟨pq|rs⟩</c> maps to weighted key <c>(p,q,s,r)</c> = <c>½·⟨pq|rs⟩</c>),
    /// one-body keys pass through. This is how every raw primary builder is
    /// implemented on top of the audited weighted core.
    /// </summary>
    let private rawToWeightedFactory (rawPhysicistFactory : string -> Complex option) : (string -> Complex option) =
        fun (key : string) ->
            let parts = key.Split(',')
            match parts.Length with
            | 4 ->
                // Weighted key (i,j,k,l) ← raw key (i,j,l,k); value = ½·⟨ij|lk⟩.
                let rawKey = sprintf "%s,%s,%s,%s" parts.[0] parts.[1] parts.[3] parts.[2]
                rawPhysicistFactory rawKey
                |> Option.map (fun g -> g * Complex(0.5, 0.0))
            | _ -> rawPhysicistFactory key

    /// <summary>
    /// Adapt a <b>legacy weighted</b> coefficient factory to the <b>raw physicist</b>
    /// contract consumed by the primary builders (0.9.0+).
    /// </summary>
    /// <param name="weightedFactory">
    /// A factory returning the FULL WEIGHTED prefactor for key <c>"i,j,k,l"</c>
    /// (the coefficient of <c>a†_i a†_j a_k a_l</c>, two-body ½ pre-folded), and the
    /// one-body coefficient <c>h_pq</c> for key <c>"p,q"</c>.
    /// </param>
    /// <returns>
    /// A raw-physicist factory. For a two-body query <c>"p,q,r,s"</c> it returns
    /// <c>2·w(p,q,s,r)</c>, so that feeding it to a raw builder reproduces exactly the
    /// weighted physics <c>w(p,q,s,r)·a†_p a†_q a_s a_r</c>. One-body keys pass through.
    /// </returns>
    /// <remarks>
    /// This is the forward migration bridge: it lets pre-adapted weighted data (or an
    /// old caller's weighted factory) drive the new raw builders — including the
    /// <see cref="T:Encodings.Optimization"/> entry points, which have no dedicated
    /// weighted overload. It is the exact inverse of the internal raw→weighted mapping.
    /// </remarks>
    let weightedToRawFactory (weightedFactory : string -> Complex option) : (string -> Complex option) =
        fun (key : string) ->
            let parts = key.Split(',')
            match parts.Length with
            | 4 ->
                // Raw key (p,q,r,s) → weighted key (p,q,s,r), value doubled (undo the ½).
                let weightedKey = sprintf "%s,%s,%s,%s" parts.[0] parts.[1] parts.[3] parts.[2]
                weightedFactory weightedKey
                |> Option.map (fun w -> w * Complex(2.0, 0.0))
            | _ -> weightedFactory key

    /// <summary>
    /// Adapt an <b>antisymmetrised double-bar</b> physicist tensor <c>⟨pq||rs⟩</c>
    /// (¼ convention) to the raw single-bar factory consumed by the primary builders.
    /// </summary>
    /// <param name="antisymmetrizedFactory">
    /// A factory returning, for key <c>"p,q,r,s"</c>, the antisymmetrised double-bar
    /// integral <c>⟨pq||rs⟩ = ⟨pq|rs⟩ − ⟨pq|sr⟩</c>, and, for key <c>"p,q"</c>, the
    /// one-body coefficient <c>h_pq</c>.
    /// </param>
    /// <returns>
    /// A raw factory that scales every two-body entry by ½ (one-body passes through).
    /// The double-bar Hamiltonian <c>¼·Σ ⟨pq||rs⟩ a†_p a†_q a_s a_r</c> equals the
    /// single-bar <c>½·Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r</c> under fermionic anticommutation,
    /// so <c>½·⟨pq||rs⟩</c> is the correct raw single-bar value for each key.
    /// </returns>
    /// <remarks>
    /// Use for chemistry codes that hand you an already-antisymmetrised tensor. The
    /// core/nuclear energy remains a separate, caller-supplied constant.
    /// </remarks>
    let antisymmetrizedToRawFactory (antisymmetrizedFactory : string -> Complex option) : (string -> Complex option) =
        fun (key : string) ->
            let parts = key.Split(',')
            match parts.Length with
            | 4 -> antisymmetrizedFactory key |> Option.map (fun g -> g * Complex(0.5, 0.0))
            | _ -> antisymmetrizedFactory key


    // ── RAW physicist integral API (primary; breaking change in 0.9.0) ──

    /// <summary>
    /// Compute a qubit Hamiltonian from <b>raw single-bar physicist integrals</b>
    /// using any encoding. <b>Primary builder (0.9.0+).</b>
    /// </summary>
    /// <param name="encode">The encoding function to transform ladder operators to Pauli strings.</param>
    /// <param name="rawPhysicistFactory">A factory returning, for key <c>"p,q,r,s"</c>, the RAW physicist integral <c>⟨pq|rs⟩</c> (no ½, no index swap), and for key <c>"p,q"</c> the one-body coefficient <c>h_pq</c>; None to skip.</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// <para>
    /// Assembles <c>Σ h_pq a†_p a_q + ½ Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r</c>: the library
    /// applies the two-body ½ and builds the annihilators in the order <c>a_s a_r</c>
    /// (the r↔s swap) internally, so the factory supplies the raw integral unmodified.
    /// <see cref="T:Encodings.Fcidump"/> adapters produce a conforming raw factory.
    /// </para>
    /// <para>
    /// To reuse an existing <b>weighted</b> factory, wrap it with
    /// <c>weightedToRawFactory</c>, or use the legacy
    /// <c>computeHamiltonianFromWeightedWith</c>. The nuclear/constant term is not
    /// added. Numerically-zero residues are removed (cancellation-aware) while
    /// standalone tiny coefficients are preserved.
    /// </para>
    /// </remarks>
    let computeHamiltonianWith (encode : EncoderFn) (rawPhysicistFactory : string -> Complex option) n =
        computeHamiltonianFromWeightedWith encode (rawToWeightedFactory rawPhysicistFactory) n

    /// <summary>
    /// Compute a qubit Hamiltonian from <b>raw single-bar physicist integrals</b>
    /// using Jordan-Wigner encoding. <b>Primary builder (0.9.0+).</b>
    /// </summary>
    /// <param name="rawPhysicistFactory">A raw physicist factory (see <c>computeHamiltonianWith</c>).</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    let computeHamiltonian (rawPhysicistFactory : string -> Complex option) n =
        computeHamiltonianWith jordanWignerTerms rawPhysicistFactory n

    /// <summary>Parallel version of <c>computeHamiltonianWith</c> (raw physicist contract).</summary>
    /// <param name="encode">The encoding function.</param>
    /// <param name="rawPhysicistFactory">A raw physicist factory (see <c>computeHamiltonianWith</c>).</param>
    /// <param name="n">The number of qubits/modes.</param>
    let computeHamiltonianWithParallel (encode : EncoderFn) (rawPhysicistFactory : string -> Complex option) n =
        computeHamiltonianFromWeightedWithParallel encode (rawToWeightedFactory rawPhysicistFactory) n

    /// <summary>Parallel version of <c>computeHamiltonian</c> (Jordan-Wigner, raw physicist contract).</summary>
    let computeHamiltonianParallel (rawPhysicistFactory : string -> Complex option) n =
        computeHamiltonianWithParallel jordanWignerTerms rawPhysicistFactory n

    /// <summary>Cached-operator version of <c>computeHamiltonianWith</c> (raw physicist contract).</summary>
    /// <param name="encode">The encoding function.</param>
    /// <param name="rawPhysicistFactory">A raw physicist factory (see <c>computeHamiltonianWith</c>).</param>
    /// <param name="n">Number of spin-orbitals (qubits).</param>
    let computeHamiltonianCached (encode : EncoderFn) (rawPhysicistFactory : string -> Complex option) (n : uint32) =
        computeHamiltonianFromWeightedCached encode (rawToWeightedFactory rawPhysicistFactory) n

    /// <summary>
    /// Precompute a sparse Pauli skeleton using a <b>raw physicist</b> factory to
    /// discover active keys, then apply with <c>applyCoefficients</c>.
    /// </summary>
    /// <param name="encode">The encoding function.</param>
    /// <param name="rawPhysicistFactory">A raw physicist factory; only presence/absence matters for discovery.</param>
    /// <param name="n">The number of qubits/modes.</param>
    let computeHamiltonianSkeletonFor (encode : EncoderFn) (rawPhysicistFactory : string -> Complex option) (n : uint32) : HamiltonianSkeleton =
        computeHamiltonianSkeletonForFromWeighted encode (rawToWeightedFactory rawPhysicistFactory) n

    /// <summary>
    /// Apply <b>raw physicist</b> integral coefficients to a precomputed skeleton.
    /// </summary>
    /// <param name="skeleton">The precomputed skeleton from <c>computeHamiltonianSkeleton</c> or <c>computeHamiltonianSkeletonFor</c>.</param>
    /// <param name="rawPhysicistFactory">A raw physicist factory (see <c>computeHamiltonianWith</c>): key <c>"p,q,r,s"</c> ↦ <c>⟨pq|rs⟩</c>. The library applies the ½ and r↔s order.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    let applyCoefficients (skeleton : HamiltonianSkeleton) (rawPhysicistFactory : string -> Complex option) : PauliRegisterSequence =
        applyCoefficientsFromWeighted skeleton (rawToWeightedFactory rawPhysicistFactory)


    // ── Deprecated compatibility aliases (PR #6 raw adapter) ─────────

    /// <summary>
    /// <b>[Obsolete]</b> Adapt a raw physicist factory to the weighted contract.
    /// </summary>
    /// <remarks>
    /// Redundant since 0.9.0: the primary builders (<c>computeHamiltonian</c>,
    /// <c>computeHamiltonianWith</c>, …) consume raw physicist integrals directly, so
    /// pass the raw factory straight to them. Retained only to bridge raw data into the
    /// legacy <c>computeHamiltonianFromWeighted…</c> functions.
    /// </remarks>
    [<Obsolete("Since 0.9.0 the primary builders consume raw physicist integrals directly; pass the raw factory to computeHamiltonian/computeHamiltonianWith. This bridge is only needed to feed raw data into the legacy computeHamiltonianFromWeighted* functions.")>]
    let rawPhysicistToWeightedFactory (rawPhysicistFactory : string -> Complex option) : (string -> Complex option) =
        rawToWeightedFactory rawPhysicistFactory

    /// <summary>
    /// <b>[Obsolete]</b> Identity alias of <c>computeHamiltonianWith</c> (both now
    /// consume raw physicist integrals). Use <c>computeHamiltonianWith</c> directly.
    /// </summary>
    [<Obsolete("Since 0.9.0 computeHamiltonianWith consumes raw physicist integrals directly; this alias is redundant. Use computeHamiltonianWith.")>]
    let computeHamiltonianFromPhysicistWith (encode : EncoderFn) (rawPhysicistFactory : string -> Complex option) n =
        computeHamiltonianWith encode rawPhysicistFactory n

    /// <summary>
    /// <b>[Obsolete]</b> Identity alias of <c>computeHamiltonian</c> (both now consume
    /// raw physicist integrals). Use <c>computeHamiltonian</c> directly.
    /// </summary>
    [<Obsolete("Since 0.9.0 computeHamiltonian consumes raw physicist integrals directly; this alias is redundant. Use computeHamiltonian.")>]
    let computeHamiltonianFromPhysicist (rawPhysicistFactory : string -> Complex option) n =
        computeHamiltonianWith jordanWignerTerms rawPhysicistFactory n
