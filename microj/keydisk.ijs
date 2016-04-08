N=:60000000
tbl =: ('key';'state') } (3!:102) ('state';'n');((N,2) $ 'ABCDEFGHIJKLMNOPQRSTUVWXYZ');(i. N)

('c:/joe/lang-lab/microj/db/';'state') (151!:4) tbl

(2;2) (151!:0) 'statekey';'c:/joe/lang-lab/microj/db/state_s2.key'
(<4) (151!:0) 'statekeyoff';'c:/joe/lang-lab/microj/db/state_l-keyoffset.key'
(<4) (151!:0) 'statekey';'c:/joe/lang-lab/microj/db/state_l.key'



NB. write to disk
(<'c:/joe/lang-lab/microj/db/') (151!:2) tbl

'' (151!:3) (<'c:/joe/lang-lab/microj/db/')

t1 =: 3 : 0
0 [ (<' ') (151!:3) (<'c:/joe/lang-lab/microj/db/')
)

NB. read by key
t2 =: 3 : 0 
0 [ ('c:/joe/lang-lab/microj/db/';'state') (151!:5) <'AB'
)