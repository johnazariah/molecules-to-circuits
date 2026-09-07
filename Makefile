# ══════════════════════════════════════════════════════════════
# From Molecules to Quantum Circuits — Build System
# ══════════════════════════════════════════════════════════════
#
# Usage:
#   make              Build molecules-to-circuits.pdf
#   make sample       Build molecules-to-circuits-sample.pdf (selected chapters)
#   make arxiv-pdflatex  Build pdflatex arXiv submission (tarball + PDF)
#   make html         Build the MyST HTML edition
#   make preview      Build HTML, then serve it locally (fails if build fails)
#   make clean        Remove generated files
#   make word-count   Print word counts per chapter
#   make diagrams     Render mermaid diagrams only
#   make data         Regenerate H₂ and H₂O data
#
# Prerequisites:
#   pandoc, xelatex, mmdc, python3; Jupyter Book 2 for HTML

SHELL := /bin/bash

# ── Directories ──
MS_DIR      := manuscript
CODE_DIR    := code
IMG_DIR     := $(MS_DIR)/mermaid-images
OUT         := $(MS_DIR)/molecules-to-circuits.pdf
SAMPLE_OUT  := $(MS_DIR)/molecules-to-circuits-sample.pdf
EPUB_OUT    := $(MS_DIR)/molecules-to-circuits.epub

