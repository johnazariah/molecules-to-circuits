"""Read-only numerical/source checks for the six foundation chapters."""

from functools import reduce
from itertools import product
from math import comb
from pathlib import Path
import hashlib
import json
import operator
import re
import sys

import numpy as np

ROOT = Path(sys.argv[1])
PAULIS = {
    "I": np.eye(2, dtype=complex),
    "X": np.array([[0, 1], [1, 0]], dtype=complex),
    "Y": np.array([[0, -1j], [1j, 0]], dtype=complex),
    "Z": np.diag([1, -1]).astype(complex),
}


def check_close(actual, expected, tolerance=1e-10):
    assert np.all(np.isfinite(actual)) and np.all(np.isfinite(expected))
    assert np.max(np.abs(np.asarray(actual) - expected)) < tolerance


def dense(signature):
    return reduce(np.kron, (PAULIS[p] for p in reversed(signature)))


def direct_ladder(j, create, n=4):
    matrix = np.zeros((2**n, 2**n), dtype=complex)
    for row in range(2**n):
        if ((row >> j) & 1) == (0 if create else 1):
            sign = (-1) ** bin(row & ((1 << j) - 1)).count("1")
            matrix[row ^ (1 << j), row] = sign
    return matrix


def jw_terms(j, create, n=4):
    return {
        "Z" * j + "X" + "I" * (n - j - 1): 0.5,
        "Z" * j + "Y" + "I" * (n - j - 1): -0.5j if create else 0.5j,
    }


def from_terms(terms):
    return sum(coefficient * dense(signature) for signature, coefficient in terms.items())


MUL = {("I", p): (1, p) for p in "IXYZ"}
MUL.update({(p, "I"): (1, p) for p in "IXYZ"})
MUL.update({(p, p): (1, "I") for p in "XYZ"})
for a, b, c in [("X", "Y", "Z"), ("Y", "Z", "X"), ("Z", "X", "Y")]:
    MUL[a, b] = (1j, c)
    MUL[b, a] = (-1j, c)


def symbolic_product(left, right):
    result = {}
    for s, c in left.items():
        for t, d in right.items():
            factors = [MUL[x, y] for x, y in zip(s, t)]
            signature = "".join(p for _, p in factors)
            coefficient = c * d * np.prod([phase for phase, _ in factors])
            result[signature] = result.get(signature, 0) + coefficient
    return {s: c for s, c in result.items() if c != 0}


def matrix_product(left, right):
    return np.einsum("ij,jk->ik", left, right, optimize=False)


def fermion_product(operations, n=4):
    return reduce(matrix_product, (direct_ladder(j, create, n) for j, create in operations))


fixture_bytes = (ROOT / "code/physicist_spin_integrals.json").read_bytes()
assert hashlib.sha256(fixture_bytes).hexdigest() == (
    "6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812"
)
record = json.loads((ROOT / "code/h2_0.74_fixture.json").read_text())["0.74"]
spatial_h = np.asarray(record["provenance"]["spatial_one_body_mo_Ha"])
spatial_eri = np.asarray(record["provenance"]["spatial_eri_chemist_mo_Ha"])
integrals = {tuple(map(int, key.split(","))): value
             for key, value in record["integrals"].items()}
