\ =========================================================
\ FORTHCOIN MULTI-SIG TEST
\ =========================================================
\ Test multi-signature wallet functionality

INCLUDE src/load.fs

CR
." ════════════════════════════════════════════════════════" CR
." 🔐 MULTI-SIGNATURE WALLET TEST" CR
." ════════════════════════════════════════════════════════" CR
CR

\ ---------------------------------------------------------
\ Test Setup
\ ---------------------------------------------------------

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
        ." 🎉 ALL MULTI-SIG TESTS PASSED!" CR
    ELSE
        ." " CR
        ." ⚠️  SOME TESTS FAILED!" CR
    THEN
    ." ════════════════════════════════════════════════════════" CR
    CR ;

TEST-START

\ ---------------------------------------------------------
\ Test 1: Create Wallets
\ ---------------------------------------------------------

CR ." Test 1: Creating test wallets..." CR

CREATE WALLET-A WALLET-SIZE ALLOT
CREATE WALLET-B WALLET-SIZE ALLOT
CREATE WALLET-C WALLET-SIZE ALLOT

WALLET-A INIT-WALLET
WALLET-B INIT-WALLET
WALLET-C INIT-WALLET

WALLET-A WALLET-PRIVKEY @ 0<> s" Wallet A created" TEST-ASSERT
WALLET-B WALLET-PRIVKEY @ 0<> s" Wallet B created" TEST-ASSERT
WALLET-C WALLET-PRIVKEY @ 0<> s" Wallet C created" TEST-ASSERT

CR ." Wallet A Address: "
WALLET-A WALLET-ADDRESS 25 0 DO
    DUP I + C@ .BYTE
LOOP DROP CR

CR ." Wallet B Address: "
WALLET-B WALLET-ADDRESS 25 0 DO
    DUP I + C@ .BYTE
LOOP DROP CR

CR ." Wallet C Address: "
WALLET-C WALLET-ADDRESS 25 0 DO
    DUP I + C@ .BYTE
LOOP DROP CR

\ ---------------------------------------------------------
\ Test 2: Create Redeem Script
\ ---------------------------------------------------------

CR ." Test 2: Creating redeem script (2-of-3)..." CR

CREATE PUBKEY-ARRAY 192 ALLOT

\ Copy public keys to array
WALLET-A WALLET-PUBKEY PUBKEY-ARRAY 64 CMOVE
WALLET-B WALLET-PUBKEY PUBKEY-ARRAY 64 + 64 CMOVE
WALLET-C WALLET-PUBKEY PUBKEY-ARRAY 128 + 64 CMOVE

2 3 PUBKEY-ARRAY CREATE-MULTISIG-SCRIPT

DUP 0> s" Redeem script created" TEST-ASSERT

CR ." Redeem Script Length: " . ." bytes" CR
CR ." Redeem Script (first 32 bytes): "
REDEEM-SCRIPT 32 0 DO
    DUP I + C@ .BYTE
LOOP DROP CR

\ ---------------------------------------------------------
\ Test 3: Generate P2SH Address
\ ---------------------------------------------------------

CR ." Test 3: Generating P2SH address..." CR

REDEEM-SCRIPT REDEEM-SCRIPT-LEN @ REDEEM-SCRIPT->P2SH

DUP C@ $05 = s" P2SH version byte correct" TEST-ASSERT

CR ." P2SH Address: "
25 0 DO
    DUP I + C@ .BYTE
LOOP DROP CR

\ ---------------------------------------------------------
\ Test 4: Create Multi-Sig Wallet
\ ---------------------------------------------------------

CR ." Test 4: Creating 2-of-3 multi-sig wallet..." CR

CREATE TEST-MULTISIG MULTISIG-WALLET-SIZE ALLOT

WALLET-A WALLET-PUBKEY
WALLET-B WALLET-PUBKEY
WALLET-C WALLET-PUBKEY
TEST-MULTISIG CREATE-2OF3-MULTISIG

