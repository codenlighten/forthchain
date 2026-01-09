\ Debug SHA-256 by comparing round outputs against Python reference
\ This finds exactly where hash diverges from NIST test vector

require src/debug.fs
require src/crypto/sha256.fs

\ Test vector: "abc" should produce DA39A3EE5E6B4B0D3255BFEF95601890AFD80709 for empty
\ Actually: "abc" should produce A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3

\ Input buffer for "abc"
3 CONSTANT TEST-LEN
CREATE TEST-INPUT 3 C, 
  CHAR a C,
  CHAR b C,
  CHAR c C,
  0 C, 0 C, 0 C, 0 C,   \ Padding starts
  
\ Expanded message schedule buffer
CREATE MSG-SCHED 512 ALLOT

\ Track a,b,c,d,e,f,g,h after each round
CREATE ROUND-TRACE 64 8 * CELLS ALLOT   \ 64 rounds, 8 values each

: DUMP-U32 ( u32 -- )
  BASE @ >R HEX
  DUP 8 0 DO
    DUP 28 RSHIFT $F AND
    DUP 9 < IF EMIT ELSE 55 + EMIT THEN
    4 LSHIFT
  LOOP DROP
  R> BASE ! ;

: TRACE-ROUND ( a b c d e f g h round -- a b c d e f g h )
  DUP 8 * ROUND-TRACE + >R
  R@ 28 + !  \ h
  R@ 24 + !  \ g
  R@ 20 + !  \ f
  R@ 16 + !  \ e
  R@ 12 + !  \ d
  R@ 8 + !   \ c
  R@ 4 + !   \ b
  R@ !       \ a
  ;

: SHA256-TRACE ( addr len -- h0 h1...h7 )
  \ Prepare input
  SHA256-INIT
  OVER SHA256-BLOCK
  ;

: SHOW-TRACE
  64 0 DO
    I 2 .R SPACE
    I 8 * ROUND-TRACE + 
    8 0 DO
      DUP I CELLS + @ DUMP-U32 SPACE
    LOOP
    DROP CR
  LOOP ;

: COMPARE-VECTORS
  CR ." Testing 'abc'..." CR
  TEST-INPUT TEST-LEN SHA256
  
  ." Result:   " 8 0 DO DUP I CELLS + @ DUMP-U32 SPACE LOOP CR
  ." Expected: A665A459 20422F9D 417E4867 EFDC4FB8"
  ." 04A1F3FF F1FA07E9 98E86F7F 7A27AE3" CR ;

\ Execution
COMPARE-VECTORS
