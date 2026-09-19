module PauliMatrix

open System
open System.IO
open System.Numerics
open System.Text.Json
open Encodings

let private matrix rows = array2D rows

let private singlePauli symbol =
    match symbol with
    | 'I' ->
        matrix [
            [ Complex.One; Complex.Zero ]
            [ Complex.Zero; Complex.One ]
        ]
    | 'X' ->
        matrix [
            [ Complex.Zero; Complex.One ]
            [ Complex.One; Complex.Zero ]
        ]
    | 'Y' ->
        matrix [
            [ Complex.Zero; -Complex.ImaginaryOne ]
            [ Complex.ImaginaryOne; Complex.Zero ]
        ]
    | 'Z' ->
        matrix [
            [ Complex.One; Complex.Zero ]
            [ Complex.Zero; -Complex.One ]
        ]
    | other -> invalidArg "symbol" (sprintf "Unknown Pauli symbol %c" other)

let private kronecker (left: Complex[,]) (right: Complex[,]) =
    let leftRows = Array2D.length1 left
    let leftCols = Array2D.length2 left
    let rightRows = Array2D.length1 right
    let rightCols = Array2D.length2 right

    Array2D.init
        (leftRows * rightRows)
        (leftCols * rightCols)
        (fun row col ->
            left[row / rightRows, col / rightCols]
            * right[row % rightRows, col % rightCols])

let signatureMatrix (signature: string) =
    signature
    |> Seq.rev
    |> Seq.map singlePauli
    |> Seq.reduce kronecker

let multiply (left: Complex[,]) (right: Complex[,]) =
    let rows = Array2D.length1 left
    let inner = Array2D.length2 left
    let cols = Array2D.length2 right

    if inner <> Array2D.length1 right then
        invalidArg "right" "Matrix dimensions do not align"

    Array2D.init rows cols (fun row col ->
        let mutable value = Complex.Zero
        for index in 0 .. inner - 1 do
            value <- value + left[row, index] * right[index, col]
        value)

let identity dimension =
    Array2D.init dimension dimension (fun row col ->
        if row = col then Complex.One else Complex.Zero)

let private trace (value: Complex[,]) =
    [| for index in 0 .. Array2D.length1 value - 1 -> value[index, index] |]
    |> Array.fold (+) Complex.Zero

let hamiltonianMatrix (hamiltonian: PauliRegisterSequence) =
    for term in hamiltonian.DistributeCoefficient.SummandTerms do
        if not (Double.IsFinite term.Coefficient.Real && Double.IsFinite term.Coefficient.Imaginary) then
            invalidArg "hamiltonian" "Nonfinite Pauli coefficient"
    let terms =
        hamiltonian.DistributeCoefficient.SummandTerms
        |> Array.filter (fun term -> Complex.Abs term.Coefficient > 1e-12)

    if terms.Length = 0 then
        invalidArg "hamiltonian" "Hamiltonian has no nonzero terms"

    let dimension = pown 2 terms[0].Signature.Length
    let result = Array2D.zeroCreate<Complex> dimension dimension

    for term in terms do
        if term.Signature.Length <> terms[0].Signature.Length then
            invalidArg "hamiltonian" "Inconsistent Pauli dimensions"
        let pauli = signatureMatrix term.Signature
        for row in 0 .. dimension - 1 do
            for col in 0 .. dimension - 1 do
                result[row, col] <- result[row, col] + term.Coefficient * pauli[row, col]

    result

let spectralMoments (value: Complex[,]) =
    let dimension = Array2D.length1 value
    let mutable power = identity dimension

    [|
        for exponent in 1 .. dimension do
            power <- multiply power value
            yield trace power
    |]

