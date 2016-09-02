(if *Dbg (daemon 'http (load "app.l")))
            
(class +User +Entity)
(rel nm (+Need +Key +String))          # User name
(rel pw (+String))               # Password
(rel slt (+String))               # Password

(class +Task +Entity)         # starts the entity definition
(rel nr (+Need +Key +Number)) # defines a key, needed collect/query
(rel usr (+Need +Ref +String))
(rel desc (+Need +String))
(rel done (+Ref +Number))   # task status

        
(class +Session +Entity)
(rel key (+Need +Key +String))
(rel usr (+Need +String))
(rel ts (+Need +Number))
                        
(de main()
    (pool "taskweb.db")
    (unless (seq *DB) (build) ) )


(de go()
        (hashpw "test" "test")
        (server 8088 "!home"))
    
(de build ()
        (msg "in init")
    (let Salt (gensalt)
         (new! '(+User) 'nm "admin" 'pw (hashpw "admin" Salt) 'slt Salt)))

(de disable-chunking ()
    (off *Chunked)
    (setq *Http1 0))

(de todo-list ()
    (disable-chunking)
    (httpHead "application/json" NIL)
    (ht:Out NIL (prin "[{\"description\" : \"abc\"}]")) )

        
(de json-get (Val)
    (cdr (assoc Val *Posted)) )

(de json-out (Val)
    (disable-chunking)
    (httpHead "application/json" NIL)
    (ht:Out NIL (printJson Val)))

(de ts () (+ (* 86400 (date)) (time T)))
(de session-genKey () (pack (getrandom 16)))
    
(de user-auth ()
    (msg *Data)
    (let (*Posted (parseJson *Data)
           UserName (json-get "username")
           Password (json-get "password")
           User (db 'nm '+User UserName)
           Salt (; User slt)
           HashedPw (hashpw Password Salt)
           Auth (= HashedPw (; User pw))
           SessionKey NIL)
    (ifn Auth
         (json-out (list (cons "noauth" 1)))
         (setq SessionKey (session-genKey))
         (new! '(+Session) 'key SessionKey 'usr UserName 'ts (ts))
         (json-out (list (cons "session" SessionKey))))))
               
         

(de home ()
    (redirect "/index.html"))

# c:\dev\picoLisp\pil @lib/http.l app.l -"server 8088 \"!home\""

