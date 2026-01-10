\ =========================================================
\ FORTHCOIN DIFFICULTY ADJUSTMENT
\ =========================================================
\ Bitcoin-compatible difficulty adjustment every 2016 blocks

\ ---------------------------------------------------------
\ 1. Difficulty Parameters
\ ---------------------------------------------------------

2016 CONSTANT DIFFICULTY-PERIOD      \ Blocks between adjustments
600 CONSTANT TARGET-BLOCK-TIME       \ 10 minutes in seconds
1209600 CONSTANT TARGET-PERIOD-TIME  \ 2016 * 600 = 2 weeks

\ Difficulty limits
4 CONSTANT MAX-DIFFICULTY-ADJUSTMENT  \ 4x max change per period

\ Current difficulty (stored as compact target)
VARIABLE CURRENT-DIFFICULTY
VARIABLE CURRENT-TARGET

\ Block time tracking
CREATE BLOCK-TIMESTAMPS 2016 CELLS ALLOT
VARIABLE TIMESTAMP-INDEX

\ ---------------------------------------------------------
\ 2. Compact Target Format
\ ---------------------------------------------------------

\ Bitcoin uses compact representation: 0xMMEEEEEE
\ Where MM = exponent, EEEEEE = mantissa
\ Value = mantissa * 256^(exponent-3)

: COMPACT-TO-TARGET ( compact -- target-addr )
    \ Extract exponent and mantissa
    DUP $FF000000 AND 24 RSHIFT  \ Exponent
    SWAP $00FFFFFF AND           \ Mantissa
    
    \ Calculate actual target (simplified)
    CREATE TEMP-TARGET 32 ALLOT
    TEMP-TARGET 32 ERASE
    
    \ Store mantissa in appropriate position based on exponent
    OVER 3 - 0 MAX CELLS TEMP-TARGET +
    SWAP OVER !
    
    TEMP-TARGET ;

: TARGET-TO-COMPACT ( target-addr -- compact )
    \ Find first non-zero byte
    32 0 DO
        DUP I + C@ 0<> IF
            \ Found it
            I 3 + 24 LSHIFT      \ Exponent
            OVER I + @ $00FFFFFF AND OR
            NIP EXIT
        THEN
    LOOP
    DROP 0 ;

\ ---------------------------------------------------------
\ 3. Difficulty Calculation
\ ---------------------------------------------------------

\ Genesis difficulty (initial value)
$1D00FFFF CONSTANT GENESIS-DIFFICULTY

: INIT-DIFFICULTY ( -- )
    GENESIS-DIFFICULTY CURRENT-DIFFICULTY !
    GENESIS-DIFFICULTY COMPACT-TO-TARGET CURRENT-TARGET 32 CMOVE
    0 TIMESTAMP-INDEX !
    BLOCK-TIMESTAMPS 2016 CELLS ERASE
    ." [DIFFICULTY] Initialized to genesis: " GENESIS-DIFFICULTY HEX. CR ;

\ Record block timestamp
: RECORD-BLOCK-TIME ( timestamp -- )
    TIMESTAMP-INDEX @ CELLS BLOCK-TIMESTAMPS +
    !
    1 TIMESTAMP-INDEX +!
    
    \ Wrap around if needed
    TIMESTAMP-INDEX @ DIFFICULTY-PERIOD >= IF
        0 TIMESTAMP-INDEX !
    THEN ;

\ Calculate time for last period
: CALCULATE-PERIOD-TIME ( -- actual-time )
    \ Get timestamp of current block
    TIMESTAMP-INDEX @ 1- 0 MAX CELLS BLOCK-TIMESTAMPS + @
    
    \ Get timestamp of first block in period
    TIMESTAMP-INDEX @ DIFFICULTY-PERIOD - 0 MAX CELLS BLOCK-TIMESTAMPS + @
    
    \ Calculate difference
    SWAP OVER - ;

