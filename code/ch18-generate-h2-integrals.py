# ══════════════════════════════════════════════════════════════
# Chapter 18 Companion: Generate H₂ integrals at multiple
# bond lengths for the dissociation curve.
# ══════════════════════════════════════════════════════════════
# Run with: python3 code/ch18-generate-h2-integrals.py
# Prereq:   python3 -m pip install -r requirements-data.txt
# Output:   code/h2_dissociation_integrals.json
#           code/h2_0.74_fixture.json
#
# This script computes the STO-3G molecular integrals for H₂
# at a range of bond lengths, in the spin-orbital physicist's
# convention that FockMap expects. The output is a JSON file
# that the F# companion script (ch18-pipeline.fsx) reads.

import hashlib
import json
from pathlib import Path

import numpy as np
from numerical_integrity import configure_solver, converged, finite, output_batch, solver_settings

try:
    import pyscf
    from pyscf import ao2mo, gto, scf

    HAS_PYSCF = True
except ImportError:
    HAS_PYSCF = False

# ── Bond lengths to scan (in Ångströms) ──
BOND_LENGTHS = [
    0.30,
    0.40,
    0.50,
    0.60,
    0.70,
    0.74,
    0.80,
    0.90,
    1.00,
    1.20,
    1.40,
    1.60,
    1.80,
    2.00,
    2.50,
    3.00,
    4.00,
    5.00,
]
INPUT_THRESHOLD = 1e-15
SCRIPT_DIR = Path(__file__).resolve().parent
CANONICAL_FIXTURE_PATH = SCRIPT_DIR / "physicist_spin_integrals.json"
CANONICAL_PROVENANCE_PATH = SCRIPT_DIR / "physicist_spin_integrals.provenance.json"
CANONICAL_SOURCE_SHA256 = (
    "6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812"
)
CANONICAL_SOURCE_BLOB = "e0477e70c0dfd35b865000bb23b7b31882b062d3"


def sha256_bytes(data):
    return hashlib.sha256(data).hexdigest()


def semantic_map_sha256(integrals):
    tensor_bytes = json.dumps(
        integrals, sort_keys=True, separators=(",", ":")
    ).encode("utf-8")
    return sha256_bytes(tensor_bytes)


def load_canonical_fixture():
    fixture_bytes = CANONICAL_FIXTURE_PATH.read_bytes()
    provenance = json.loads(CANONICAL_PROVENANCE_PATH.read_text())
    if sha256_bytes(fixture_bytes) != CANONICAL_SOURCE_SHA256:
        raise RuntimeError("Canonical physicist fixture SHA-256 does not match")
    blob_header = f"blob {len(fixture_bytes)}\0".encode("ascii")
    if hashlib.sha1(blob_header + fixture_bytes).hexdigest() != CANONICAL_SOURCE_BLOB:
        raise RuntimeError("Canonical physicist fixture git blob does not match")
    if provenance["file_sha256"] != CANONICAL_SOURCE_SHA256:
        raise RuntimeError("Canonical fixture provenance SHA-256 does not match")
    if provenance["git_blob_sha1"] != CANONICAL_SOURCE_BLOB:
        raise RuntimeError("Canonical fixture provenance git blob does not match")

    fixture = json.loads(fixture_bytes)
    counts = fixture["metadata"]["counts"]
    one_body = fixture["one_body_spin"]
    two_body = fixture["two_body_spin_physicist"]
    if counts != {"one_body": 4, "two_body": 32}:
        raise RuntimeError(f"Unexpected canonical counts: {counts}")
    if len(one_body) != 4 or len(two_body) != 32:
        raise RuntimeError("Canonical fixture entry counts do not match metadata")

    integrals = {
        f"{entry['p']},{entry['q']}": entry["value"] for entry in one_body
    }
    integrals.update(
        {
            f"{entry['p']},{entry['q']},{entry['r']},{entry['s']}": entry["value"]
            for entry in two_body
        }
    )
    return integrals, provenance


