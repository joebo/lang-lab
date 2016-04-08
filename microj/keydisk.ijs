NB. 60 million rows of a number and a 2 character state
N=:60000000
tbl =: ('key';'state') } (3!:102) ('state';'n');((N,2) $ 'ABCDEFGHIJKLMNOPQRSTUVWXYZ');(i. N)

NB. write state key to disk
('c:/joe/lang-lab/microj/db/';'state') (151!:4) tbl

NB. write table to disk
(<'c:/joe/lang-lab/microj/db/') (151!:2) tbl

NB. timing 1 to read the entire table into memory (15 seconds and 1.8gb of ram)
NB. C:\dev\microj>bin\x64\Release\microj.exe -js "# (151!:3) (<'c:/joe/lang-lab/microj/db/')"
NB. 60000000
NB. Took: 14960 ms                                                                                  
NB. 1875219 After the test.
t1 =: 3 : 0
0 [ (151!:3) (<'c:/joe/lang-lab/microj/db/')
)

NB. read by key (2.5 seconds and 142mb of ram, 4.6 million rows)
NB. C:\dev\microj>bin\x64\Release\microj.exe -js "# ('c:/joe/lang-lab/microj/db/';'state') (151!:5) <'AB'"
NB. ('c:/joe/lang-lab/microj/db/';'state') (151!:5) <'AB'
NB. 4615385
NB. Took: 2475 ms
NB. 146204 After the test.
t2 =: 3 : 0 
0 [ ('c:/joe/lang-lab/microj/db/';'state') (151!:5) <'AB'
)