let hermitianEigenvaluesAtTolerance accuracy (value: Complex[,]) =
    if not (Double.IsFinite accuracy) || accuracy <= 0.0 then
        invalidArg "accuracy" "Expected a positive finite absolute accuracy"
    let n = Array2D.length1 value
    if n = 0 || Array2D.length2 value <> n then
        invalidArg "value" "Expected a nonempty square matrix"
    for z in value |> Seq.cast<Complex> do
        if not (Double.IsFinite z.Real && Double.IsFinite z.Imaginary) then
            invalidArg "value" "Matrix contains a nonfinite entry"
    let scale = max 1.0 (value |> Seq.cast<Complex> |> Seq.map Complex.Abs |> Seq.max)
    if Math.BitIncrement(scale) - scale > accuracy / 8.0 then
        failwithf "Requested absolute eigensolver accuracy %.3e is below arithmetic resolution at matrix scale %.3e" accuracy scale
    let mutable hermiticityDefect = 0.0
    for i in 0 .. n - 1 do
        for j in 0 .. n - 1 do
            hermiticityDefect <-
                Double.Hypot(hermiticityDefect,
                    Complex.Abs(value[i,j] - Complex.Conjugate(value[j,i])))
    let inputError = hermiticityDefect / 2.0
    if not (Double.IsFinite inputError) || inputError > accuracy / 16.0 then
        invalidArg "value" "Global Hermiticity defect exceeds the input accuracy budget"
    let hermitian = Array2D.init n n (fun i j ->
        0.5 * value[i,j] + 0.5 * Complex.Conjugate(value[j,i]))

    // Realification preserves the COMPLETE complex operator; every eigenvalue
    // occurs twice. This is not diagonalisation of the real part of H.
    let size = 2 * n
    let a = Array2D.init size size (fun i j ->
        let z = hermitian[i % n, j % n]
        if (i < n) = (j < n) then z.Real
        elif i < n then -z.Imaginary else z.Imaginary)
    let norm = a |> Seq.cast<float> |> Seq.fold (fun acc x -> Double.Hypot(acc, x)) 0.0
    let unitRoundoff = (Math.BitIncrement(1.0) - 1.0) / 2.0
    let mutable rotations = 0
    // Budget accumulated floating-point work, not just the largest input ULP.
    // The Frobenius norm bounds every intermediate exact orthogonal similarity.
    let roundoffBudget () =
        let operations = 64.0 * (float size + float rotations)
        let accumulated = operations * unitRoundoff
        if accumulated >= 0.5 then Double.PositiveInfinity
        else accumulated / (1.0 - accumulated) * norm
    let requireAccuracy () =
        let bound = roundoffBudget () + inputError
        if not (Double.IsFinite bound) || bound > accuracy / 2.0 then
            failwithf "Cannot certify absolute eigenvalue accuracy %.3e: input/roundoff budget %.3e" accuracy bound
    requireAccuracy ()
    let mutable residual = Double.PositiveInfinity
    let mutable sweeps = 0
    let residualTarget = accuracy / 16.0
    let entryCutoff = residualTarget / float size
    while residual > residualTarget && sweeps < 100 do
        for p in 0 .. size - 2 do
            for q in p + 1 .. size - 1 do
                let apq = a[p,q]
                if abs apq > entryCutoff then
                    rotations <- rotations + 1
                    requireAccuracy ()
                    let tau = (a[q,q] - a[p,p]) / (2.0 * apq)
                    if not (Double.IsFinite tau) then failwith "Jacobi rotation exceeds finite arithmetic range"
                    let root = Double.Hypot(1.0, tau)
                    let t = (if tau >= 0.0 then 1.0 else -1.0) / (abs tau + root)
                    let c = 1.0 / sqrt (1.0 + t*t)
                    let s = t*c
                    a[p,p] <- a[p,p] - t*apq
                    a[q,q] <- a[q,q] + t*apq
                    a[p,q] <- 0.0
                    a[q,p] <- 0.0
                    for k in 0 .. size - 1 do
                        if k <> p && k <> q then
                            let kp, kq = a[k,p], a[k,q]
                            a[k,p] <- c*kp - s*kq
                            a[p,k] <- a[k,p]
                            a[k,q] <- s*kp + c*kq
                            a[q,k] <- a[k,q]
        residual <-
            [| for i in 0 .. size - 1 ->
                [| for j in 0 .. size - 1 do if i <> j then yield abs a[i,j] |] |> Array.sum |]
            |> Array.max
        sweeps <- sweeps + 1
    if not (Double.IsFinite residual) || residual > residualTarget then
        failwithf "Hermitian eigensolver failed to converge: off-diagonal row-sum %.3e" residual
    requireAccuracy ()
    let duplicated = [| for i in 0 .. size - 1 -> a[i,i] |] |> Array.sort
    Array.init n (fun i ->
        if abs (duplicated[2*i] - duplicated[2*i+1]) > accuracy / 4.0 then
            failwith "Realified Hermitian eigenvalue pairing failed"
        (duplicated[2*i] + duplicated[2*i+1]) / 2.0)

