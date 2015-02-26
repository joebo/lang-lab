require 'csv'
require 'jmf'
CSV_PATH=:'c:/users/joe bogner/downloads/'
IPMAP_PATH =: 'c:/temp/ip.jmf'
COUNTRIES_PATH =: 'c:/temp/countries.jmf'
PADDING=: 250


init=: 3 : 0
if. _1 = 4!:0 <'csv' do.
csv=:readcsv CSV_PATH,'IP2LOCATION-LITE-DB3.CSV'
ip=:". every 1{"1 csv
''
end.
)

createjmf=: 3 : 0
size=. 7!:5 <'ip'
createjmf_jmf_ IPMAP_PATH;size
map_jmf_ 'IPDB';IPMAP_PATH
ipmap=:ip
unmap_jmf_ 'IPDB'
)

writefixed =: 3 : 0
fixed=. PADDING {."1 (,"2 '|'(,~)"1 > _4{."1 csv)
fixed fwrite COUNTRIES_PATH
)

(JCHAR;PADDING) map_jmf_ 'CountriesDB';COUNTRIES_PATH
map_jmf_ 'IPDB';IPMAP_PATH

NB.  http://www.jsoftware.com/jwiki/JPhrases/RandomNumbers
m3=: 9!:1@<.@(+/ .^&2@(6!:0@]))
m3''

ip=. +/ (16777216 65536 256 1) * '.' (".;._2@,~) '24.210.19.47'
ip=. ((?@#) { ]) IPDB

response_request_ =: (ip I.~ IPDB) { CountriesDB


unmap_jmf_ 'CountriesDB'
unmap_jmf_ 'IPDB'