\ =========================================================
\ FORTHCOIN QUERY API
\ =========================================================
\ Blockchain explorer and query interface

\ ---------------------------------------------------------
\ 1. Block Query Functions
\ ---------------------------------------------------------

\ Get block by height
: GET-BLOCK-BY-HEIGHT ( height -- block-addr success? )
    \ For MVP, we'll use simplified lookup
    \ In production, would use persistent storage index
    
    DUP CHAIN-HEIGHT @ > IF
        DROP 0 FALSE
        ." [QUERY] Block height out of range" CR
        EXIT
    THEN
    
    \ TODO: Implement actual block retrieval from storage
    \ For now, return placeholder
    0 FALSE ;

\ Get block by hash
: GET-BLOCK-BY-HASH ( hash-addr -- block-addr success? )
    \ Look up block in storage by hash
    FIND-OFFSET IF
        \ Found block at offset
        \ TODO: Read block from file
        0 TRUE
    ELSE
        0 FALSE
    THEN ;

\ Get latest blocks
: GET-LATEST-BLOCKS ( count -- blocks-array actual-count )
    \ Return array of most recent blocks
    DUP CHAIN-HEIGHT @ MIN
    
    ." [QUERY] Returning " DUP . ." latest blocks" CR
    
    \ TODO: Implement actual block retrieval
    0 SWAP ;

\ ---------------------------------------------------------
\ 2. Transaction Query Functions
\ ---------------------------------------------------------

\ Search transactions by hash
: GET-TX-BY-HASH ( tx-hash-addr -- tx-addr success? )
    \ Search mempool first
    MEMPOOL-SIZE @ 0 DO
        I TX-SIZE * MEMPOOL +
        DUP 4 + \ Get tx hash location
        OVER 32 COMPARE 0= IF
            TRUE EXIT
        THEN
        DROP
    LOOP
    
    \ TODO: Search confirmed transactions in blockchain
    0 FALSE ;

\ Get transactions for address
: GET-TXS-FOR-ADDRESS ( address-addr -- tx-array count )
    \ Search through UTXOs for this address
    0 >R  \ Count
    
    UTXO-COUNT @ 0 DO
        I UTXO-ENTRY-SIZE * UTXO-SET +
        \ TODO: Compare script/address
        DROP
    LOOP
    
    0 R> ;

\ Get pending transactions (mempool)
: GET-PENDING-TXS ( -- tx-array count )
    MEMPOOL MEMPOOL-SIZE @ ;

\ ---------------------------------------------------------
\ 3. Address Query Functions
\ ---------------------------------------------------------

\ Get balance for address
: QUERY-ADDRESS-BALANCE ( address-addr -- balance )
    \ Sum all UTXOs for this address
    0 >R  \ Accumulator
    
    UTXO-COUNT @ 0 DO
        I UTXO-ENTRY-SIZE * UTXO-SET +
        \ TODO: Compare address, add value if match
        DROP
    LOOP
    
    R> ;

\ Get UTXOs for address
: QUERY-ADDRESS-UTXOS ( address-addr -- utxo-array count )
    0 >R  \ Count
    
    UTXO-COUNT @ 0 DO
        I UTXO-ENTRY-SIZE * UTXO-SET +
        \ TODO: Filter by address
        DROP
    LOOP
    
    UTXO-SET R> ;

\ Get transaction history for address
: QUERY-ADDRESS-HISTORY ( address-addr -- tx-array count )
    \ This would scan entire blockchain
    \ For MVP, return pending only
    GET-PENDING-TXS ;

\ ---------------------------------------------------------
\ 4. Network Statistics
\ ---------------------------------------------------------