let hermitianEigenvalues value = hermitianEigenvaluesAtTolerance 1e-11 value

let assertSpectrumMatrix name tolerance (expected: float[]) (value: Complex[,]) =
    if not (Double.IsFinite tolerance) || tolerance <= 0.0 then
        invalidArg "tolerance" "Expected a positive finite eigenvalue tolerance"
    if expected.Length <> Array2D.length1 value || expected |> Array.exists (Double.IsFinite >> not) then
        invalidArg "expected" "Spectrum dimension or finiteness mismatch"
    let actual = hermitianEigenvaluesAtTolerance (tolerance / 4.0) value
    let maximumError = Array.map2 (fun x y -> abs (x-y)) actual (Array.sort expected) |> Array.max
    let comparisonBound = maximumError + tolerance / 4.0
    if comparisonBound > tolerance then
        failwithf "%s eigenvalue mismatch: max sorted error %.3e Ha exceeds %.3e Ha" name maximumError tolerance
    printfn "  %-25s eigenvalues pass (computed error %.3e Ha; including solver allowance %.3e Ha)" name maximumError comparisonBound

let private oraclePath =
    Path.Combine(__SOURCE_DIRECTORY__, "..", "code", "h2_0.74_oracle.json")

if not (File.Exists(oraclePath)) then
    failwith "Missing committed H2 oracle. Restore it, or explicitly regenerate with python3 code/ch09-verify-h2.py --write"

let private oracleDocument = JsonDocument.Parse(File.ReadAllText(oraclePath))
let private oracleRoot = oracleDocument.RootElement
let private spectra =
    oracleRoot.GetProperty("spectra_Ha_by_particle_number")

let private oracleMatrix =
    let coefficients = oracleRoot.GetProperty("coefficients_Ha")
    let result = Array2D.zeroCreate<Complex> 16 16

    for property in coefficients.EnumerateObject() do
        let pauli = signatureMatrix property.Name
        let coefficient = Complex(property.Value.GetDouble(), 0.0)
        for row in 0 .. 15 do
            for col in 0 .. 15 do
                result[row, col] <- result[row, col] + coefficient * pauli[row, col]

    result

let h2ElectronicSpectrum =
    [|
        for electronCount in 0 .. 4 do
            let values = spectra.GetProperty(string electronCount)
            for value in values.EnumerateArray() do
                yield value.GetDouble()
    |]

let assertH2JwMatrix name tolerance hamiltonian =
    let actual = hamiltonianMatrix hamiltonian
    hermitianEigenvalues actual |> ignore
    if Array2D.length1 actual <> 16 then
        invalidArg "hamiltonian" "H2 oracle requires a 16x16 matrix"
    let mutable maximumError = 0.0

    for row in 0 .. 15 do
        for col in 0 .. 15 do
            maximumError <-
                max maximumError (Complex.Abs(actual[row, col] - oracleMatrix[row, col]))

    if maximumError > tolerance then
        failwithf
            "%s dense matrix mismatch: max error %.3e exceeds %.3e"
            name
            maximumError
            tolerance

    let acceptanceAnchors = oracleRoot.GetProperty("acceptance_anchors")
    let expectedHfDiagonal =
        acceptanceAnchors.GetProperty("hf_electronic_matrix_diagonal_row_3_Ha").GetDouble()

    if abs (actual[3, 3].Real - expectedHfDiagonal) > tolerance then
        failwithf "%s HF row-3 diagonal mismatch" name

    printfn "  %-25s dense matrix passes (max error %.3e)" name maximumError

let assertH2Spectrum name tolerance hamiltonian =
    let value = hamiltonianMatrix hamiltonian
    assertSpectrumMatrix name tolerance h2ElectronicSpectrum value
    let actual = spectralMoments value
    let expected =
        [|
            for exponent in 1 .. h2ElectronicSpectrum.Length ->
                h2ElectronicSpectrum |> Array.sumBy (fun value -> pown value exponent)
        |]

    let mutable maximumRelativeError = 0.0

    for index in 0 .. expected.Length - 1 do
        let scale = max 1.0 (abs expected[index])
        let relativeError =
            Complex.Abs(actual[index] - Complex(expected[index], 0.0)) / scale
        maximumRelativeError <- max maximumRelativeError relativeError

    printfn "  %-25s moment diagnostic only (max relative error %.3e)" name maximumRelativeError
