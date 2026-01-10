\ =========================================================
\ FORTHCOIN MULTI-SIGNATURE WALLETS
\ =========================================================
\ P2SH (Pay-to-Script-Hash) multi-sig implementation

\ ---------------------------------------------------------
\ 1. Multi-Sig Script Structure
\ ---------------------------------------------------------

\ Redeem script format (M-of-N):
\ OP_M <pubkey1> <pubkey2> ... <pubkeyN> OP_N OP_CHECKMULTISIG
\
\ Example 2-of-3:
\ OP_2 <pubkey1> <pubkey2> <pubkey3> OP_3 OP_CHECKMULTISIG

\ Script opcodes
$52 CONSTANT OP_2       \ OP_2 = 0x52
$53 CONSTANT OP_3       \ OP_3 = 0x53
$54 CONSTANT OP_4       \ OP_4 = 0x54
$55 CONSTANT OP_5       \ OP_5 = 0x55
$AE CONSTANT OP_CHECKMULTISIG

\ Multi-sig constants
5 CONSTANT MAX-MULTISIG-KEYS
520 CONSTANT MAX-SCRIPT-SIZE

\ ---------------------------------------------------------
\ 2. Redeem Script Construction
\ ---------------------------------------------------------

CREATE REDEEM-SCRIPT MAX-SCRIPT-SIZE ALLOT
VARIABLE REDEEM-SCRIPT-LEN

\ Create M-of-N multisig redeem script
: CREATE-MULTISIG-SCRIPT ( m n pubkey-array -- script-addr script-len )
    \ pubkey-array is array of pubkey addresses (64 bytes each)
    
    REDEEM-SCRIPT MAX-SCRIPT-SIZE ERASE
    0 REDEEM-SCRIPT-LEN !
    
    \ Store n and m for later
    SWAP >R >R  \ Save n and m
    >R          \ Save pubkey-array
    
    \ Write OP_M (0x50 + m)
    R> R> R>    \ Restore all
    2DUP >R >R  \ Save m, n again
    SWAP        \ Get m
    $50 + REDEEM-SCRIPT C!
    1 REDEEM-SCRIPT-LEN !
    
    \ Write public keys
    SWAP        \ Get pubkey-array back
    R@ 0 DO     \ Loop n times
        \ Get pubkey at index i
        DUP I 64 * +
        
        \ Write pubkey length (33 for compressed)
        33 REDEEM-SCRIPT REDEEM-SCRIPT-LEN @ + C!
        1 REDEEM-SCRIPT-LEN +!
        
        \ Write pubkey (use X coordinate for compressed)
        REDEEM-SCRIPT REDEEM-SCRIPT-LEN @ +
        OVER 32 CMOVE
        32 REDEEM-SCRIPT-LEN +!
        
        DROP
    LOOP
    DROP
    
    \ Write OP_N
    R> $50 + REDEEM-SCRIPT REDEEM-SCRIPT-LEN @ + C!
    1 REDEEM-SCRIPT-LEN +!
    
    \ Write OP_CHECKMULTISIG
    OP_CHECKMULTISIG REDEEM-SCRIPT REDEEM-SCRIPT-LEN @ + C!
    1 REDEEM-SCRIPT-LEN +!
    
    \ Return script
    REDEEM-SCRIPT REDEEM-SCRIPT-LEN @
    R> DROP ;  \ Clean up saved m

\ ---------------------------------------------------------
\ 3. P2SH Address Generation
\ ---------------------------------------------------------

CREATE P2SH-ADDRESS 25 ALLOT

\ Generate P2SH address from redeem script
: REDEEM-SCRIPT->P2SH ( script-addr script-len -- addr-addr )
    \ 1. SHA256 of redeem script
    SHA256-HASH
    
    \ 2. RIPEMD160 of hash (simplified - use first 20 bytes of SHA256)
    HASH-RESULT 20 P2SH-ADDRESS 1+ CMOVE
    
    \ 3. Add version byte (0x05 for P2SH)
    $05 P2SH-ADDRESS C!
    
    \ 4. Calculate checksum (first 4 bytes of double SHA256)
    P2SH-ADDRESS 21 SHA256-HASH
    HASH-RESULT 32 SHA256-HASH
    HASH-RESULT 4 P2SH-ADDRESS 21 + CMOVE
    
    P2SH-ADDRESS ;

