"""Shared fail-closed numerical checks and staged chemistry output."""

from contextlib import contextmanager
import json
import os
from pathlib import Path
import tempfile

import numpy as np


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def finite(value, label):
    require(np.all(np.isfinite(value)), f"{label}: nonfinite numerical result")
    return value


def converged(solver, label, *values):
    require(bool(np.all(solver.converged)), f"{label}: solver did not converge")
    for value in values:
        finite(value, label)


def configure_solver(solver, kind):
    # Explicit overrides allow bounded nonconvergence experiments in isolated runs.
    key = f"BOOK_{kind}_MAX_CYCLE"
    if key in os.environ:
        solver.max_cycle = int(os.environ[key])
        require(solver.max_cycle >= 0, f"{key} must be nonnegative")
    return solver


def solver_settings(solver):
    return {"max_cycle": int(solver.max_cycle), "conv_tol": float(solver.conv_tol),
            "converged": bool(np.all(solver.converged))}


def compare_document(actual, expected, tolerance=5e-10, path="root", archival=False):
    """Compare schema/labels exactly and numerical values at absolute tolerance."""
    if isinstance(expected, dict):
        require(isinstance(actual, dict) and actual.keys() == expected.keys(), f"{path}: keys differ")
        for key in expected:
            # Versions are provenance, not a portable numerical acceptance test.
            if key in ("numpy_version", "pyscf_version") and not archival:
                require(isinstance(actual[key], str) and bool(actual[key]), f"{path}.{key}: missing version")
            else:
                compare_document(actual[key], expected[key], tolerance, f"{path}.{key}", archival)
    elif isinstance(expected, list):
        require(isinstance(actual, list) and len(actual) == len(expected), f"{path}: dimensions differ")
        for index, (left, right) in enumerate(zip(actual, expected)):
            compare_document(left, right, tolerance, f"{path}[{index}]", archival)
    elif isinstance(expected, (int, float)) and not isinstance(expected, bool):
        require(isinstance(actual, (int, float)) and not isinstance(actual, bool), f"{path}: expected number")
        finite([actual, expected], path)
        exact_fields = {"input_threshold", "combined_pauli_threshold", "bond_length_angstrom"}
        if isinstance(expected, int) or path.rsplit(".", 1)[-1] in exact_fields:
            require(type(actual) is type(expected) and actual == expected, f"{path}: fixed metadata type/value mismatch")
        else:
            require(abs(actual - expected) <= tolerance, f"{path}: absolute error {abs(actual-expected):.3e} exceeds {tolerance:.3e}")
    else:
        require(actual == expected, f"{path}: expected {expected!r}, got {actual!r}")


def write_json(path, document):
    Path(path).write_text(json.dumps(document, indent=2, allow_nan=False) + "\n")


@contextmanager
def output_batch(root, relative_paths):
    """Stage a complete batch; validation/solver failures never touch accepted files.

    Promotion uses atomic per-file replacement after ALL outputs exist. A caught
    I/O error restores previous bytes. This is not crash-atomic across files.
    """
    root = Path(root)
    with tempfile.TemporaryDirectory(prefix=".chemistry-stage-", dir=root) as temporary:
        stage = Path(temporary)
        for path in relative_paths:
            (stage / path).parent.mkdir(parents=True, exist_ok=True)
        yield stage
        for path in relative_paths:
            require((stage / path).is_file(), f"Incomplete output batch: {path}")
        previous = {path: (root / path).read_bytes() if (root / path).exists() else None for path in relative_paths}
        promoted = []
        try:
            for path in relative_paths:
                (root / path).parent.mkdir(parents=True, exist_ok=True)
                os.replace(stage / path, root / path)
                promoted.append(path)
        except OSError:
            for path in reversed(promoted):
                if previous[path] is None:
                    (root / path).unlink()
                else:
                    (stage / path).write_bytes(previous[path])
                    os.replace(stage / path, root / path)
            raise