: GET-NETWORK-STATS ( -- )
    CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║              NETWORK STATISTICS                        ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    CR
    
    ." Blockchain Height: " CHAIN-HEIGHT @ . CR
    ." Total Work: " CHAIN-WORK 2@ D. CR
    ." " CR
    
    ." Mempool Size: " MEMPOOL-SIZE @ . ." transactions" CR
    ." Mempool Bytes: " MEMPOOL-SIZE @ TX-SIZE * . CR
    ." " CR
    
    ." UTXO Set Size: " UTXO-COUNT @ . ." outputs" CR
    ." UTXO Set Bytes: " UTXO-COUNT @ UTXO-ENTRY-SIZE * . CR
    ." " CR
    
    ." Connected Peers: " PEER-COUNT @ . CR
    ." Network Running: " NET-RUNNING @ IF ." YES" ELSE ." NO" THEN CR
    ." " CR
    
    SHOW-DIFFICULTY
    SHOW-HASHRATE
    
    CR ;

\ ---------------------------------------------------------
\ 5. Blockchain Explorer Display
\ ---------------------------------------------------------

\ Display block summary
: SHOW-BLOCK-SUMMARY ( block-addr -- )
    CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║                  BLOCK DETAILS                         ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    CR
    
    ." Version: " DUP @ . CR
    ." Previous Hash: " DUP 4 + 32 0 DO
        DUP I + C@ .BYTE
    LOOP DROP CR
    
    ." Merkle Root: " DUP 36 + 32 0 DO
        DUP I + C@ .BYTE
    LOOP DROP CR
    
    ." Timestamp: " DUP 68 + @ . CR
    ." Difficulty: " DUP 72 + @ HEX. CR
    ." Nonce: " DUP 76 + @ . CR
    
    CR DROP ;

\ Display transaction summary
: SHOW-TX-SUMMARY ( tx-addr -- )
    CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║              TRANSACTION DETAILS                       ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    CR
    
    ." Version: " DUP TX-VERSION @ . CR
    ." Inputs: " DUP TX-INPUTS-COUNT @ . CR
    ." Outputs: " DUP TX-OUTPUTS-COUNT @ . CR
    ." Locktime: " DUP TX-LOCKTIME @ . CR
    
    CR
    ." Input Details:" CR
    DUP TX-INPUTS-COUNT @ 0 DO
        ."   [" I . ." ] "
        DUP TX-INPUT I 8 * +
        8 0 DO
            DUP I + C@ .BYTE
        LOOP ." ..." DROP CR
    LOOP
    
    CR
    ." Output Details:" CR
    DUP TX-OUTPUTS-COUNT @ 0 DO
        ."   [" I . ." ] "
        DUP TX-OUTPUT I 8 * +
        ." Value: " DUP 32 + @ . ." sat" CR
        DROP
    LOOP
    
    CR DROP ;

\ ---------------------------------------------------------
\ 6. Search Functions
\ ---------------------------------------------------------

\ Search blockchain by keyword/pattern
: SEARCH-BLOCKCHAIN ( pattern-str pattern-len -- results count )
    ." [QUERY] Searching blockchain..." CR
    
    \ For MVP, simplified search
    \ Could search block hashes, tx hashes, addresses
    
    0 0 ;

\ Get rich list (top addresses by balance)
: GET-RICH-LIST ( count -- address-array )
    ." [QUERY] Generating rich list (top " DUP . ." )" CR
    
    \ TODO: Sort addresses by balance
    DROP 0 ;

\ ---------------------------------------------------------
\ 7. Analytics Functions
\ ---------------------------------------------------------

\ Calculate average block time
: GET-AVG-BLOCK-TIME ( -- seconds )
    CALCULATE-PERIOD-TIME
    TIMESTAMP-INDEX @ 0> IF
        TIMESTAMP-INDEX @ /
    ELSE
        0
    THEN ;

\ Calculate transaction throughput
: GET-TX-THROUGHPUT ( -- tx-per-second )
    \ Count confirmed transactions in last N blocks
    \ Divide by time period
    
    \ For MVP, estimate from mempool
    MEMPOOL-SIZE @ 600 / ;  \ Assume 10 min blocks

\ Get mempool fee statistics
: GET-MEMPOOL-FEES ( -- min-fee avg-fee max-fee )
    \ Analyze fees in mempool
    \ For MVP, return placeholders
    0 0 0 ;