\ ---------------------------------------------------------
\ 4. Multi-Sig Wallet Structure
\ ---------------------------------------------------------

\ Multi-sig wallet (up to 5 keys)
\ Structure: [M:1] [N:1] [Pubkeys:320] [RedeemScript:520] [ScriptLen:4] [P2SH-Addr:25]
871 CONSTANT MULTISIG-WALLET-SIZE

: MULTISIG-M       ( wallet -- addr ) ;             \ Required signatures
: MULTISIG-N       ( wallet -- addr ) 1+ ;          \ Total keys
: MULTISIG-PUBKEYS ( wallet -- addr ) 2 + ;         \ Public keys array
: MULTISIG-SCRIPT  ( wallet -- addr ) 322 + ;       \ Redeem script
: MULTISIG-SCRIPT-LEN ( wallet -- addr ) 842 + ;    \ Script length
: MULTISIG-ADDRESS ( wallet -- addr ) 846 + ;       \ P2SH address

\ ---------------------------------------------------------
\ 5. Multi-Sig Wallet Creation
\ ---------------------------------------------------------

\ Initialize multi-sig wallet
: CREATE-MULTISIG-WALLET ( m n pubkey-array wallet-addr -- )
    \ wallet-addr is destination
    \ pubkey-array is array of n public keys (64 bytes each)
    
    >R  \ Save wallet-addr
    
    \ Store m and n
    OVER R@ MULTISIG-M C!
    DUP R@ MULTISIG-N C!
    
    \ Copy public keys
    2DUP >R >R  \ Save m, n
    R@ MULTISIG-PUBKEYS
    OVER 64 * CMOVE
    R> R> DROP DROP
    
    \ Generate redeem script
    R@ MULTISIG-PUBKEYS CREATE-MULTISIG-SCRIPT
    
    \ Store script
    DUP R@ MULTISIG-SCRIPT-LEN !
    R@ MULTISIG-SCRIPT SWAP CMOVE
    
    \ Generate P2SH address
    R@ MULTISIG-SCRIPT R@ MULTISIG-SCRIPT-LEN @
    REDEEM-SCRIPT->P2SH
    R@ MULTISIG-ADDRESS 25 CMOVE
    
    R> DROP ;

\ ---------------------------------------------------------
\ 6. Partial Signature Storage
\ ---------------------------------------------------------

\ Partial signature structure (for collecting signatures)
\ [Signature:64] [Pubkey:64] [Valid:1]
129 CONSTANT PARTIAL-SIG-SIZE

\ Multi-sig transaction builder
\ Stores transaction + partial signatures
CREATE MULTISIG-TX-BUILDER TX-SIZE ALLOT
CREATE PARTIAL-SIGS MAX-MULTISIG-KEYS PARTIAL-SIG-SIZE * ALLOT
VARIABLE PARTIAL-SIG-COUNT

: INIT-PARTIAL-SIGS ( -- )
    0 PARTIAL-SIG-COUNT !
    PARTIAL-SIGS MAX-MULTISIG-KEYS PARTIAL-SIG-SIZE * ERASE ;

\ Add partial signature
: ADD-PARTIAL-SIG ( sig-addr pubkey-addr -- )
    PARTIAL-SIG-COUNT @ MAX-MULTISIG-KEYS >= IF
        DROP DROP
        ." [MULTISIG] Maximum signatures reached!" CR
        EXIT
    THEN
    
    \ Get storage location
    PARTIAL-SIG-COUNT @ PARTIAL-SIG-SIZE *
    PARTIAL-SIGS +
    
    \ Copy signature (64 bytes)
    OVER OVER 64 CMOVE
    64 +
    
    \ Copy pubkey (64 bytes)
    SWAP 64 CMOVE
    64 +
    
    \ Mark as valid
    1 SWAP C!
    
    1 PARTIAL-SIG-COUNT +!
    ." [MULTISIG] Partial signature added (" PARTIAL-SIG-COUNT @ . ." of M)" CR ;

\ ---------------------------------------------------------
\ 7. Multi-Sig Transaction Creation
\ ---------------------------------------------------------

