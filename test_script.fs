\ =========================================================
\ FORTHCOIN SCRIPT VM TEST
\ =========================================================
\ Test Bitcoin Script execution engine

INCLUDE src/load.fs

CR
." ════════════════════════════════════════════════════════" CR
." 📜 SCRIPT VM TEST SUITE" CR
." ════════════════════════════════════════════════════════" CR
CR

VARIABLE TEST-PASSED
VARIABLE TEST-FAILED

: TEST-START ( -- )
    0 TEST-PASSED !
    0 TEST-FAILED ! ;

: TEST-ASSERT ( flag test-name -- )
    SWAP IF
        ." ✓ " TYPE CR
        1 TEST-PASSED +!
    ELSE
        ." ✗ " TYPE CR
        1 TEST-FAILED +!
    THEN ;

: TEST-RESULTS ( -- )
    CR
    ." ════════════════════════════════════════════════════════" CR
    ." TEST RESULTS:" CR
    ." ════════════════════════════════════════════════════════" CR
    ." Passed: " TEST-PASSED @ . CR
    ." Failed: " TEST-FAILED @ . CR
    TEST-FAILED @ 0= IF
        ." " CR
        ." 🎉 ALL SCRIPT TESTS PASSED!" CR
    ELSE
        ." " CR
        ." ⚠️  SOME TESTS FAILED!" CR
    THEN
    ." ════════════════════════════════════════════════════════" CR
    CR ;

TEST-START

\ ---------------------------------------------------------
\ Test 1: Stack Operations
\ ---------------------------------------------------------

CR ." Test 1: Basic stack operations..." CR

INIT-SCRIPT-STACK

\ Push value
CREATE TEST-VAL1 32 ALLOT
TEST-VAL1 32 ERASE
42 TEST-VAL1 !

TEST-VAL1 SCRIPT-PUSH
SCRIPT-DEPTH 1 = s" Push increases depth" TEST-ASSERT

\ Duplicate
EXEC-OP_DUP
SCRIPT-DEPTH 2 = s" DUP increases depth" TEST-ASSERT

\ Swap
CREATE TEST-VAL2 32 ALLOT
TEST-VAL2 32 ERASE
99 TEST-VAL2 !
TEST-VAL2 SCRIPT-PUSH

EXEC-OP_SWAP
SCRIPT-DEPTH 3 = s" SWAP maintains depth" TEST-ASSERT

\ Drop
EXEC-OP_DROP
SCRIPT-DEPTH 2 = s" DROP decreases depth" TEST-ASSERT

\ ---------------------------------------------------------
\ Test 2: Arithmetic Operations
\ ---------------------------------------------------------

CR ." Test 2: Arithmetic operations..." CR

INIT-SCRIPT-STACK

\ Push 5 and 3
CREATE VAL-5 32 ALLOT
CREATE VAL-3 32 ALLOT
5 VAL-5 INT-TO-SCRIPT
3 VAL-3 INT-TO-SCRIPT

VAL-5 SCRIPT-PUSH
VAL-3 SCRIPT-PUSH

\ Add: 5 + 3 = 8
EXEC-OP_ADD

CREATE RESULT 32 ALLOT
RESULT SCRIPT-POP
RESULT SCRIPT-TO-INT 8 = s" ADD works (5+3=8)" TEST-ASSERT

\ Subtraction: 10 - 4 = 6
INIT-SCRIPT-STACK
CREATE VAL-10 32 ALLOT
CREATE VAL-4 32 ALLOT
10 VAL-10 INT-TO-SCRIPT
4 VAL-4 INT-TO-SCRIPT

VAL-10 SCRIPT-PUSH
VAL-4 SCRIPT-PUSH
EXEC-OP_SUB

RESULT SCRIPT-POP
RESULT SCRIPT-TO-INT 6 = s" SUB works (10-4=6)" TEST-ASSERT

\ ---------------------------------------------------------
\ Test 3: Comparison Operations
\ ---------------------------------------------------------

CR ." Test 3: Comparison operations..." CR

INIT-SCRIPT-STACK

\ Equal: 42 == 42
CREATE VAL-42A 32 ALLOT
CREATE VAL-42B 32 ALLOT
42 VAL-42A INT-TO-SCRIPT
42 VAL-42B INT-TO-SCRIPT

VAL-42A SCRIPT-PUSH
VAL-42B SCRIPT-PUSH
EXEC-OP_NUMEQUAL

RESULT SCRIPT-POP
RESULT SCRIPT-TRUE? s" NUMEQUAL works (42==42)" TEST-ASSERT

\ Less than: 5 < 10
INIT-SCRIPT-STACK
5 VAL-5 INT-TO-SCRIPT
10 VAL-10 INT-TO-SCRIPT

VAL-5 SCRIPT-PUSH
VAL-10 SCRIPT-PUSH
EXEC-OP_LESSTHAN