\ Adjust difficulty based on actual block times
: ADJUST-DIFFICULTY ( -- )
    \ Only adjust every 2016 blocks
    CHAIN-HEIGHT @ DIFFICULTY-PERIOD MOD 0<> IF
        EXIT
    THEN
    
    ." [DIFFICULTY] Adjustment at height " CHAIN-HEIGHT @ . CR
    
    \ Calculate actual time for last period
    CALCULATE-PERIOD-TIME
    
    ." [DIFFICULTY] Last period time: " DUP . ." seconds" CR
    ." [DIFFICULTY] Target period time: " TARGET-PERIOD-TIME . ." seconds" CR
    
    \ Calculate adjustment factor
    \ New_Difficulty = Old_Difficulty * (Actual_Time / Target_Time)
    
    DUP TARGET-PERIOD-TIME < IF
        \ Blocks came too fast, increase difficulty
        TARGET-PERIOD-TIME SWAP /
        MAX-DIFFICULTY-ADJUSTMENT MIN
        ." [DIFFICULTY] Increasing by factor: " DUP . CR
        CURRENT-DIFFICULTY @ SWAP / CURRENT-DIFFICULTY !
    ELSE
        \ Blocks came too slow, decrease difficulty
        TARGET-PERIOD-TIME SWAP /
        MAX-DIFFICULTY-ADJUSTMENT MIN
        ." [DIFFICULTY] Decreasing by factor: " DUP . CR
        CURRENT-DIFFICULTY @ SWAP * CURRENT-DIFFICULTY !
    THEN
    
    \ Update target
    CURRENT-DIFFICULTY @ COMPACT-TO-TARGET CURRENT-TARGET 32 CMOVE
    
    ." [DIFFICULTY] New difficulty: " CURRENT-DIFFICULTY @ HEX. CR ;

\ ---------------------------------------------------------
\ 4. Block Validation
\ ---------------------------------------------------------

\ Check if block hash meets difficulty target
: MEETS-DIFFICULTY? ( block-hash-addr -- flag )
    \ Compare block hash with current target
    CURRENT-TARGET 32 COMPARE 1 = ;  \ Hash must be less than target

\ Validate block difficulty
: VALIDATE-BLOCK-DIFFICULTY ( block-addr -- valid? )
    \ Extract block hash
    DUP 32 + \ Skip header to hash location
    
    \ Check against current difficulty
    MEETS-DIFFICULTY? IF
        ." [DIFFICULTY] Block meets difficulty target" CR
        TRUE
    ELSE
        ." [DIFFICULTY] Block FAILS difficulty check!" CR
        FALSE
    THEN ;

\ ---------------------------------------------------------
\ 5. Mining with Difficulty
\ ---------------------------------------------------------

\ Mine block with current difficulty target
: MINE-WITH-DIFFICULTY ( block-header-addr -- nonce )
    ." [DIFFICULTY] Mining with target: " CURRENT-TARGET 8 0 DO
        DUP I + C@ .BYTE
    LOOP ." ..." DROP CR
    
    \ Start nonce at 0
    0 OVER 76 + !
    
    \ Try nonces until we find valid hash
    BEGIN
        \ Increment nonce
        DUP 76 + DUP @ 1+ SWAP !
        
        \ Hash the block header
        DUP 80 SHA256-HASH
        
        \ Check if it meets difficulty
        HASH-RESULT MEETS-DIFFICULTY? IF
            ." [DIFFICULTY] Found valid block!" CR
            DUP 76 + @ \ Return nonce
            SWAP DROP
            EXIT
        THEN
        
        \ Show progress every 10000 hashes
        DUP 76 + @ 10000 MOD 0= IF
            ." [MINING] Tried " DUP 76 + @ . ." hashes..." CR
        THEN
    AGAIN ;

\ ---------------------------------------------------------
\ 6. Difficulty Statistics
\ ---------------------------------------------------------

