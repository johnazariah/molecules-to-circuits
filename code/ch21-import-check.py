#!/usr/bin/env python3
"""Real QASM 2/3 and Q# imports, plus strict JSON-to-Qiskit translation.

Run dotnet fsi code/ch21-export-check.fsx first, then use the isolated
scripts/requirements-import.txt environment. No hardware or cloud service.
"""
import copy
from importlib.metadata import version
import json
from pathlib import Path

import numpy as np
from qiskit import QuantumCircuit, qasm2, qasm3, transpile
from qiskit.quantum_info import Operator
import qsharp

from quantum_reference import hamiltonian_matrix, pauli_matrix, matrix_product, hermitian_evolution

ROOT = Path(__file__).resolve().parents[1]
DIRECTORY = ROOT / "_build/data/export"
TOLERANCE = 1e-11
VERSIONS = {"qiskit": "2.2.3", "qiskit-qasm3-import": "0.6.0", "qsharp": "1.22.0"}


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def compare(actual, expected, label):
    require(actual.shape == expected.shape and np.all(np.isfinite(actual)), f"{label}: shape/finite failure")
    error = float(np.max(np.abs(actual - expected)))
    require(error <= TOLERANCE, f"{label}: unitary max error {error:.3e} exceeds {TOLERANCE:.3e}")
    print(f"{label}: all labelled columns agree, max error {error:.3e}")
    return error


def circuit_from_json(document):
    require(set(document) == {"numQubits", "gateCount", "metadata", "gates"}, "Unexpected circuit JSON schema")
    n = document["numQubits"]
    require(type(n) is int and 0 < n <= 10, "Invalid register width")
    require(type(document["gateCount"]) is int and document["gateCount"] == len(document["gates"]), "Gate count mismatch")
    require(isinstance(document["metadata"], dict), "Invalid circuit metadata")
    circuit = QuantumCircuit(n)
    for gate in document["gates"]:
        name = gate.get("gate")
        schemas = {"H": {"gate","qubit"}, "S": {"gate","qubit"}, "Sdg": {"gate","qubit"},
                   "Rz": {"gate","qubit","angle"}, "CNOT": {"gate","control","target"}}
        require(name in schemas, f"Unknown gate: {name!r}")
        require(set(gate) == schemas[name], f"Invalid {name} schema")
        indices = [gate["control"], gate["target"]] if name == "CNOT" else [gate["qubit"]]
        require(all(type(i) is int and 0 <= i < n for i in indices), "Invalid qubit index")
        if name == "CNOT":
            require(indices[0] != indices[1], "CNOT control equals target")
            circuit.cx(*indices)
        elif name == "Rz":
            angle = gate["angle"]
            require(type(angle) in (float,int) and np.isfinite(angle), "Nonfinite/invalid rotation angle")
            circuit.rz(angle, indices[0])
        else:
            {"H": circuit.h, "S": circuit.s, "Sdg": circuit.sdg}[name](indices[0])
    return circuit


def qsharp_unitary(source, n):
    columns = []
    reverse = [int(f"{i:0{n}b}"[::-1], 2) for i in range(2**n)]
    for column in range(2**n):
        qsharp.init()
        qsharp.eval(source)  # Compile the unmodified FockMap export, not a rewrite.
        qsharp.eval(f"use qs = Qubit[{n}];")
        for qubit in range(n):
            if column & (1 << qubit):
                qsharp.eval(f"X(qs[{qubit}]);")
        before = np.asarray(qsharp.dump_machine().as_dense_state(), dtype=complex)[reverse]
        require(np.argmax(np.abs(before)) == column, "Q# labelled basis-order anchor failed")
        qsharp.eval("FockMap.Generated.TrotterStep(qs);")
        # Q# dumps q0 leftmost (most significant); the book matrix uses bit 0.
        columns.append(np.asarray(qsharp.dump_machine().as_dense_state(), dtype=complex)[reverse])
    return np.column_stack(columns)