# ── Source files ──
CHAPTERS     := $(addprefix $(MS_DIR)/,$(shell sed '/^[[:space:]]*$$/d; /^\#/d' $(MS_DIR)/Book.txt))
FIGURES      := $(wildcard $(MS_DIR)/figures/*)
BUILD_SCRIPTS := scripts/render-mermaid.py scripts/check-book-manifests.py
PYTHON       ?= python3
PORT         ?= 8000

# ── Pandoc settings ──
PANDOC      := pandoc
LUA_FILTER  := $(MS_DIR)/mermaid.lua
PREAMBLE    := $(MS_DIR)/preamble.tex

PANDOC_COMMON := \
  --pdf-engine=xelatex \
  --lua-filter=$(LUA_FILTER) \
  --resource-path=$(MS_DIR):. \
  -H $(PREAMBLE) \
  -V geometry:margin=1in \
  -V fontsize=11pt \
  -V classoption=oneside \
  -V mainfont="DejaVu Serif" \
  -V sansfont="DejaVu Sans" \
  -V monofont="DejaVu Sans Mono" \
  -V title="From Molecules to Quantum Circuits" \
  -V subtitle="A Computational Guide to Fermion-to-Qubit Encodings" \
  -V author="John S Azariah" \
  -V date="Expanded working manuscript" \
  --toc \
  --toc-depth=2 \
  --highlight-style=tango \
  --top-level-division=chapter \
  -V colorlinks=true \
  -V linkcolor=blue \
  -V urlcolor=blue

PANDOC_OPTS := $(PANDOC_COMMON) \
  --metadata=abstract:"This tutorial develops the translation layer from molecular electronic structure to logical quantum circuits. For H₂/STO-3G at 0.74 Å, generated PySCF integrals feed an independently verified fermionic matrix and Jordan-Wigner Pauli Hamiltonian before encoding, symmetry, product-formula, cost, and export concepts are applied. Separate PySCF scripts provide the H₂ dissociation reference and an H₂O FCI angular scan at fixed experimental O-H length; these chemistry references are not represented as energies produced by circuit construction alone. The tutorial covers six fermion-to-qubit encodings, physical-sector tapering requirements, Trotter decomposition, CNOT accounting, and OpenQASM/Q\# export across 23 chapters, 10 companion scripts, and 10 laboratory sessions. Companion software and source at https://github.com/johnazariah/encodings."

SAMPLE_FILTER := $(MS_DIR)/sample-filter.lua
SAMPLE_OPTS := --lua-filter=$(SAMPLE_FILTER) $(PANDOC_COMMON)

# ══════════════════════════════════════════════════════════════
#  Targets
# ══════════════════════════════════════════════════════════════

.PHONY: all clean word-count diagrams data sample epub verify-data pipeline-check \
	manifest-check tooling-check support-check html preview

all: $(OUT)

manifest-check:
	$(PYTHON) scripts/check-book-manifests.py

tooling-check:
	$(PYTHON) scripts/check-publication-tooling.py

support-check:
	$(PYTHON) scripts/check-support-examples.py

$(OUT): $(CHAPTERS) $(FIGURES) $(BUILD_SCRIPTS) $(LUA_FILTER) $(PREAMBLE) $(MS_DIR)/Book.txt Makefile | manifest-check
	@echo "Building manuscript..."
	$(PANDOC) $(CHAPTERS) -o $(OUT) $(PANDOC_OPTS)
	@echo "Done: $$(python3 -c "import pymupdf; d=pymupdf.open('$(OUT)'); print(f'{d.page_count} pages'); d.close()" 2>/dev/null || echo '(install pymupdf for page count)')"
	@ls -lh $(OUT)

sample: $(SAMPLE_OUT)

$(SAMPLE_OUT): $(CHAPTERS) $(FIGURES) $(BUILD_SCRIPTS) $(LUA_FILTER) $(SAMPLE_FILTER) $(PREAMBLE) $(MS_DIR)/Book.txt $(MS_DIR)/Sample.txt Makefile | manifest-check
	@echo "Building sample..."
	$(PANDOC) $(CHAPTERS) -o $(SAMPLE_OUT) $(SAMPLE_OPTS)
	@echo "Done: $$(python3 -c "import pymupdf; d=pymupdf.open('$(SAMPLE_OUT)'); print(f'{d.page_count} pages'); d.close()" 2>/dev/null || echo '(install pymupdf for page count)')"
	@ls -lh $(SAMPLE_OUT)

epub: $(EPUB_OUT)

$(EPUB_OUT): $(CHAPTERS) $(FIGURES) $(BUILD_SCRIPTS) $(LUA_FILTER) $(MS_DIR)/Book.txt Makefile | manifest-check
	@echo "Building EPUB..."
	$(PANDOC) $(CHAPTERS) \
	  -o $(EPUB_OUT) \
	  --toc --toc-depth=2 \
	  --lua-filter=$(LUA_FILTER) \
	  --resource-path=$(MS_DIR):. \
	  --highlight-style=tango \
	  --metadata title="From Molecules to Quantum Circuits" \
	  --metadata subtitle="A Computational Guide to Fermion-to-Qubit Encodings" \
	  --metadata author="John S Azariah" \
	  --top-level-division=chapter \
	  --mathml
	@ls -lh $(EPUB_OUT)

# ── arXiv submission ──
ARXIV_DIR   := arxiv-submission
ARXIV_TEX   := $(ARXIV_DIR)/manuscript.tex

arxiv: $(CHAPTERS) $(FIGURES) $(BUILD_SCRIPTS) $(LUA_FILTER) $(PREAMBLE) $(MS_DIR)/Book.txt | manifest-check
	@echo "Building arXiv submission package (xelatex)..."
	@rm -rf $(ARXIV_DIR)
	@mkdir -p $(ARXIV_DIR)
	$(PANDOC) $(CHAPTERS) -o $(ARXIV_TEX) -s $(PANDOC_OPTS)
	@if [ -d $(IMG_DIR) ] && [ "$$(ls -A $(IMG_DIR))" ]; then \
	  cp $(IMG_DIR)/*.png $(ARXIV_DIR)/; \
	fi
	@cp $(MS_DIR)/figures/*.png $(ARXIV_DIR)/
	@$(PYTHON) scripts/localise-tex-images.py $(ARXIV_TEX)
	@cd $(ARXIV_DIR) && tar czf ../arxiv-submission.tar.gz *
	@echo "Created arxiv-submission.tar.gz with:"
	@tar tzf arxiv-submission.tar.gz | sed 's/^/  /'
	@ls -lh arxiv-submission.tar.gz

# ── arXiv submission (pdflatex — required by arXiv) ──
PREAMBLE_ARXIV := $(MS_DIR)/preamble-arxiv.tex
CONVERT_SCRIPT := scripts/convert-to-pdflatex.py

PANDOC_ARXIV_OPTS := \
  --pdf-engine=pdflatex \
  --lua-filter=$(LUA_FILTER) \
  --resource-path=$(MS_DIR):. \
  -H $(PREAMBLE_ARXIV) \
  -V geometry:margin=1in \
  -V fontsize=11pt \
  -V classoption=oneside \
  -V title="From Molecules to Quantum Circuits" \
  -V subtitle="A Computational Guide to Fermion-to-Qubit Encodings" \
  -V author="John S Azariah" \
  -V date="Expanded working manuscript" \
  --toc \
  --toc-depth=2 \
  --highlight-style=tango \
  --top-level-division=chapter \
  -V colorlinks=true \
  -V linkcolor=blue \
  -V urlcolor=blue

arxiv-pdflatex: $(CHAPTERS) $(FIGURES) $(BUILD_SCRIPTS) $(LUA_FILTER) $(PREAMBLE_ARXIV) $(MS_DIR)/Book.txt $(CONVERT_SCRIPT) | manifest-check
	@echo "Building arXiv submission package (pdflatex)..."
	@rm -rf $(ARXIV_DIR)
	@mkdir -p $(ARXIV_DIR)
	$(PANDOC) $(CHAPTERS) -o $(ARXIV_TEX) -s $(PANDOC_ARXIV_OPTS)
	@if [ -d $(IMG_DIR) ] && [ "$$(ls -A $(IMG_DIR))" ]; then \
	  cp $(IMG_DIR)/*.png $(ARXIV_DIR)/; \
	fi
	@cp $(MS_DIR)/figures/*.png $(ARXIV_DIR)/
	@$(PYTHON) scripts/localise-tex-images.py $(ARXIV_TEX)
	@python3 $(CONVERT_SCRIPT) $(ARXIV_TEX)
	@echo "Compiling PDF (two passes)..."
	@cd $(ARXIV_DIR) && pdflatex -interaction=nonstopmode manuscript.tex > /dev/null 2>&1
	@cd $(ARXIV_DIR) && pdflatex -interaction=nonstopmode manuscript.tex > /dev/null 2>&1
	@if grep -q '^!' $(ARXIV_DIR)/manuscript.log; then \
	  echo "ERROR: LaTeX errors found:"; \
	  grep '^!' $(ARXIV_DIR)/manuscript.log; \
	  exit 1; \
	fi
	@grep "Output written" $(ARXIV_DIR)/manuscript.log
	@rm -f $(ARXIV_DIR)/manuscript.aux $(ARXIV_DIR)/manuscript.log \
	       $(ARXIV_DIR)/manuscript.out $(ARXIV_DIR)/manuscript.toc
	@cd $(ARXIV_DIR) && tar czf ../arxiv-submission.tar.gz manuscript.tex *.png
	@echo "Created arxiv-submission.tar.gz with:"
	@tar tzf arxiv-submission.tar.gz | sed 's/^/  /'
	@ls -lh arxiv-submission.tar.gz $(ARXIV_DIR)/manuscript.pdf

clean:
	rm -rf $(IMG_DIR) $(OUT) $(SAMPLE_OUT) $(EPUB_OUT) $(ARXIV_DIR) arxiv-submission.tar.gz

word-count:
	@echo "Chapter word counts:"
	@for f in $(CHAPTERS); do \
	  printf "  %-40s %5d\n" "$$(basename $$f)" "$$(wc -w < $$f)"; \
	done
	@echo "  ────────────────────────────────────────────────"
	@printf "  %-40s %5d\n" "TOTAL" "$$(cat $(CHAPTERS) | wc -w)"

diagrams: manifest-check
	@echo "Rendering mermaid diagrams..."
	@$(PANDOC) $(CHAPTERS) -t native --lua-filter=$(LUA_FILTER) > /dev/null
	@echo "Rendered $$(ls $(IMG_DIR)/*.png 2>/dev/null | wc -l) diagrams"

# ── Data generation (requires requirements-data.txt) ──
data:
	python3 $(CODE_DIR)/ch18-generate-h2-integrals.py
	python3 $(CODE_DIR)/ch18-dissociation-scan.py
	python3 $(CODE_DIR)/ch19-bond-angle-scan.py
	python3 $(CODE_DIR)/ch09-verify-h2.py

verify-data:
	python3 $(CODE_DIR)/ch09-verify-h2.py
	dotnet fsi labs/03-compare-encodings.fsx

.PHONY: data-reproduction-check
data-reproduction-check:
	bash scripts/check-data-idempotence.sh

pipeline-check:
	bash scripts/check-ch18-output-isolation.sh

# ── Labs ──
.PHONY: lab-check
lab-check:
	@set -euo pipefail; \
	echo "Executing labs..."; \
	for f in labs/[0-9][0-9]-*.fsx; do \
	  echo "  $$f"; \
	  dotnet fsi "$$f"; \
	done

html:
	PYTHON="$(PYTHON)" bash scripts/build-site.sh

preview: html
	$(PYTHON) -m http.server $(PORT) --bind 127.0.0.1 --directory _build/html