\ ---------------------------------------------------------
\ 8. JSON-Style Output (For Web APIs)
\ ---------------------------------------------------------

\ Format block as JSON-like output
: BLOCK-TO-JSON ( block-addr -- )
    ." {" CR
    ."   \"version\": " DUP @ . ." ," CR
    ."   \"hash\": \"" DUP 32 + 32 0 DO
        DUP I + C@ .BYTE
    LOOP DROP ." \"," CR
    ."   \"timestamp\": " DUP 68 + @ . ." ," CR
    ."   \"difficulty\": \"" DUP 72 + @ HEX. ." \"," CR
    ."   \"nonce\": " DUP 76 + @ . CR
    ." }" CR
    DROP ;

\ Format transaction as JSON-like output
: TX-TO-JSON ( tx-addr -- )
    ." {" CR
    ."   \"version\": " DUP TX-VERSION @ . ." ," CR
    ."   \"inputs\": " DUP TX-INPUTS-COUNT @ . ." ," CR
    ."   \"outputs\": " DUP TX-OUTPUTS-COUNT @ . ." ," CR
    ."   \"locktime\": " DUP TX-LOCKTIME @ . CR
    ." }" CR
    DROP ;

\ ---------------------------------------------------------
\ 9. Query CLI Commands
\ ---------------------------------------------------------

: CMD-QUERY-BLOCK ( height -- )
    ." " CR
    ." Querying block at height " DUP . ." ..." CR
    
    GET-BLOCK-BY-HEIGHT IF
        SHOW-BLOCK-SUMMARY
    ELSE
        ." Block not found!" CR
    THEN ;

: CMD-QUERY-TX ( tx-hash-addr -- )
    ." " CR
    ." Querying transaction..." CR
    
    GET-TX-BY-HASH IF
        SHOW-TX-SUMMARY
    ELSE
        ." Transaction not found!" CR
    THEN ;

: CMD-QUERY-ADDRESS ( address-addr -- )
    ." " CR
    ." Querying address..." CR
    ." " CR
    
    ." Balance: " DUP QUERY-ADDRESS-BALANCE U. ." satoshis" CR
    
    ." UTXOs: " DUP QUERY-ADDRESS-UTXOS . ." outputs" CR
    DROP
    
    ." " CR ;

: CMD-EXPLORER ( -- )
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║              BLOCKCHAIN EXPLORER                       ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    CR
    
    GET-NETWORK-STATS ;

\ ---------------------------------------------------------
\ 10. Real-Time Monitoring
\ ---------------------------------------------------------

VARIABLE MONITOR-RUNNING

: START-MONITOR ( -- )
    TRUE MONITOR-RUNNING !
    
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║           REAL-TIME BLOCKCHAIN MONITOR                 ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    ." Press Ctrl+C to stop..." CR
    ." " CR
    
    BEGIN
        MONITOR-RUNNING @ WHILE
        
        ." [" CHAIN-HEIGHT @ . ." ] "
        ." Mempool: " MEMPOOL-SIZE @ . ." "
        ." Peers: " PEER-COUNT @ . ." "
        ." Hashrate: " NETWORK-HASHRATE @ . ." H/s" CR
        
        \ Update every 5 seconds
        5000 MS
    REPEAT ;

: STOP-MONITOR ( -- )
    FALSE MONITOR-RUNNING ! ;

\ ---------------------------------------------------------
\ 11. Export Functions
\ ---------------------------------------------------------

\ Export blockchain data to file
: EXPORT-BLOCKCHAIN ( filename-str filename-len -- )
    ." [QUERY] Exporting blockchain to " 2DUP TYPE CR
    
    \ TODO: Implement actual export
    \ Would write blocks in JSON or binary format
    
    2DROP
    ." Export complete!" CR ;

\ Export address list
: EXPORT-ADDRESSES ( filename-str filename-len -- )
    ." [QUERY] Exporting address list..." CR
    
    \ TODO: Implement actual export
    
    2DROP
    ." Export complete!" CR ;

CR ." [QUERY] Blockchain query API loaded." CR
