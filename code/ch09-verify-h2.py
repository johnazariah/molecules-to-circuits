#!/usr/bin/env python3
"""Independently verify the canonical H2/STO-3G reference at 0.74 Angstrom."""

import itertools
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
import pyscf
from pyscf import ao2mo, fci, gto, scf
from numerical_integrity import compare_document, configure_solver, converged, finite, output_batch, write_json
from quantum_reference import pauli_matrix


TOLERANCE = 5e-10
PAULI_THRESHOLD = 1e-12
EXPECTED_IDENTITY = -0.8121706072
EXPECTED_HF_ELECTRONIC = -1.8318636465
EXPECTED_FOUR_BODY_MAGNITUDE = 0.0453026155
BROKEN_IDENTITY = -3.5607946918
BROKEN_FOUR_BODY_MAGNITUDE = 0.0906052310
SCRIPT_DIR = Path(__file__).resolve().parent
REFERENCE_PATH = SCRIPT_DIR / "h2_0.74_fixture.json"
ORACLE_PATH = SCRIPT_DIR / "h2_0.74_oracle.json"
CANONICAL_FIXTURE_PATH = SCRIPT_DIR / "physicist_spin_integrals.json"
CANONICAL_PROVENANCE_PATH = SCRIPT_DIR / "physicist_spin_integrals.provenance.json"
CANONICAL_SOURCE_SHA256 = (
    "6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812"
)
CANONICAL_SOURCE_BLOB = "e0477e70c0dfd35b865000bb23b7b31882b062d3"

REFERENCE_DOCUMENT = json.loads(REFERENCE_PATH.read_text())
REFERENCE_METADATA = REFERENCE_DOCUMENT["_metadata"]
REFERENCE_RECORD = REFERENCE_DOCUMENT["0.74"]
BOND_LENGTH = float(REFERENCE_RECORD["bond_length_angstrom"])

def require(condition, message):
    if not condition:
        raise RuntimeError(message)


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
    fixture_sha256 = sha256_bytes(fixture_bytes)
    blob_header = f"blob {len(fixture_bytes)}\0".encode("ascii")
    fixture_blob = hashlib.sha1(blob_header + fixture_bytes).hexdigest()
    require(
        fixture_sha256 == CANONICAL_SOURCE_SHA256,
        "Canonical physicist fixture SHA-256 does not match",
    )
    require(
        fixture_blob == CANONICAL_SOURCE_BLOB,
        "Canonical physicist fixture git blob does not match",
    )
    require(
        provenance["file_sha256"] == fixture_sha256,
        "Canonical provenance SHA-256 does not match fixture bytes",
    )
    require(
        provenance["git_blob_sha1"] == fixture_blob,
        "Canonical provenance git blob does not match fixture bytes",
    )

    fixture = json.loads(fixture_bytes)
    one_body = fixture["one_body_spin"]
    two_body = fixture["two_body_spin_physicist"]
    require(
        fixture["metadata"]["counts"] == {"one_body": 4, "two_body": 32},
        "Canonical fixture metadata counts differ",
    )
    require(
        len(one_body) == 4 and len(two_body) == 32,
        "Canonical fixture entry counts differ",
    )
    integrals = {
        f"{entry['p']},{entry['q']}": entry["value"] for entry in one_body
    }
    integrals.update(
        {
            f"{entry['p']},{entry['q']},{entry['r']},{entry['s']}": entry["value"]
            for entry in two_body
        }
    )
    return integrals, provenance, fixture_sha256, fixture_blob


def make_molecule():
    mol = gto.M(
        atom=f"H 0 0 0; H 0 0 {BOND_LENGTH}",
        basis="sto-3g",
        symmetry=False,
        verbose=0,
    )
    mean_field = configure_solver(scf.RHF(mol), "SCF").run()
    converged(mean_field, "H2 RHF", mean_field.e_tot, mean_field.mo_coeff, mean_field.mo_energy)
    return mol, mean_field


