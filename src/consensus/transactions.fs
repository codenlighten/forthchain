\ =========================================================
\ Transaction Structures and Operations
\ =========================================================
\ Bitcoin-compatible transaction format for ForthCoin
\
\ Transaction structure:
\   - Version (4 bytes)
\   - Input count (varint)
\   - Inputs (array of TX-INPUT)
\   - Output count (varint)
\   - Outputs (array of TX-OUTPUT)
\   - Locktime (4 bytes)

\ Dependencies are loaded by load.fs in correct order

\ =========================================================
\ Buffer Declarations (must be before helper functions)
\ =========================================================

CREATE TX-HASH-BUFFER 1024 ALLOT
256VAR TX-HASH-RESULT

\ =========================================================
\ Helper Functions (defined early for forward references)
\ =========================================================

\ Dummy hash input buffer creation (needs proper implementation)
: CREATE-HASH-INPUT-BUFFER ( -- addr )
    TX-HASH-BUFFER ;

\ Dummy compute hash (uses global SHA-256)
: COMPUTE-HASH ( -- )
    \ This would call SHA-256 functions
    \ For now: stub
    ;

\ =========================================================
\ Transaction Input (TxIn)
\ =========================================================
\ Previous transaction hash (32 bytes)
\ Previous output index (4 bytes)
\ Script signature length (1 byte for now)
\ Script signature (variable, signature + pubkey)
\ Sequence (4 bytes)