def compute_integrals(R_angstrom):
    """
    Compute STO-3G integrals for H₂ at bond length R (Å).
    Returns (Vnn, integral_map, provenance, E_HF) in spin-orbital
    physicist's convention with interleaved spin indexing.
    """
    mol = gto.M(
        atom=f"H 0 0 0; H 0 0 {R_angstrom}",
        basis="sto-3g",
        symmetry=False,
        verbose=0,
    )
    mf = configure_solver(scf.RHF(mol), "SCF")
    mf.kernel()
    converged(mf, f"H2 R={R_angstrom} RHF", mf.e_tot, mf.mo_coeff, mf.mo_energy)

    Vnn = mol.energy_nuc()
    nao = mol.nao  # 2 for STO-3G H₂

    # One-body integrals in MO basis (spatial)
    h1_spatial = mf.mo_coeff.T @ mf.get_hcore() @ mf.mo_coeff

    # Two-body integrals in MO basis (spatial, chemist's notation)
    eri_spatial = ao2mo.full(mol, mf.mo_coeff)
    eri_spatial = ao2mo.restore(1, eri_spatial, nao)  # (nao,nao,nao,nao)
    finite(Vnn, "H2 nuclear repulsion")
    finite(h1_spatial, "H2 one-body tensor")
    finite(eri_spatial, "H2 two-body tensor")

    # ── Convert to spin-orbital with interleaved indexing ──
    # Spatial orbital p → spin-orbitals 2p (α), 2p+1 (β)
    nso = 2 * nao  # 4 spin-orbitals

    integrals = {}

    # One-body: h(p_σ, q_σ) = h_spatial(p, q) if same spin, 0 otherwise
    for p in range(nao):
        for q in range(nao):
            val = float(h1_spatial[p, q])
            if abs(val) > INPUT_THRESHOLD:
                # α-α
                integrals[f"{2 * p},{2 * q}"] = val
                # β-β
                integrals[f"{2 * p + 1},{2 * q + 1}"] = val

    # Two-body: physicist's convention ⟨pq|rs⟩ = (pr|qs)_chemist
    # with spin: nonzero only if σ_p=σ_r AND σ_q=σ_s
    for p in range(nao):
        for q in range(nao):
            for r in range(nao):
                for s in range(nao):
                    val = float(eri_spatial[p, r, q, s])  # chemist→physicist
                    if abs(val) > INPUT_THRESHOLD:
                        # αα-αα
                        integrals[f"{2 * p},{2 * q},{2 * r},{2 * s}"] = val
                        # ββ-ββ
                        integrals[
                            f"{2 * p + 1},{2 * q + 1},{2 * r + 1},{2 * s + 1}"
                        ] = val
                        # αβ-αβ
                        integrals[f"{2 * p},{2 * q + 1},{2 * r},{2 * s + 1}"] = val
                        # βα-βα
                        integrals[f"{2 * p + 1},{2 * q},{2 * r + 1},{2 * s}"] = val

    provenance = {
        "rhf_mo_coefficients": mf.mo_coeff.tolist(),
        "rhf_mo_energies_Ha": mf.mo_energy.tolist(),
        "spatial_one_body_mo_Ha": h1_spatial.tolist(),
        "spatial_eri_chemist_mo_Ha": eri_spatial.tolist(),
        "spin_integrals_sha256": semantic_map_sha256(integrals),
        "rhf_solver": solver_settings(mf),
    }

    return Vnn, integrals, provenance, float(mf.e_tot)


