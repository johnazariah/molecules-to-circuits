import json
from pathlib import Path

import numpy as np
np.set_printoptions(precision=12)

ROOT = Path("/Users/johnaz/PhD/copilot-worktrees/encodings-book/johnazariah-fluffy-doodle")
P = {
    "I": np.eye(2, dtype=complex),
    "X": np.array([[0, 1], [1, 0]], dtype=complex),
    "Y": np.array([[0, -1j], [1j, 0]], dtype=complex),
    "Z": np.diag([1, -1]).astype(complex),
}


def matrix(signature):
    out = np.ones((1, 1), dtype=complex)
    for letter in reversed(signature):
        out = np.kron(out, P[letter])
    return out


def hamiltonian(terms):
    return sum(c * matrix(s) for s, c in terms.items())


def cnot(n, c, t):
    out = np.zeros((2**n, 2**n), dtype=complex)
    for b in range(2**n):
        out[b ^ (((b >> c) & 1) << t), b] = 1
    return out


def mm(a, b):
    return np.einsum("ij,jk->ik", a, b, optimize=False)


def mv(a, b):
    return np.einsum("ij,j->i", a, b, optimize=False)


def decompose(h, n):
    from itertools import product
    terms = {}
    for chars in product("IXYZ", repeat=n):
        s = "".join(chars)
        coeff = np.trace(mm(matrix(s), h)) / 2**n
        assert abs(coeff.imag) < 1e-12
        if abs(coeff) > 1e-12:
            terms[s] = coeff.real
    return terms


def metrics(terms):
    weights = [sum(c != "I" for c in s) for s in terms]
    return len(terms), max(weights), sum(weights), sum(2*max(0,w-1) for w in weights)


oracle = json.loads((ROOT / "code/h2_0.74_oracle.json").read_text())
terms = oracle["coefficients_Ha"]
h = hamiltonian(terms)
u = mm(cnot(4, 1, 3), cnot(4, 0, 2))
rot = mm(mm(u, h), u.conj().T)
reduced = rot[12:16, 12:16]
old_rows = [12, 9, 6, 3]
for new_row, old_row in enumerate(old_rows):
    assert u[new_row + 12, old_row] == 1
assert np.allclose(reduced, h[np.ix_(old_rows, old_rows)], atol=1e-14)
print("H2 FOUR TO TWO TERM LEDGER")
for s, coeff in terms.items():
    rotated = mm(mm(u, matrix(s)), u.conj().T)
    print(s, f"{coeff:.13f}", decompose(rotated, 4), "=>", decompose(rotated[12:16, 12:16], 2))
print("H2 two-qubit", decompose(reduced, 2))
print("matrix", reduced.real)
print("spectrum", np.linalg.eigvalsh(reduced))
u2 = cnot(2, 0, 1)
rot2 = mm(mm(u2, reduced), u2.conj().T)
one = rot2[:2, :2]
print("one-qubit", decompose(one, 1))
print("one matrix", one.real, "spectrum", np.linalg.eigvalsh(one))
for title, ts in [("H2 4", terms), ("H2 2", decompose(reduced, 2)), ("H2 1", decompose(one, 1))]:
    print(title, metrics(ts))
assert abs(np.linalg.eigvalsh(one)[0] - oracle["energies_Ha"]["fci_electronic"]) < 1e-12
gammas = [matrix(s) for s in ["XZ", "YI", "XX", "XY"]]
creators = [(gammas[2*j] - 1j*gammas[2*j+1])/2 for j in range(2)]
annihilators = [a.conj().T for a in creators]
for i in range(2):
    for j in range(2):
        assert np.allclose(mm(creators[i], annihilators[j]) + mm(annihilators[j], creators[i]), (i == j)*np.eye(4))
        assert np.allclose(mm(creators[i], creators[j]) + mm(creators[j], creators[i]), 0)
        assert np.allclose(mm(annihilators[i], annihilators[j]) + mm(annihilators[j], annihilators[i]), 0)
vacuum = np.array([1, 0, 0, 0])
assert all(np.allclose(mv(a, vacuum), 0) for a in annihilators)
assert np.allclose(mv(creators[0], mv(creators[1], vacuum)), [0,0,-1,0])
print("Tree vacuum", vacuum, "number operators", [decompose(mm(creators[j], annihilators[j]), 2) for j in range(2)])
mixed = {"ZIZI": .5, "IXIX": -.3, "ZIIZ": .2, "IYIY": .1}
mh = hamiltonian(mixed)
mr = mh[np.ix_([0, 2, 8, 10], [0, 2, 8, 10])]
print("Mixed reduction", decompose(mr, 2), "spectrum", np.linalg.eigvalsh(mr))
print("Toy diagonal spectrum", np.linalg.eigvalsh(hamiltonian({"ZZ":.8,"ZI":-.4,"IZ":-.3,"II":-.2})))
print("Heisenberg", decompose(mm(mm(cnot(2,0,1), hamiltonian({"XX":1,"YY":1,"ZZ":1})), cnot(2,0,1)), 2))

def binary_rank(rows):
    rows = np.array(rows, dtype=np.uint8)
    rank = 0
    for col in range(rows.shape[1]):
        candidates = [r for r in range(rank, len(rows)) if rows[r, col]]
        if not candidates:
            continue
        rows[[rank, candidates[0]]] = rows[[candidates[0], rank]]
        for r in range(len(rows)):
            if r != rank and rows[r, col]:
                rows[r] ^= rows[rank]
        rank += 1
    return rank

assert binary_rank([[0,0,1,1], [1,1,1,1], [1,1,0,0]]) == 2
check_rows = [[int(p in "ZY") for p in s]+[int(p in "XY") for p in s] for s in terms]
assert binary_rank(check_rows) == 5
delta = 1e-7
moment_error = max(abs(((1-delta)**k+(1+delta)**k)/2-1) for k in range(1,17))
assert moment_error < 1e-10
assert delta > 1e-10
print("Moment negative control", moment_error, "eigenvalue error", delta)
bad_creator = (matrix("XZ")-matrix("YI"))/2
assert np.allclose(mm(bad_creator,bad_creator),np.eye(4)/2)
assert np.allclose(hamiltonian({"ZZ":.8,"ZI":.4,"IZ":.3,"II":-.2}).diagonal(),[1.3,-1.1,-.9,-.1])
eigenvalues, eigenvectors = np.linalg.eigh(one)
v = eigenvectors[:,0]
print("Ground ratio", v[0]/v[1], "HF probability",abs(v[1])**2)
print("H2 electronic and total",eigenvalues[0],eigenvalues[0]+oracle["energies_Ha"]["nuclear_repulsion"])
print("All chapter calculations passed.")
