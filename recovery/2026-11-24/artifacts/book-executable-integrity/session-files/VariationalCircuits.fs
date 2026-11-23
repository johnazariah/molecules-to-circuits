namespace Encodings

open System
open System.Numerics

/// <summary>
/// Symbolic support for VQE measurement programs and QPE resource estimation.
/// </summary>
/// <remarks>
/// <para>
/// Provides tools for grouping Hamiltonian terms into qubit-wise commuting
/// measurement bases (reducing the number of measurement circuits needed in VQE),
/// shot-budget estimation, and QPE circuit resource estimation using
/// first-order Trotterization.
/// </para>
/// </remarks>
module VariationalCircuits =

    open Encodings.Trotterization

    // ── Types ───────────────────────────────────────────────────────

    /// <summary>
    /// A measurement basis for simultaneous measurement of commuting Pauli terms.
    /// </summary>
    /// <remarks>
    /// In VQE, terms that qubit-wise commute can be measured simultaneously
    /// in a single basis rotation circuit, reducing the total number of
    /// measurement circuits needed.
    /// </remarks>
    type MeasurementBasis =
        { /// <summary>Basis rotation needed for each qubit (I = computational basis).</summary>
          BasisRotation : Pauli[]
          /// <summary>Hamiltonian terms measurable in this basis.</summary>
          Terms : PauliRegister[]
          /// <summary>Sum of |coefficients| for shot allocation weighting.</summary>
          Weight : float }

    /// <summary>
    /// Complete measurement program for estimating ⟨H⟩.
    /// </summary>
    /// <remarks>
    /// Groups all Hamiltonian terms into qubit-wise commuting bases so each
    /// group can be measured with a single circuit configuration.
    /// </remarks>
    type MeasurementProgram =
        { /// <summary>Array of measurement bases covering all terms.</summary>
          Bases : MeasurementBasis[]
          /// <summary>Total number of Hamiltonian terms across all bases.</summary>
          TotalTerms : int
          /// <summary>Number of distinct measurement bases (groups).</summary>
          GroupCount : int }

    /// <summary>
    /// QPE resource estimate for a Hamiltonian simulation circuit.
    /// </summary>
    type QPEResourceEstimate =
        { /// <summary>Number of system (data) qubits.</summary>
          SystemQubits : int
          /// <summary>Number of ancilla qubits for phase readout.</summary>
          AncillaQubits : int
          /// <summary>Trotter steps per controlled-U application.</summary>
          TrotterStepsPerControlledU : int
          /// <summary>Total CNOT gate count across the full QPE circuit.</summary>
          TotalCnots : int
          /// <summary>Estimated circuit depth (sequential gate layers).</summary>
          CircuitDepth : int
          /// <summary>Number of bits of precision in the phase estimate.</summary>
          PrecisionBits : int }

    // ── Core functions ──────────────────────────────────────────────

    /// <summary>
    /// Two Pauli operators qubit-wise commute if at each position
    /// either both are the same or at least one is I.
    /// </summary>
    /// <param name="a">First Pauli register.</param>
    /// <param name="b">Second Pauli register.</param>
    /// <returns>true if the two registers qubit-wise commute.</returns>
    let qubitWiseCommutes (a : PauliRegister) (b : PauliRegister) : bool =
        let n = max a.Size b.Size
        seq { 0 .. n - 1 }
        |> Seq.forall (fun i ->
            match a.[i], b.[i] with
            | Some pa, Some pb -> pa = pb || pa = I || pb = I
            | _ -> true)

    /// <summary>
    /// Merge two basis-rotation arrays: at each position, take the non-identity
    /// Pauli if one exists.
    /// </summary>
    let private mergeBasis (basis : Pauli[]) (term : PauliRegister) : Pauli[] =
        let n = max basis.Length term.Size
        Array.init n (fun i ->
            let b = if i < basis.Length then basis.[i] else I
            let t = term.[i] |> Option.defaultValue I
            match b, t with
            | I, p | p, I -> p
            | p, _ -> p)

    /// <summary>
    /// Group Hamiltonian terms into qubit-wise commuting measurement bases.
    /// </summary>
    /// <remarks>
    /// Uses a greedy algorithm: sort terms by |coefficient| descending, then
    /// assign each term to the first compatible group. This is a heuristic —
    /// optimal grouping is NP-hard in general.
    /// </remarks>
    /// <param name="hamiltonian">The Hamiltonian as a sum of Pauli strings.</param>
    /// <returns>A MeasurementProgram describing the grouped bases.</returns>
    let groupCommutingTerms (hamiltonian : PauliRegisterSequence) : MeasurementProgram =
        let terms =
            hamiltonian.DistributeCoefficient.SummandTerms
            |> Array.sortByDescending (fun t -> t.Coefficient.Magnitude)

        let groups = ResizeArray<ResizeArray<PauliRegister> * Pauli[]>()

        for term in terms do
            let mutable placed = false
            for i in 0 .. groups.Count - 1 do
                if not placed then
                    let members, basis = groups.[i]
                    let commutesWithAll =
                        members |> Seq.forall (fun m -> qubitWiseCommutes m term)
                    if commutesWithAll then
                        members.Add(term)
                        groups.[i] <- (members, mergeBasis basis term)
                        placed <- true
            if not placed then
                let newGroup = ResizeArray<PauliRegister>()
                newGroup.Add(term)
                let basis = Array.init term.Size (fun i ->
                    term.[i] |> Option.defaultValue I)
                groups.Add(newGroup, basis)

        let bases =
            groups
            |> Seq.map (fun (members, basis) ->
                let termsArr = members.ToArray()
                { BasisRotation = basis
                  Terms = termsArr
                  Weight =
                    termsArr
                    |> Array.sumBy (fun t -> t.Coefficient.Magnitude) })
            |> Seq.toArray

        { Bases = bases
          TotalTerms = terms.Length
          GroupCount = bases.Length }

    /// <summary>
    /// Estimate total measurement shots needed for precision ε on ⟨H⟩.
    /// </summary>
    /// <remarks>
    /// Uses the formula: shots ≈ (Σ|cₖ|)² / ε², derived from the variance
    /// of the estimator for the expectation value of a sum of Pauli terms.
    /// </remarks>
    /// <param name="epsilon">Target precision for the energy estimate.</param>
    /// <param name="hamiltonian">The Hamiltonian as a sum of Pauli strings.</param>
    /// <returns>Estimated number of measurement shots (rounded up).</returns>
    let estimateShots (epsilon : float) (hamiltonian : PauliRegisterSequence) : int =
        let sumAbsCoeffs =
            hamiltonian.DistributeCoefficient.SummandTerms
            |> Array.sumBy (fun t -> t.Coefficient.Magnitude)
        int (ceil (sumAbsCoeffs * sumAbsCoeffs / (epsilon * epsilon)))

    /// <summary>
    /// QPE resource estimation for a given precision and Hamiltonian.
    /// </summary>
    /// <remarks>
    /// Estimates the resources for quantum phase estimation using first-order
    /// Trotterization for the controlled-U circuit. The number of controlled-U
    /// applications scales as 2^(precisionBits) - 1.
    /// </remarks>
    /// <param name="precisionBits">Number of ancilla bits for phase readout.</param>
    /// <param name="hamiltonian">The Hamiltonian as a sum of Pauli strings.</param>
    /// <param name="dt">Trotter time step size.</param>
    /// <returns>A QPEResourceEstimate with circuit resource counts.</returns>
    let qpeResources (precisionBits : int) (hamiltonian : PauliRegisterSequence) (dt : float) : QPEResourceEstimate =
        let step = firstOrderTrotter dt hamiltonian
        let stats = trotterStepStats step

        let systemQubits =
            hamiltonian.DistributeCoefficient.SummandTerms
            |> Array.map (fun t -> t.Size)
            |> Array.fold max 0

        // QPE uses 2^k - 1 controlled-U applications (k = precisionBits)
        let controlledUApplications = (1 <<< precisionBits) - 1
        let cnotsPerStep = stats.CnotCount
        let totalCnots = controlledUApplications * cnotsPerStep
        let depthPerStep = stats.TotalGates
        let circuitDepth = controlledUApplications * depthPerStep

        { SystemQubits = systemQubits
          AncillaQubits = precisionBits
          TrotterStepsPerControlledU = 1
          TotalCnots = totalCnots
          CircuitDepth = circuitDepth
          PrecisionBits = precisionBits }
