# =========================================================
# FORTHCOIN BUILD SYSTEM
# =========================================================

# The Interpreter
FORTH = gforth
# Flags: Fast startup, extensive memory for the heap (128MB)
FLAGS = -m 128M 

# Directories
SRC_DIR = src
TEST_DIR = tests
DATA_FILES = blockchain.dat debug.log

.PHONY: all clean test run shell help

# Default Target: Show help
all: help

# ---------------------------------------------------------
# HELP
# ---------------------------------------------------------
help:
	@echo "ForthCoin Build System"
	@echo "====================="
	@echo ""
	@echo "Available targets:"
	@echo "  make test     - Run unit tests"
	@echo "  make shell    - Interactive Forth shell"
	@echo "  make run      - Launch the node (after test passes)"
	@echo "  make clean    - Remove data files"
	@echo "  make help     - Show this message"
	@echo ""

# ---------------------------------------------------------
# 1. RUN THE NODE
# ---------------------------------------------------------
run: test
	@echo "--- LAUNCHING NODE ---"
	@echo "Note: Full node not yet implemented"
	@echo "Use 'make shell' for interactive development"

# ---------------------------------------------------------
# 2. RUN UNIT TESTS
# ---------------------------------------------------------
test:
	@echo "--- RUNNING TEST SUITE ---"
	@./run_tests.sh

# ---------------------------------------------------------
# 3. DEVELOPMENT
# ---------------------------------------------------------
# Interactive Mode (Drops you into the Forth shell with all files loaded)
shell:
	@echo "--- INTERACTIVE SHELL ---"
	@echo "Type 'bye' to exit"
	@$(FORTH) $(FLAGS) $(SRC_DIR)/load.fs

# ---------------------------------------------------------
# 4. HOUSEKEEPING
# ---------------------------------------------------------
# Wipe the database and logs
clean:
	@echo "--- CLEANING ARTIFACTS ---"
	@rm -f $(DATA_FILES)
	@echo "Cleaned: $(DATA_FILES)"

# Deep Clean: Remove editor temp files too
distclean: clean
	@rm -f *~ */*~ */*/*~
	@echo "Deep clean complete"
