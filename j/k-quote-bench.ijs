
CSV=:'c:/users/joebog~1/downloads/q/q.csv'
split =: (',',LF) (e.~ ];._1 ]) LF,]
col=: 4 : 0
1 (<:x)}(y#0)
)
getCol =: (] #~ (#@] $ [))

NB. attempted to use jmf, but didn't help much since the file wasn't being split and I had to drop the last LF
NB. require 'jmf'
NB. len=.1!:4 <CSV
NB. (JCHAR;len) map_jmf_ 'txt';CSV

bench=: 3 : 0
txt=. _1}. (fread CSV)
txtb=.split txt
txt=.''
sym=:s:(1 col 8) getCol txtb
bid=:(4 col 8) getCol txtb
(~.sym); (sym {:/.bid)
)

toInt=. ([: ". ((' '&~: * CR&~:) # ]))"1

Note 'K'

K takes awhile to load
q)\t quote:flip `sym`time`ex`bid`bsize`ask`asize`mode!("STCIIIIC";",")0:`q.csv
14058

but is faster to query

q)select last bid by sym from quote
sym| bid
---| ----
AAF| 1244
AAG| 1280
AAI| 1297
AAM| 1277
AAP| 1409
AAT| 1433
AAY| 1434
AAZ| 1064
ABC| 1266
ABF| 1377
ABH| 1307
ABI| 1364
ABK| 1039
ABL| 1323
ABN| 1363
ABO| 1012
ABP| 1207
ABV| 1377
ABY| 1057
ACD| 1184
..
q)\t select last bid by sym from quote
156

q)\t select count bid by sym from quote
187

)

Note 'J'

6!:2 'ret=:bench 0'
13.2156 seconds
15{. each ret

|: <"_1@> 15 {. each ret
+----+------------+
|`AAF|1244        |
+----+------------+
|`AAG|1280        |
+----+------------+
|`AAI|1297        |
+----+------------+
|`AAM|1277        |
+----+------------+
|`AAP|1409        |
+----+------------+
|`AAT|1433        |
+----+------------+
|`AAY|1434        |
+----+------------+
|`AAZ|1064        |
+----+------------+
|`ABC|1266        |
+----+------------+
|`ABF|1377        |
+----+------------+
|`ABH|1307        |
+----+------------+
|`ABI|1364        |
+----+------------+
|`ABK|1039        |
+----+------------+
|`ABL|1323        |
+----+------------+
|`ABN|1363        |
+----+------------+


J queries are about 4x slower (0.2 vs 0.96, but overall it was faster)
6!:2 '(~.sym); (sym {:/.bid)'
0.968643

{: isn't overly optimized for this, but we can write our own version that runs nearly as fast

6!:2 '(~.sym);((~. i:~ sym) { bid)'
0.24893

 |:<"_1@>15{. each ((~.sym);((~. i:~ sym) { bid))
+----+------------+
|`AAF|1244        |
+----+------------+
|`AAG|1280        |
+----+------------+
|`AAI|1297        |
+----+------------+
|`AAM|1277        |
+----+------------+
|`AAP|1409        |
+----+------------+
|`AAT|1433        |
+----+------------+
|`AAY|1434        |
+----+------------+
|`AAZ|1064        |
+----+------------+
|`ABC|1266        |
+----+------------+
|`ABF|1377        |
+----+------------+
|`ABH|1307        |
+----+------------+
|`ABI|1364        |
+----+------------+
|`ABK|1039        |
+----+------------+
|`ABL|1323        |
+----+------------+
|`ABN|1363        |
+----+------------+


count is fast and runs even faster than K

6!:2 '(sym #/.bid)'
0.0973729   
)