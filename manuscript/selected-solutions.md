# Selected Worked Solutions

These are worked answers to selected exercises, not a complete answer
key. Each identifies the chapter and exercise by name so that the
reasoning remains findable if exercises are reordered. Use the
question's stated model and inputs: a hypothetical coefficient table
is not the canonical H₂ fixture.

## The Quantum Computer's Vocabulary: Bell state

Starting in $\lvert00\rangle$, the Hadamard on qubit 0 gives
$(\lvert00\rangle+\lvert10\rangle)/\sqrt2$ in the book's
q0-leftmost display. The controlled-NOT with control 0 and target 1
therefore gives

$$
\lvert\Phi^+\rangle
=\frac{\lvert00\rangle+\lvert11\rangle}{\sqrt2}.
$$

If this were a product
$(a\lvert0\rangle+b\lvert1\rangle)
\otimes(c\lvert0\rangle+d\lvert1\rangle)$,
we would need $ac=bd=1/\sqrt2$ and $ad=bc=0$.
The nonzero values of $ac$ and $bd$ make all four factors
nonzero, contradicting the two zero cross terms. The state is
therefore not a product.

Perfectly correlated Z outcomes alone do not prove that result:
the equal classical mixture of `00` and `11` has those same
outcome probabilities. The coherent Bell state also has
$\langle XX\rangle=1$; that mixture has
$\langle XX\rangle=0$.

## The Quantum Computer's Vocabulary: CNOT cost

The hypothetical Hamiltonian has 100 nonidentity terms. In JW,
20 have weight 50 and 80 have weight 2; in the ternary example,
those same 20 heavy terms have weight 5. Using the unoptimised
logical staircase cost $2(w-1)$ per term gives

$$
C_\mathrm{JW}=20[2(50-1)]+80[2(2-1)]=2120,
$$

$$
C_\mathrm{tree}=20[2(5-1)]+80[2(2-1)]=320.
$$

The difference is 1800 CNOTs per first-order step under this model,
about an 84.9% reduction. Nothing in this arithmetic establishes
a molecular error tolerance, routing overhead or a hardware
runtime advantage. It is a comparison of the supplied weight
distributions under one declared compilation rule.

## Checking Our Answer: Sector analysis

The question uses the normal-ordered electronic Hamiltonian,
without nuclear repulsion. Every one-body monomial
$a_p^\dagger a_q$ kills the vacuum because its rightmost
annihilator does. Every two-body monomial
$a_p^\dagger a_q^\dagger a_s a_r$ does so for the same reason.
Thus

$$
H_\mathrm{el}\lvert0000\rangle=0.
$$

The zero-electron sector contains only this one state, so its
electronic spectrum is the single eigenvalue 0. Adding $V_{nn}I$
would change that eigenvalue to $V_{nn}$. The vacuum argument
does not say that the full molecular Hamiltonian, including its
nuclear constant, has a zero-energy vacuum.

The Pauli expansion can contain an identity coefficient even
though this eigenvalue is zero. Other diagonal Pauli terms
cancel that coefficient on the vacuum. Treating the identity
coefficient alone as the vacuum energy misses those terms.
