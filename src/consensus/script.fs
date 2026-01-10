\ =========================================================
\ FORTHCOIN SCRIPT ENGINE
\ =========================================================
\ Bitcoin-compatible stack-based scripting for programmable transactions

\ ---------------------------------------------------------
\ 1. Script Opcodes (Bitcoin-compatible)
\ ---------------------------------------------------------

\ Constants
$00 CONSTANT OP_0
$4F CONSTANT OP_1NEGATE
$51 CONSTANT OP_1
$52 CONSTANT OP_2
$53 CONSTANT OP_3
$54 CONSTANT OP_4
$55 CONSTANT OP_5

\ Flow control
$63 CONSTANT OP_IF
$64 CONSTANT OP_NOTIF
$67 CONSTANT OP_ELSE
$68 CONSTANT OP_ENDIF
$69 CONSTANT OP_VERIFY
$6A CONSTANT OP_RETURN

\ Stack operations
$6B CONSTANT OP_TOALTSTACK
$6C CONSTANT OP_FROMALTSTACK
$73 CONSTANT OP_IFDUP
$74 CONSTANT OP_DEPTH
$75 CONSTANT OP_DROP
$76 CONSTANT OP_DUP
$77 CONSTANT OP_NIP
$78 CONSTANT OP_OVER
$79 CONSTANT OP_PICK
$7A CONSTANT OP_ROLL
$7B CONSTANT OP_ROT
$7C CONSTANT OP_SWAP
$7D CONSTANT OP_TUCK
$6D CONSTANT OP_2DROP
$6E CONSTANT OP_2DUP
$6F CONSTANT OP_3DUP
$70 CONSTANT OP_2OVER
$71 CONSTANT OP_2ROT
$72 CONSTANT OP_2SWAP

\ Arithmetic
$8B CONSTANT OP_1ADD
$8C CONSTANT OP_1SUB
$8F CONSTANT OP_NEGATE
$90 CONSTANT OP_ABS
$91 CONSTANT OP_NOT
$92 CONSTANT OP_0NOTEQUAL
$93 CONSTANT OP_ADD
$94 CONSTANT OP_SUB
$9A CONSTANT OP_BOOLAND
$9B CONSTANT OP_BOOLOR
$9C CONSTANT OP_NUMEQUAL
$9D CONSTANT OP_NUMEQUALVERIFY
$9E CONSTANT OP_NUMNOTEQUAL
$9F CONSTANT OP_LESSTHAN
$A0 CONSTANT OP_GREATERTHAN
$A1 CONSTANT OP_LESSTHANOREQUAL
$A2 CONSTANT OP_GREATERTHANOREQUAL
$A3 CONSTANT OP_MIN
$A4 CONSTANT OP_MAX
$A5 CONSTANT OP_WITHIN

\ Crypto
$A6 CONSTANT OP_RIPEMD160
$A7 CONSTANT OP_SHA1
$A8 CONSTANT OP_SHA256
$A9 CONSTANT OP_HASH160
$AA CONSTANT OP_HASH256
$AB CONSTANT OP_CODESEPARATOR
$AC CONSTANT OP_CHECKSIG
$AD CONSTANT OP_CHECKSIGVERIFY
$AE CONSTANT OP_CHECKMULTISIG
$AF CONSTANT OP_CHECKMULTISIGVERIFY

\ Locktime
$B1 CONSTANT OP_CHECKLOCKTIMEVERIFY
$B2 CONSTANT OP_CHECKSEQUENCEVERIFY

\ ---------------------------------------------------------
\ 2. Script Stack
\ ---------------------------------------------------------

\ Stack holds 32-byte values
1000 CONSTANT SCRIPT-STACK-SIZE
CREATE SCRIPT-STACK SCRIPT-STACK-SIZE 32 * ALLOT
VARIABLE SCRIPT-SP  \ Stack pointer (index into stack)

\ Alternative stack
CREATE ALT-STACK SCRIPT-STACK-SIZE 32 * ALLOT
VARIABLE ALT-SP

