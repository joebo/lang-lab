(de gensalt-not-fork-safe ()
        (default *WorkFactor 12)
        (use (Hash Salt)
             (setq Hash (native "@" "mymalloc" 'N 64))
             (setq Salt (native "@" "mymalloc" 'N 64))
         # uses RtlGenRandom - https://msdn.microsoft.com/en-us/library/windows/desktop/aa387694(v=vs.85).aspx
           (native "advapi32.dll" "SystemFunction036" NIL Salt 16)
           (msg "Salt IS " Salt)
           (msg "hasxh is " Hash)
           (native "bcrypt.so" "encode_salt" NIL Hash (cons NIL (16) Salt) 16 *WorkFactor)
           (prog1
               (struct Hash 'S)
               (native "@" "myfree" NIL Hash))))

(de gensalt-reuse-not-fork-safe ()
    # should we reuse or create new each time?
    (default *Hash (native "@" "mymalloc" 'N 64))
    (default *WorkFactor 12)
    (use (Salt)
         # uses RtlGenRandom - https://msdn.microsoft.com/en-us/library/windows/desktop/aa387694(v=vs.85).aspx
           (native "advapi32.dll" "SystemFunction036" NIL '(Salt (16 B . 16)) 16)
           (msg "hash is " *Hash)
           (msg "Salt IS " Salt)
         (native "bcrypt.so" "encode_salt" NIL *Hash (cons NIL (16) Salt) 16 *WorkFactor))
          (struct *Hash 'S))
    

(de gensalt ()
    # should we reuse or create new each time?
    (default *Hash (native "@" "mymalloc" 'N 64))
    (default *WorkFactor 12)
    (use (Salt)
         (setq Salt (getrandom 16))
         (msg "Salt IS " Salt)
         (native "bcrypt.so" "encode_salt" NIL *Hash (cons NIL (16) Salt) 16 *WorkFactor))
          (struct *Hash 'S))



(de hashpw (Passwd Salt)
    (default *HashPw (native "@" "mymalloc" 'N 128))
    (use (Salt)
         (native "bcrypt.so" "pybc_bcrypt" NIL Passwd Salt *HashPw 128))
    (prog1
        (struct *HashPw 'S)
        (native "@" "mybzero" NIL *HashPw 128)))

