#!/usr/bin/env python3
"""Toy H2 VQE and ideal controlled-evolution QPE, not a hardware resource claim.

python3 code/ch20-vqe-qpe.py [--phase-bits 12]
Uses NumPy/SciPy; the accepted canonical oracle is read-only.
"""
import argparse
import json
from pathlib import Path
import numpy as np
from scipy.optimize import minimize_scalar
from quantum_reference import hamiltonian_matrix, hermitian_eigh, hermitian_evolution, matrix_product

ROOT = Path(__file__).resolve().parents[1]


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--phase-bits", type=int, default=12, choices=range(3,17))
    args = parser.parse_args()
    oracle = json.loads((ROOT / "code/h2_0.74_oracle.json").read_text())
    full = hamiltonian_matrix(oracle["coefficients_Ha"])
    # Ordering differs deliberately from the taper helper: HF first here.
    rows = [3,12]  # displayed |1100>, |0011>
    block = full[np.ix_(rows,rows)]
    exact, _ = hermitian_eigh(block)
    require(abs(block[0,0].real + 1.831863646477506) < 1e-10, "HF row-order mismatch")
    require(abs(block[0,1].real - 0.18121046201519672) < 1e-10, "Configuration coupling sign mismatch")

    def state(angle):
        return np.array([np.cos(angle),np.sin(angle)],dtype=complex)
    def energy(angle):
        psi = state(angle)
        return float(np.vdot(psi,block @ psi).real)
    result = minimize_scalar(energy, bounds=(-np.pi/2,0), method="bounded", options={"xatol":1e-13})
    require(result.success and np.isfinite(result.fun), "VQE optimiser failed")
    require(abs(result.fun-exact[0]) < 1e-10, "VQE did not reach the closed-shell ground state")
    psi = state(result.x)
    require(np.linalg.norm(block @ psi-result.fun*psi) < 1e-7, "VQE state is not the intended eigenstate")
    require(abs(exact[0]-oracle["energies_Ha"]["fci_electronic"]) < 1e-10, "Physical FCI mismatch")
    print(f"VQE angle={result.x:.12f}; electronic energy={result.fun:.12f} Ha")
    print(f"Total energy={result.fun+oracle['energies_Ha']['nuclear_repulsion']:.12f} Ha; Vnn added once.")

    shift, time = 0.5, 1.0
    lower, upper = -2.0,0.25
    full_spectrum, _ = hermitian_eigh(full)
    require(lower <= full_spectrum[0] and full_spectrum[-1] <= upper, "Chosen spectral interval is not valid")
    interval = [(shift-upper)*time/(2*np.pi), (shift-lower)*time/(2*np.pi)]
    require(0 < interval[0] < interval[1] < 1, "Phase interval wraps or touches a branch boundary")
    m, count = args.phase_bits, 2**args.phase_bits
    joint = np.tile(psi,(count,1)) / np.sqrt(count)
    for bit in range(m):
        selected = (np.arange(count) & (1 << bit)) != 0
        power = hermitian_evolution(block-shift*np.eye(2), time*(1 << bit))
        joint[selected] = matrix_product(joint[selected], power.T)
    # The inverse QFT has negative exponents; NumPy FFT uses that convention.
    joint = np.fft.fft(joint,axis=0) / np.sqrt(count)
    probabilities = np.sum(np.abs(joint)**2,axis=1)
    require(abs(np.sum(probabilities)-1) < 1e-10, "QPE probabilities not normalised")
    phase = (shift-exact[0])*time/(2*np.pi)
    delta = phase - np.arange(count)/count
    expected = (np.sinc(count*delta)/np.sinc(delta))**2
    require(np.max(np.abs(probabilities-expected)) < 1e-8, "Controlled powers/inverse-QFT distribution mismatch")
    peak = int(np.argmax(probabilities))
    decoded = shift-2*np.pi*(peak/count)/time
    bin_width = 2*np.pi/(count*time)
    require(abs(decoded-exact[0]) <= bin_width/2+1e-12, "QPE peak is not the nearest energy bin")
    if m == 12:
        require(peak == 1534 and abs(decoded+1.8531265286165737) < 1e-12, "12-bit chapter anchor changed")
    print(f"QPE phase={phase:.15f}; peak={peak} ({peak:0{m}b}); probability={probabilities[peak]:.6f}")
    print(f"Decoded electronic energy={decoded:.12f} Ha; bin width={bin_width:.12f} Ha")
    print("The peak is a probable outcome, not a deterministic energy or a confidence guarantee.")
    print(f"Ideal powers represent {count-1} base-U applications; no compiled controlled-gate or QFT resource count is claimed.")
    identity = oracle["coefficients_Ha"]["IIII"]
    wrong_phase = (shift-(exact[0]-identity))*time/(2*np.pi)
    require(abs(wrong_phase-phase) > 0.1, "Controlled identity-phase negative control ineffective")
    output = ROOT / "_build/data/h2_vqe_qpe.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps({
        "vqe_angle": result.x, "electronic_energy_Ha": result.fun,
        "basis_rows": rows, "phase_bits": m, "shift_Ha": shift, "base_time_au": time,
        "phase": phase, "peak_integer": peak, "peak_probability": float(probabilities[peak]),
        "decoded_energy_Ha": decoded, "energy_bin_width_Ha": bin_width,
        "controlled_identity_phase_angle": -(identity-shift)*time,
        "model": "exact controlled 2x2 evolution, no noise or product-formula approximation",
    }, indent=2, allow_nan=False)+"\n")


if __name__ == "__main__":
    main()
