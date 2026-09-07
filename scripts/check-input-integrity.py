#!/usr/bin/env python3
"""Actual FSI loader rejects corrupt grids, provenance, tensors and water data."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix="book-input-") as temporary:
    root = Path(temporary)
    for path in ("code", "labs", "manuscript/figures"):
        shutil.copytree(ROOT / path, root / path)
    integral_path = root / "code/h2_dissociation_integrals.json"
    original = integral_path.read_bytes()
    cases = [
        ("missing geometry", lambda d: d.pop("5.00")),
        ("wrong geometry", lambda d: d["1.40"].update(bond_length_angstrom=1.41)),
        ("wrong ordering", lambda d: d["_metadata"].update(spin_orbital_order="blocked")),
        ("changed one-body", lambda d: d["0.74"]["integrals"].update({"0,0":-1.25,"1,1":-1.2566195732919546})),
        ("changed raw provenance", lambda d: d["1.40"]["provenance"]["spatial_one_body_mo_Ha"][0].__setitem__(0, 123.0)),
    ]
    def rejects(command, label):
        result = subprocess.run(command, cwd=root, text=True, capture_output=True, timeout=120)
        if result.returncode == 0:
            raise RuntimeError(f"False acceptance: {label}")
        if "error FS" in result.stderr:
            raise RuntimeError(f"Compile failure instead of integrity rejection: {result.stderr}")
        print(f"Rejected: {label}")
    for label, mutate in cases:
        data = json.loads(original)
        mutate(data)
        integral_path.write_text(json.dumps(data))
        rejects(["dotnet", "fsi", "code/ch03-spin-orbitals.fsx"], label)
    integral_path.write_bytes(original)
    provenance = root / "code/physicist_spin_integrals.provenance.json"
    data = json.loads(provenance.read_text())
    data["source_commit"] = "wrong-source"
    provenance.write_text(json.dumps(data))
    rejects(["dotnet", "fsi", "code/ch18-pipeline.fsx"], "wrong immutable source")
    if (root / "_build/data/h2_circuit_costs.csv").exists():
        raise RuntimeError("Invalid input produced a circuit-cost artifact")
    water = root / "code/h2o_bond_angle_fine.csv"
    lines = water.read_text().splitlines()
    lines[2] = lines[1]
    water.write_text("\n".join(lines) + "\n")
    rejects(["dotnet", "fsi", "labs/08-h2o-workshop.fsx"], "duplicate water angle")
