\ =========================================================
\ Wallet Functions
\ =========================================================
\ Key management, address generation, transaction creation
\
\ Features:
\   - Private/public key generation
\   - Address creation (base58check encoding simplified)
\   - Transaction building
\   - Balance tracking

REQUIRE ../crypto/ecc.fs
REQUIRE consensus/transactions.fs

\ =========================================================
\ Wallet Structure
\ =========================================================
\ Stores private key, public key, and address

: WALLET-SIZE ( -- n )
    32      \ private key
    64 +    \ public key (point)
    25 +    \ address (simplified)
    ;       \ = 121 bytes

\ Allocate wallet
: CREATE-WALLET ( -- addr )
    WALLET-SIZE ALLOCATE THROW ;

\ Wallet offsets
: WALLET-PRIVKEY ( wallet-addr -- privkey-addr ) ;
: WALLET-PUBKEY ( wallet-addr -- pubkey-addr ) 32 + ;
: WALLET-ADDRESS ( wallet-addr -- address-addr ) 96 + ;

\ =========================================================
\ Key Generation
\ =========================================================

\ Generate a new random private key (simplified - use pseudorandom)
VARIABLE PRNG-STATE
12345 PRNG-STATE !

: RANDOM-32BIT ( -- n )
    PRNG-STATE @
    1103515245 * 12345 +
    DUP PRNG-STATE !
    ;

: GENERATE-PRIVKEY ( wallet-addr -- )
    WALLET-PRIVKEY
    \ Generate 4 random 64-bit values
    8 0 DO
        RANDOM-32BIT OVER I + !
    LOOP
    DROP ;

\ Generate public key from private key
: GENERATE-WALLET-PUBKEY ( wallet-addr -- )
    DUP WALLET-PRIVKEY          \ wallet privkey-addr
    OVER WALLET-PUBKEY          \ wallet privkey pubkey-addr
    GENERATE-PUBKEY
    DROP ;

\ =========================================================
\ Address Generation
\ =========================================================
\ Simplified Bitcoin address: Hash(PubKey) with prefix
\ Full implementation would use base58check encoding

256VAR PUBKEY-HASH-TEMP

: HASH160 ( data-addr len -- hash-addr )
    \ Simplified: should be SHA-256 then RIPEMD-160
    \ For now: just use first 20 bytes of SHA-256
    DROP                        \ Drop length
    
    \ Create hash buffer
    CREATE-HASH-INPUT-BUFFER
    SWAP 64 CMOVE               \ Copy public key
    
    \ Compute SHA-256 (simplified)
    COMPUTE-HASH
    
    \ Return first 20 bytes
    PUBKEY-HASH-TEMP ;

: GENERATE-ADDRESS ( wallet-addr -- )
    DUP WALLET-PUBKEY           \ Get public key
    64 HASH160                  \ Hash it (20 bytes)
    
    \ Store in wallet address field
    SWAP WALLET-ADDRESS 20 CMOVE ;

\ =========================================================
\ Initialize Wallet
\ =========================================================

: INIT-WALLET ( wallet-addr -- )
    DUP GENERATE-PRIVKEY
    DUP GENERATE-WALLET-PUBKEY
    GENERATE-ADDRESS ;

\ =========================================================
\ Create Transaction
\ =========================================================

\ Build a simple transaction: send value to recipient
: CREATE-SEND-TX ( wallet-addr recipient-addr value -- tx-addr )
    >R >R                       \ Save recipient and value
    
    \ Create new transaction
    CREATE-TX DUP INIT-TX
    
    \ TODO: Add inputs from UTXO set
    \ For now: create a dummy input
    DUP
    HERE 32 ALLOT               \ Allocate space for prev tx hash
    DUP 32 0 FILL               \ Zero it (coinbase-like)
    0                           \ Previous output index 0
    ADD-INPUT
    
    \ Add output to recipient
    DUP R> R@                   \ tx recipient value
    
    \ Create P2PKH script for recipient
    HERE 25 ALLOT DUP >R        \ Allocate script space
    ROT CREATE-P2PKH-SCRIPT     \ Create script
    R> 25                       \ script-addr script-len
    ADD-OUTPUT
    
    R> DROP                     \ Drop value
    ;

\ =========================================================
\ Sign Transaction with Wallet
\ =========================================================

: WALLET-SIGN-TX ( wallet-addr tx-addr input-index -- )
    >R >R                       \ Save tx-addr and input-index
    WALLET-PRIVKEY              \ Get private key
    R> R>                       \ Restore tx-addr and input-index
    SIGN-TX-INPUT ;

\ =========================================================
\ UTXO Set (Simplified)
\ =========================================================
\ Track unspent transaction outputs
\ For MVP: simple array of UTXOs

