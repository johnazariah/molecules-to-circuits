#!/usr/bin/env bash
set -euo pipefail
mode="${1:---portable}"
if [[ "$mode" != "--portable" && "$mode" != "--archival" ]]; then
  echo "Usage: $0 [--portable|--archival]" >&2
  exit 2
fi
export OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1 MKL_NUM_THREADS=1 VECLIB_MAXIMUM_THREADS=1
python="$(command -v "${PYTHON:-python3}")"

repository_root="$(cd "$(dirname "$0")/.." && pwd)"
temporary_root="$(mktemp -d)"
trap 'rm -rf "$temporary_root"' EXIT

copy_root="$temporary_root/repository"
mkdir -p "$copy_root"

rsync -a \
  --exclude .git \
  --exclude .review \
  --exclude _build \
  --exclude '.venv*' \
  --exclude __pycache__ \
  --exclude manuscript/mermaid-images \
  --exclude manuscript/molecules-to-circuits.pdf \
  --exclude manuscript/molecules-to-circuits-sample.pdf \
  --exclude manuscript/molecules-to-circuits.epub \
  "$repository_root/" "$copy_root/"

cd "$copy_root"

"$python" code/ch18-generate-h2-integrals.py >/dev/null
"$python" code/ch18-dissociation-scan.py >/dev/null
"$python" code/ch19-bond-angle-scan.py >/dev/null
"$python" code/ch09-verify-h2.py --write >/dev/null
"$python" scripts/compare-data-parity.py "$repository_root" "$copy_root"

canonical_files=(
  code/physicist_spin_integrals.json
  code/physicist_spin_integrals.provenance.json
  code/h2_dissociation.csv
  code/h2_dissociation_integrals.json
  code/h2_0.74_fixture.json
  code/h2_0.74_oracle.json
  code/h2o_bond_angle_coarse.csv
  code/h2o_bond_angle_fine.csv
  code/h2o_bond_angle_metadata.json
  manuscript/figures/h2_dissociation.png
  manuscript/figures/h2o_bond_angle.png
)

if [[ "$mode" == "--archival" ]]; then
for path in "${canonical_files[@]}"; do
  if ! cmp -s "$repository_root/$path" "$copy_root/$path"; then
    echo "ERROR: archival byte reproduction differs for $path (portable parity is a separate contract)" >&2
    exit 1
  fi
done
fi

"$python" - "$copy_root" <<'PY'
import sys
from pathlib import Path

root = Path(sys.argv[1])

required_loaders = {
    "code/ch03-spin-orbitals.fsx": "h2_dissociation_integrals.json",
    "labs/02-h2-molecule.fsx": '#load "../code/ch03-spin-orbitals.fsx"',
    "labs/03-compare-encodings.fsx": '#load "../code/ch03-spin-orbitals.fsx"',
    "labs/07-trotter-cost.fsx": '#load "../code/ch03-spin-orbitals.fsx"',
    "labs/10-vlasov-tree.fsx": '#load "../code/ch03-spin-orbitals.fsx"',
    "labs/PauliMatrix.fsx": "h2_0.74_oracle.json",
    "manuscript/03-spin-orbitals.md": '#load "code/ch03-spin-orbitals.fsx"',
    "manuscript/06-building-hamiltonian.md": '#load "code/ch03-spin-orbitals.fsx"',
    "manuscript/18-complete-pipeline.md": '#load "code/ch03-spin-orbitals.fsx"',
}

for relative_path, required_text in required_loaders.items():
    text = (root / relative_path).read_text()
    if required_text not in text:
        raise SystemExit(
            f"ERROR: {relative_path} does not consume the canonical H2 loader"
        )

for relative_path in required_loaders:
    text = (root / relative_path).read_text()
    if "let h2Integrals = Map [" in text:
        raise SystemExit(f"ERROR: duplicate literal H2 map in {relative_path}")
PY

echo "Chemistry regeneration passed $mode checks in an isolated copy; accepted artifacts were not changed."