TEST-MULTISIG MULTISIG-M C@ 2 = s" M=2 set correctly" TEST-ASSERT
TEST-MULTISIG MULTISIG-N C@ 3 = s" N=3 set correctly" TEST-ASSERT
TEST-MULTISIG MULTISIG-SCRIPT-LEN @ 0> s" Script stored" TEST-ASSERT

CR
TEST-MULTISIG SHOW-MULTISIG-WALLET

\ ---------------------------------------------------------
\ Test 5: Treasury Wallet (3-of-5)
\ ---------------------------------------------------------

CR ." Test 5: Creating treasury wallet (3-of-5)..." CR

CREATE BOARD-WALLETS 5 WALLET-SIZE * ALLOT
CREATE BOARD-PUBKEYS 5 64 * ALLOT
CREATE TREASURY-MULTISIG MULTISIG-WALLET-SIZE ALLOT

\ Generate 5 board member wallets
5 0 DO
    I WALLET-SIZE * BOARD-WALLETS +
    DUP INIT-WALLET
    
    ." [Board " I 1+ . ." ] "
    DUP WALLET-ADDRESS 10 0 DO
        DUP I + C@ .BYTE
    LOOP ." ..." DROP CR
    
    \ Copy pubkey to array
    WALLET-PUBKEY
    I 64 * BOARD-PUBKEYS +
    64 CMOVE
LOOP

BOARD-PUBKEYS TREASURY-MULTISIG CREATE-TREASURY-WALLET

TREASURY-MULTISIG MULTISIG-M C@ 3 = s" Treasury M=3" TEST-ASSERT
TREASURY-MULTISIG MULTISIG-N C@ 5 = s" Treasury N=5" TEST-ASSERT

CR
TREASURY-MULTISIG SHOW-MULTISIG-WALLET

\ ---------------------------------------------------------
\ Test 6: Budget Wallet (2-of-3)
\ ---------------------------------------------------------

CR ." Test 6: Creating budget wallet (2-of-3)..." CR

CREATE MGR-WALLETS 3 WALLET-SIZE * ALLOT
CREATE MGR-PUBKEYS 3 64 * ALLOT
CREATE BUDGET-MULTISIG MULTISIG-WALLET-SIZE ALLOT

3 0 DO
    I WALLET-SIZE * MGR-WALLETS +
    DUP INIT-WALLET
    
    ." [Manager " I 1+ . ." ] "
    DUP WALLET-ADDRESS 10 0 DO
        DUP I + C@ .BYTE
    LOOP ." ..." DROP CR
    
    WALLET-PUBKEY
    I 64 * MGR-PUBKEYS +
    64 CMOVE
LOOP

MGR-PUBKEYS BUDGET-MULTISIG CREATE-BUDGET-WALLET

BUDGET-MULTISIG MULTISIG-M C@ 2 = s" Budget M=2" TEST-ASSERT
BUDGET-MULTISIG MULTISIG-N C@ 3 = s" Budget N=3" TEST-ASSERT

CR
BUDGET-MULTISIG SHOW-MULTISIG-WALLET

\ ---------------------------------------------------------
\ Test 7: Partial Signatures
\ ---------------------------------------------------------

CR ." Test 7: Testing partial signatures..." CR

INIT-PARTIAL-SIGS

\ Create dummy signatures
CREATE SIG1 64 ALLOT
CREATE SIG2 64 ALLOT

SIG1 64 ERASE
SIG2 64 ERASE

$AABBCCDD SIG1 !
$11223344 SIG2 !

\ Add partial signatures
SIG1 WALLET-A WALLET-PUBKEY ADD-PARTIAL-SIG
SIG2 WALLET-B WALLET-PUBKEY ADD-PARTIAL-SIG

PARTIAL-SIG-COUNT @ 2 = s" Partial signatures added" TEST-ASSERT

CR
SHOW-PARTIAL-SIGS

\ ---------------------------------------------------------
\ Test 8: Check Signature Threshold
\ ---------------------------------------------------------

CR ." Test 8: Testing signature threshold..." CR

\ 2-of-3 wallet with 2 signatures should pass
TEST-MULTISIG HAVE-ENOUGH-SIGS? s" Threshold met (2-of-3)" TEST-ASSERT

