#!/usr/bin/env python3
"""Portable scientific parity between accepted data and an isolated regeneration."""
import csv
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "code"))
from numerical_integrity import compare_document, finite, require
from quantum_reference import hermitian_eigh


def load(root, path):
    return json.loads((root/path).read_text())


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    accepted, generated = map(Path, sys.argv[1:])
    spec = importlib.util.spec_from_file_location("oracle_builder", ROOT/"code/ch09-verify-h2.py")
    builder = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(builder)
    left = load(accepted, "code/h2_dissociation_integrals.json")
    right = load(generated, "code/h2_dissociation_integrals.json")
    compare_document(right["_metadata"], left["_metadata"])
    require(left.keys() == right.keys(), "Generated H2 geometry grid differs")
    for geometry, expected in left.items():
        if geometry == "_metadata":
            continue
        actual = right[geometry]
        for field in ("bond_length_angstrom", "Vnn", "E_HF"):
            compare_document(actual[field], expected[field], 5e-9, path=field)
        for record in (actual,expected):
            values = record["integrals"]
            finite(list(values.values()), "spin integrals")
            require(builder.semantic_map_sha256(values) == record["provenance"]["spin_integrals_sha256"], "Stored tensor hash mismatch")
        require(actual["provenance"]["rhf_solver"]["converged"] is True, "Missing RHF convergence evidence")
        a,_ = builder.build_fermionic_matrix(actual["integrals"])
        b,_ = builder.build_fermionic_matrix(expected["integrals"])
        require(np.max(np.abs(a-b)) < 5e-9, f"R={geometry}: full fermionic matrix parity failed")
        require(np.max(np.abs(hermitian_eigh(a)[0]-hermitian_eigh(b)[0])) < 5e-9, "Eigenvalue parity failed")
        ca,cb = builder.decompose_paulis(a),builder.decompose_paulis(b)
        for signature in ca.keys() | cb.keys():
            require(abs(ca.get(signature,0)-cb.get(signature,0)) < 5e-9, "Full coefficient parity failed")
    compare_document(load(generated,"code/h2_0.74_oracle.json"), load(accepted,"code/h2_0.74_oracle.json"))
    for path in ("code/h2_dissociation.csv","code/h2o_bond_angle_coarse.csv","code/h2o_bond_angle_fine.csv"):
        def rows(root):
            with (root/path).open() as stream:
                return list(csv.DictReader(stream))
        a,b = rows(generated),rows(accepted)
        require(len(a)==len(b), f"{path}: row count mismatch")
        for actual,expected in zip(a,b):
            require(actual.keys()==expected.keys(), f"{path}: schema mismatch")
            for field in expected:
                tolerance = 0.0 if field in ("R_angstrom","angle_degrees") else 5e-9
                compare_document(float(actual[field]),float(expected[field]),tolerance,path=field)
    for root in (accepted,generated):
        water = load(root,"code/h2o_bond_angle_metadata.json")
        for path,digest in water["generated_files_sha256"].items():
            require(sha(root/path)==digest, f"Water provenance hash mismatch: {path}")
    left_water = load(accepted,"code/h2o_bond_angle_metadata.json")
    right_water = load(generated,"code/h2o_bond_angle_metadata.json")
    for key in left_water:
        if key not in ("generated_files_sha256","numpy_version","pyscf_version"):
            compare_document(right_water[key],left_water[key])
    for label,settings in right_water["solver_settings_by_pass"].items():
        for angle,solvers in settings.items():
            require(all(solver["converged"] is True for solver in solvers.values()), f"{label}/{angle}: unconverged")
    require(sha(accepted/"code/physicist_spin_integrals.json") == sha(generated/"code/physicist_spin_integrals.json"), "Canonical bytes changed")
    print("Portable parity passes: complete H2 matrices/coefficients/spectra, scan energies, grids and bound provenance.")
    print("PNG byte equality and regenerated floating-point hashes are deliberately not scientific tolerance tests.")


if __name__ == "__main__":
    main()
