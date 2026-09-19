# ══════════════════════════════════════════════════════════════
# Chapter 19 Companion: H₂O Bond Angle Scan
# ══════════════════════════════════════════════════════════════
# Run with: python3 code/ch19-bond-angle-scan.py
# Prereq:   python3 -m pip install -r requirements-data.txt
# Output:   code/h2o_bond_angle_coarse.csv
#           code/h2o_bond_angle_fine.csv
#           manuscript/figures/h2o_bond_angle.png
#
# Default mode computes the STO-3G total energy (RHF + FCI)
# of H₂O as a function of the H-O-H bond angle. Two passes:
#   1. Coarse scan: 60°–180° in 5° steps
#   2. Fine scan: 95°–115° in 1° steps
# Plots both and finds the minimum.

import csv
import hashlib
import json
import os
from pathlib import Path

import numpy as np
import pyscf
from pyscf import fci, gto, scf
from numerical_integrity import configure_solver, converged, finite, output_batch, solver_settings

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
FIGURE_DIR = os.path.join(os.path.dirname(SCRIPT_DIR), "manuscript", "figures")
BOND_LENGTH = 0.9584  # O-H bond length in Ångströms (experimental)


def h2o_energy(angle_degrees):
    """Compute H₂O total energy at a given H-O-H bond angle (STO-3G, FCI)."""
    angle_rad = np.radians(angle_degrees)
    hx = BOND_LENGTH * np.sin(angle_rad / 2)
    hz = BOND_LENGTH * np.cos(angle_rad / 2)

    mol = gto.M(
        atom=f"O 0 0 0; H {hx} 0 {hz}; H {-hx} 0 {hz}",
        basis="sto-3g",
        symmetry=False,
        verbose=0,
    )
    mf = configure_solver(scf.RHF(mol), "SCF")
    mf.kernel()
    converged(mf, f"H2O angle={angle_degrees} RHF", mf.e_tot, mf.mo_coeff)

    # Full CI for exact ground-state energy
    cisolver = configure_solver(fci.FCI(mf), "FCI")
    e_fci, vector = cisolver.kernel()
    converged(cisolver, f"H2O angle={angle_degrees} FCI", e_fci, vector)
    finite(mol.energy_nuc(), "H2O nuclear repulsion")

    return mol.energy_nuc(), mf.e_tot, e_fci, {"rhf": solver_settings(mf), "fci": solver_settings(cisolver)}


def scan(angles, label, settings):
    """Run the scan and return list of (angle, Vnn, E_HF, E_FCI) tuples."""
    results = []
    print(f"\n{label}")
    print(f"  {'Angle':>6}  {'Vnn':>12}  {'E_HF':>14}  {'E_FCI':>14}")
    print(
        f"  {'──────':>6}  {'────────────':>12}  {'──────────────':>14}  {'──────────────':>14}"
    )
    for angle in angles:
        vnn, e_hf, e_fci, settings[str(angle)] = h2o_energy(angle)
        results.append((angle, vnn, e_hf, e_fci))
        print(f"  {angle:6.1f}  {vnn:12.6f}  {e_hf:14.8f}  {e_fci:14.8f}")
    return results


def write_csv(filename, results, output_root):
    """Write results to CSV."""
    path = output_root / "code" / filename
    with open(path, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["angle_degrees", "Vnn_Ha", "E_HF_Ha", "E_FCI_Ha"])
        for angle, vnn, e_hf, e_fci in results:
            writer.writerow(
                [f"{angle:.1f}", f"{vnn:.10f}", f"{e_hf:.10f}", f"{e_fci:.10f}"]
            )
    print(f"  Written to: {filename}")
    return path