\ 3-of-5 treasury with only 2 signatures should fail
INIT-PARTIAL-SIGS
SIG1 WALLET-A WALLET-PUBKEY ADD-PARTIAL-SIG
SIG2 WALLET-B WALLET-PUBKEY ADD-PARTIAL-SIG

TREASURY-MULTISIG HAVE-ENOUGH-SIGS? 0= s" Threshold NOT met (2-of-5)" TEST-ASSERT

\ Add third signature
CREATE SIG3 64 ALLOT
SIG3 64 ERASE
$55667788 SIG3 !
SIG3 WALLET-C WALLET-PUBKEY ADD-PARTIAL-SIG

TREASURY-MULTISIG HAVE-ENOUGH-SIGS? s" Threshold met (3-of-5)" TEST-ASSERT

\ ---------------------------------------------------------
\ Test Results
\ ---------------------------------------------------------

TEST-RESULTS

\ ---------------------------------------------------------
\ Usage Examples
\ ---------------------------------------------------------

CR
." ════════════════════════════════════════════════════════" CR
." 💼 GOVERNMENT USE CASE EXAMPLES" CR
." ════════════════════════════════════════════════════════" CR
CR

." 1. TREASURY MANAGEMENT:" CR
." " CR
."    Board of 5 members controls government funds" CR
."    Requires 3 signatures for any expenditure" CR
."    " CR
."    CREATE TREASURY-WALLET MULTISIG-WALLET-SIZE ALLOT" CR
."    BOARD-KEYS TREASURY-WALLET CREATE-TREASURY-WALLET" CR
CR

." 2. DEPARTMENT BUDGET:" CR
." " CR
."    3 managers control department spending" CR
."    Requires 2 signatures for approval" CR
."    " CR
."    CREATE DEPT-WALLET MULTISIG-WALLET-SIZE ALLOT" CR
."    MANAGER-KEYS DEPT-WALLET CREATE-BUDGET-WALLET" CR
CR

." 3. LAND REGISTRY:" CR
." " CR
."    Joint property ownership" CR
."    Both owners must sign for transfer" CR
."    " CR
."    OWNER1-KEY OWNER2-KEY PROPERTY-WALLET" CR
."    CREATE-PROPERTY-WALLET" CR
CR

." 4. ESCROW SERVICE:" CR
." " CR
."    Buyer, Seller, and Arbiter" CR
."    Any 2 of 3 can release funds" CR
."    " CR
."    BUYER-KEY SELLER-KEY ARBITER-KEY ESCROW-WALLET" CR
."    CREATE-ESCROW-WALLET" CR
CR

." ════════════════════════════════════════════════════════" CR
." 📊 MULTI-SIG CAPABILITIES" CR
." ════════════════════════════════════════════════════════" CR
CR

." ✓ P2SH address generation (Pay-to-Script-Hash)" CR
." ✓ M-of-N signature requirements (configurable)" CR
." ✓ Redeem script construction" CR
." ✓ Partial signature collection" CR
." ✓ Threshold verification" CR
." ✓ Common configurations (2-of-3, 3-of-5)" CR
." ✓ Government-specific wallets" CR
." ✓ CLI interface integration" CR
CR

." ════════════════════════════════════════════════════════" CR
." 🎯 NEXT STEPS" CR
." ════════════════════════════════════════════════════════" CR
CR

." Try the interactive CLI:" CR
." " CR
."   gforth src/load.fs" CR
."   wallet-cli" CR
."   multisig-treasury     \ Create 3-of-5 treasury" CR
."   multisig-budget       \ Create 2-of-3 budget" CR
."   multisig-info         \ Show wallet details" CR
CR

." ════════════════════════════════════════════════════════" CR
CR

: .BYTE ( c -- )
    BASE @ >R HEX
    DUP 4 RSHIFT 15 AND
    DUP 10 < IF 48 + ELSE 55 + THEN EMIT
    15 AND
    DUP 10 < IF 48 + ELSE 55 + THEN EMIT
    R> BASE ! ;