def generate_spin_integrals(mol, mean_field):
    nao = mol.nao
    h1 = mean_field.mo_coeff.T @ mean_field.get_hcore() @ mean_field.mo_coeff
    eri = ao2mo.full(mol, mean_field.mo_coeff)
    eri = ao2mo.restore(1, eri, nao)
    finite(h1, "H2 one-body tensor")
    finite(eri, "H2 two-body tensor")

    integrals = {}
    for p in range(nao):
        for q in range(nao):
            value = float(h1[p, q])
            if abs(value) > 1e-12:
                integrals[f"{2 * p},{2 * q}"] = value
                integrals[f"{2 * p + 1},{2 * q + 1}"] = value

    for p, q, r, s in itertools.product(range(nao), repeat=4):
        value = float(eri[p, r, q, s])
        if abs(value) <= 1e-12:
            continue
        integrals[f"{2 * p},{2 * q},{2 * r},{2 * s}"] = value
        integrals[
            f"{2 * p + 1},{2 * q + 1},{2 * r + 1},{2 * s + 1}"
        ] = value
        integrals[f"{2 * p},{2 * q + 1},{2 * r},{2 * s + 1}"] = value
        integrals[f"{2 * p + 1},{2 * q},{2 * r + 1},{2 * s}"] = value

    raw = {
        "rhf_mo_coefficients": mean_field.mo_coeff,
        "rhf_mo_energies_Ha": mean_field.mo_energy,
        "spatial_one_body_mo_Ha": h1,
        "spatial_eri_chemist_mo_Ha": eri,
    }
    return integrals, raw


def apply_ladder(state, mode, create):
    occupied = state[mode]
    if (create and occupied) or (not create and not occupied):
        return None

    phase = -1 if sum(state[:mode]) % 2 else 1
    result = list(state)
    result[mode] = 1 if create else 0
    return phase, tuple(result)


def apply_sequence(state, operations):
    phase = 1
    current = state
    for mode, create in operations:
        applied = apply_ladder(current, mode, create)
        if applied is None:
            return None
        step_phase, current = applied
        phase *= step_phase
    return phase, current


def basis_states(num_qubits):
    return [
        tuple((index >> mode) & 1 for mode in range(num_qubits))
        for index in range(2**num_qubits)
    ]


def build_fermionic_matrix(integrals, num_qubits=4):
    states = basis_states(num_qubits)
    index_of = {state: index for index, state in enumerate(states)}
    matrix = np.zeros((len(states), len(states)), dtype=complex)

    for key, value in integrals.items():
        indices = tuple(int(part) for part in key.split(","))
        if len(indices) == 2:
            p, q = indices
            operations = [(q, False), (p, True)]
            scale = value
        else:
            p, q, r, s = indices
            operations = [(r, False), (s, False), (q, True), (p, True)]
            scale = 0.5 * value

        for column, state in enumerate(states):
            applied = apply_sequence(state, operations)
            if applied is None:
                continue
            phase, result = applied
            matrix[index_of[result], column] += scale * phase

    return matrix, states


def occupation_integer(state):
    return sum(occupied * (2**mode) for mode, occupied in enumerate(state))


def verify_number_operators(states):
    dimension = len(states)
    identity = np.eye(dimension, dtype=complex)
    for mode in range(4):
        signature = ["I"] * 4
        signature[mode] = "Z"
        number_operator = 0.5 * (identity - pauli_matrix("".join(signature)))
        for matrix_row, state in enumerate(states):
            expected = state[mode]
            require(
                abs(number_operator[matrix_row, matrix_row] - expected) < TOLERANCE,
                f"Number operator n_{mode} fails on state {state}",
            )
            off_diagonal = np.delete(number_operator[matrix_row], matrix_row)
            require(
                np.max(np.abs(off_diagonal)) < TOLERANCE,
                f"Number operator n_{mode} is not diagonal",
            )