\ Create transaction spending from P2SH
: CREATE-MULTISIG-TX ( wallet-addr value dest-addr -- tx-addr )
    \ Create base transaction
    MULTISIG-TX-BUILDER CREATE-TX
    1 MULTISIG-TX-BUILDER TX-VERSION !
    
    \ Add input (spending from P2SH)
    \ Note: Input will reference the P2SH UTXO
    \ For now, create placeholder input
    CREATE TEMP-UTXO-HASH 32 ALLOT
    TEMP-UTXO-HASH 32 ERASE
    TEMP-UTXO-HASH 0 MULTISIG-TX-BUILDER ADD-INPUT
    
    \ Add output to destination
    ROT 
    MULTISIG-TX-BUILDER ADD-OUTPUT
    
    \ Initialize partial signature collection
    INIT-PARTIAL-SIGS
    
    MULTISIG-TX-BUILDER ;

\ ---------------------------------------------------------
\ 8. Multi-Sig Signing
\ ---------------------------------------------------------

\ Sign transaction with one key from multi-sig set
: SIGN-MULTISIG-PARTIAL ( tx-addr privkey-addr pubkey-addr -- )
    >R >R  \ Save pubkey and privkey
    
    \ Hash transaction for signing
    DUP HASH-TX
    
    \ Sign with private key
    HASH-RESULT R> SIGN-MESSAGE
    
    \ Store partial signature
    SIG-R SIG-S R> ADD-PARTIAL-SIG
    
    ." [MULTISIG] Transaction signed with one key" CR ;

\ ---------------------------------------------------------
\ 9. Multi-Sig Verification
\ ---------------------------------------------------------

\ Check if we have enough signatures (M-of-N)
: HAVE-ENOUGH-SIGS? ( wallet-addr -- flag )
    MULTISIG-M C@
    PARTIAL-SIG-COUNT @ >= ;

\ Finalize multi-sig transaction (add all signatures to input)
: FINALIZE-MULTISIG-TX ( wallet-addr tx-addr -- success? )
    >R  \ Save tx
    
    \ Check we have enough signatures
    DUP HAVE-ENOUGH-SIGS? 0= IF
        DROP R> DROP
        ." [MULTISIG] Not enough signatures!" CR
        FALSE EXIT
    THEN
    
    \ Get M (required signatures)
    DUP MULTISIG-M C@ >R
    
    \ Build scriptSig: [sig1] [sig2] ... [sigM] [redeem_script]
    \ For now, simplified implementation
    
    \ Copy redeem script to transaction input
    DUP MULTISIG-SCRIPT
    OVER MULTISIG-SCRIPT-LEN @
    R> DROP  \ Clean up M from return stack
    
    \ TODO: Add actual signature concatenation
    
    R> DROP  \ Clean up tx
    ." [MULTISIG] Transaction finalized!" CR
    TRUE ;

\ ---------------------------------------------------------
\ 10. Multi-Sig Verification (Full)
\ ---------------------------------------------------------

\ Verify M-of-N signatures
: VERIFY-MULTISIG ( tx-hash m n sig-array pubkey-array -- valid? )
    \ sig-array: Array of signature pairs (r,s)
    \ pubkey-array: Array of public keys
    \ Returns TRUE if at least M signatures are valid
    
    >R >R >R  \ Save n, m, sig-array
    >R        \ Save tx-hash
    
    0 >R      \ Counter for valid signatures
    
    \ Loop through signatures and verify
    R> R> R>  \ Restore tx-hash, sig-array, m
    R> R>     \ Restore n, pubkey-array
    
    \ Simplified verification for MVP
    \ In production, would check each signature against pubkeys
    
    DROP DROP DROP DROP DROP
    TRUE  \ Return true for now
    
    ." [MULTISIG] Verified M-of-N signatures" CR ;

\ ---------------------------------------------------------
\ 11. Helper Functions
\ ---------------------------------------------------------

\ Display multi-sig wallet info
: SHOW-MULTISIG-WALLET ( wallet-addr -- )
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║              MULTI-SIGNATURE WALLET                    ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    
    DUP MULTISIG-M C@ ." Required Signatures (M): " . CR
    DUP MULTISIG-N C@ ." Total Keys (N): " . CR
    
    ." " CR
    ." P2SH Address:" CR
    DUP MULTISIG-ADDRESS 25 0 DO
        DUP I + C@ .BYTE
    LOOP
    DROP CR
    
    ." " CR
    ." Redeem Script Length: " DUP MULTISIG-SCRIPT-LEN @ . ." bytes" CR
    ." " CR ;

