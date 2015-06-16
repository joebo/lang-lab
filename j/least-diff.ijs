NB. brute force solution to https://raw.githubusercontent.com/hakank/hakank/master/picat/least_diff.pi
arr=:(i.!10) A. i. 10
tonum=: ". @: (,/"1 @: ":"0)
a=: tonum (5{."1 arr)
b=: tonum (5}."1 arr)
mindiff=: (<./) (#~ >&0) a-b
idx=:(a-b) i. 247

Note 'output'

   idx { a
50123
   idx { b
49876
   mindiff
247

)