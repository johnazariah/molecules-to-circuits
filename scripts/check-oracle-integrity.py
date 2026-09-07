#!/usr/bin/env python3
"""Exercise the actual verifier CLI in isolated copies, including nonmutation."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
ENV = {**os.environ, "OMP_NUM_THREADS": "1", "OPENBLAS_NUM_THREADS": "1", "MKL_NUM_THREADS": "1"}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


with tempfile.TemporaryDirectory(prefix="book-oracle-") as temporary:
    code = Path(temporary) / "code"
    shutil.copytree(ROOT / "code", code)
    oracle = code / "h2_0.74_oracle.json"
    original = oracle.read_bytes()

    def run(*args, success):
        before = {p.name: digest(p) for p in code.glob("*.json")}
        result = subprocess.run([sys.executable, str(code / "ch09-verify-h2.py"), *args],
                                env=ENV, capture_output=True, text=True, timeout=90)
        if (result.returncode == 0) != success:
            raise RuntimeError(result.stdout + result.stderr)
        if not args and before != {p.name: digest(p) for p in code.glob("*.json")}:
            raise RuntimeError("Read-only verifier mutated a JSON artifact")
        return result

    run(success=True)
    mutations = [
        ("coefficient", lambda d: d["coefficients_Ha"].update(IIII=123.0)),
        ("spectrum", lambda d: d["spectra_Ha_by_particle_number"]["2"].__setitem__(0, 0.0)),
        ("ordering", lambda d: d["ordering_anchor"].update(hf_matrix_row=12)),
        ("metadata", lambda d: d.update(matrix_order="unreversed")),
        ("NaN", lambda d: d["coefficients_Ha"].update(IIII=float("nan"))),
        ("negative threshold", lambda d: d["acceptance_anchors"].update(input_threshold=-1e-10)),
        ("zero threshold", lambda d: d["acceptance_anchors"].update(combined_pauli_threshold=0.0)),
        ("fractional count", lambda d: d["acceptance_anchors"].update(input_one_body_entries=4.0)),
    ]
    for label, mutate in mutations:
        document = json.loads(original)
        mutate(document)
        oracle.write_text(json.dumps(document))
        result = run(success=False)
        if "RuntimeError" not in result.stderr:
            raise RuntimeError(f"{label}: failure was not the intended integrity guard")
        print(f"Rejected without mutation: {label}")
    oracle.write_bytes(original)
    regenerated = Path(temporary) / "regenerated.json"
    run("--output", str(regenerated), success=True)
    if digest(oracle) != hashlib.sha256(original).hexdigest():
        raise RuntimeError("--output mutated the committed oracle")
    if not regenerated.is_file():
        raise RuntimeError("--output did not produce an oracle")
    print("Read-only success/failure and explicit output regeneration passed.")
