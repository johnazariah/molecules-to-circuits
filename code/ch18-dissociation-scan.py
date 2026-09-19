# ══════════════════════════════════════════════════════════════
# Chapter 18 Companion: H₂ Dissociation Curve
# ══════════════════════════════════════════════════════════════
# Run with: python3 code/ch18-dissociation-scan.py
# Prereq:   python3 -m pip install -r requirements-data.txt
# Output:   code/h2_dissociation.csv
#           manuscript/figures/h2_dissociation.png
#
# Computes the STO-3G total energy (HF, FCI) of H₂ at multiple
# bond lengths and produces the dissociation curve.

import csv
import os
from pathlib import Path

from pyscf import fci, gto, scf
from numerical_integrity import configure_solver, converged, finite, output_batch, solver_settings, write_json

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
FIGURE_DIR = os.path.join(os.path.dirname(SCRIPT_DIR), "manuscript", "figures")

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


def h2_energy(R_angstrom):
    """Compute H₂ total energy at bond length R (STO-3G, FCI)."""
    mol = gto.M(
        atom=f"H 0 0 0; H 0 0 {R_angstrom}",
        basis="sto-3g",
        symmetry=False,
        verbose=0,
    )
    mf = configure_solver(scf.RHF(mol), "SCF")
    mf.kernel()
    converged(mf, f"H2 R={R_angstrom} RHF", mf.e_tot, mf.mo_coeff)

    cisolver = configure_solver(fci.FCI(mf), "FCI")
    e_fci, vector = cisolver.kernel()
    converged(cisolver, f"H2 R={R_angstrom} FCI", e_fci, vector)
    finite(mol.energy_nuc(), "H2 nuclear repulsion")

    return mol.energy_nuc(), mf.e_tot, e_fci, {"rhf": solver_settings(mf), "fci": solver_settings(cisolver)}


def separated_atom_limit():
    atom = gto.M(atom="H 0 0 0", basis="sto-3g", spin=1, verbose=0)
    # One electron in one spatial basis function: there is no electron-electron
    # term. This is the neutral-atom FCI limit, not the restricted-HF limit.
    hcore = scf.hf.get_hcore(atom)
    overlap = atom.intor("int1e_ovlp")
    energy = 2.0 * float(hcore[0, 0] / overlap[0, 0])
    finite(energy, "STO-3G separated-atom limit")
    return energy


def generate(output_root):
    results = []
    print("H₂ Dissociation Curve (STO-3G)")
    print(f"  {'R (Å)':>8}  {'Vnn':>12}  {'E_HF':>14}  {'E_FCI':>14}")
    print(
        f"  {'────────':>8}  {'────────────':>12}  {'──────────────':>14}  {'──────────────':>14}"
    )

    settings = {}
    for R in BOND_LENGTHS:
        vnn, e_hf, e_fci, settings[f"{R:.2f}"] = h2_energy(R)
        results.append((R, vnn, e_hf, e_fci))
        print(f"  {R:8.2f}  {vnn:12.6f}  {e_hf:14.8f}  {e_fci:14.8f}")

    # Write CSV
    csv_path = output_root / "code/h2_dissociation.csv"
    with open(csv_path, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["R_angstrom", "Vnn_Ha", "E_HF_Ha", "E_FCI_Ha"])
        for R, vnn, e_hf, e_fci in results:
            writer.writerow(
                [f"{R:.2f}", f"{vnn:.10f}", f"{e_hf:.10f}", f"{e_fci:.10f}"]
            )
    print(f"\n  Written to: {os.path.basename(csv_path)}")

    # Find minimum
    min_idx = min(range(len(results)), key=lambda i: results[i][3])
    min_R = results[min_idx][0]
    min_E = results[min_idx][3]
    print(f"  Minimum at R = {min_R:.2f} Å, E = {min_E:.8f} Ha")

    # Plot
    try:
        import matplotlib

        matplotlib.use("Agg")
        import matplotlib.pyplot as plt

        Rs = [r[0] for r in results]
        E_hf = [r[2] for r in results]
        E_fci = [r[3] for r in results]

        fig, ax = plt.subplots(figsize=(9, 6))
        ax.plot(
            Rs,
            E_hf,
            "s--",
            color="#9ca3af",
            markersize=4,
            linewidth=1.2,
            label="Restricted Hartree–Fock (RHF)",
        )
        ax.plot(
            Rs,
            E_fci,
            "o-",
            color="#2563eb",
            markersize=5,
            linewidth=1.5,
            label="Full CI (exact within STO-3G)",
        )
        ax.axvline(
            min_R,
            color="#dc2626",
            linestyle="--",
            alpha=0.4,
            label=f"Lowest sampled energy: R = {min_R:.2f} Å",
        )
        ax.axhline(
            -1.0,
            color="#6b7280",
            linestyle=":",
            alpha=0.3,
            label="Two H atoms, complete-basis\nnonrelativistic reference: −1.0 Ha",
        )
        atom_limit = separated_atom_limit()
        ax.axhline(
            atom_limit,
            color="#7c3aed",
            linestyle="-.",
            alpha=0.6,
            label=f"Two isolated H atoms, STO-3G: {atom_limit:.6f} Ha",
        )
        ax.set_xlabel("Bond length R (Å)")
        ax.set_ylabel("Total energy (Hartrees)")
        ax.set_title("H₂ Dissociation Curve — PySCF / STO-3G")
        ax.legend(fontsize=9, loc="upper center", bbox_to_anchor=(0.5, -0.15))
        ax.grid(True, alpha=0.3)
        ax.set_xlim(0.2, 5.2)

        plot_path = output_root / "manuscript/figures/h2_dissociation.png"
        plt.savefig(plot_path, dpi=150, bbox_inches="tight")
        plt.close(fig)
        print(f"  Plot saved to: {os.path.basename(plot_path)}")
    except ImportError as error:
        raise RuntimeError("Plotting is required for the complete output batch; install requirements-data.txt in a venv") from error
    write_json(output_root / "code/h2_dissociation_solver_metadata.json", settings)

    # Markdown table for Chapter 18
    print("\n\nMarkdown table for Chapter 18:")
    print("| $R$ (Å) | $E_\\text{FCI}$ (Ha) | | $R$ (Å) | $E_\\text{FCI}$ (Ha) |")
    print("|:---:|:---:|:---:|:---:|:---:|")
    half = len(results) // 2
    for i in range(half):
        j = i + half
        R1, E1 = results[i][0], results[i][3]
        R1s = f"**{R1:.2f}**" if R1 == min_R else f"{R1:.2f}"
        E1s = f"**{E1:.6f}**" if R1 == min_R else f"{E1:.6f}"
        if j < len(results):
            R2, E2 = results[j][0], results[j][3]
            print(f"| {R1s} | {E1s} | | {R2:.2f} | {E2:.6f} |")
        else:
            print(f"| {R1s} | {E1s} | | | |")


def main():
    with output_batch(Path(SCRIPT_DIR).parent, [
        "code/h2_dissociation.csv", "manuscript/figures/h2_dissociation.png",
        "code/h2_dissociation_solver_metadata.json"
    ]) as stage:
        generate(stage)


if __name__ == "__main__":
    main()
