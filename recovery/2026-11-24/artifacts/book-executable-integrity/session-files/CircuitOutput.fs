namespace Encodings

open System
open System.Text.Json
open Encodings.Trotterization

/// <summary>
/// Renders Gate arrays into OpenQASM (2.0 and 3.0), Q#, and JSON circuit formats.
/// </summary>
/// <remarks>
/// Uses a <c>CodeFragment</c> computation expression for structured text
/// generation. All functions are pure — no side effects.
/// </remarks>
module CircuitOutput =

    // ── CodeFragment CE ─────────────────────────────────────────────

    /// <summary>Lines of generated code — the basic building block for all backends.</summary>
    type CodeFragment = CodeFragment of string list

    /// <summary>Combinators for building and transforming code fragments.</summary>
    module CodeFragment =
        /// <summary>An empty fragment with no lines.</summary>
        let empty = CodeFragment []

        /// <summary>A fragment containing a single line.</summary>
        let line s = CodeFragment [s]

        /// <summary>A fragment containing a single blank line.</summary>
        let blank = CodeFragment [""]

        /// <summary>Build a fragment from a list of strings.</summary>
        let ofList xs = CodeFragment xs

        /// <summary>Extract the raw string list from a fragment.</summary>
        let toLines (CodeFragment lines) = lines

        /// <summary>Indent every non-empty line by four spaces.</summary>
        let indent (CodeFragment lines) =
            CodeFragment (lines |> List.map (fun s -> if s = "" then "" else "    " + s))

        /// <summary>Concatenate a list of fragments into one.</summary>
        let concat (fragments : CodeFragment list) =
            fragments |> List.collect toLines |> CodeFragment

        /// <summary>Join all lines with newlines into a single string.</summary>
        let render (CodeFragment lines) = lines |> String.concat "\n"

    /// <summary>Computation expression builder for <see cref="CodeFragment"/>.</summary>
    type CodeBuilder() =
        member _.Yield(s : string) = CodeFragment [s]
        member _.Yield(()) = CodeFragment.empty
        member _.YieldFrom(cf : CodeFragment) = cf
        member _.Combine(CodeFragment a, b : unit -> CodeFragment) =
            let (CodeFragment bLines) = b()
            CodeFragment (a @ bLines)
        member _.Delay(f) = f
        member _.Run(f : unit -> CodeFragment) = f()
        member _.Zero() = CodeFragment.empty
        member _.For(xs : 'a seq, f : 'a -> CodeFragment) =
            xs |> Seq.map f |> Seq.toList |> CodeFragment.concat

    /// <summary>Computation expression instance for building code fragments.</summary>
    let code = CodeBuilder()

    // ── OpenQASM ──────────────────────────────────────────────────────

    /// <summary>OpenQASM language version.</summary>
    /// <remarks>
    /// QASM 2.0 is required by Quokka, older Qiskit, and many hardware backends.
    /// QASM 3.0 is the modern standard with richer control flow and typing.
    /// Gate names (h, cx, rz, s, sdg) are identical across both versions.
    /// </remarks>
    [<RequireQualifiedAccess>]
    type QasmVersion =
        /// <summary>OpenQASM 2.0 — uses <c>qreg</c> declarations and <c>qelib1.inc</c>.</summary>
        | V2
        /// <summary>OpenQASM 3.0 — uses <c>qubit[]</c> declarations and <c>stdgates.inc</c>.</summary>
        | V3

    /// <summary>Options controlling OpenQASM output.</summary>
    type OpenQasmOptions =
        { /// <summary>Whether to emit the OPENQASM header and include directive.</summary>
          IncludeHeader : bool
          /// <summary>Name of the qubit register.</summary>
          QubitName : string
          /// <summary>Number of decimal places for floating-point angles.</summary>
          Precision : int
          /// <summary>OpenQASM version (2.0 or 3.0). Default: V3.</summary>
          Version : QasmVersion }

    /// <summary>Sensible defaults for OpenQASM 3.0 generation.</summary>
    let defaultOpenQasmOptions =
        { IncludeHeader = true; QubitName = "q"; Precision = 6; Version = QasmVersion.V3 }

    /// <summary>Defaults for OpenQASM 2.0 (Quokka-compatible).</summary>
    let defaultOpenQasm2Options =
        { IncludeHeader = true; QubitName = "q"; Precision = 6; Version = QasmVersion.V2 }

    /// Render a single gate to QASM (syntax is identical across 2.0 and 3.0).
    let private gateToQasm (opts : OpenQasmOptions) (gate : Gate) =
        let q = opts.QubitName
        let fmt (a : float) = a.ToString($"F{opts.Precision}")
        match gate with
        | Gate.H i        -> $"h {q}[{i}];"
        | Gate.S i        -> $"s {q}[{i}];"
        | Gate.Sdg i      -> $"sdg {q}[{i}];"
        | Gate.Rz(i, a)   -> $"rz({fmt a}) {q}[{i}];"
        | Gate.CNOT(c, t) -> $"cx {q}[{c}], {q}[{t}];"

    /// <summary>Render a gate array to an OpenQASM program (2.0 or 3.0).</summary>
    /// <param name="opts">Formatting options including version selection.</param>
    /// <param name="numQubits">Number of qubits in the register declaration.</param>
    /// <param name="gates">Gate sequence to render.</param>
    let toOpenQasm (opts : OpenQasmOptions) (numQubits : int) (gates : Gate[]) =
        code {
            if opts.IncludeHeader then
                match opts.Version with
                | QasmVersion.V2 ->
                    "OPENQASM 2.0;"
                    "include \"qelib1.inc\";"
                | QasmVersion.V3 ->
                    "OPENQASM 3.0;"
                    "include \"stdgates.inc\";"
                ""
            match opts.Version with
            | QasmVersion.V2 -> $"qreg {opts.QubitName}[{numQubits}];"
            | QasmVersion.V3 -> $"qubit[{numQubits}] {opts.QubitName};"
            ""
            for gate in gates do
                yield! CodeFragment.line (gateToQasm opts gate)
        }
        |> CodeFragment.render

    // ── Q# Output ───────────────────────────────────────────────────

    /// <summary>Options controlling Q# output.</summary>
    type QSharpOptions =
        { /// <summary>Q# namespace for the generated operation.</summary>
          Namespace : string
          /// <summary>Name of the generated operation.</summary>
          OperationName : string
          /// <summary>Number of decimal places for floating-point angles.</summary>
          Precision : int }

    /// <summary>Sensible defaults for Q# generation.</summary>
    let defaultQSharpOptions =
        { Namespace = "FockMap.Generated"; OperationName = "TrotterStep"; Precision = 6 }

    /// Render a single gate to Q# syntax.
    let private gateToQSharp (opts : QSharpOptions) (gate : Gate) =
        let fmt (a : float) = a.ToString($"F{opts.Precision}")
        match gate with
        | Gate.H i        -> $"H(qs[{i}]);"
        | Gate.S i        -> $"S(qs[{i}]);"
        | Gate.Sdg i      -> $"Adjoint S(qs[{i}]);"
        | Gate.Rz(i, a)   -> $"Rz({fmt a}, qs[{i}]);"
        | Gate.CNOT(c, t) -> $"CNOT(qs[{c}], qs[{t}]);"

    /// <summary>Render a gate array to a Q# operation.</summary>
    /// <param name="opts">Formatting options.</param>
    /// <param name="numQubits">Number of qubits (for documentation; the operation takes a Qubit array).</param>
    /// <param name="gates">Gate sequence to render.</param>
    let toQSharp (opts : QSharpOptions) (numQubits : int) (gates : Gate[]) =
        code {
            $"namespace {opts.Namespace} {{"
            yield! code {
                "open Microsoft.Quantum.Intrinsic;"
                ""
                $"operation {opts.OperationName}(qs : Qubit[]) : Unit is Adj + Ctl {{"
                yield! code {
                    for gate in gates do
                        yield! CodeFragment.line (gateToQSharp opts gate)
                } |> CodeFragment.indent
                "}"
            } |> CodeFragment.indent
            "}"
        }
        |> CodeFragment.render

    // ── JSON Interchange Format ─────────────────────────────────────

    /// Serialize a single gate to a JsonWriter.
    let private writeGateJson (writer : Utf8JsonWriter) (gate : Gate) =
        writer.WriteStartObject()
        match gate with
        | Gate.H i ->
            writer.WriteString("gate", "H")
            writer.WriteNumber("qubit", i)
        | Gate.S i ->
            writer.WriteString("gate", "S")
            writer.WriteNumber("qubit", i)
        | Gate.Sdg i ->
            writer.WriteString("gate", "Sdg")
            writer.WriteNumber("qubit", i)
        | Gate.Rz(i, a) ->
            writer.WriteString("gate", "Rz")
            writer.WriteNumber("qubit", i)
            writer.WriteNumber("angle", a)
        | Gate.CNOT(c, t) ->
            writer.WriteString("gate", "CNOT")
            writer.WriteNumber("control", c)
            writer.WriteNumber("target", t)
        writer.WriteEndObject()

    /// <summary>Serialize a gate array to a JSON circuit interchange format.</summary>
    /// <param name="numQubits">Number of qubits in the circuit.</param>
    /// <param name="metadata">Arbitrary key-value metadata to include.</param>
    /// <param name="gates">Gate sequence to serialize.</param>
    let toCircuitJson (numQubits : int) (metadata : Map<string, string>) (gates : Gate[]) : string =
        let options = JsonWriterOptions(Indented = true)
        use stream = new IO.MemoryStream()
        use writer = new Utf8JsonWriter(stream, options)

        writer.WriteStartObject()
        writer.WriteNumber("numQubits", numQubits)
        writer.WriteNumber("gateCount", gates.Length)

        writer.WriteStartObject("metadata")
        metadata |> Map.iter (fun k v -> writer.WriteString(k, v))
        writer.WriteEndObject()

        writer.WriteStartArray("gates")
        gates |> Array.iter (writeGateJson writer)
        writer.WriteEndArray()

        writer.WriteEndObject()
        writer.Flush()

        Text.Encoding.UTF8.GetString(stream.ToArray())

    // ── Convenience: TrotterStep helpers ────────────────────────────

    /// Extract the maximum qubit index referenced in a gate array, +1 for register size.
    let private inferNumQubits (gates : Gate[]) =
        if Array.isEmpty gates then 0
        else
            gates
            |> Array.map (fun g ->
                match g with
                | Gate.H i | Gate.S i | Gate.Sdg i | Gate.Rz(i, _) -> i
                | Gate.CNOT(c, t) -> max c t)
            |> Array.max
            |> (+) 1

    /// <summary>Render a TrotterStep to OpenQASM (2.0 or 3.0), inferring numQubits from the gates.</summary>
    /// <param name="opts">Formatting options including version selection.</param>
    /// <param name="step">The Trotter step to render.</param>
    let trotterStepToOpenQasm (opts : OpenQasmOptions) (step : TrotterStep) =
        let gates = decomposeTrotterStep step
        let n = inferNumQubits gates
        toOpenQasm opts n gates

    /// <summary>Render a TrotterStep to Q#, inferring numQubits from the gates.</summary>
    /// <param name="opts">Formatting options.</param>
    /// <param name="step">The Trotter step to render.</param>
    let trotterStepToQSharp (opts : QSharpOptions) (step : TrotterStep) =
        let gates = decomposeTrotterStep step
        let n = inferNumQubits gates
        toQSharp opts n gates