def decompose_paulis(matrix, num_qubits=4):
    coefficients = {}
    for symbols in itertools.product("IXYZ", repeat=num_qubits):
        signature = "".join(symbols)
        coefficient = np.vdot(pauli_matrix(signature), matrix) / (2**num_qubits)
        if abs(coefficient) > PAULI_THRESHOLD:
            require(
                abs(coefficient.imag) < TOLERANCE,
                f"Complex coefficient for {signature}: {coefficient}",
            )
            coefficients[signature] = float(coefficient.real)
    return coefficients


def sector_spectra(matrix, states):
    spectra = {}
    for electron_count in range(5):
        indices = [
            index
            for index, state in enumerate(states)
            if sum(state) == electron_count
        ]
        spectra[electron_count] = np.linalg.eigvalsh(
            matrix[np.ix_(indices, indices)]
        )
    return spectra


def anticommuting(signature_a, signature_b):
    conflicts = sum(
        a != "I" and b != "I" and a != b
        for a, b in zip(signature_a, signature_b)
    )
    return conflicts % 2 == 1


def commutator_sum(coefficients):
    terms = [
        (signature, coefficient)
        for signature, coefficient in coefficients.items()
        if signature != "IIII"
    ]
    total = 0.0
    anticommuting_pairs = 0
    for index, (signature_a, coefficient_a) in enumerate(terms):
        for signature_b, coefficient_b in terms[index + 1 :]:
            if anticommuting(signature_a, signature_b):
                total += 2 * abs(coefficient_a * coefficient_b)
                anticommuting_pairs += 1
    return total, anticommuting_pairs


def compare_maps(actual, expected, label):
    require(actual.keys() == expected.keys(), f"{label} keys differ")
    finite(list(actual.values()), label)
    finite(list(expected.values()), label)
    maximum_error = max(abs(actual[key] - expected[key]) for key in actual)
    require(maximum_error < TOLERANCE, f"{label} max error: {maximum_error}")
    return maximum_error