RESULT SCRIPT-POP
RESULT SCRIPT-TRUE? s" LESSTHAN works (5<10)" TEST-ASSERT

\ ---------------------------------------------------------
\ Test 4: Cryptographic Operations
\ ---------------------------------------------------------

CR ." Test 4: Crypto operations..." CR

INIT-SCRIPT-STACK

\ SHA256 of known value
CREATE TEST-DATA 32 ALLOT
TEST-DATA 32 ERASE
$DEADBEEF TEST-DATA !

TEST-DATA SCRIPT-PUSH
EXEC-OP_SHA256

SCRIPT-DEPTH 1 = s" SHA256 returns hash" TEST-ASSERT

\ HASH256 (double SHA256)
INIT-SCRIPT-STACK
TEST-DATA SCRIPT-PUSH
EXEC-OP_HASH256

SCRIPT-DEPTH 1 = s" HASH256 returns hash" TEST-ASSERT

\ ---------------------------------------------------------
\ Test 5: P2PKH Script
\ ---------------------------------------------------------

CR ." Test 5: P2PKH script creation..." CR

CREATE PUBKEY-HASH 32 ALLOT
PUBKEY-HASH 32 ERASE
$ABCDEF01 PUBKEY-HASH !

PUBKEY-HASH CREATE-P2PKH-SCRIPT

DUP 25 = s" P2PKH script is 25 bytes" TEST-ASSERT
DROP

P2PKH-SCRIPT C@ OP_DUP = s" P2PKH starts with OP_DUP" TEST-ASSERT
P2PKH-SCRIPT 1+ C@ OP_HASH160 = s" P2PKH has OP_HASH160" TEST-ASSERT
P2PKH-SCRIPT 24 + C@ OP_CHECKSIG = s" P2PKH ends with OP_CHECKSIG" TEST-ASSERT

CR ." P2PKH Script:" CR
P2PKH-SCRIPT 25 DISASSEMBLE-SCRIPT

\ ---------------------------------------------------------
\ Test 6: Time-Lock Script
\ ---------------------------------------------------------

CR ." Test 6: Time-lock script..." CR

\ Create time-locked script (locktime = 1000000)
1000000 PUBKEY-HASH CREATE-TIMELOCK-SCRIPT

DUP 0> s" Timelock script created" TEST-ASSERT
DROP

TIMELOCK-SCRIPT 5 + C@ OP_CHECKLOCKTIMEVERIFY = 
s" Timelock has CHECKLOCKTIMEVERIFY" TEST-ASSERT

CR ." Time-Lock Script:" CR
TIMELOCK-SCRIPT 32 DISASSEMBLE-SCRIPT

\ ---------------------------------------------------------
\ Test 7: Hash-Lock Script (Atomic Swaps)
\ ---------------------------------------------------------

CR ." Test 7: Hash-lock script..." CR

\ Create hash-lock with known hash
CREATE SECRET-HASH 32 ALLOT
SECRET-HASH 32 ERASE
$12345678 SECRET-HASH !

SECRET-HASH CREATE-HASHLOCK-SCRIPT

DUP 0> s" Hashlock script created" TEST-ASSERT
DROP

HASHLOCK-SCRIPT C@ OP_SHA256 = s" Hashlock starts with OP_SHA256" TEST-ASSERT

CR ." Hash-Lock Script:" CR
HASHLOCK-SCRIPT 35 DISASSEMBLE-SCRIPT

\ ---------------------------------------------------------
\ Test 8: Script Execution
\ ---------------------------------------------------------

CR ." Test 8: Simple script execution..." CR

\ Create simple script: PUSH 1, PUSH 1, ADD, PUSH 2, NUMEQUAL
CREATE SIMPLE-SCRIPT 10 ALLOT
SIMPLE-SCRIPT 10 ERASE

\ Script bytes (simplified)
$51 SIMPLE-SCRIPT C!      \ OP_1
$51 SIMPLE-SCRIPT 1+ C!   \ OP_1
OP_ADD SIMPLE-SCRIPT 2 + C!
$52 SIMPLE-SCRIPT 3 + C!  \ OP_2
OP_NUMEQUAL SIMPLE-SCRIPT 4 + C!

SIMPLE-SCRIPT 5 RUN-SCRIPT s" Simple script executes" TEST-ASSERT

\ ---------------------------------------------------------
\ Test 9: Script Stack Depth
\ ---------------------------------------------------------

CR ." Test 9: Stack depth management..." CR

INIT-SCRIPT-STACK
0 SCRIPT-DEPTH = s" Fresh stack is empty" TEST-ASSERT

TEST-VAL1 SCRIPT-PUSH
TEST-VAL1 SCRIPT-PUSH
TEST-VAL1 SCRIPT-PUSH