: UTXO-SIZE ( -- n )
    32      \ tx hash
    4 +     \ output index
    8 +     \ value
    25 +    \ script
    ;       \ = 69 bytes per UTXO

128 CONSTANT MAX-UTXOS
CREATE UTXO-SET MAX-UTXOS UTXO-SIZE * ALLOT
VARIABLE UTXO-COUNT
0 UTXO-COUNT !

\ UTXO offsets
: UTXO-TXHASH ( utxo-addr -- hash-addr ) ;
: UTXO-INDEX ( utxo-addr -- index-addr ) 32 + ;
: UTXO-VALUE ( utxo-addr -- value-addr ) 36 + ;
: UTXO-SCRIPT ( utxo-addr -- script-addr ) 44 + ;

\ Get UTXO by index
: GET-UTXO ( index -- utxo-addr )
    UTXO-SIZE * UTXO-SET + ;

\ Add UTXO to set
: ADD-UTXO ( txhash output-index value script -- )
    UTXO-COUNT @ MAX-UTXOS >= IF
        CR ." Error: UTXO set full" CR
        DROP DROP DROP DROP EXIT
    THEN
    
    \ Get address of new UTXO
    UTXO-COUNT @ GET-UTXO
    
    \ Copy script (25 bytes)
    OVER UTXO-SCRIPT 25 CMOVE
    
    \ Set value
    OVER UTXO-VALUE !
    
    \ Set output index
    OVER UTXO-INDEX !
    
    \ Copy tx hash (32 bytes)
    SWAP UTXO-TXHASH 32 CMOVE
    
    \ Increment count
    1 UTXO-COUNT +! ;

\ Remove UTXO from set (mark as spent)
: REMOVE-UTXO ( index -- )
    GET-UTXO UTXO-SIZE 0 FILL ;

\ Calculate balance for an address
: GET-BALANCE ( address -- balance )
    0 SWAP                      \ balance address
    UTXO-COUNT @ 0 ?DO
        \ Check if UTXO belongs to this address
        \ Simplified: compare script
        OVER I GET-UTXO UTXO-SCRIPT 25 COMPARE 0= IF
            I GET-UTXO UTXO-VALUE @ +
        THEN
    LOOP
    NIP ;

\ =========================================================
\ Display Functions
\ =========================================================

: .WALLET ( wallet-addr -- )
    CR ." Wallet:" CR
    ."   Private Key: " DUP WALLET-PRIVKEY @ HEX. DECIMAL CR
    ."   Public Key X: " DUP WALLET-PUBKEY POINT-X @ HEX. DECIMAL CR
    ."   Public Key Y: " DUP WALLET-PUBKEY POINT-Y @ HEX. DECIMAL CR
    ."   Address: " WALLET-ADDRESS @ HEX. DECIMAL CR ;

: .UTXO-SET ( -- )
    CR ." UTXO Set:" CR
    ."   Count: " UTXO-COUNT @ . CR
    UTXO-COUNT @ 0 ?DO
        ."   [" I . ." ] "
        I GET-UTXO UTXO-VALUE @ . ." satoshis" CR
    LOOP ;

\ =========================================================
\ Coinbase Transaction (for mining rewards)
\ =========================================================

: CREATE-COINBASE-TX ( recipient-addr reward -- tx-addr )
    >R                          \ Save reward
    
    \ Create new transaction
    CREATE-TX DUP INIT-TX
    
    \ Add coinbase input (no previous tx)
    DUP
    HERE 32 ALLOT DUP 32 0 FILL \ Zero hash (coinbase marker)
    $FFFFFFFF                    \ Max index (coinbase marker)
    ADD-INPUT
    
    \ Add output with mining reward
    DUP R>                      \ tx reward
    
    \ Create P2PKH script
    HERE 25 ALLOT DUP >R
    ROT CREATE-P2PKH-SCRIPT
    R> 25
    ADD-OUTPUT ;

\ =========================================================
\ Initialization
\ =========================================================

CR ." =========================================" CR
CR ." Wallet module loaded" CR
CR ." =========================================" CR
CR ." Features:" CR
CR ."   - Key generation (private/public)" CR
CR ."   - Address generation" CR
CR ."   - Transaction creation" CR
CR ."   - UTXO tracking" CR
CR ."   - Balance calculation" CR
CR ." =========================================" CR
CR ." Functions:" CR
CR ."   - CREATE-WALLET, INIT-WALLET" CR
CR ."   - CREATE-SEND-TX" CR
CR ."   - WALLET-SIGN-TX" CR
CR ."   - ADD-UTXO, GET-BALANCE" CR
CR ."   - CREATE-COINBASE-TX" CR
CR ." =========================================" CR
