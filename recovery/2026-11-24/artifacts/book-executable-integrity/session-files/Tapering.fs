namespace Encodings

/// <summary>
/// Symbolic qubit tapering utilities for Pauli Hamiltonians.
/// </summary>
/// <remarks>
/// <para>v1: diagonal Z2 tapering (single-qubit Z generators).</para>
/// <para>v2: general Z₂ symmetry detection via symplectic representation,
/// independent generator selection over GF(2), Clifford rotation synthesis,
/// and a unified tapering pipeline.</para>
/// </remarks>
module Tapering =
    open System
    open System.Numerics

    /// <summary>
    /// Result of a Z2 tapering operation.
    /// </summary>
    type Z2TaperingResult =
        { OriginalQubitCount : int
          TaperedQubitCount  : int
          RemovedQubits      : int[]
          Sector             : (int * int) list
          Hamiltonian        : PauliRegisterSequence }

    let private normalizeSector (sector : (int * int) list) =
        if sector |> List.exists (fun (_, eigen) -> eigen <> 1 && eigen <> -1) then
            invalidArg "sector" "Sector eigenvalues must be +1 or -1."

        sector
        |> List.distinctBy fst
        |> List.sortBy fst

    let private qubitCountOf (h : PauliRegisterSequence) =
        h.SummandTerms
        |> Array.map (fun t -> t.Operators.Length)
        |> Array.append [| 0 |]
        |> Array.max

    /// <summary>
    /// Detects qubit indices i such that all terms are diagonal (I or Z) on qubit i.
    /// </summary>
    let diagonalZ2SymmetryQubits (hamiltonian : PauliRegisterSequence) : int[] =
        let terms = hamiltonian.DistributeCoefficient.SummandTerms
        let n = qubitCountOf hamiltonian

        [| for i in 0 .. n - 1 do
               let isDiagonalAtI =
                   terms
                   |> Array.forall (fun t ->
                       match t.Operators.[i] with
                       | I | Z -> true
                       | X | Y -> false)
               if isDiagonalAtI then
                   yield i |]

    /// <summary>
    /// Returns single-qubit Z generators for all detected diagonal Z2 symmetries.
    /// </summary>
    let diagonalZ2Generators (hamiltonian : PauliRegisterSequence) : PauliRegister[] =
        let n = qubitCountOf hamiltonian

        diagonalZ2SymmetryQubits hamiltonian
        |> Array.map (fun i ->
            let ops = Array.init n (fun j -> if i = j then Z else I)
            PauliRegister(ops, Complex.One))

    /// <summary>
    /// Tapers a Hamiltonian by fixing diagonal Z2 symmetry sectors and removing those qubits.
    /// </summary>
    /// <param name="sector">List of (qubitIndex, eigenvalue) pairs with eigenvalue in {+1,-1}.</param>
    /// <param name="hamiltonian">Input Hamiltonian as a Pauli sum.</param>
    let taperDiagonalZ2 (sector : (int * int) list) (hamiltonian : PauliRegisterSequence) : Z2TaperingResult =
        let normalizedSector = normalizeSector sector
        let terms = hamiltonian.DistributeCoefficient.SummandTerms
        let n = qubitCountOf hamiltonian

        let removable = diagonalZ2SymmetryQubits hamiltonian |> Set.ofArray

        normalizedSector
        |> List.iter (fun (q, _) ->
            if q < 0 || q >= n then
                invalidArg "sector" (sprintf "Qubit index %d is out of range [0, %d)." q n)
            if not (Set.contains q removable) then
                invalidArg "sector" (sprintf "Qubit %d is not a diagonal Z2 symmetry (found X/Y on that qubit)." q))

        let removed = normalizedSector |> List.map fst |> List.sort |> List.toArray
        let removedSet = removed |> Set.ofArray

        let taperedTerms =
            terms
            |> Array.map (fun t ->
                let mutable coeff = t.Coefficient

                for (q, eigen) in normalizedSector do
                    match t.Operators.[q] with
                    | I -> ()
                    | Z -> coeff <- coeff * Complex(float eigen, 0.0)
                    | X | Y ->
                        // Guarded earlier by validation, kept for safety.
                        invalidArg "hamiltonian" (sprintf "Term contains non-diagonal Pauli on tapered qubit %d." q)

                let taperedOps =
                    t.Operators
                    |> Array.mapi (fun i op -> (i, op))
                    |> Array.choose (fun (i, op) -> if Set.contains i removedSet then None else Some op)

                PauliRegister(taperedOps, coeff))
            |> PauliRegisterSequence

        { OriginalQubitCount = n
          TaperedQubitCount = max 0 (n - removed.Length)
          RemovedQubits = removed
          Sector = normalizedSector
          Hamiltonian = taperedTerms }

    /// <summary>
    /// Convenience helper: taper all detected diagonal Z2 symmetries in the +1 sector.
    /// </summary>
    let taperAllDiagonalZ2WithPositiveSector (hamiltonian : PauliRegisterSequence) : Z2TaperingResult =
        let sector =
            diagonalZ2SymmetryQubits hamiltonian
            |> Array.map (fun i -> (i, 1))
            |> Array.toList

        taperDiagonalZ2 sector hamiltonian


    // ══════════════════════════════════════════════════════════════════
    //  v2: General Z₂ symmetry detection + Clifford tapering
    // ══════════════════════════════════════════════════════════════════

    // ── Phase 1: Symplectic representation ────────────────────────────

    /// <summary>
    /// Binary (GF(2)) symplectic vector representing an n-qubit Pauli string.
    /// Layout: [x₀ x₁ … xₙ₋₁ | z₀ z₁ … zₙ₋₁] where
    /// I=(0,0), X=(1,0), Y=(1,1), Z=(0,1).
    /// </summary>
    type SymplecticVector = { X : bool[]; Z : bool[]; N : int }

    /// <summary>Convert a Pauli operator to its (x,z) symplectic bits.</summary>
    let private pauliToBits = function
        | I -> (false, false)
        | X -> (true,  false)
        | Y -> (true,  true)
        | Z -> (false, true)

    /// <summary>Convert symplectic (x,z) bits back to a Pauli operator.</summary>
    let private bitsToPauli = function
        | (false, false) -> I
        | (true,  false) -> X
        | (true,  true)  -> Y
        | (false, true)  -> Z

    /// <summary>
    /// Convert a PauliRegister to its symplectic binary representation.
    /// Coefficient/phase is discarded — only the Pauli structure matters.
    /// </summary>
    let toSymplectic (r : PauliRegister) : SymplecticVector =
        let ops = r.Operators
        let n = ops.Length
        let xs = Array.init n (fun i -> fst (pauliToBits ops.[i]))
        let zs = Array.init n (fun i -> snd (pauliToBits ops.[i]))
        { X = xs; Z = zs; N = n }

    /// <summary>
    /// Convert a symplectic vector back to a PauliRegister with coefficient +1.
    /// </summary>
    let fromSymplectic (sv : SymplecticVector) : PauliRegister =
        let ops = Array.init sv.N (fun i -> bitsToPauli (sv.X.[i], sv.Z.[i]))
        PauliRegister(ops, Complex.One)

    /// <summary>
    /// Symplectic inner product mod 2: returns true if the two Pauli strings anti-commute.
    /// Two n-qubit Paulis commute iff ⟨a, b⟩_s = Σᵢ (aₓᵢ bᵤᵢ ⊕ aᵤᵢ bₓᵢ) = 0 (mod 2).
    /// </summary>
    let symplecticInnerProduct (a : SymplecticVector) (b : SymplecticVector) : bool =
        let mutable count = 0
        for i in 0 .. a.N - 1 do
            if a.X.[i] && b.Z.[i] then count <- count + 1
            if a.Z.[i] && b.X.[i] then count <- count + 1
        (count % 2) = 1  // true = anti-commute

    /// <summary>
    /// Returns true if two Pauli strings commute (symplectic inner product = 0 mod 2).
    /// </summary>
    let commutes (a : SymplecticVector) (b : SymplecticVector) : bool =
        not (symplecticInnerProduct a b)

    // ── GF(2) linear algebra ──────────────────────────────────────────

    /// <summary>XOR two boolean arrays element-wise.</summary>
    let private xorArrays (a : bool[]) (b : bool[]) =
        Array.init a.Length (fun i -> a.[i] <> b.[i])

    /// <summary>
    /// GF(2) Gaussian elimination on a boolean matrix (array of rows).
    /// Returns (rref, rank) where rref is the row-reduced form.
    /// </summary>
    let private gf2Rref (rows : bool[][]) : bool[][] * int =
        if rows.Length = 0 then ([||], 0)
        else
        let m = rows.Length
        let n = rows.[0].Length
        let mat = rows |> Array.map Array.copy
        let mutable pivotRow = 0
        for col in 0 .. n - 1 do
            // Find a pivot in this column at or below pivotRow
            let mutable found = -1
            for r in pivotRow .. m - 1 do
                if found = -1 && mat.[r].[col] then found <- r
            if found >= 0 then
                // Swap
                let tmp = mat.[pivotRow]
                mat.[pivotRow] <- mat.[found]
                mat.[found] <- tmp
                // Eliminate
                for r in 0 .. m - 1 do
                    if r <> pivotRow && mat.[r].[col] then
                        mat.[r] <- xorArrays mat.[r] mat.[pivotRow]
                pivotRow <- pivotRow + 1
        (mat, pivotRow)

    /// <summary>
    /// Find the null space (kernel) of a GF(2) matrix.
    /// Each row of the input is a boolean vector; the kernel consists of
    /// vectors v such that M·v = 0 (mod 2), computed via RREF.
    /// </summary>
    let private gf2Kernel (rows : bool[][]) : bool[][] =
        if rows.Length = 0 then [||]
        else
        let m = rows.Length
        let n = rows.[0].Length
        // Augment: [M | Iₘ] — track row operations
        let augmented =
            rows |> Array.mapi (fun i row ->
                let aug = Array.create (n + m) false
                Array.blit row 0 aug 0 n
                aug.[n + i] <- true
                aug)
        let (rref, rank) = gf2Rref augmented
        // Zero rows in the RREF correspond to kernel vectors
        // The identity part tells us the linear combination
        [| for r in 0 .. m - 1 do
               let isZeroRow = Array.TrueForAll(rref.[r].[0..n-1], fun b -> not b)
               if isZeroRow then
                   yield rref.[r].[n .. n + m - 1] |]

    /// <summary>
    /// Find all Pauli strings (as symplectic vectors) that commute with every
    /// term in the Hamiltonian. This is the commutant / symmetry group.
    /// </summary>
    let findCommutingGenerators (hamiltonian : PauliRegisterSequence) : SymplecticVector[] =
        let terms = hamiltonian.DistributeCoefficient.SummandTerms
        let n = qubitCountOf hamiltonian
        if n = 0 || terms.Length = 0 then [||]
        else
        let termVecs = terms |> Array.map toSymplectic

        // Build the commutation check matrix over GF(2).
        // For each term tₖ, we need ⟨g, tₖ⟩_s = 0 for the unknown generator g.
        // ⟨g, tₖ⟩_s = Σᵢ (gₓᵢ·tₖ_zᵢ ⊕ gᵤᵢ·tₖ_xᵢ)
        // This is linear in the 2n bits of g, so each term gives one row
        // of a matrix M where M·g = 0 (mod 2).
        // Row layout: [tₖ_z₀ tₖ_z₁ … tₖ_zₙ₋₁ | tₖ_x₀ tₖ_x₁ … tₖ_xₙ₋₁]
        // (swapped x↔z because the symplectic form pairs gₓ with tᵤ and vice versa)
        let commutationMatrix =
            termVecs |> Array.map (fun tv ->
                let row = Array.create (2 * n) false
                for i in 0 .. n - 1 do
                    row.[i]     <- tv.Z.[i]  // pairs with gₓᵢ
                    row.[n + i] <- tv.X.[i]  // pairs with gᵤᵢ
                row)

        // Kernel of the commutation matrix = generators that commute with all terms
        let kernelVecs = gf2Kernel commutationMatrix

        // Each kernel vector is a linear combination of the original rows (from the
        // augmented identity block). But here we computed the kernel of the
        // *commutation check* matrix, so each kernel vector is directly a 2n-bit
        // symplectic vector for a commuting Pauli.
        // Actually, the kernel vectors are in the *row space* of the augmented identity,
        // meaning they are coefficient vectors. We need the null space of M directly.

        // Let's use a more direct approach: enumerate the null space of M^T.
        // The null space of M (m × 2n) consists of 2n-bit vectors g with M·g = 0.
        // We can compute this by transposing M and finding the left null space.

        // Simpler: build M^T (2n × m), find its kernel (m-bit vectors), then those
        // give linear dependencies among the rows. Instead, just find the null space
        // of M directly using a standard approach:

        // Standard null space: augment M^T with identity, rref, zero rows give kernel.
        let mt = Array.init (2 * n) (fun j ->
            Array.init terms.Length (fun i -> commutationMatrix.[i].[j]))

        // We want vectors g (length 2n) such that M·g=0, i.e., for each row i of M,
        // Σⱼ M[i,j]·g[j] = 0. This is: g is in null(M). To find null(M):
        // RREF M, find free variables, back-substitute.

        let (rref, rank) = gf2Rref commutationMatrix
        let cols = 2 * n

        // Identify pivot columns
        let pivotCols = Array.create rank -1
        let mutable pr = 0
        for c in 0 .. cols - 1 do
            if pr < rank && rref.[pr].[c] then
                pivotCols.[pr] <- c
                pr <- pr + 1

        let pivotSet = Set.ofArray pivotCols
        let freeCols = [| for c in 0 .. cols - 1 do if not (Set.contains c pivotSet) then yield c |]

        // For each free variable, construct a null-space vector
        let nullVecs =
            freeCols |> Array.map (fun fc ->
                let vec = Array.create cols false
                vec.[fc] <- true
                // Back-substitute: for each pivot row r with pivot in column pivotCols[r],
                // vec[pivotCols[r]] = XOR of rref[r][freeCols] where those free cols are set
                for r in 0 .. rank - 1 do
                    if rref.[r].[fc] then
                        vec.[pivotCols.[r]] <- vec.[pivotCols.[r]] <> true
                vec)

        // Convert to SymplecticVectors, filter out the identity (all-zero)
        nullVecs
        |> Array.map (fun vec ->
            { X = vec.[0 .. n - 1]; Z = vec.[n .. 2*n - 1]; N = n })
        |> Array.filter (fun sv ->
            sv.X |> Array.exists id || sv.Z |> Array.exists id)


    // ── Phase 2: Independent generator selection ──────────────────────

    /// <summary>
    /// Select a maximal linearly independent subset of symplectic vectors over GF(2).
    /// </summary>
    let independentGenerators (generators : SymplecticVector[]) : SymplecticVector[] =
        if generators.Length = 0 then [||]
        else
        let n = generators.[0].N
        // Convert to 2n-bit rows
        let rows = generators |> Array.map (fun sv ->
            Array.append sv.X sv.Z)
        let (_, rank) = gf2Rref rows

        // Re-do RREF tracking which original generators survive
        let m = rows.Length
        let cols = 2 * n
        let mat = rows |> Array.map Array.copy
        let indices = Array.init m id
        let mutable pivotRow = 0
        for col in 0 .. cols - 1 do
            let mutable found = -1
            for r in pivotRow .. m - 1 do
                if found = -1 && mat.[r].[col] then found <- r
            if found >= 0 then
                let tmpR = mat.[pivotRow]
                mat.[pivotRow] <- mat.[found]
                mat.[found] <- tmpR
                let tmpI = indices.[pivotRow]
                indices.[pivotRow] <- indices.[found]
                indices.[found] <- tmpI
                for r in 0 .. m - 1 do
                    if r <> pivotRow && mat.[r].[col] then
                        mat.[r] <- xorArrays mat.[r] mat.[pivotRow]
                pivotRow <- pivotRow + 1

        // The first `rank` rows after RREF are the independent generators
        // But they may have been modified by elimination. Return the RREF rows
        // as new symplectic vectors (they represent valid Pauli generators).
        [| for r in 0 .. rank - 1 ->
               { X = mat.[r].[0 .. n-1]; Z = mat.[r].[n .. 2*n-1]; N = n } |]

    /// <summary>
    /// Count the number of independent Z₂ symmetries of the Hamiltonian.
    /// This equals the maximum number of qubits that can be tapered.
    /// </summary>
    let z2SymmetryCount (hamiltonian : PauliRegisterSequence) : int =
        findCommutingGenerators hamiltonian
        |> independentGenerators
        |> Array.length


    // ── Phase 3: Clifford rotation synthesis ──────────────────────────

    /// <summary>Elementary Clifford gate for tapering rotation.</summary>
    type CliffordGate =
        /// <summary>Hadamard on qubit i: X↔Z.</summary>
        | Had of int
        /// <summary>Phase gate S on qubit i: X→Y, Z→Z.</summary>
        | Sgate of int
        /// <summary>CNOT with control c and target t.</summary>
        | CNOT of int * int

    /// <summary>
    /// Apply a single Clifford gate to a symplectic vector (conjugation).
    /// Transforms the Pauli string as P → U P U†.
    /// </summary>
    let private applyGateToSymplectic (gate : CliffordGate) (sv : SymplecticVector) : SymplecticVector =
        let x = Array.copy sv.X
        let z = Array.copy sv.Z
        match gate with
        | Had i ->
            // H: X↔Z on qubit i
            let tmp = x.[i]
            x.[i] <- z.[i]
            z.[i] <- tmp
        | Sgate i ->
            // S: X→Y (x stays, z flips), Z→Z
            // S X S† = Y = XZ, so z[i] <- z[i] XOR x[i]
            z.[i] <- z.[i] <> x.[i]
        | CNOT (c, t) ->
            // CNOT: X_c → X_c X_t, Z_t → Z_c Z_t
            // In symplectic: x_t <- x_t XOR x_c, z_c <- z_c XOR z_t
            x.[t] <- x.[t] <> x.[c]
            z.[c] <- z.[c] <> z.[t]
        { X = x; Z = z; N = sv.N }

    /// <summary>
    /// Apply a sequence of Clifford gates to a symplectic vector.
    /// </summary>
    let applyGatesToSymplectic (gates : CliffordGate list) (sv : SymplecticVector) : SymplecticVector =
        gates |> List.fold (fun s g -> applyGateToSymplectic g s) sv

    /// <summary>
    /// Synthesize a Clifford circuit that rotates a set of independent generators
    /// each onto a single-qubit Z operator. Returns the gate list and target qubit
    /// indices (one per generator).
    /// </summary>
    let synthesizeTaperingClifford (generators : SymplecticVector[]) : CliffordGate list * int[] =
        let k = generators.Length
        if k = 0 then ([], [||])
        else
        let n = generators.[0].N
        // Work on mutable copies
        let gs = generators |> Array.map (fun sv -> { X = Array.copy sv.X; Z = Array.copy sv.Z; N = n })
        let mutable gates : CliffordGate list = []
        let targets = Array.create k -1

        for gi in 0 .. k - 1 do
            let g = gs.[gi]

            // Find a qubit where this generator has support (x or z bit set)
            // Prefer a qubit not already used as a target
            let usedTargets = targets |> Array.filter (fun t -> t >= 0) |> Set.ofArray
            let mutable targetQ = -1
            // First pass: prefer unused qubits
            for q in 0 .. n - 1 do
                if targetQ = -1 && not (Set.contains q usedTargets) && (g.X.[q] || g.Z.[q]) then
                    targetQ <- q
            // Fallback: any qubit with support
            if targetQ = -1 then
                for q in 0 .. n - 1 do
                    if targetQ = -1 && (g.X.[q] || g.Z.[q]) then
                        targetQ <- q

            if targetQ = -1 then
                invalidOp (sprintf "Generator %d is the identity — cannot taper." gi)

            targets.[gi] <- targetQ

            // Step 1: If there is X support on targetQ, apply H to swap X↔Z
            if g.X.[targetQ] && not g.Z.[targetQ] then
                // Pure X → apply H to make it Z
                let gate = Had targetQ
                gates <- gates @ [gate]
                for i in 0 .. k - 1 do
                    gs.[i] <- applyGateToSymplectic gate gs.[i] |> fun sv -> { sv with N = n }
            elif g.X.[targetQ] && g.Z.[targetQ] then
                // Y (both bits set) → apply S to convert Y→X, then H to get Z
                let s = Sgate targetQ
                gates <- gates @ [s]
                for i in 0 .. k - 1 do
                    gs.[i] <- applyGateToSymplectic s gs.[i] |> fun sv -> { sv with N = n }
                let h = Had targetQ
                gates <- gates @ [h]
                for i in 0 .. k - 1 do
                    gs.[i] <- applyGateToSymplectic h gs.[i] |> fun sv -> { sv with N = n }

            // Now g should have Z on targetQ (z bit set, x bit clear)
            // Step 2: Clear all other qubits' support via CNOTs
            for q in 0 .. n - 1 do
                if q <> targetQ then
                    let gCurr = gs.[gi]
                    if gCurr.Z.[q] then
                        // Z on another qubit: CNOT(targetQ, q) clears z[q]
                        // because CNOT: Z_t → Z_c Z_t, so z_c <- z_c XOR z_t
                        // We want to clear z[q], so CNOT(q, targetQ) would set
                        // z_q <- z_q XOR z_targetQ. That adds; we need to subtract.
                        // Actually: CNOT(targetQ, q): z[targetQ] <- z[targetQ] XOR z[q]
                        // That changes the target qubit. Let's use CNOT(q, targetQ):
                        // x[targetQ] <- x[targetQ] XOR x[q], z[q] <- z[q] XOR z[targetQ]
                        // Since z[targetQ] is 1, z[q] flips — clears it if it was 1.
                        let gate = CNOT (q, targetQ)
                        gates <- gates @ [gate]
                        for i in 0 .. k - 1 do
                            gs.[i] <- applyGateToSymplectic gate gs.[i] |> fun sv -> { sv with N = n }
                    let gCurr2 = gs.[gi]
                    if gCurr2.X.[q] then
                        // X on another qubit: CNOT(targetQ, q) propagates X
                        // CNOT(targetQ, q): x[q] <- x[q] XOR x[targetQ]
                        // Since x[targetQ] should be 0, this won't help directly.
                        // Instead: CNOT(q, targetQ) sets x[targetQ] <- x[targetQ] XOR x[q]
                        // That would dirty targetQ. Better approach: H on q, then CNOT to clear Z, then H back.
                        let h1 = Had q
                        gates <- gates @ [h1]
                        for i in 0 .. k - 1 do
                            gs.[i] <- applyGateToSymplectic h1 gs.[i] |> fun sv -> { sv with N = n }
                        let gCurr3 = gs.[gi]
                        if gCurr3.Z.[q] then
                            let gate = CNOT (q, targetQ)
                            gates <- gates @ [gate]
                            for i in 0 .. k - 1 do
                                gs.[i] <- applyGateToSymplectic gate gs.[i] |> fun sv -> { sv with N = n }
                        let h2 = Had q
                        gates <- gates @ [h2]
                        for i in 0 .. k - 1 do
                            gs.[i] <- applyGateToSymplectic h2 gs.[i] |> fun sv -> { sv with N = n }

        (gates, targets)

    /// <summary>
    /// Apply a sequence of Clifford gates to a PauliRegisterSequence.
    /// Uses the symplectic representation for exact conjugation.
    /// </summary>
    let applyClifford (gates : CliffordGate list) (hamiltonian : PauliRegisterSequence) : PauliRegisterSequence =
        let terms = hamiltonian.DistributeCoefficient.SummandTerms
        let n = qubitCountOf hamiltonian

        let transformedTerms =
            terms |> Array.map (fun reg ->
                // Convert to symplectic, apply all gates, convert back
                let sv = toSymplectic reg
                let sv' = applyGatesToSymplectic gates sv
                let newOps = Array.init sv'.N (fun i -> bitsToPauli (sv'.X.[i], sv'.Z.[i]))

                // Compute the phase change from the Clifford conjugation.
                // Each gate contributes a phase depending on the Pauli at the gate's qubit.
                let mutable coeff = reg.Coefficient
                let mutable currentOps = Array.copy reg.Operators

                for gate in gates do
                    match gate with
                    | Had i ->
                        match currentOps.[i] with
                        | Y -> coeff <- coeff * Complex(-1.0, 0.0)
                        | _ -> ()
                    | Sgate i ->
                        match currentOps.[i] with
                        | Y -> coeff <- coeff * Complex(-1.0, 0.0)
                        | _ -> ()
                    | CNOT (c, t) ->
                        // CNOT phase contribution:
                        // XY → YZ (× i·(-i) = +1? No.)
                        // The CNOT conjugation on a Pauli P_c P_t gives:
                        // Multiply the transformed single-qubit Paulis and track phases
                        let (pcOp, ptOp) = (currentOps.[c], currentOps.[t])
                        // CNOT: P_c P_t → (P_c * σ_x^{is_x(P_c)}_t) * (σ_z^{is_z(P_t)}_c * P_t)
                        // This gets complicated. Use a direct computation:
                        // conjugate P_c ⊗ P_t by CNOT and compute the phase.
                        let (cx, cz) = pauliToBits pcOp
                        let (tx, tz) = pauliToBits ptOp
                        // After CNOT: control x unchanged, target x ← x_t XOR x_c
                        //             control z ← z_c XOR z_t, target z unchanged
                        let cx' = cx
                        let cz' = cz <> tz
                        let tx' = tx <> cx
                        let tz' = tz
                        let newC = bitsToPauli (cx', cz')
                        let newT = bitsToPauli (tx', tz')
                        // Determine the phase: CNOT · (P_c ⊗ P_t) · CNOT† = phase · (P_c' ⊗ P_t').
                        // In the convention I=(0,0), X=(1,0), Y=(1,1), Z=(0,1) — where the
                        // Hermitian letter equals i^{xz} X^x Z^z — the only two-qubit Paulis that
                        // pick up a −1 under CNOT are XZ → −YY and YY → −XZ. Enumerating all 16
                        // cases, the sign is −1 exactly when the control carries an X-part, the
                        // target carries a Z-part, and the target X-bit equals the control Z-bit:
                        //   XZ: (cx,cz,tx,tz)=(1,0,0,1), tx=cz=0 → −1
                        //   YY: (cx,cz,tx,tz)=(1,1,1,1), tx=cz=1 → −1
                        //   XY: (1,0,1,1), tx≠cz → +1     YZ: (1,1,0,1), tx≠cz → +1
                        // (The earlier rule `cx && tz` wrongly flipped XY→−YZ and YZ→−XY.)
                        if cx && tz && (tx = cz) then
                            coeff <- coeff * Complex(-1.0, 0.0)
                        ignore (newC, newT)

                    // Update currentOps to reflect the gate (track the running Pauli)
                    let svCurrent = { X = Array.init n (fun i -> fst (pauliToBits currentOps.[i]))
                                      Z = Array.init n (fun i -> snd (pauliToBits currentOps.[i]))
                                      N = n }
                    let svNext = applyGateToSymplectic gate svCurrent
                    currentOps <- Array.init n (fun i -> bitsToPauli (svNext.X.[i], svNext.Z.[i]))

                PauliRegister(newOps, coeff))
            |> PauliRegisterSequence

        transformedTerms


    // ── Phase 4: Unified tapering pipeline ────────────────────────────

    /// <summary>Tapering method selection.</summary>
    type TaperingMethod =
        /// <summary>v1: only single-qubit Z generators (diagonal symmetries).</summary>
        | DiagonalOnly
        /// <summary>v2: full Clifford rotation for general Z₂ generators.</summary>
        | FullClifford

    /// <summary>Options for the unified tapering pipeline.</summary>
    type TaperingOptions =
        { /// <summary>Explicit sector; if empty, uses +1 for all generators.</summary>
          Sector : (int * int) list
          /// <summary>Maximum qubits to remove (None = remove all possible).</summary>
          MaxQubitsToRemove : int option
          /// <summary>Tapering method.</summary>
          Method : TaperingMethod }

    /// <summary>Defaults: +1 sector, no cap, full Clifford.</summary>
    let defaultTaperingOptions =
        { Sector = []; MaxQubitsToRemove = None; Method = FullClifford }

    /// <summary>
    /// Result of the unified tapering pipeline (extends v1 result).
    /// </summary>
    type TaperingResult =
        { OriginalQubitCount : int
          TaperedQubitCount  : int
          RemovedQubits      : int[]
          Sector             : (int * int) list
          Generators         : PauliRegister[]
          CliffordGates      : CliffordGate list
          TargetQubits       : int[]
          Hamiltonian        : PauliRegisterSequence }

    /// <summary>
    /// Unified tapering pipeline: detect Z₂ symmetries, synthesize Clifford rotation,
    /// apply it, then taper the rotated Hamiltonian diagonally.
    /// </summary>
    let taper (options : TaperingOptions) (hamiltonian : PauliRegisterSequence) : TaperingResult =
        match options.Method with
        | DiagonalOnly ->
            // Fall back to v1
            let sector =
                if options.Sector.Length > 0 then options.Sector
                else
                    diagonalZ2SymmetryQubits hamiltonian
                    |> Array.map (fun i -> (i, 1))
                    |> Array.toList

            let sector' =
                match options.MaxQubitsToRemove with
                | Some max -> sector |> List.truncate max
                | None -> sector

            let v1result = taperDiagonalZ2 sector' hamiltonian
            { OriginalQubitCount = v1result.OriginalQubitCount
              TaperedQubitCount  = v1result.TaperedQubitCount
              RemovedQubits      = v1result.RemovedQubits
              Sector             = v1result.Sector
              Generators         = diagonalZ2Generators hamiltonian
              CliffordGates      = []
              TargetQubits       = v1result.RemovedQubits
              Hamiltonian        = v1result.Hamiltonian }

        | FullClifford ->
            let n = qubitCountOf hamiltonian

            // 1. Find all commuting generators
            let allGens = findCommutingGenerators hamiltonian
            // 2. Select independent subset
            let indGens = independentGenerators allGens

            let indGens' =
                match options.MaxQubitsToRemove with
                | Some max -> indGens |> Array.truncate max
                | None -> indGens

            if indGens'.Length = 0 then
                { OriginalQubitCount = n
                  TaperedQubitCount  = n
                  RemovedQubits      = [||]
                  Sector             = []
                  Generators         = [||]
                  CliffordGates      = []
                  TargetQubits       = [||]
                  Hamiltonian        = hamiltonian }
            else

            // 3. Synthesize Clifford to rotate generators onto single-qubit Zs
            let (gates, targets) = synthesizeTaperingClifford indGens'

            // 4. Apply Clifford to the Hamiltonian
            let rotatedH = applyClifford gates hamiltonian

            // 5. Build sector for the target qubits
            let sector =
                if options.Sector.Length > 0 then
                    options.Sector
                else
                    targets |> Array.map (fun t -> (t, 1)) |> Array.toList

            // 6. Diagonal taper the rotated Hamiltonian on the target qubits
            let v1result = taperDiagonalZ2 sector rotatedH

            let generators = indGens' |> Array.map fromSymplectic

            { OriginalQubitCount = n
              TaperedQubitCount  = v1result.TaperedQubitCount
              RemovedQubits      = v1result.RemovedQubits
              Sector             = v1result.Sector
              Generators         = generators
              CliffordGates      = gates
              TargetQubits       = targets
              Hamiltonian        = v1result.Hamiltonian }