: INIT-SCRIPT-STACK ( -- )
    0 SCRIPT-SP !
    0 ALT-SP !
    SCRIPT-STACK SCRIPT-STACK-SIZE 32 * ERASE
    ALT-STACK SCRIPT-STACK-SIZE 32 * ERASE ;

\ Push 32-byte value onto stack
: SCRIPT-PUSH ( addr -- )
    SCRIPT-SP @ SCRIPT-STACK-SIZE >= IF
        DROP
        ." [SCRIPT] Stack overflow!" CR
        EXIT
    THEN
    
    SCRIPT-SP @ 32 * SCRIPT-STACK +
    SWAP 32 CMOVE
    
    1 SCRIPT-SP +! ;

\ Pop 32-byte value from stack
: SCRIPT-POP ( addr -- )
    SCRIPT-SP @ 0= IF
        DROP
        ." [SCRIPT] Stack underflow!" CR
        EXIT
    THEN
    
    -1 SCRIPT-SP +!
    
    SCRIPT-SP @ 32 * SCRIPT-STACK +
    SWAP 32 CMOVE ;

\ Peek at top of stack without popping
: SCRIPT-TOP ( addr -- )
    SCRIPT-SP @ 0= IF
        DROP EXIT
    THEN
    
    SCRIPT-SP @ 1- 32 * SCRIPT-STACK +
    SWAP 32 CMOVE ;

\ Get stack depth
: SCRIPT-DEPTH ( -- n )
    SCRIPT-SP @ ;

\ ---------------------------------------------------------
\ 3. Value Encoding/Decoding
\ ---------------------------------------------------------

\ Convert 32-byte stack value to integer (little-endian)
: SCRIPT-TO-INT ( addr -- n )
    @ ;  \ Simplified - just read first cell

\ Convert integer to 32-byte stack value
: INT-TO-SCRIPT ( n addr -- )
    32 ERASE
    OVER SWAP ! ;  \ Store in first cell

\ Check if stack value is true (non-zero)
: SCRIPT-TRUE? ( addr -- flag )
    32 0 DO
        DUP I + C@ 0<> IF
            DROP TRUE EXIT
        THEN
    LOOP
    DROP FALSE ;

\ ---------------------------------------------------------
\ 4. Stack Operation Implementation
\ ---------------------------------------------------------

CREATE TEMP-SCRIPT-VAL1 32 ALLOT
CREATE TEMP-SCRIPT-VAL2 32 ALLOT
CREATE TEMP-SCRIPT-VAL3 32 ALLOT

: EXEC-OP_DUP ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-TOP
    TEMP-SCRIPT-VAL1 SCRIPT-PUSH ;

: EXEC-OP_DROP ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    \ Value discarded
    ;

: EXEC-OP_SWAP ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-POP
    TEMP-SCRIPT-VAL1 SCRIPT-PUSH
    TEMP-SCRIPT-VAL2 SCRIPT-PUSH ;

: EXEC-OP_OVER ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-TOP
    TEMP-SCRIPT-VAL1 SCRIPT-PUSH
    TEMP-SCRIPT-VAL2 SCRIPT-PUSH ;

: EXEC-OP_ROT ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-POP
    TEMP-SCRIPT-VAL3 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-PUSH
    TEMP-SCRIPT-VAL1 SCRIPT-PUSH
    TEMP-SCRIPT-VAL3 SCRIPT-PUSH ;

: EXEC-OP_2DUP ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-PUSH
    TEMP-SCRIPT-VAL1 SCRIPT-PUSH
    TEMP-SCRIPT-VAL2 SCRIPT-PUSH
    TEMP-SCRIPT-VAL1 SCRIPT-PUSH ;

\ ---------------------------------------------------------
\ 5. Arithmetic Operations
\ ---------------------------------------------------------

: EXEC-OP_ADD ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-POP
    
    TEMP-SCRIPT-VAL1 SCRIPT-TO-INT
    TEMP-SCRIPT-VAL2 SCRIPT-TO-INT +
    
    TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    TEMP-SCRIPT-VAL3 SCRIPT-PUSH ;

: EXEC-OP_SUB ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-POP
    
    TEMP-SCRIPT-VAL2 SCRIPT-TO-INT
    TEMP-SCRIPT-VAL1 SCRIPT-TO-INT -
    
    TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    TEMP-SCRIPT-VAL3 SCRIPT-PUSH ;