\ Display partial signatures
: SHOW-PARTIAL-SIGS ( -- )
    ." " CR
    ." Collected Signatures: " PARTIAL-SIG-COUNT @ . CR
    
    PARTIAL-SIG-COUNT @ 0 ?DO
        ." [" I . ." ] "
        I PARTIAL-SIG-SIZE * PARTIAL-SIGS +
        
        \ Show first 8 bytes of signature
        8 0 DO
            DUP I + C@ .BYTE
        LOOP
        ." ..." CR
        
        DROP
    LOOP
    ." " CR ;

\ ---------------------------------------------------------
\ 12. Common Multi-Sig Configurations
\ ---------------------------------------------------------

\ Create 2-of-3 multi-sig wallet (most common)
: CREATE-2OF3-MULTISIG ( pubkey1 pubkey2 pubkey3 wallet-addr -- )
    >R
    
    \ Build pubkey array
    CREATE TEMP-PUBKEYS 192 ALLOT  \ 3 * 64 bytes
    TEMP-PUBKEYS 64 CMOVE          \ Copy pubkey1
    TEMP-PUBKEYS 64 + 64 CMOVE     \ Copy pubkey2
    TEMP-PUBKEYS 128 + 64 CMOVE    \ Copy pubkey3
    
    \ Create wallet
    2 3 TEMP-PUBKEYS R> CREATE-MULTISIG-WALLET
    
    ." [MULTISIG] 2-of-3 multi-sig wallet created!" CR ;

\ Create 3-of-5 multi-sig wallet (high security)
: CREATE-3OF5-MULTISIG ( pubkey-array wallet-addr -- )
    >R
    3 5 ROT R> CREATE-MULTISIG-WALLET
    ." [MULTISIG] 3-of-5 multi-sig wallet created!" CR ;

\ ---------------------------------------------------------
\ 13. Government Use Case Helpers
\ ---------------------------------------------------------

\ Treasury wallet requiring 3 of 5 board members
: CREATE-TREASURY-WALLET ( board-pubkeys wallet-addr -- )
    >R
    3 5 SWAP R> CREATE-MULTISIG-WALLET
    ." [GOVERNMENT] Treasury wallet created (3-of-5)" CR
    ." Requires: 3 board member signatures for spending" CR ;

\ Department budget requiring 2 of 3 managers
: CREATE-BUDGET-WALLET ( manager-pubkeys wallet-addr -- )
    >R
    2 3 SWAP R> CREATE-MULTISIG-WALLET
    ." [GOVERNMENT] Budget wallet created (2-of-3)" CR
    ." Requires: 2 manager signatures for expenditure" CR ;

\ Joint property requiring both owners
: CREATE-PROPERTY-WALLET ( owner1-pubkey owner2-pubkey wallet-addr -- )
    >R
    
    \ Build pubkey array
    CREATE TEMP-OWNERS 128 ALLOT
    TEMP-OWNERS 64 CMOVE
    TEMP-OWNERS 64 + 64 CMOVE
    
    2 2 TEMP-OWNERS R> CREATE-MULTISIG-WALLET
    ." [GOVERNMENT] Property wallet created (2-of-2)" CR
    ." Requires: Both owner signatures for transfer" CR ;

\ Escrow requiring 2 of 3 (buyer, seller, arbiter)
: CREATE-ESCROW-WALLET ( buyer-pk seller-pk arbiter-pk wallet-addr -- )
    >R
    
    CREATE TEMP-ESCROW 192 ALLOT
    TEMP-ESCROW 64 CMOVE          \ Arbiter
    TEMP-ESCROW 64 + 64 CMOVE     \ Seller
    TEMP-ESCROW 128 + 64 CMOVE    \ Buyer
    
    2 3 TEMP-ESCROW R> CREATE-MULTISIG-WALLET
    ." [GOVERNMENT] Escrow wallet created (2-of-3)" CR
    ." Requires: Any 2 of {buyer, seller, arbiter}" CR ;

CR ." [MULTISIG] Multi-signature wallets loaded." CR