def main():
    for package, expected in VERSIONS.items():
        require(version(package) == expected, f"Use scripts/requirements-import.txt: expected {package}=={expected}")
    contract = json.loads((DIRECTORY / "h2.contract.json").read_text())
    oracle = json.loads((ROOT / "code/h2_0.74_oracle.json").read_text())
    n, time = contract["qubits"], contract["time_step"]
    require(n == 4 and time == 0.1 and contract["decimal_places"] == 15, "Unexpected export contract")
    rotations = contract["rotations"]
    require(len(rotations) == 15, "Expected 15 Hamiltonian factors")
    angles = {term["signature"]: term["angle"] for term in rotations}
    require(angles.keys() == oracle["coefficients_Ha"].keys(), "Export/oracle Pauli set mismatch")
    for signature, coefficient in oracle["coefficients_Ha"].items():
        require(abs(angles[signature] - time*coefficient) < 1e-12, "Export/oracle coefficient mismatch")
    expected = np.eye(2**n, dtype=complex)
    for rotation in rotations:
        if rotation["signature"] != "IIII":
            angle = rotation["angle"]
            factor = np.cos(angle)*np.eye(16)-1j*np.sin(angle)*pauli_matrix(rotation["signature"])
            expected = matrix_product(factor, expected)
    q2 = qasm2.loads((DIRECTORY / "h2.qasm2").read_text())
    q3 = qasm3.loads((DIRECTORY / "h2.qasm3").read_text())
    document = json.loads((DIRECTORY / "h2.circuit.json").read_text())
    circuits = {"QASM 2 Qiskit":q2, "QASM 3 Qiskit":q3, "JSON Qiskit":circuit_from_json(document)}
    errors = {label: compare(Operator(circuit).data, expected, label) for label,circuit in circuits.items()}
    for label,circuit in circuits.items():
        compiled = transpile(circuit, basis_gates=["rz","sx","x","cx"], optimization_level=1, seed_transpiler=7)
        compare(Operator(compiled).data, expected, f"{label} transpiled")
    errors["Q#"] = compare(qsharp_unitary((DIRECTORY / "h2.qs").read_text(), n), expected, "Q# compiled/simulated")
    bad = copy.deepcopy(document)
    bad["gates"][0]["gate"] = "NOT_A_GATE"
    try:
        circuit_from_json(bad)
    except RuntimeError as error:
        require("Unknown gate" in str(error), "Wrong unknown-gate rejection")
    else:
        raise RuntimeError("Unknown gate silently accepted")
    identity_phase = np.exp(-1j*angles["IIII"])
    full_product = identity_phase * expected
    require(np.max(np.abs(full_product - expected)) > 1e-3, "Missing identity phase negative control ineffective")
    # In a controlled block this is a relative phase, not disposable global phase.
    controlled = np.zeros((32,32),dtype=complex)
    controlled[:16,:16] = np.eye(16)
    controlled[16:,16:] = full_product
    omitted = controlled.copy()
    omitted[16:,16:] = expected
    require(np.max(np.abs(controlled-omitted)) > 1e-3, "Controlled identity-phase omission was accepted")
    restored = q2.copy()
    restored.global_phase = -angles["IIII"]
    controlled_circuit = QuantumCircuit(5)
    controlled_circuit.append(restored.to_gate().control(1), [4,0,1,2,3])
    compare(Operator(controlled_circuit).data, controlled, "Controlled imported step with restored identity phase")
    compiled_controlled = transpile(controlled_circuit, basis_gates=["u","cx"], optimization_level=1, seed_transpiler=7)
    compare(Operator(compiled_controlled).data, controlled, "Compiled controlled step")
    controlled_counts = {
        "qubits": compiled_controlled.num_qubits,
        "cnots": int(compiled_controlled.count_ops().get("cx",0)),
        "total_gates": len(compiled_controlled.data),
        "depth": compiled_controlled.depth(),
    }
    print(f"Controlled ONE step, Qiskit u/cx logical basis, no routing: {controlled_counts}")
    print("These measured controlled-step resources are not FockMap's uncontrolled proxy or a full QPE budget.")
    exact = hermitian_evolution(hamiltonian_matrix(oracle["coefficients_Ha"]), time)
    product_error = float(np.linalg.norm(full_product-exact, ord=2))
    print(f"First-order product vs exact evolution error (separate from import): {product_error:.3e}")
    print("Identity/global phase is omitted by the export and restored only for the full/controlled product.")
    print("Unknown JSON gates rejected; no silent fallback.")
    (DIRECTORY / "validation.json").write_text(json.dumps({
        "importer_versions": VERSIONS, "absolute_matrix_tolerance": TOLERANCE,
        "maximum_errors": errors, "product_formula_operator_error": product_error,
        "controlled_single_step_resources": controlled_counts,
        "controlled_compiler": "Qiskit 2.2.3; basis u,cx; optimisation 1; seed 7; no coupling map",
        "identity_phase_policy": "omitted uncontrolled; must restore for controlled U",
    }, indent=2, allow_nan=False) + "\n")


if __name__ == "__main__":
    main()