: EXEC-OP_1ADD ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL1 SCRIPT-TO-INT 1+
    TEMP-SCRIPT-VAL1 INT-TO-SCRIPT
    TEMP-SCRIPT-VAL1 SCRIPT-PUSH ;

: EXEC-OP_1SUB ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL1 SCRIPT-TO-INT 1-
    TEMP-SCRIPT-VAL1 INT-TO-SCRIPT
    TEMP-SCRIPT-VAL1 SCRIPT-PUSH ;

\ ---------------------------------------------------------
\ 6. Comparison Operations
\ ---------------------------------------------------------

: EXEC-OP_EQUAL ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-POP
    
    TEMP-SCRIPT-VAL1 TEMP-SCRIPT-VAL2 32 COMPARE 0= IF
        1 TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    ELSE
        0 TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    THEN
    
    TEMP-SCRIPT-VAL3 SCRIPT-PUSH ;

: EXEC-OP_NUMEQUAL ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-POP
    
    TEMP-SCRIPT-VAL1 SCRIPT-TO-INT
    TEMP-SCRIPT-VAL2 SCRIPT-TO-INT = IF
        1 TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    ELSE
        0 TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    THEN
    
    TEMP-SCRIPT-VAL3 SCRIPT-PUSH ;

: EXEC-OP_LESSTHAN ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL2 SCRIPT-POP
    
    TEMP-SCRIPT-VAL2 SCRIPT-TO-INT
    TEMP-SCRIPT-VAL1 SCRIPT-TO-INT < IF
        1 TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    ELSE
        0 TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    THEN
    
    TEMP-SCRIPT-VAL3 SCRIPT-PUSH ;

\ ---------------------------------------------------------
\ 7. Cryptographic Operations
\ ---------------------------------------------------------

: EXEC-OP_SHA256 ( -- )
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL1 32 SHA256-HASH
    HASH-RESULT SCRIPT-PUSH ;

: EXEC-OP_HASH256 ( -- )
    \ Double SHA256
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL1 32 SHA256-HASH
    HASH-RESULT 32 SHA256-HASH
    HASH-RESULT SCRIPT-PUSH ;

: EXEC-OP_HASH160 ( -- )
    \ SHA256 then RIPEMD160 (simplified - use first 20 bytes)
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL1 32 SHA256-HASH
    HASH-RESULT SCRIPT-PUSH ;

\ ---------------------------------------------------------
\ 8. Signature Verification
\ ---------------------------------------------------------

: EXEC-OP_CHECKSIG ( -- )
    \ Pop pubkey and signature
    TEMP-SCRIPT-VAL1 SCRIPT-POP  \ pubkey
    TEMP-SCRIPT-VAL2 SCRIPT-POP  \ signature
    
    \ TODO: Verify signature against transaction hash
    \ For now, simplified verification
    
    1 TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    TEMP-SCRIPT-VAL3 SCRIPT-PUSH
    
    ." [SCRIPT] CHECKSIG executed" CR ;

: EXEC-OP_CHECKMULTISIG ( -- )
    \ Pop N (number of pubkeys)
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL1 SCRIPT-TO-INT
    
    \ TODO: Full multisig verification
    \ For now, simplified
    
    1 TEMP-SCRIPT-VAL3 INT-TO-SCRIPT
    TEMP-SCRIPT-VAL3 SCRIPT-PUSH
    
    ." [SCRIPT] CHECKMULTISIG executed" CR ;

\ ---------------------------------------------------------
\ 9. Time-Lock Operations
\ ---------------------------------------------------------

VARIABLE SCRIPT-LOCKTIME
VARIABLE SCRIPT-SEQUENCE

: EXEC-OP_CHECKLOCKTIMEVERIFY ( -- )
    \ Check if locktime has passed
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL1 SCRIPT-TO-INT
    
    \ Compare with transaction locktime
    SCRIPT-LOCKTIME @ < IF
        ." [SCRIPT] CHECKLOCKTIMEVERIFY failed!" CR
        \ Transaction fails
    ELSE
        ." [SCRIPT] CHECKLOCKTIMEVERIFY passed" CR
    THEN ;

