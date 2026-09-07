"""Small independent dense references; q0 is the least-significant row bit."""
import numpy as np

PAULIS = {
    "I": np.eye(2, dtype=complex),
    "X": np.array([[0, 1], [1, 0]], dtype=complex),
    "Y": np.array([[0, -1j], [1j, 0]], dtype=complex),
    "Z": np.diag([1, -1]).astype(complex),
}


def pauli_matrix(signature):
    if not signature or any(symbol not in PAULIS for symbol in signature):
        raise ValueError(f"Invalid Pauli signature: {signature!r}")
    result = np.array([[1]], dtype=complex)
    for symbol in reversed(signature):
        result = np.kron(result, PAULIS[symbol])
    return result


def hamiltonian_matrix(coefficients):
    if not coefficients:
        raise ValueError("Empty Hamiltonian")
    width = len(next(iter(coefficients)))
    result = np.zeros((2**width, 2**width), dtype=complex)
    for signature, coefficient in coefficients.items():
        if len(signature) != width or not np.isfinite(coefficient):
            raise ValueError("Inconsistent Pauli width or nonfinite coefficient")
        result += coefficient * pauli_matrix(signature)
    return result


def hermitian_eigh(matrix):
    matrix = np.asarray(matrix, dtype=complex)
    if matrix.ndim != 2 or matrix.shape[0] == 0 or matrix.shape[0] != matrix.shape[1]:
        raise ValueError("Expected nonempty square matrix")
    if not np.all(np.isfinite(matrix)):
        raise ValueError("Nonfinite matrix")
    if np.max(np.abs(matrix - matrix.conj().T)) > 1e-12:
        raise ValueError("Non-Hermitian matrix")
    return np.linalg.eigh(matrix)


def matrix_product(left, right):
    # These teaching matrices are tiny. Direct contraction avoids backend-specific
    # complex-BLAS floating-status warnings without suppressing numerical errors.
    result = np.einsum("ik,kj->ij", left, right, optimize=False)
    if not np.all(np.isfinite(result)):
        raise ValueError("Nonfinite matrix product")
    return result


def hermitian_evolution(matrix, time):
    if not np.isfinite(time):
        raise ValueError("Nonfinite evolution time")
    eigenvalues, vectors = hermitian_eigh(matrix)
    return matrix_product(vectors * np.exp(-1j*time*eigenvalues), vectors.conj().T)