def reconstruct_pauli_matrix(coefficients):
    dimension = 2 ** len(next(iter(coefficients)))
    result = np.zeros((dimension, dimension), dtype=complex)
    for signature, coefficient in coefficients.items():
        result += coefficient * pauli_matrix(signature)
    return result


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--write", action="store_true", help="explicitly replace the committed oracle")
    modes.add_argument("--output", type=Path, help="write a regenerated oracle to this path")
    parser.add_argument("--archival", action="store_true", help="also require the original PySCF version and bit-identical local tensor")
    args = parser.parse_args(argv)
    metadata = REFERENCE_METADATA
    committed_record = REFERENCE_RECORD
    committed_integrals = committed_record["integrals"]
    canonical_integrals, canonical_source, canonical_sha256, canonical_blob = (
        load_canonical_fixture()
    )
    one_body_count = sum(key.count(",") == 1 for key in committed_integrals)
    two_body_count = sum(key.count(",") == 3 for key in committed_integrals)
    require(one_body_count == 4, f"Expected 4 one-body entries, found {one_body_count}")
    require(two_body_count == 32, f"Expected 32 two-body entries, found {two_body_count}")
    require(
        committed_integrals == canonical_integrals,
        "Committed 0.74 Angstrom tensor is not bit-identical to the canonical artifact",
    )
    canonical_metadata = metadata["canonical_fixture"]
    for field in ("source_repo", "source_commit", "source_path"):
        require(
            canonical_metadata[field] == canonical_source[field],
            f"Canonical source metadata differs for {field}",
        )
    require(
        canonical_metadata["git_blob_sha1"] == canonical_blob,
        "Canonical source git blob metadata differs",
    )
    require(
        canonical_metadata["file_sha256"] == canonical_sha256,
        "Canonical source file SHA-256 metadata differs",
    )
    require(
        metadata["two_body_convention"]
        == "<pq|rs>_physicist = (pr|qs)_chemist",
        "Unexpected chemist-to-physicist convention",
    )
    require(
        metadata["hamiltonian_contract"]
        == "0.5 sum_pqrs <pq|rs> a^p a^q a_s a_r; Vnn separate",
        "Unexpected Hamiltonian contract",
    )

    mol, mean_field = make_molecule()
    generated_integrals, generated_raw = generate_spin_integrals(mol, mean_field)
    integral_error = compare_maps(
        committed_integrals, generated_integrals, "Committed integral tensor"
    )
    generated_semantic_hash = semantic_map_sha256(generated_integrals)
    require(
        not args.archival or
        committed_record["provenance"]["local_pyscf_spin_integrals_sha256"]
        == generated_semantic_hash,
        "Local PySCF semantic tensor checksum differs",
    )
    require(
        not args.archival or
        abs(
            committed_record["provenance"][
                "canonical_max_abs_error_vs_local_pyscf"
            ]
            - integral_error
        )
        < np.finfo(float).eps,
        "Recorded canonical/PySCF maximum error differs",
    )
    require(
        not args.archival or
        metadata["pyscf_version"] == pyscf.__version__,
        "Committed PySCF version does not match the verification environment",
    )
    for key, generated in generated_raw.items():
        committed = np.asarray(committed_record["provenance"][key])
        require(
            np.max(np.abs(committed - generated)) < TOLERANCE,
            f"Committed raw provenance differs for {key}",
        )
    tensor_hash = semantic_map_sha256(committed_integrals)
    require(
        tensor_hash == committed_record["provenance"]["spin_integrals_sha256"],
        "Committed spin-integral checksum differs",
    )
    require(
        tensor_hash == canonical_metadata["semantic_map_sha256"],
        "Canonical semantic-map checksum differs",
    )

    hamiltonian, states = build_fermionic_matrix(committed_integrals)
    finite(hamiltonian, "Fermionic Hamiltonian")
    verify_number_operators(states)
    require(
        np.max(np.abs(hamiltonian - hamiltonian.conj().T)) < TOLERANCE,
        "Fermionic Hamiltonian is not Hermitian",
    )

    for row, row_state in enumerate(states):
        for column, column_state in enumerate(states):
            if sum(row_state) != sum(column_state):
                require(
                    abs(hamiltonian[row, column]) < TOLERANCE,
                    "Hamiltonian changes particle number",
                )

    coefficients = decompose_paulis(hamiltonian)
    require(len(coefficients) == 15, f"Expected 15 nonzero terms, found {len(coefficients)}")
    identity_from_trace = float(np.trace(hamiltonian).real / hamiltonian.shape[0])
    require(
        abs(identity_from_trace - coefficients["IIII"]) < TOLERANCE,
        "Trace/identity coefficient mismatch",
    )
    require(
        abs(identity_from_trace - EXPECTED_IDENTITY) < TOLERANCE,
        f"Unexpected identity coefficient: {identity_from_trace}",
    )
    require(
        abs(abs(coefficients["XXYY"]) - EXPECTED_FOUR_BODY_MAGNITUDE) < TOLERANCE,
        f"Unexpected XXYY magnitude: {coefficients['XXYY']}",
    )
    require(
        abs(identity_from_trace - BROKEN_IDENTITY) > 1e-3,
        "Broken library identity coefficient was accepted",
    )
    require(
        abs(abs(coefficients["XXYY"]) - BROKEN_FOUR_BODY_MAGNITUDE) > 1e-3,
        "Broken doubled four-body coefficient was accepted",
    )
    reconstructed = reconstruct_pauli_matrix(coefficients)
    reconstruction_error = float(np.max(np.abs(reconstructed - hamiltonian)))
    require(
        reconstruction_error < TOLERANCE,
        f"Pauli reconstruction max error: {reconstruction_error}",
    )

    spectra = sector_spectra(hamiltonian, states)
    lambda_sum, anticommuting_pairs = commutator_sum(coefficients)
    coefficient_l1 = sum(abs(value) for value in coefficients.values())
    measurement_l1 = sum(
        abs(value)
        for signature, value in coefficients.items()
        if signature != "IIII"
    )
    nuclear_repulsion = float(mol.energy_nuc())
    cisolver = configure_solver(fci.FCI(mean_field), "FCI")
    fci_total, fci_vector = cisolver.kernel()
    converged(cisolver, "H2 FCI", fci_total, fci_vector)
    fci_electronic = float(fci_total - nuclear_repulsion)
    computed_electronic = float(spectra[2][0])
    hf_state = (1, 1, 0, 0)
    hf_state_index = states.index(hf_state)
    hf_occupation_integer = occupation_integer(hf_state)
    require(hf_occupation_integer == 3, "H2 HF occupation integer must be 3")
    require(hf_state_index == 3, "H2 HF matrix row must be 3")
    require("ZZXI"[::-1] == "IXZZ", "External Pauli-label reversal failed")
    hf_total = float(hamiltonian[hf_state_index, hf_state_index].real)
    hf_electronic = hf_total
    require(
        abs(hf_electronic - EXPECTED_HF_ELECTRONIC) < TOLERANCE,
        f"Unexpected HF electronic diagonal: {hf_electronic}",
    )
    hf_total += nuclear_repulsion
    two_electron_indices = [
        index for index, state in enumerate(states) if sum(state) == 2
    ]
    _, two_electron_vectors = np.linalg.eigh(
        hamiltonian[np.ix_(two_electron_indices, two_electron_indices)]
    )
    hf_sector_index = two_electron_indices.index(hf_state_index)
    ground_hf_amplitude_squared = float(
        abs(two_electron_vectors[hf_sector_index, 0]) ** 2
    )
    require(
        0.98 < ground_hf_amplitude_squared < 1.0,
        "Unexpected H2 ground-state HF amplitude",
    )

    require(
        abs(nuclear_repulsion - committed_record["Vnn"]) < TOLERANCE,
        "Nuclear repulsion differs from the committed record",
    )
    require(
        abs(float(mean_field.e_tot) - committed_record["E_HF"]) < TOLERANCE,
        "PySCF HF energy differs from the committed record",
    )
    require(
        abs(hf_total - float(mean_field.e_tot)) < TOLERANCE,
        "Hamiltonian HF determinant does not reproduce PySCF HF",
    )
    require(
        abs(computed_electronic - fci_electronic) < TOLERANCE,
        "Two-electron ground state does not reproduce PySCF FCI",
    )

    correlation = float(fci_total - mean_field.e_tot)

    print("H2/STO-3G reference verification (R = 0.74 Angstrom)")
    print(f"  integral max error:    {integral_error:.3e}")
    print(f"  PySCF version:         {pyscf.__version__}")
    print(f"  canonical source:      {canonical_source['source_commit']}")
    print(f"  canonical file SHA:    {canonical_sha256}")
    print(f"  tensor SHA-256:        {tensor_hash}")
    print(f"  input entries:         {one_body_count} one-body + {two_body_count} two-body")
    print(f"  input threshold:       {metadata['input_threshold']:.1e}")
    print(f"  Pauli reconstruction:  {reconstruction_error:.3e}")
    print(f"  Tr(H)/16 = IIII:       {identity_from_trace:.10f} Ha")
    print(f"  |XXYY|:                {abs(coefficients['XXYY']):.10f} Ha")
    print(f"  nuclear repulsion:     {nuclear_repulsion:.10f} Ha")
    print(f"  HF electronic row 3:   {hf_electronic:.10f} Ha")
    print(f"  HF total:              {float(mean_field.e_tot):.10f} Ha")
    print(f"  FCI electronic:        {fci_electronic:.10f} Ha")
    print(f"  FCI total:             {float(fci_total):.10f} Ha")
    print(f"  correlation energy:    {correlation:.10f} Ha")
    print(f"  anticommuting pairs:   {anticommuting_pairs}")
    print(f"  coefficient 1-norm:    {coefficient_l1:.10f} Ha")
    print(f"  measurement 1-norm:    {measurement_l1:.10f} Ha")
    print(f"  commutator sum:        {lambda_sum:.10f} Ha^2")
    print(f"  HF displayed ket:      |1100>")
    print(f"  HF occupation integer: {hf_occupation_integer} (0b0011)")
    print(f"  HF matrix row:         {hf_state_index} (0b0011)")
    print(f"  ground HF amplitude^2: {ground_hf_amplitude_squared:.10f}")
    print("  a2 dagger FockMap:      ZZXI / ZZYI")
    print("  external labels:       IXZZ / IYZZ")

    oracle = {
        "source_fixture": str(REFERENCE_PATH.relative_to(SCRIPT_DIR.parent)),
        "source_spin_integrals_sha256": tensor_hash,
        "canonical_source": {
            "repo": canonical_source["source_repo"],
            "commit": canonical_source["source_commit"],
            "path": canonical_source["source_path"],
            "git_blob_sha1": canonical_blob,
            "file_sha256": canonical_sha256,
        },
        "pyscf_version": pyscf.__version__,
        "bond_length_angstrom": BOND_LENGTH,
        "signature_order": "P0 P1 P2 P3, qubit 0 leftmost",
        "matrix_order": "reverse displayed signature: P3 outermost, P0 innermost",
        "occupation_integer": "sum_j n_j * 2^j",
        "coefficients_Ha": coefficients,
        "acceptance_anchors": {
            "trace_over_16_identity_Ha": identity_from_trace,
            "hf_electronic_matrix_diagonal_row_3_Ha": hf_electronic,
            "xx_yy_magnitude_Ha": abs(coefficients["XXYY"]),
            "input_one_body_entries": one_body_count,
            "input_two_body_entries": two_body_count,
            "input_threshold": metadata["input_threshold"],
            "combined_pauli_threshold": PAULI_THRESHOLD,
        },
        "spectra_Ha_by_particle_number": {
            str(electron_count): [float(value) for value in eigenvalues]
            for electron_count, eigenvalues in spectra.items()
        },
        "energies_Ha": {
            "nuclear_repulsion": nuclear_repulsion,
            "hf_total": float(mean_field.e_tot),
            "fci_electronic": fci_electronic,
            "fci_total": float(fci_total),
            "correlation": correlation,
        },
        "norms": {
            "nonzero_terms_threshold_1e-12": len(coefficients),
            "coefficient_l1_Ha": coefficient_l1,
            "nonidentity_measurement_l1_Ha": measurement_l1,
            "first_order_commutator_sum_Ha2": lambda_sum,
            "anticommuting_pairs": anticommuting_pairs,
        },
        "ordering_anchor": {
            "hf_displayed_ket": "1100",
            "hf_occupation_integer": hf_occupation_integer,
            "hf_matrix_row": hf_state_index,
            "ground_hf_amplitude_squared": ground_hf_amplitude_squared,
            "fockmap_a2_dagger_labels": ["ZZXI", "ZZYI"],
            "external_a2_dagger_labels": ["IXZZ", "IYZZ"],
        },
    }
    if args.write or args.output is not None:
        destination = (args.output or ORACLE_PATH).resolve()
        destination.parent.mkdir(parents=True, exist_ok=True)
        with output_batch(destination.parent, [destination.name]) as stage:
            write_json(stage / destination.name, oracle)
        print(f"  regenerated oracle:    {destination}")
    else:
        require(ORACLE_PATH.exists(), "Missing committed oracle; restore it or use explicit --write/--output regeneration")
        compare_document(json.loads(ORACLE_PATH.read_text()), oracle, TOLERANCE, archival=args.archival)
        print(f"  oracle verified:       {ORACLE_PATH.name} (read-only)")
    print()
    print("JW coefficients (FockMap signature order q0,q1,q2,q3):")
    for signature, coefficient in coefficients.items():
        print(f"  {signature}  {coefficient:+.10f}")
    print()
    print("Electronic spectra by particle number:")
    for electron_count, eigenvalues in spectra.items():
        values = ", ".join(f"{value:.10f}" for value in eigenvalues)
        print(f"  N={electron_count}: {values}")


if __name__ == "__main__":
    main()