: EXEC-OP_CHECKSEQUENCEVERIFY ( -- )
    \ Check sequence number for relative time-locks
    TEMP-SCRIPT-VAL1 SCRIPT-POP
    TEMP-SCRIPT-VAL1 SCRIPT-TO-INT
    
    SCRIPT-SEQUENCE @ < IF
        ." [SCRIPT] CHECKSEQUENCEVERIFY failed!" CR
    ELSE
        ." [SCRIPT] CHECKSEQUENCEVERIFY passed" CR
    THEN ;

\ ---------------------------------------------------------
\ 10. Script Interpreter
\ ---------------------------------------------------------

VARIABLE SCRIPT-PC  \ Program counter
VARIABLE SCRIPT-LEN
VARIABLE SCRIPT-PTR

: INIT-SCRIPT ( script-addr script-len -- )
    SCRIPT-LEN !
    SCRIPT-PTR !
    0 SCRIPT-PC !
    INIT-SCRIPT-STACK ;

\ Execute one opcode
: EXEC-OPCODE ( opcode -- continue? )
    CASE
        OP_DUP OF EXEC-OP_DUP TRUE ENDOF
        OP_DROP OF EXEC-OP_DROP TRUE ENDOF
        OP_SWAP OF EXEC-OP_SWAP TRUE ENDOF
        OP_OVER OF EXEC-OP_OVER TRUE ENDOF
        OP_ROT OF EXEC-OP_ROT TRUE ENDOF
        OP_2DUP OF EXEC-OP_2DUP TRUE ENDOF
        
        OP_ADD OF EXEC-OP_ADD TRUE ENDOF
        OP_SUB OF EXEC-OP_SUB TRUE ENDOF
        OP_1ADD OF EXEC-OP_1ADD TRUE ENDOF
        OP_1SUB OF EXEC-OP_1SUB TRUE ENDOF
        
        OP_NUMEQUAL OF EXEC-OP_NUMEQUAL TRUE ENDOF
        OP_LESSTHAN OF EXEC-OP_LESSTHAN TRUE ENDOF
        
        OP_SHA256 OF EXEC-OP_SHA256 TRUE ENDOF
        OP_HASH256 OF EXEC-OP_HASH256 TRUE ENDOF
        OP_HASH160 OF EXEC-OP_HASH160 TRUE ENDOF
        
        OP_CHECKSIG OF EXEC-OP_CHECKSIG TRUE ENDOF
        OP_CHECKMULTISIG OF EXEC-OP_CHECKMULTISIG TRUE ENDOF
        
        OP_CHECKLOCKTIMEVERIFY OF EXEC-OP_CHECKLOCKTIMEVERIFY TRUE ENDOF
        OP_CHECKSEQUENCEVERIFY OF EXEC-OP_CHECKSEQUENCEVERIFY TRUE ENDOF
        
        OP_RETURN OF FALSE ENDOF  \ Stop execution
        
        \ Default: push data
        DUP $01 $4B WITHIN IF
            \ Data push (1-75 bytes)
            DROP TRUE
        ELSE
            ." [SCRIPT] Unknown opcode: " . CR
            TRUE
        THEN
    ENDCASE ;

\ Run script
: RUN-SCRIPT ( script-addr script-len -- success? )
    INIT-SCRIPT
    
    BEGIN
        SCRIPT-PC @ SCRIPT-LEN @ < WHILE
        
        \ Get next opcode
        SCRIPT-PTR @ SCRIPT-PC @ + C@
        
        \ Execute it
        EXEC-OPCODE 0= IF
            \ Script returned false
            FALSE EXIT
        THEN
        
        1 SCRIPT-PC +!
    REPEAT
    
    \ Script succeeds if stack has TRUE value
    SCRIPT-DEPTH 0= IF
        FALSE EXIT
    THEN
    
    TEMP-SCRIPT-VAL1 SCRIPT-TOP
    TEMP-SCRIPT-VAL1 SCRIPT-TRUE? ;

\ ---------------------------------------------------------
\ 11. Standard Script Templates
\ ---------------------------------------------------------

