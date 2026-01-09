Based on the friction points we encountered—specifically regarding file paths, stack corruption, and variable scoping—here is a revised **Engineering Standard & Roadmap** for the team.

This document serves as a "Post-Mortem & Protocol" to ensure the next phase of development (Integration) proceeds smoothly.

---

## 1. The Core Friction Points (Retrospective)

We lost significant time on three specific categories of errors. The new team must treat these as "Solved Problems" and not repeat them.

### A. The "Return Stack" Trap (SHA-256 Crash)

* **The Issue:** We tried to use `>R` and `R>` inside a `DO...LOOP` to store temporary hash variables.
* **The Physics:** In Forth, the loop index and limit are stored on the Return Stack. Pushing data onto it inside a loop buries the loop counter. When `LOOP` tries to increment, it reads your data instead of the counter, causing immediate crashes.
* **The Fix:** **Never** use the Return Stack for data storage inside a loop. Use `VALUE` or `VARIABLE` for temporary state.

### B. The "Stateful" Base Trap (Hex vs Decimal)

* **The Issue:** We relied on `HEX` and `DECIMAL` commands changing the global state. One failed test left the compiler in Hex mode, causing subsequent code (like `100 DO`) to run `256` times instead of `100`.
* **The Fix:** **Always** use `$` prefix for Hex literals (e.g., `$D9B4BEF9`) and `#` for Decimal if ambiguous. Never change the global `BASE` in a library file.

### C. The "Path Hell" (Build System)

* **The Issue:** `INCLUDED` in Gforth is relative to the *Process Working Directory*, not the *File Location*. Running `gforth src/main.fs` works, but running `gforth tests/run_tests.fs` fails because it looks for `src/` inside `tests/`.
* **The Fix:** The Build System (`make` / `run_tests.sh`) must strictly enforce the root directory before launching Gforth.

---

## 2. Revised Roadmap: "The Clean Start"

If we were starting today, this is the exact order of operations the team should follow.

### Phase 1: The Environment (Day 1)

Do not write a single line of crypto code until the harness is solid.

1. **Define `root.fs`:** A file that normalizes paths.
2. **Strict Makefile:**
* `make test` -> Enters `./` root, executes `./run_tests.sh`.
* `make shell` -> Enters `./` root, loads `src/load.fs`, leaves session open.


3. **The Style Guide:** Enforce the "No Shadowing" rule (e.g., do not name a word `W@` if Gforth already has it). Use prefixes like `FC-` (ForthCoin) for generic names.

### Phase 2: The Memory Model (Day 2)

Forth is untyped. We need strict conventions for our 256-bit BigInts.

* **Rule:** A 256-bit number is *always* passed as an Address on the stack `( addr -- )`. Never try to push 4x 64-bit cells onto the stack to pass a number.
* **Verification:** Write `test_math.fs` first. If `256+!` (Add with Carry) fails, stop. Do not build SHA-256.

### Phase 3: The "Stateless" Crypto (Day 3-4)

When building SHA-256:

* **Input:** Address + Length.
* **Output:** Address.
* **Internal State:** Use `VALUE` for registers `a` through `h`.
* **Testing:** NIST Vectors are mandatory. If `SHA256("abc")` doesn't match `ba78...`, the code is useless.

---

## 3. "Watch Out" List for the Team

Hand this list to any developer joining the project.

### ⚠️ The Stack Effect Comment is Law

In C++, function signatures check types. In Forth, only comments exist.

* **Bad:** `: ADD-HASH ( addr -- ) ... ;` (Vague)
* **Good:** `: ADD-HASH ( addr_hash_in addr_target -- ) ... ;`
* **Mandate:** Every definition longer than 1 line must have a `( -- )` stack comment.

### ⚠️ Endianness is the Enemy

* **Forth (Gforth on x86):** Little Endian.
* **Bitcoin Network Protocol:** Big Endian (mostly).
* **SHA-256:** Big Endian (internal operations).
* **Bitcoin Block Hash:** Little Endian (in the header).
* **Directive:** You must explicitly byte-swap (`BSWAP32`) at the boundaries (Network Ingress/Egress). Do not assume the memory layout matches the wire layout.

### ⚠️ "Create does not Run"

* **Pitfall:** Writing `256VAR myVar` inside a colon definition `: MY-FUNC ... 256VAR myVar ... ;`.
* **Result:** The variable is created at *Compile Time*, not *Run Time*. If you run `MY-FUNC` twice, it reuses the exact same memory address, causing massive data corruption.
* **Solution:** All `CREATE`, `VARIABLE`, and `256VAR` declarations must be at the global scope (top of the file).

### ⚠️ Buffer Overflows are Silent

Forth does not check array bounds.

* If you write to `W-ARRAY` at index 65, you might overwrite the Mining Target.
* **Defense:** In `debug.fs`, enable a `CHECK-BOUNDS` word during testing that aborts if an index is out of range. Disable it for production speed.

---

## 4. Next Step for the Current Team

We are currently transitioning from **Unit Implementation** to **System Integration**.

**The immediate task:**

1. **Refactor `math256.fs**`: Fix the `256+!` word. It currently has stack juggling bugs. Simplification is key—use variables/values to hold the carry bit rather than the stack.
2. **Wire the Mining Loop**: Connect `src/consensus/mining.fs` to the now-working `src/crypto/sha256.fs`.
3. **Run the Simulation**:
* Set difficulty to `0x20FFFFFF` (easiest possible).
* Run `MINE-BLOCK`.
* Assert that it finds a nonce within < 1 second.



If we clear these three, the system is viable.