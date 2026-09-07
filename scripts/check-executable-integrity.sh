#!/usr/bin/env bash
# Bounded semantic gate. --imports additionally requires requirements-import.txt.
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ $# -gt 1 || ( $# -eq 1 && "$1" != "--imports" ) ]]; then
  echo "Usage: $0 [--imports]" >&2
  exit 2
fi
export OMP_NUM_THREADS=1 OPENBLAS_NUM_THREADS=1 MKL_NUM_THREADS=1 VECLIB_MAXIMUM_THREADS=1
python="${PYTHON:-python3}"
"$python" code/ch09-verify-h2.py
dotnet fsi scripts/check-spectral-certification.fsx
dotnet fsi scripts/check-h2-contract.fsx
"$python" scripts/check-oracle-integrity.py
"$python" scripts/check-generation-integrity.py
"$python" scripts/check-input-integrity.py
bash scripts/check-ch18-output-isolation.sh
dotnet fsi code/ch07-fenwick.fsx
dotnet fsi code/ch08-car-census.fsx 6
dotnet fsi code/ch12-h2-physical-taper.fsx
dotnet fsi code/ch20-measurement-groups.fsx
"$python" code/ch20-vqe-qpe.py
dotnet fsi code/ch21-export-check.fsx
if [[ "${1:-}" == "--imports" ]]; then
  "${IMPORT_PYTHON:-$python}" code/ch21-import-check.py
else
  echo "Real external importers not run; use --imports with scripts/requirements-import.txt."
fi
echo "Bounded executable-integrity gate passed; no canonical regeneration performed."
