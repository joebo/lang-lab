coclass 'jtableFilter'


coclass 'jtable'
OUTPUTROWS=:10

create=: 3 : 0
    outputCols=:''
    headers=:''
    rowCount=:0
    rowLimit=: OUTPUTROWS
    orderIdx=:''
    whereIdx=:''
)

reset =: 4 : 0
    orderIdx=:''
    whereIdx=:''
    y
)

selectcol =: 3 : 0
    if. (y-:'*') do. (outputCols =: i. # headers) else. (outputCols =: headers i. (boxxopen y) ) end.
)

limit =: 4 : 0
    rowLimit__CURRENT=.x
    y
)

order =: 4 : 0
    sortCol =. headers__CURRENT i. (cutopen x)
    sortData=. sortCol {:: data__CURRENT
    orderIdx__CURRENT=: /: sortData
    y
)

where =: 4 : 0
    y
)

count =: 3 : 0
   ct =. rowCount__CURRENT
   18!:4 <'base'
   ct
)

select =: 3 : 0
    NB. can't call dyadic version because 18!:4 won't due to nested named
    selectcol__CURRENT '*'
    18!:4 <'base'
    y
:
   selectcol__CURRENT x
    18!:4 <'base'
    y
)

NB. gets a column from a ((N*C), W) shaped array
NB. where N is the number of rows, C is the # of columns
NB. and W is the widest
getCol =: 2 : 'y #~ (0{::$y) $ 1 (m) } n$0'

readcsv =: 3 : 0
'' readcsv y
:
    create''
    if. (1=#y) do. txt=. fread y else. txt=. y end.
    headers =: ',' cut 0 {:: LF cut 4000 {. txt
    rawData=: (',',LF) (e.~ ];._1 ]) LF,txt
    data=:''
    for_i. headers do.
        d=. (}. i_index getCol (# headers) rawData)
        
        NB. chop off the trailing LF on the first col
        if. (i_index = 0)  do. d=. _1 }. d end.

        NB. hack to convert
        NB. todo add dynamic detection
        if. (i_index=2) do. d =. ". d end.
        data =: data , (<d)
    end.
    rowCount =: (# 0{:: data)
)

NB. from_z_ =: (3 : 'CURRENT_jtable_ =: y [ 18!:4 <''jtable''')
from_z_ =: (3 : 'CURRENT_jtable_ =: y [ 18!:4 y')

jt_z_ =: 3 : 0
    cur =. CURRENT_jtable_
    cols =. outputCols__cur
    
    limit =. <. / (OUTPUTROWS_jtable_ ,  rowCount__cur, rowLimit__cur)
    row=.''
    for_c. cols do.
        row=. row, (c { headers__cur)
    end.
    output =: row

    for_i. (i. limit) do.
        row=.''
        for_c. cols do.
            colData=. c {:: data__cur
            NB. sort the data if it's ordered
            if. (# orderIdx__cur) > 0 do. colData=. orderIdx__cur { colData end.
            row=. row , (<(i {:: colData))
        end.
        output =.  output , row
    end.
    smoutput (-1*(#cols))[\ output
)

coclass 'base'
NB. 'base' copath 'jtable'

tbl=:conew 'jtable'
tbl2=:conew 'jtable'

readcsv__tbl (0 : 0)
name,gender,id
joe,m,1
sally,f,2
jane,f,3
jack,m,4
)


N=:1e2
testData=: ('a',',b',LF) , (,  ,&LF"1  ((2&{.),',' , (_3&{.))"1  (_5[\ a. {~ 97+(?. (N*5)#26)))
readcsv__tbl2 testData

smoutput 'BASIC test'
jt select from tbl

Note 'foo'
smoutput 'SELECT name;gender'

jt ('name';'gender') select from tbl

smoutput 'SELECT gender;name'
jt ('gender';'name') select from tbl

smoutput 'LIMIT 1'
jt ('id';'name') select 1 limit from tbl


smoutput 'order data order by name'
jt ('id';'name') select 'name' order 999 limit from tbl

smoutput 'junk data'
jt select from tbl2

jt select 'a -: ''mw''' where from tbl2