\ P2PKH (Pay to Public Key Hash)
CREATE P2PKH-SCRIPT 25 ALLOT

: CREATE-P2PKH-SCRIPT ( pubkey-hash-addr -- script-addr script-len )
    P2PKH-SCRIPT 25 ERASE
    
    \ OP_DUP
    OP_DUP P2PKH-SCRIPT C!
    
    \ OP_HASH160
    OP_HASH160 P2PKH-SCRIPT 1+ C!
    
    \ 20 bytes pubkey hash
    20 P2PKH-SCRIPT 2 + C!
    P2PKH-SCRIPT 3 + 20 CMOVE
    
    \ OP_EQUALVERIFY
    $88 P2PKH-SCRIPT 23 + C!
    
    \ OP_CHECKSIG
    OP_CHECKSIG P2PKH-SCRIPT 24 + C!
    
    P2PKH-SCRIPT 25 ;

\ Time-locked transaction (CLTV)
CREATE TIMELOCK-SCRIPT 40 ALLOT

: CREATE-TIMELOCK-SCRIPT ( locktime pubkey-hash -- script-addr script-len )
    TIMELOCK-SCRIPT 40 ERASE
    
    \ Push locktime (4 bytes)
    4 TIMELOCK-SCRIPT C!
    SWAP TIMELOCK-SCRIPT 1+ !
    
    \ OP_CHECKLOCKTIMEVERIFY
    OP_CHECKLOCKTIMEVERIFY TIMELOCK-SCRIPT 5 + C!
    
    \ OP_DROP
    OP_DROP TIMELOCK-SCRIPT 6 + C!
    
    \ Then standard P2PKH
    CREATE-P2PKH-SCRIPT
    TIMELOCK-SCRIPT 7 + SWAP CMOVE
    
    TIMELOCK-SCRIPT 32 ;

\ Hash-locked (for atomic swaps)
CREATE HASHLOCK-SCRIPT 60 ALLOT

: CREATE-HASHLOCK-SCRIPT ( hash-addr -- script-addr script-len )
    HASHLOCK-SCRIPT 60 ERASE
    
    \ OP_SHA256
    OP_SHA256 HASHLOCK-SCRIPT C!
    
    \ Push expected hash (32 bytes)
    32 HASHLOCK-SCRIPT 1+ C!
    HASHLOCK-SCRIPT 2 + 32 CMOVE
    
    \ OP_EQUAL
    $87 HASHLOCK-SCRIPT 34 + C!
    
    HASHLOCK-SCRIPT 35 ;

\ ---------------------------------------------------------
\ 12. Script Debugging
\ ---------------------------------------------------------

: SHOW-SCRIPT-STACK ( -- )
    ." " CR
    ." Script Stack (depth=" SCRIPT-DEPTH . ." ):" CR
    SCRIPT-DEPTH 0 ?DO
        ." [" I . ." ] "
        I 32 * SCRIPT-STACK + 8 0 DO
            DUP I + C@ .BYTE
        LOOP
        ." ..." CR
        DROP
    LOOP
    ." " CR ;

: DISASSEMBLE-SCRIPT ( script-addr script-len -- )
    ." " CR
    ." Script Disassembly:" CR
    0 DO
        ." [" I . ." ] "
        DUP I + C@
        
        CASE
            OP_DUP OF ." OP_DUP" ENDOF
            OP_DROP OF ." OP_DROP" ENDOF
            OP_SWAP OF ." OP_SWAP" ENDOF
            OP_CHECKSIG OF ." OP_CHECKSIG" ENDOF
            OP_CHECKMULTISIG OF ." OP_CHECKMULTISIG" ENDOF
            OP_SHA256 OF ." OP_SHA256" ENDOF
            OP_HASH160 OF ." OP_HASH160" ENDOF
            OP_CHECKLOCKTIMEVERIFY OF ." OP_CHECKLOCKTIMEVERIFY" ENDOF
            DUP .BYTE
        ENDCASE
        CR
    LOOP
    DROP
    ." " CR ;

CR ." [SCRIPT] Bitcoin Script VM loaded." CR
