namespace Encodings

open System.Numerics

/// <summary>
/// Pluggable optimisation framework for selecting the best encoding.
/// </summary>
/// <remarks>
/// <para>
/// Provides a composable pipeline for encoding selection: users supply a
/// <em>cost function</em> (any <c>PauliRegisterSequence → float</c>) and a set of
/// <em>encoding candidates</em>. The framework evaluates each candidate and returns
/// the one that minimises cost, together with full comparison data.
/// </para>
/// <para>
/// Built-in cost functions cover the most common metrics: λ-norm (qubitization),
/// total Pauli weight (Trotter CNOT cost), term count (measurement overhead),
/// and maximum Pauli weight (circuit depth). Users can define custom cost
/// functions (e.g., weighted combinations) and pass them directly.
/// </para>
/// <para>
/// The framework is intentionally <em>strategy-agnostic</em>: it evaluates
/// whatever candidates you give it, so external optimisers (genetic algorithms,
/// Bayesian optimisation, simulated annealing) can generate candidates and use
/// this module for evaluation.
/// </para>
/// <para>
/// <b>Coefficient factory convention.</b> Every function here builds its
/// Hamiltonian through the raw-physicist <c>computeHamiltonianWith</c> (0.9.0+), so
/// the <c>coefficientFactory</c> argument follows the same <b>raw single-bar
/// physicist</b> contract: the value for key <c>"p,q,r,s"</c> is the raw integral
/// <c>⟨pq|rs⟩</c> (no ½, no index swap), and for key <c>"p,q"</c> the one-body
/// coefficient <c>h_pq</c>. Build one with <see cref="T:Encodings.Fcidump"/>. To reuse
/// a legacy <b>weighted</b> factory here, wrap it once with
/// <c>Hamiltonian.weightedToRawFactory</c>.
/// </para>
/// </remarks>
module Optimization =

    open Encodings.Hamiltonian
    open Encodings.CostAnalysis

    // ── Types ───────────────────────────────────────────────────────

    /// <summary>
    /// A cost function maps an encoded Hamiltonian to a scalar to minimise.
    /// </summary>
    type CostFunction = PauliRegisterSequence -> float

    /// <summary>
    /// An encoding candidate: a named encoder function.
    /// </summary>
    type EncodingCandidate =
        { /// <summary>Human-readable name for display and comparison.</summary>
          Name : string
          /// <summary>The encoder function (same signature as <see cref="T:Encodings.Hamiltonian.EncoderFn"/>).</summary>
          Encoder : EncoderFn }

    /// <summary>
    /// Result of evaluating a single encoding candidate.
    /// </summary>
    type EvaluationResult =
        { /// <summary>The candidate that was evaluated.</summary>
          Candidate : EncodingCandidate
          /// <summary>The cost value produced by the cost function.</summary>
          Cost : float
          /// <summary>The full encoded Hamiltonian.</summary>
          Hamiltonian : PauliRegisterSequence
          /// <summary>Comprehensive cost metrics.</summary>
          Costs : HamiltonianCosts }

    /// <summary>
    /// Result of an optimisation run across multiple candidates.
    /// </summary>
    type OptimizationResult =
        { /// <summary>The candidate with the lowest cost.</summary>
          Best : EvaluationResult
          /// <summary>All candidates, sorted by cost ascending.</summary>
          AllResults : EvaluationResult[] }

    // ── Built-in Cost Functions ─────────────────────────────────────

    /// <summary>
    /// LCU 1-norm: Σ|cₖ|. Minimising this minimises qubitization query complexity.
    /// </summary>
    let lambdaNormCost : CostFunction =
        fun h -> (hamiltonianCosts h).LambdaNorm

    /// <summary>
    /// Total Pauli weight: Σ weight(Pₖ). Proxy for total CNOT count in Trotter.
    /// </summary>
    let totalPauliWeightCost : CostFunction =
        fun h -> float (hamiltonianCosts h).TotalPauliWeight

    /// <summary>
    /// Number of distinct Pauli terms. Proxy for measurement overhead in VQE.
    /// </summary>
    let termCountCost : CostFunction =
        fun h -> float (hamiltonianCosts h).TermCount

    /// <summary>
    /// Maximum Pauli weight across all terms. Proxy for worst-case circuit depth per rotation.
    /// </summary>
    let maxPauliWeightCost : CostFunction =
        fun h -> float (hamiltonianCosts h).MaxPauliWeight

    /// <summary>
    /// Actual first-order Trotter CNOT count at the given time step.
    /// </summary>
    /// <param name="dt">Time step size for the Trotter decomposition.</param>
    /// <returns>A cost function computing the CNOT count.</returns>
    let trotterCnotCost (dt : float) : CostFunction =
        fun h ->
            let step = Trotterization.firstOrderTrotter dt h
            float (Trotterization.trotterCnotCount step)

    /// <summary>
    /// Combine two cost functions with weights: w₁·f₁ + w₂·f₂.
    /// </summary>
    /// <param name="w1">Weight for the first cost function.</param>
    /// <param name="f1">First cost function.</param>
    /// <param name="w2">Weight for the second cost function.</param>
    /// <param name="f2">Second cost function.</param>
    /// <returns>A combined cost function.</returns>
    let combinedCost (w1 : float) (f1 : CostFunction) (w2 : float) (f2 : CostFunction) : CostFunction =
        fun h -> w1 * f1 h + w2 * f2 h

    // ── Built-in Encoding Candidates ────────────────────────────────

    /// <summary>
    /// The six standard encodings built into FockMap.
    /// </summary>
    /// <param name="n">Number of qubits / spin-orbitals.</param>
    /// <returns>Array of encoding candidates.</returns>
    /// <remarks>
    /// For tree-based encodings (balanced binary, balanced ternary, Vlasov),
    /// the tree is rebuilt per call to the encoder function, matching the
    /// library's existing convention for these encodings.
    /// </remarks>
    let standardEncodings (n : uint32) : EncodingCandidate[] =
        [| { Name = "Jordan-Wigner";     Encoder = JordanWigner.jordanWignerTerms }
           { Name = "Bravyi-Kitaev";     Encoder = BravyiKitaev.bravyiKitaevTerms }
           { Name = "Parity";            Encoder = MajoranaEncoding.parityTerms }
           { Name = "Balanced Binary";   Encoder = TreeEncoding.balancedBinaryTreeTerms }
           { Name = "Balanced Ternary";  Encoder = TreeEncoding.ternaryTreeTerms }
           { Name = "Vlasov";            Encoder = TreeEncoding.vlasovTreeTerms } |]

    // ── Evaluation ──────────────────────────────────────────────────

    /// <summary>
    /// Evaluate a single encoding candidate against a cost function.
    /// </summary>
    /// <param name="costFn">The cost function to evaluate.</param>
    /// <param name="coefficientFactory">Molecular integral lookup.</param>
    /// <param name="n">Number of spin-orbitals (qubits).</param>
    /// <param name="candidate">The encoding candidate to evaluate.</param>
    /// <returns>An <see cref="T:Encodings.Optimization.EvaluationResult"/> with cost and Hamiltonian data.</returns>
    let evaluate
        (costFn : CostFunction)
        (coefficientFactory : string -> Complex option)
        (n : uint32)
        (candidate : EncodingCandidate) : EvaluationResult =
        let hamiltonian = computeHamiltonianWith candidate.Encoder coefficientFactory n
        let cost = costFn hamiltonian
        let costs = hamiltonianCosts hamiltonian
        { Candidate = candidate; Cost = cost; Hamiltonian = hamiltonian; Costs = costs }

    // ── Optimisation Strategies ─────────────────────────────────────

    /// <summary>
    /// Evaluate all candidates and return the best (lowest cost).
    /// </summary>
    /// <param name="costFn">The cost function to minimise.</param>
    /// <param name="candidates">Array of encoding candidates to evaluate.</param>
    /// <param name="coefficientFactory">Molecular integral lookup.</param>
    /// <param name="n">Number of spin-orbitals (qubits).</param>
    /// <returns>An <see cref="T:Encodings.Optimization.OptimizationResult"/> with the best candidate and full comparison.</returns>
    let optimizeOver
        (costFn : CostFunction)
        (candidates : EncodingCandidate[])
        (coefficientFactory : string -> Complex option)
        (n : uint32) : OptimizationResult =
        let results =
            candidates
            |> Array.map (evaluate costFn coefficientFactory n)
            |> Array.sortBy (fun r -> r.Cost)
        { Best = results.[0]; AllResults = results }

    /// <summary>
    /// Optimise over all six standard encodings.
    /// </summary>
    /// <param name="costFn">The cost function to minimise.</param>
    /// <param name="coefficientFactory">Molecular integral lookup.</param>
    /// <param name="n">Number of spin-orbitals (qubits).</param>
    /// <returns>An <see cref="T:Encodings.Optimization.OptimizationResult"/>.</returns>
    let optimizeStandard
        (costFn : CostFunction)
        (coefficientFactory : string -> Complex option)
        (n : uint32) : OptimizationResult =
        optimizeOver costFn (standardEncodings n) coefficientFactory n

    /// <summary>
    /// Evaluate a custom encoder (e.g., from an external optimiser generating trees)
    /// without needing to construct a full <see cref="T:Encodings.Optimization.EncodingCandidate"/>.
    /// </summary>
    /// <param name="costFn">The cost function to evaluate.</param>
    /// <param name="name">Display name for the encoding.</param>
    /// <param name="encoder">The encoder function.</param>
    /// <param name="coefficientFactory">Molecular integral lookup.</param>
    /// <param name="n">Number of spin-orbitals (qubits).</param>
    /// <returns>An <see cref="T:Encodings.Optimization.EvaluationResult"/>.</returns>
    let evaluateCustom
        (costFn : CostFunction)
        (name : string)
        (encoder : EncoderFn)
        (coefficientFactory : string -> Complex option)
        (n : uint32) : EvaluationResult =
        evaluate costFn coefficientFactory n { Name = name; Encoder = encoder }