: SHOW-DIFFICULTY ( -- )
    CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║              DIFFICULTY STATISTICS                     ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    CR
    
    ." Current Difficulty: " CURRENT-DIFFICULTY @ HEX. CR
    
    ." Current Target: "
    CURRENT-TARGET 32 0 DO
        DUP I + C@ .BYTE
    LOOP DROP CR
    
    ." Next Adjustment: " 
    CHAIN-HEIGHT @ DIFFICULTY-PERIOD MOD 
    DIFFICULTY-PERIOD SWAP - . ." blocks" CR
    
    ." Average Block Time: "
    CALCULATE-PERIOD-TIME 
    TIMESTAMP-INDEX @ 0> IF
        TIMESTAMP-INDEX @ /
    THEN
    . ." seconds" CR
    
    ." Target Block Time: " TARGET-BLOCK-TIME . ." seconds" CR
    
    CR ;

\ ---------------------------------------------------------
\ 7. Hashrate Estimation
\ ---------------------------------------------------------

VARIABLE NETWORK-HASHRATE

\ Estimate network hashrate from difficulty
: ESTIMATE-HASHRATE ( -- hashes-per-second )
    \ Hashrate = Difficulty * 2^32 / Target_Time
    \ Simplified calculation
    CURRENT-DIFFICULTY @ 
    TARGET-BLOCK-TIME / ;

: UPDATE-HASHRATE ( -- )
    ESTIMATE-HASHRATE NETWORK-HASHRATE ! ;

: SHOW-HASHRATE ( -- )
    UPDATE-HASHRATE
    ." Network Hashrate: " 
    NETWORK-HASHRATE @ . ." hashes/second" CR
    
    ." Estimated: "
    NETWORK-HASHRATE @ 1000 / . ." KH/s  "
    NETWORK-HASHRATE @ 1000000 / . ." MH/s" CR ;

\ ---------------------------------------------------------
\ 8. Difficulty History
\ ---------------------------------------------------------

\ Store last 10 difficulty adjustments
CREATE DIFFICULTY-HISTORY 10 CELLS ALLOT
VARIABLE HISTORY-INDEX

: RECORD-DIFFICULTY ( -- )
    CURRENT-DIFFICULTY @
    HISTORY-INDEX @ CELLS DIFFICULTY-HISTORY +
    !
    
    1 HISTORY-INDEX +!
    HISTORY-INDEX @ 10 >= IF
        0 HISTORY-INDEX !
    THEN ;

: SHOW-DIFFICULTY-HISTORY ( -- )
    CR
    ." Difficulty History (last 10 adjustments):" CR
    
    10 0 DO
        I CELLS DIFFICULTY-HISTORY + @
        DUP 0> IF
            ." [" I . ." ] " HEX. CR
        THEN
    LOOP
    CR ;

\ ---------------------------------------------------------
\ 9. Integration with Block Processing
\ ---------------------------------------------------------

\ Called when adding new block to chain
: ON-BLOCK-ADDED ( block-addr -- )
    \ Record timestamp
    DUP 68 + @ RECORD-BLOCK-TIME
    
    \ Check if we need difficulty adjustment
    ADJUST-DIFFICULTY
    
    \ Record in history if adjustment happened
    CHAIN-HEIGHT @ DIFFICULTY-PERIOD MOD 0= IF
        RECORD-DIFFICULTY
    THEN
    
    DROP ;

\ ---------------------------------------------------------
\ 10. Testnet Difficulty (Faster Adjustment)
\ ---------------------------------------------------------

VARIABLE TESTNET-MODE

: ENABLE-TESTNET ( -- )
    TRUE TESTNET-MODE !
    ." [DIFFICULTY] Testnet mode enabled (faster adjustment)" CR ;

: DISABLE-TESTNET ( -- )
    FALSE TESTNET-MODE !
    ." [DIFFICULTY] Mainnet mode (2016 block adjustment)" CR ;

\ Testnet adjusts every 20 blocks for testing
: TESTNET-ADJUST ( -- )
    TESTNET-MODE @ IF
        CHAIN-HEIGHT @ 20 MOD 0= IF
            ADJUST-DIFFICULTY
        THEN
    THEN ;

CR ." [DIFFICULTY] Difficulty adjustment loaded." CR