SCRIPT-DEPTH 3 = s" Stack depth tracking" TEST-ASSERT

RESULT SCRIPT-POP
RESULT SCRIPT-POP
RESULT SCRIPT-POP

SCRIPT-DEPTH 0 = s" Stack empties correctly" TEST-ASSERT

\ ---------------------------------------------------------
\ Test 10: CHECKSIG Operation
\ ---------------------------------------------------------

CR ." Test 10: Signature verification..." CR

INIT-SCRIPT-STACK

\ Push dummy signature and pubkey
CREATE DUMMY-SIG 64 ALLOT
CREATE DUMMY-PUBKEY 64 ALLOT
DUMMY-SIG 64 ERASE
DUMMY-PUBKEY 64 ERASE

DUMMY-SIG SCRIPT-PUSH
DUMMY-PUBKEY SCRIPT-PUSH

EXEC-OP_CHECKSIG

SCRIPT-DEPTH 1 = s" CHECKSIG returns result" TEST-ASSERT

\ ---------------------------------------------------------
\ Test Results
\ ---------------------------------------------------------

TEST-RESULTS

\ ---------------------------------------------------------
\ Usage Examples
\ ---------------------------------------------------------

CR
." ════════════════════════════════════════════════════════" CR
." 💡 SCRIPT USE CASES" CR
." ════════════════════════════════════════════════════════" CR
CR

." 1. TIME-LOCKED VESTING:" CR
." " CR
."    Government salary locked until date" CR
."    Employee can spend after vesting period" CR
."    " CR
."    VESTING-DATE EMPLOYEE-PUBKEY-HASH" CR
."    CREATE-TIMELOCK-SCRIPT" CR
CR

." 2. HASH-LOCKED ATOMIC SWAP:" CR
." " CR
."    Cross-chain trades without trust" CR
."    Reveal secret to claim funds" CR
."    " CR
."    SECRET-HASH CREATE-HASHLOCK-SCRIPT" CR
."    \ Sender must reveal preimage" CR
CR

." 3. MULTI-SIG + TIME-LOCK:" CR
." " CR
."    2-of-3 approval, but after 30 days" CR
."    Single key can recover if others lost" CR
."    " CR
."    \ Combine multisig with CLTV" CR
CR

." 4. CONDITIONAL PAYMENTS:" CR
." " CR
."    Pay if conditions met" CR
."    Refund if not claimed by deadline" CR
."    " CR
."    IF <condition> THEN <pay> ELSE <refund>" CR
CR

." ════════════════════════════════════════════════════════" CR
." 🎯 SCRIPT CAPABILITIES" CR
." ════════════════════════════════════════════════════════" CR
CR

." ✓ Stack operations (DUP, DROP, SWAP, etc.)" CR
." ✓ Arithmetic (ADD, SUB, comparison)" CR
." ✓ Cryptographic hashing (SHA256, HASH160)" CR
." ✓ Signature verification (CHECKSIG)" CR
." ✓ Multi-signature (CHECKMULTISIG)" CR
." ✓ Time-locks (CHECKLOCKTIMEVERIFY)" CR
." ✓ Sequence locks (CHECKSEQUENCEVERIFY)" CR
." ✓ Hash-locks (for atomic swaps)" CR
." ✓ Script templates (P2PKH, P2SH)" CR
." ✓ Full interpreter with execution" CR
CR

." ════════════════════════════════════════════════════════" CR
." 🔧 GOVERNMENT APPLICATIONS" CR
." ════════════════════════════════════════════════════════" CR
CR

." BUDGET RELEASE SCHEDULE:" CR
."   • Funds locked until fiscal quarter" CR
."   • Automatic release on date" CR
."   • No manual intervention needed" CR
CR

." CONTRACTOR ESCROW:" CR
."   • Payment locked with hash" CR
."   • Contractor proves work completion" CR
."   • Reveals secret to claim funds" CR
CR

." PENSION VESTING:" CR
."   • Benefits locked for 5 years" CR
."   • Employee keeps keys" CR
."   • Automatic claim after period" CR
CR

." PROCUREMENT DEPOSITS:" CR
."   • Bidder locks deposit" CR
."   • Refunded if not selected" CR
."   • Forfeited if backing out" CR
CR

." ════════════════════════════════════════════════════════" CR
CR

." 🚀 Try creating scripts:" CR
." " CR
."   gforth src/load.fs" CR
."   " CR
."   \ Create time-lock" CR
."   1704067200 PUBKEY-HASH CREATE-TIMELOCK-SCRIPT" CR
."   " CR
."   \ Create hash-lock" CR
."   MY-HASH CREATE-HASHLOCK-SCRIPT" CR
."   " CR
."   \ Execute script" CR
."   SCRIPT-ADDR SCRIPT-LEN RUN-SCRIPT" CR
CR

." ════════════════════════════════════════════════════════" CR
CR