def generate(output_root):
    if not HAS_PYSCF:
        raise SystemExit(
            "PySCF is required. Run: "
            "python3 -m pip install -r requirements-data.txt"
        )

    canonical_integrals, canonical_source = load_canonical_fixture()
    canonical_semantic_sha256 = semantic_map_sha256(canonical_integrals)
    results = {
        "_metadata": {
            "generator": "code/ch18-generate-h2-integrals.py",
            "command": "python3 code/ch18-generate-h2-integrals.py",
            "upstream_generator": "encodings-research/research/tools/generate_h2_integrals.py",
            "canonical_fixture": {
                **canonical_source,
                "vendored_file": "code/physicist_spin_integrals.json",
                "semantic_map_sha256": canonical_semantic_sha256,
            },
            "pyscf_version": pyscf.__version__,
            "numpy_version": np.__version__,
            "molecule": "H2",
            "basis": "sto-3g (PySCF built-in basis data)",
            "mean_field": "RHF canonical molecular orbitals",
            "geometry": "H 0 0 0; H 0 0 R, R in Angstrom",
            "spin_orbital_order": "interleaved: spatial p -> 2p alpha, 2p+1 beta",
            "two_body_convention": "<pq|rs>_physicist = (pr|qs)_chemist",
            "hamiltonian_contract": "0.5 sum_pqrs <pq|rs> a^p a^q a_s a_r; Vnn separate",
            "input_threshold": INPUT_THRESHOLD,
            "bond_lengths_angstrom": BOND_LENGTHS,
        }
    }
    print("Generating H₂/STO-3G integrals for the dissociation curve...")
    print()
    print(f"  {'R (Å)':>8}  {'Vnn (Ha)':>12}  {'E_HF (Ha)':>12}  {'#integrals':>10}")
    print(f"  {'─' * 8}  {'─' * 12}  {'─' * 12}  {'─' * 10}")

    for R in BOND_LENGTHS:
        Vnn, integrals, provenance, ehf = compute_integrals(R)
        if R == 0.74:
            if integrals.keys() != canonical_integrals.keys():
                raise RuntimeError("PySCF and canonical 0.74 Angstrom keys differ")
            maximum_error = max(
                abs(integrals[key] - canonical_integrals[key])
                for key in canonical_integrals
            )
            if maximum_error > 5e-10:
                raise RuntimeError(
                    "PySCF and canonical 0.74 Angstrom tensors differ: "
                    f"{maximum_error:.3e}"
                )
            provenance["local_pyscf_spin_integrals_sha256"] = provenance[
                "spin_integrals_sha256"
            ]
            provenance["canonical_max_abs_error_vs_local_pyscf"] = maximum_error
            provenance["spin_integrals_sha256"] = canonical_semantic_sha256
            integrals = canonical_integrals

        results[f"{R:.2f}"] = {
            "bond_length_angstrom": R,
            "Vnn": Vnn,
            "E_HF": ehf,
            "provenance": provenance,
            "integrals": integrals,
        }
        print(f"  {R:8.2f}  {Vnn:12.6f}  {ehf:12.6f}  {len(integrals):10d}")

    # Write to JSON
    out_path = output_root / "code/h2_dissociation_integrals.json"
    with out_path.open("w") as f:
        json.dump(results, f, indent=2, allow_nan=False)

    fixture_path = output_root / "code/h2_0.74_fixture.json"
    fixture = {
        "_metadata": results["_metadata"],
        "0.74": results["0.74"],
    }
    with fixture_path.open("w") as f:
        json.dump(fixture, f, indent=2, allow_nan=False)
        f.write("\n")

    print()
    print(f"Written to: {out_path}")
    print(f"Fixture:    {fixture_path}")
    print(f"Canonical:  {CANONICAL_FIXTURE_PATH.name} ({CANONICAL_SOURCE_SHA256})")
    print(f"Bond lengths: {len(BOND_LENGTHS)}")
    print("Use with:     dotnet fsi code/ch18-pipeline.fsx")
    print("Verify with:  python3 code/ch09-verify-h2.py")


def main():
    with output_batch(SCRIPT_DIR.parent, [
        "code/h2_dissociation_integrals.json", "code/h2_0.74_fixture.json"
    ]) as stage:
        generate(stage)


if __name__ == "__main__":
    main()
