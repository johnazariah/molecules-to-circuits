#!/usr/bin/env python3
"""Bounded solver/staging negative controls; accepted chemistry is never edited."""
import importlib.util
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "code"))
from numerical_integrity import finite, output_batch


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


for value in (float("nan"), float("inf")):
    try:
        finite(value, "negative control")
    except RuntimeError:
        pass
    else:
        raise RuntimeError("Nonfinite numerical result accepted")

with tempfile.TemporaryDirectory(prefix="book-generation-") as temporary:
    root = Path(temporary)
    shutil.copytree(ROOT / "code", root / "code")
    shutil.copytree(ROOT / "manuscript/figures", root / "manuscript/figures")
    before = {str(p.relative_to(root)): p.read_bytes() for p in root.rglob("*") if p.is_file()}
    env = {**os.environ, "OMP_NUM_THREADS": "1", "OPENBLAS_NUM_THREADS": "1",
           "MKL_NUM_THREADS": "1", "BOOK_SCF_MAX_CYCLE": "0"}
    for script in ("ch18-generate-h2-integrals.py", "ch18-dissociation-scan.py", "ch19-bond-angle-scan.py"):
        result = subprocess.run([sys.executable, str(root / "code" / script)],
                                env=env, capture_output=True, text=True, timeout=90)
        if result.returncode == 0 or "solver did not converge" not in result.stderr:
            raise RuntimeError(f"{script}: did not reject nonconvergent RHF\n{result.stdout}\n{result.stderr}")
        for relative, data in before.items():
            if (root / relative).read_bytes() != data:
                raise RuntimeError(f"Nonconvergent run changed {relative}")
        print(f"Nonconvergence rejected without promotion: {script}")

    water = load("water_stage_test", root / "code/ch19-bond-angle-scan.py")
    water.SCRIPT_DIR = str(root / "code")
    # Let a complete coarse CSV be staged, then fail the fine pass.
    coarse = [(float(a), 9.0, -74.0, -75.0) for a in range(60, 185, 5)]
    with patch.object(water, "scan", side_effect=[coarse, RuntimeError("forced fine-pass failure")]):
        try:
            water.main()
        except RuntimeError as error:
            if "forced fine-pass failure" not in str(error):
                raise
        else:
            raise RuntimeError("Fine-pass failure was swallowed")
    for relative, data in before.items():
        if (root / relative).read_bytes() != data:
            raise RuntimeError(f"Fine-pass failure changed {relative}")
    print("Late water scan failure leaves the complete accepted batch unchanged.")
    (root / "first").write_text("accepted first")
    (root / "second").write_text("accepted second")
    real_replace = os.replace
    calls = 0

    def fail_second(source, destination):
        global calls
        calls += 1
        if calls == 2:
            raise OSError("forced second-promotion failure")
        return real_replace(source, destination)

    try:
        with patch("numerical_integrity.os.replace", side_effect=fail_second):
            with output_batch(root, ["first", "second"]) as stage:
                (stage / "first").write_text("new first")
                (stage / "second").write_text("new second")
    except OSError:
        pass
    else:
        raise RuntimeError("Promotion failure was swallowed")
    if (root / "first").read_text() != "accepted first" or (root / "second").read_text() != "accepted second":
        raise RuntimeError("Batch rollback did not restore accepted bytes")
    print("I/O promotion failure rolls back already promoted files.")