def file_sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as stream:
        for chunk in iter(lambda: stream.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def write_metadata(coarse_path, fine_path, plot_path, output_root, settings):
    generated_files = {
        os.path.relpath(coarse_path, output_root): file_sha256(
            coarse_path
        ),
        os.path.relpath(fine_path, output_root): file_sha256(
            fine_path
        ),
    }
    if plot_path and os.path.exists(plot_path):
        generated_files[
            os.path.relpath(plot_path, output_root)
        ] = file_sha256(plot_path)

    metadata = {
        "generator": "code/ch19-bond-angle-scan.py",
        "command": "python3 code/ch19-bond-angle-scan.py",
        "pyscf_version": pyscf.__version__,
        "numpy_version": np.__version__,
        "molecule": "H2O",
        "basis": "sto-3g (PySCF built-in basis data)",
        "method": "RHF canonical molecular orbitals followed by PySCF FCI",
        "charge": 0,
        "spin_2S": 0,
        "fixed_OH_bond_length_angstrom": BOND_LENGTH,
        "geometry": "O at origin; H atoms symmetric about z axis",
        "coarse_grid_degrees": list(range(60, 185, 5)),
        "fine_grid_degrees": list(range(95, 116)),
        "generated_files_sha256": generated_files,
        "solver_settings_by_pass": settings,
    }
    metadata_path = output_root / "code/h2o_bond_angle_metadata.json"
    with open(metadata_path, "w") as stream:
        json.dump(metadata, stream, indent=2, allow_nan=False)
        stream.write("\n")
    print("  Written to: h2o_bond_angle_metadata.json")


def generate(output_root):
    settings = {"coarse": {}, "fine": {}}
    # ── Pass 1: Coarse scan ──
    coarse_angles = list(range(60, 185, 5))  # 60, 65, ..., 180
    coarse = scan(coarse_angles, "Pass 1: Coarse scan (60°–180°, 5° steps)", settings["coarse"])
    coarse_path = write_csv("h2o_bond_angle_coarse.csv", coarse, output_root)

    # Find approximate minimum
    min_idx = min(range(len(coarse)), key=lambda i: coarse[i][3])
    coarse_min_angle = coarse[min_idx][0]
    print(f"  Coarse minimum near: {coarse_min_angle:.0f}°")

    # ── Pass 2: Fine scan ──
    fine_angles = [float(a) for a in range(95, 116)]  # 95, 96, ..., 115
    fine = scan(fine_angles, "Pass 2: Fine scan (95°–115°, 1° steps)", settings["fine"])
    fine_path = write_csv("h2o_bond_angle_fine.csv", fine, output_root)

    # Find precise minimum
    min_idx_fine = min(range(len(fine)), key=lambda i: fine[i][3])
    fine_min_angle = fine[min_idx_fine][0]
    fine_min_energy = fine[min_idx_fine][3]
    print(f"  Fine minimum at: {fine_min_angle:.0f}°  E = {fine_min_energy:.8f} Ha")

    # ── Plot ──
    plot_path = None
    try:
        import matplotlib

        matplotlib.use("Agg")
        import matplotlib.pyplot as plt

        fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 5))
        fig.suptitle(
            f"H₂O — PySCF FCI / STO-3G; fixed O–H bond length = {BOND_LENGTH:.4f} Å"
        )

        # Coarse scan
        c_angles = [r[0] for r in coarse]
        c_energies = [r[3] for r in coarse]
        ax1.plot(
            c_angles, c_energies, "o-", color="#2563eb", markersize=4, linewidth=1.5
        )
        ax1.axvline(
            fine_min_angle,
            color="#dc2626",
            linestyle="--",
            alpha=0.5,
            label=f"Lowest sampled angle: {fine_min_angle:.0f}°\n(from the fine grid)",
        )
        ax1.set_xlabel("H–O–H bond angle (degrees)")
        ax1.set_ylabel("Total energy (Hartrees)")
        ax1.set_title("Coarse angular scan (5° steps)")
        ax1.legend()
        ax1.grid(True, alpha=0.3)

        # Fine scan
        f_angles = [r[0] for r in fine]
        f_energies = [r[3] for r in fine]
        ax2.plot(
            f_angles, f_energies, "s-", color="#059669", markersize=4, linewidth=1.5
        )
        ax2.axvline(
            fine_min_angle,
            color="#dc2626",
            linestyle="--",
            alpha=0.5,
            label=f"Lowest sampled angle: {fine_min_angle:.0f}°",
        )
        ax2.axhline(fine_min_energy, color="#dc2626", linestyle=":", alpha=0.3)
        ax2.set_xlabel("H–O–H bond angle (degrees)")
        ax2.set_ylabel("Total energy (Hartrees)")
        ax2.set_title("Fine angular scan (1° steps)")
        ax2.legend()
        ax2.grid(True, alpha=0.3)

        plt.tight_layout()
        plot_path = output_root / "manuscript/figures/h2o_bond_angle.png"
        plt.savefig(plot_path, dpi=150, bbox_inches="tight")
        plt.close(fig)
        print("\n  Plot saved to: h2o_bond_angle.png")
    except ImportError as error:
        raise RuntimeError("Plotting is required for the complete output batch; install requirements-data.txt in a venv") from error

    write_metadata(coarse_path, fine_path, plot_path, output_root, settings)

    # ── Print markdown table for the book ──
    print("\n\nMarkdown table for Chapter 19 (coarse scan, selected angles):")
    print("| Angle (°) | $E$ (Ha) | | Angle (°) | $E$ (Ha) |")
    print("|:---:|:---:|:---:|:---:|:---:|")
    # Pick representative angles for the two-column table
    left_angles = [60, 70, 80, 85, 90, 95, 100, 105]
    right_angles = [110, 115, 120, 130, 140, 150, 160, 180]
    coarse_dict = {r[0]: r[3] for r in coarse}
    for la, ra in zip(left_angles, right_angles):
        le = coarse_dict.get(la, 0)
        re = coarse_dict.get(ra, 0)
        la_str = f"**{la:.0f}**" if la == fine_min_angle else f"{la:.0f}"
        le_str = f"**{le:.6f}**" if la == fine_min_angle else f"{le:.6f}"
        print(f"| {la_str} | {le_str} | | {ra:.0f} | {re:.6f} |")

    print("\n\nMarkdown table for Chapter 19 (fine scan):")
    print("| Angle (°) | $E$ (Ha) | | Angle (°) | $E$ (Ha) |")
    print("|:---:|:---:|:---:|:---:|:---:|")
    half = len(fine) // 2 + 1
    for i in range(half):
        j = i + half
        la, le = fine[i][0], fine[i][3]
        la_str = f"**{la:.0f}**" if la == fine_min_angle else f"{la:.0f}"
        le_str = f"**{le:.8f}**" if la == fine_min_angle else f"{le:.8f}"
        if j < len(fine):
            ra, re = fine[j][0], fine[j][3]
            ra_str = f"**{ra:.0f}**" if ra == fine_min_angle else f"{ra:.0f}"
            re_str = f"**{re:.8f}**" if ra == fine_min_angle else f"{re:.8f}"
            print(f"| {la_str} | {le_str} | | {ra_str} | {re_str} |")
        else:
            print(f"| {la_str} | {le_str} | | | |")


def main():
    with output_batch(Path(SCRIPT_DIR).parent, [
        "code/h2o_bond_angle_coarse.csv", "code/h2o_bond_angle_fine.csv",
        "code/h2o_bond_angle_metadata.json", "manuscript/figures/h2o_bond_angle.png"
    ]) as stage:
        generate(stage)


if __name__ == "__main__":
    main()