raw = np.zeros((4, 4, 4, 4))
one = np.zeros((4, 4))
for p, q in product(range(4), repeat=2):
    one[p, q] = integrals.get((p, q), 0)
    check_close(one[p, q], spatial_h[p // 2, q // 2] * (p % 2 == q % 2))
for p, q, r, s in product(range(4), repeat=4):
    raw[p, q, r, s] = integrals.get((p, q, r, s), 0)
    expected = spatial_eri[p // 2, r // 2, q // 2, s // 2]
    expected *= (p % 2 == r % 2) * (q % 2 == s % 2)
    check_close(raw[p, q, r, s], expected)
assert np.count_nonzero(one) == 4 and np.count_nonzero(raw) == 32
assert sum(v != 0 and p % 2 != q % 2 for (p, q, r, s), v in np.ndenumerate(raw)) == 16
assert (14**2, 14**4, 2 * 7**2, 4 * 7**4) == (196, 38416, 98, 9604)
assert (comb(14, 10), comb(20, 2)) == (1001, 190)
print("PASS units/counts and every spatial-to-spin raw entry")

# The eight real physicist permutations are checked on distinct indices too.
rng = np.random.default_rng(17)
pair = {(i, j): k for k, (i, j) in enumerate((i, j) for i in range(4) for j in range(i, 4))}
pair_values = rng.normal(size=(10, 10))
pair_values = pair_values + pair_values.T
def real_v(p, q, r, s):
    return pair_values[pair[tuple(sorted((p, r)))], pair[tuple(sorted((q, s)))]]
for p, q, r, s in product(range(4), repeat=4):
    variants = [(p,q,r,s), (r,q,p,s), (p,s,r,q), (r,s,p,q),
                (q,p,s,r), (s,p,q,r), (q,r,s,p), (s,r,q,p)]
    check_close([real_v(*indices) for indices in variants], real_v(p,q,r,s))
print("PASS all eight real physicist permutations")

one_matrix = sum(one[p, q] * fermion_product([(p, True), (q, False)])
                 for p, q in product(range(4), repeat=2))
two_raw = np.zeros((16, 16), complex)
two_anti = np.zeros_like(two_raw)
two_restricted = np.zeros_like(two_raw)
for p, q, r, s in product(range(4), repeat=4):
    monomial = fermion_product([(p, True), (q, True), (s, False), (r, False)])
    coefficient = raw[p, q, r, s] - raw[p, q, s, r]
    two_raw += 0.5 * raw[p, q, r, s] * monomial
    two_anti += 0.25 * coefficient * monomial
    if p < q and r < s:
        two_restricted += coefficient * monomial
check_close(two_raw, two_anti)
check_close(two_raw, two_restricted)
toy = {
    (0,1,0,1): 0.7, (1,0,1,0): 0.7,
    (0,1,1,0): 0.2, (1,0,0,1): 0.2,
}
toy_raw = sum(0.5 * value * fermion_product([(p,1),(q,1),(s,0),(r,0)], 2)
              for (p,q,r,s), value in toy.items())
check_close(toy_raw, np.diag([0, 0, 0, 0.5]))
check_close(0.7 * fermion_product([(0,1),(1,1),(1,0),(0,0)], 2)[3,3], 0.7)
check_close(2 * toy_raw[3,3], 1)
print("PASS raw/antisymmetrised/restricted matrix equivalence and J-K negative controls")

for n in [4, 8]:
    for j, create in product(range(n), [False, True]):
        check_close(from_terms(jw_terms(j, create, n)), direct_ladder(j, create, n))
for j, k in product(range(4), repeat=2):
    a = direct_ladder(j, False)
    b = direct_ladder(k, False)
    check_close(matrix_product(a, b) + matrix_product(b, a), 0)
    check_close(matrix_product(a, b.conj().T) + matrix_product(b.conj().T, a), np.eye(16) * (j == k))
print("PASS full signed JW action on every 4/8-mode occupation and four-mode CAR")

def lowbit(k):
    return k & -k


def storage(occupation):
    return [reduce(operator.xor, occupation[k - lowbit(k):k], 0)
            for k in range(1, len(occupation) + 1)]


def prefix_set(j):
    positions = set()
    while j > 0:
        positions.add(j - 1)
        j -= lowbit(j)
    return positions


def update_set(j, n):
    positions = set()
    k = j + 1
    while k <= n:
        positions.add(k - 1)
        k += lowbit(k)
    return positions


def bit_sum(bits):
    return sum(bit << i for i, bit in enumerate(bits))


def signature_for(letter, positions, n=8):
    return "".join(letter if i in positions else "I" for i in range(n))


permutation = []
for occupation in product([0, 1], repeat=8):
    stored = storage(occupation)
    permutation.append((bit_sum(occupation), bit_sum(stored)))
    for j in range(9):
        assert reduce(operator.xor, (stored[k] for k in prefix_set(j)), 0) == sum(occupation[:j]) % 2
    for j in range(8):
        recovery = prefix_set(j + 1) ^ prefix_set(j)
        assert reduce(operator.xor, (stored[k] for k in recovery), 0) == occupation[j]
        updated = stored.copy()
        for k in update_set(j, 8):
            updated[k] ^= 1
        changed = list(occupation)
        changed[j] ^= 1
        assert storage(changed) == updated
index_map = dict(permutation)
for j, create in product(range(8), [False, True]):
    x = {signature_for("X", update_set(j, 8)): 1}
    z = {signature_for("Z", prefix_set(j)): 1}
    projector = {"I" * 8: 0.5,
                 signature_for("Z", prefix_set(j + 1) ^ prefix_set(j)): 0.5 if create else -0.5}
    terms = symbolic_product(symbolic_product(x, projector), z)
    expected = np.zeros((256, 256), complex)
    original = direct_ladder(j, create, 8)
    for row, col in zip(*np.nonzero(original)):
        expected[index_map[row], index_map[col]] = original[row, col]
    check_close(from_terms(terms), expected)
    if j == 1 and create:
        assert terms == {"ZXIXIIIX": 0.5, "IYIXIIIX": -0.5j}
example = [1,0,1,1,0,1,0,1]
assert storage(example) == [1,1,1,1,0,1,0,1]
assert update_set(6, 8) == {6, 7}
assert update_set(2, 8) == {2, 3, 7}
print("PASS exhaustive n=8 Fenwick storage/query/update/recovery and both BK ladder actions")

chapter6 = (ROOT / "manuscript/06-building-hamiltonian.md").read_text()
operations = [
    [(0,1),(1,1),(3,0),(2,0)],
    [(2,1),(3,1),(1,0),(0,0)],
    [(0,1),(3,1),(2,0),(1,0)],
    [(1,1),(2,1),(3,0),(0,0)],
]
monomials = [reduce(symbolic_product, (jw_terms(j, create) for j, create in ops))
             for ops in operations]
phase_values = {"1":1, "−1":-1, "0":0, "4":4, "−4":-4, "$i$":1j, "$-i$":-1j}
phase_table = chapter6.split("Entries below are sixteen")[1].split("Odd numbers")[0]
rows = re.findall(r"^\| `([XY]{4})` \| (.+)$", phase_table, re.M)
assert len(rows) == 16
for signature, entries in rows:
    values = [phase_values[value.strip()] for value in entries.rstrip("|").split("|")]
    expected = [16 * monomials[k][signature] * (1 if k < 2 else -1) for k in range(4)]
    check_close(values, expected + [sum(expected)])
print("PASS all 64 published monomial phase coefficients and all 16 row sums")

complete = chapter6.split("## The Complete 15-Term Hamiltonian")[1].split("\n---")[0]
displayed = {signature: float(value) for signature, value in re.findall(
    r"\| \d+ \| \$([IXYZ]{4})\$ \| \$([+-]?\d+\.\d+)\$", complete)}
assert len(displayed) == 15
electronic = one_matrix + two_raw
check_close(from_terms(displayed), electronic, 1e-9)
check_close(electronic[3,3].real, -1.831863646477506)
check_close(electronic[3,12].real, 0.1812104620)
rows_two = [row for row in range(16) if bin(row).count("1") == 2]
eig = np.linalg.eigvalsh(electronic[np.ix_(rows_two, rows_two)])
check_close(eig, [-1.8523881735695826, -1.2458776960825393,
                 -1.2458776960825393, -1.2458776960825390,
                 -0.8834567720521458, -0.23196166596181889])
block = electronic[np.ix_([3,12], [3,12])].real
energies, vectors = np.linalg.eigh(block)
state = vectors[:,0]
if state[0] < 0:
    state *= -1
rho = np.outer(state, state)
mix = np.diag(np.diag(rho))
check_close(energies[0] + record["Vnn"], -1.1372838345)
check_close(np.trace(rho @ block), energies[0])
check_close(state[0] ** 2, 0.9873338735)
weights = [sum(p != "I" for p in signature) for signature in displayed]
assert sum(weights) == 32
assert sum(2 * (w - 1) for w in weights if w > 0) == 36
assert sum(w > 0 for w in weights) == 14
print("PASS all 15 published H2 coefficients, labelled matrix, complete two-electron spectrum and costs")
print("Closed-shell block:", block.tolist())
print("State:", state.tolist(), "mixture energy:", np.trace(mix @ block))

check_close(dense("XZ")[:,2], [0,0,0,-1])
assert not np.allclose(dense("XZ"), np.kron(PAULIS["X"], PAULIS["Z"]))
bell = np.array([1,0,0,1]) / np.sqrt(2)
coin = np.diag([0.5,0,0,0.5])
hadamard = np.array([[1,1],[1,-1]]) / np.sqrt(2)
hh = np.kron(hadamard, hadamard)
check_close(np.abs(hh @ bell) ** 2, [0.5,0,0,0.5])
check_close(np.diag(hh @ coin @ hh), [0.25]*4)
check_close(dense("XZ") @ dense("YX"), -dense("ZY"))
assert (20*2*49 + 80*2, 20*2*4 + 80*2) == (2120, 320)
print("PASS label negative control, Bell/mixture probabilities, products and exercise costs")

for path in sorted((ROOT / "manuscript").glob("0[1-6]-*.md")):
    lines = path.read_text().splitlines()
    in_fence = False
    expected_pipes = None
    for number, line in enumerate(lines, 1):
        if line.startswith("```"):
            in_fence = not in_fence
        if in_fence or not line.startswith("|"):
            expected_pipes = None
            continue
        count = len(re.findall(r"(?<!\\)\|", line))
        if expected_pipes is None:
            expected_pipes = count
        assert count == expected_pipes, (path.name, number, count, expected_pipes)
    assert not in_fence, path.name
print("PASS chapter code-fence closure and all Markdown table column counts")