: TXIN-SIZE ( -- n ) 
    32      \ prev tx hash
    4 +     \ prev output index
    1 +     \ script length
    72 +    \ signature (r+s = 64 bytes) + pubkey (64 bytes compressed to 33, but we'll use 64)
    4 +     \ sequence
    ;       \ = 113 bytes per input

\ Allocate space for a transaction input
: CREATE-TXIN ( -- addr ) 
    TXIN-SIZE ALLOCATE THROW ;

\ Offsets within TXIN
: TXIN-PREVHASH ( txin-addr -- hash-addr ) ;
: TXIN-PREVINDEX ( txin-addr -- index-addr ) 32 + ;
: TXIN-SCRIPTLEN ( txin-addr -- len-addr ) 36 + ;
: TXIN-SCRIPT ( txin-addr -- script-addr ) 37 + ;
: TXIN-SEQUENCE ( txin-addr -- seq-addr ) 109 + ;

\ =========================================================
\ Transaction Output (TxOut)
\ =========================================================
\ Value (8 bytes, satoshis)
\ Script pubkey length (1 byte)
\ Script pubkey (variable, P2PKH is ~25 bytes)

: TXOUT-SIZE ( -- n )
    8       \ value
    1 +     \ script length
    25 +    \ script pubkey (P2PKH)
    ;       \ = 34 bytes per output

\ Allocate space for a transaction output
: CREATE-TXOUT ( -- addr )
    TXOUT-SIZE ALLOCATE THROW ;

\ Offsets within TXOUT
: TXOUT-VALUE ( txout-addr -- value-addr ) ;
: TXOUT-SCRIPTLEN ( txout-addr -- len-addr ) 8 + ;
: TXOUT-SCRIPT ( txout-addr -- script-addr ) 9 + ;

\ =========================================================
\ Transaction Structure
\ =========================================================
\ For simplicity: support up to 8 inputs and 8 outputs

8 CONSTANT MAX-INPUTS
8 CONSTANT MAX-OUTPUTS

: TX-SIZE ( -- n )
    4                           \ version
    1 +                         \ input count
    TXIN-SIZE MAX-INPUTS * +    \ inputs
    1 +                         \ output count
    TXOUT-SIZE MAX-OUTPUTS * +  \ outputs
    4 +                         \ locktime
    ;

\ Allocate space for a transaction
: CREATE-TX ( -- addr )
    TX-SIZE ALLOCATE THROW ;

\ Transaction structure offsets
: TX-VERSION ( tx-addr -- version-addr ) ;
: TX-INPUTCOUNT ( tx-addr -- count-addr ) 4 + ;
: TX-INPUTS ( tx-addr -- inputs-addr ) 5 + ;
: TX-INPUT ( tx-addr index -- input-addr ) 
    TXIN-SIZE * SWAP TX-INPUTS + ;
: TX-OUTPUTCOUNT ( tx-addr -- count-addr ) 
    5 TXIN-SIZE MAX-INPUTS * + ;
: TX-OUTPUTS ( tx-addr -- outputs-addr )
    TX-OUTPUTCOUNT 1+ ;
: TX-OUTPUT ( tx-addr index -- output-addr )
    TXOUT-SIZE * SWAP TX-OUTPUTS + ;
: TX-LOCKTIME ( tx-addr -- locktime-addr )
    5 TXIN-SIZE MAX-INPUTS * + 
    1 + TXOUT-SIZE MAX-OUTPUTS * + ;

\ =========================================================
\ Transaction Initialization
\ =========================================================

: INIT-TX ( tx-addr -- )
    DUP TX-SIZE 0 FILL
    DUP TX-VERSION 1 SWAP !     \ Version 1
    DUP TX-INPUTCOUNT 0 SWAP C! \ 0 inputs
    DUP TX-OUTPUTCOUNT 0 SWAP C! \ 0 outputs
    TX-LOCKTIME 0 SWAP !        \ Locktime 0
    ;

\ =========================================================
\ Add Input to Transaction
\ =========================================================

: ADD-INPUT ( tx-addr prevhash previndex -- )
    >R >R                       \ Save previndex and prevhash
    DUP TX-INPUTCOUNT C@        \ Get current input count
    DUP MAX-INPUTS >= IF
        CR ." Error: Max inputs reached" CR
        R> R> 2DROP DROP EXIT
    THEN
    
    \ Get address of new input
    2DUP TX-INPUT               \ tx-addr count input-addr
    
    \ Copy previous tx hash (32 bytes)
    R> OVER TXIN-PREVHASH 32 CMOVE
    
    \ Set previous output index
    R> OVER TXIN-PREVINDEX !
    
    \ Initialize script length to 0 (will be set when signing)
    0 OVER TXIN-SCRIPTLEN C!
    
    \ Set sequence to 0xFFFFFFFF (standard)
    $FFFFFFFF SWAP TXIN-SEQUENCE !
    
    \ Increment input count
    1+ SWAP TX-INPUTCOUNT C! ;

\ =========================================================
\ Add Output to Transaction
\ =========================================================

: ADD-OUTPUT ( tx-addr value scriptpubkey scriptlen -- )
    >R >R >R                    \ Save scriptlen, scriptpubkey, value
    DUP TX-OUTPUTCOUNT C@       \ Get current output count
    DUP MAX-OUTPUTS >= IF
        CR ." Error: Max outputs reached" CR
        R> R> R> 3DROP DROP EXIT
    THEN
    
    \ Get address of new output
    2DUP TX-OUTPUT              \ tx-addr count output-addr
    
    \ Set value
    R> OVER TXOUT-VALUE !
    
    \ Set script length
    R> DUP OVER TXOUT-SCRIPTLEN C!
    
    \ Copy script pubkey
    R> OVER TXOUT-SCRIPT ROT CMOVE
    
    \ Increment output count
    1+ SWAP TX-OUTPUTCOUNT C! ;

\ =========================================================
\ Transaction Hashing (for signing)
\ =========================================================
\ Compute hash of transaction for signing
\ Simplified: just hash the serialized transaction

: SERIALIZE-TX ( tx-addr buffer -- length )
    >R                          \ Save buffer address
    
    \ Start at buffer
    0                           \ Length counter
    
    \ Copy version (4 bytes)
    OVER TX-VERSION @ R@ !
    4 +
    
    \ Copy input count (1 byte)
    OVER TX-INPUTCOUNT C@ R@ 4 + C!
    1+
    
    \ Copy inputs
    OVER TX-INPUTCOUNT C@ 0 ?DO
        OVER I TX-INPUT         \ Get input address
        R@ 5 I TXIN-SIZE * + +  \ Destination in buffer
        TXIN-SIZE CMOVE         \ Copy entire input
        TXIN-SIZE +             \ Update length
    LOOP
    
    \ Copy output count (1 byte)
    OVER TX-OUTPUTCOUNT C@
    R@ 5 TXIN-SIZE MAX-INPUTS * + + C!
    1+
    
    \ Copy outputs
    OVER TX-OUTPUTCOUNT C@ 0 ?DO
        OVER I TX-OUTPUT        \ Get output address
        R@ 5 TXIN-SIZE MAX-INPUTS * + 1+
        I TXOUT-SIZE * + +      \ Destination in buffer
        TXOUT-SIZE CMOVE        \ Copy entire output
        TXOUT-SIZE +            \ Update length
    LOOP
    
    \ Copy locktime (4 bytes)
    OVER TX-LOCKTIME @ 
    R@ 5 TXIN-SIZE MAX-INPUTS * + 1+
    TXOUT-SIZE MAX-OUTPUTS * + + !
    4 +
    
    NIP R> DROP                 \ Return length, clean up
    ;

: HASH-TX ( tx-addr -- hash-addr )
    TX-HASH-BUFFER SERIALIZE-TX \ Get serialized transaction
    
    \ Hash the serialized data (simplified - should be double SHA-256)
    TX-HASH-BUFFER SWAP         \ buffer length
    
    \ For now: just hash first 64 bytes (will need proper implementation)
    DROP 64                     \ Simplified: hash 64 bytes
    
    \ Prepare for SHA-256
    CREATE-HASH-INPUT-BUFFER
    TX-HASH-BUFFER OVER 64 CMOVE
    
    \ Compute SHA-256
    COMPUTE-HASH
    
    \ Return hash result address
    TX-HASH-RESULT ;

\ =========================================================
\ Transaction Signing
\ =========================================================

: SIGN-TX-INPUT ( tx-addr input-index privkey -- )
    >R                          \ Save privkey
    
    \ Get transaction hash
    OVER HASH-TX                \ tx-addr input-index hash-addr
    
    \ Sign with private key
    R> SIGN-MESSAGE             \ Returns r-addr s-addr
    
    \ Store signature in input's script
    \ Simplified: just mark as signed for now
    ROT ROT TX-INPUT            \ Get input address
    DUP TXIN-SCRIPTLEN 64 SWAP C!  \ Set script length (r + s = 64 bytes)
    
    \ TODO: Copy r and s into script field
    DROP DROP                   \ Clean up r-addr s-addr for now
    ;

\ =========================================================
\ Transaction Verification
\ =========================================================

: VERIFY-TX-INPUT ( tx-addr input-index pubkey -- valid? )
    >R                          \ Save pubkey
    
    \ Get transaction hash
    OVER HASH-TX                \ tx-addr input-index hash-addr
    
    \ Get signature from input
    ROT ROT TX-INPUT            \ hash-addr input-addr
    
    \ Get r and s from script (simplified)
    DUP TXIN-SCRIPT             \ hash-addr input-addr script-addr
    
    \ For now: return true (full implementation needs signature extraction)
    R> 2DROP DROP TRUE ;

\ =========================================================
\ Transaction Display
\ =========================================================

: .TX ( tx-addr -- )
    CR ." Transaction:" CR
    ."   Version: " DUP TX-VERSION @ . CR
    ."   Inputs: " DUP TX-INPUTCOUNT C@ . CR
    ."   Outputs: " DUP TX-OUTPUTCOUNT C@ . CR
    ."   Locktime: " TX-LOCKTIME @ . CR ;

\ =========================================================
\ Helper Functions
\ =========================================================

\ Create a P2PKH (Pay to Public Key Hash) script
: CREATE-P2PKH-SCRIPT ( pubkey-hash script-addr -- )
    \ OP_DUP OP_HASH160 <pubkeyhash> OP_EQUALVERIFY OP_CHECKSIG
    $76 OVER C!         \ OP_DUP
    $A9 OVER 1+ C!      \ OP_HASH160
    $14 OVER 2 + C!     \ Push 20 bytes
    OVER 3 + 20 CMOVE   \ Copy pubkey hash
    $88 OVER 23 + C!    \ OP_EQUALVERIFY
    $AC SWAP 24 + C!    \ OP_CHECKSIG
    ;                   \ Total: 25 bytes

\ =========================================================
\ Initialization
\ =========================================================

CR ." =========================================" CR
CR ." Transaction module loaded" CR
CR ." =========================================" CR
CR ." Structures:" CR
CR ."   - Transaction (inputs + outputs)" CR
CR ."   - TX-INPUT (prev tx + signature)" CR
CR ."   - TX-OUTPUT (value + script)" CR
CR ." =========================================" CR
CR ." Functions:" CR
CR ."   - CREATE-TX, INIT-TX" CR
CR ."   - ADD-INPUT, ADD-OUTPUT" CR
CR ."   - HASH-TX, SIGN-TX-INPUT" CR
CR ."   - VERIFY-TX-INPUT" CR
CR ." =========================================" CR
