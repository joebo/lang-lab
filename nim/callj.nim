import os

proc getDLLName: string = 
     result = "c:/users/joe/j803/bin/j.dll" 

proc JInit(): int {.stdcall, importc, dynlib: getDLLName().}
proc JDo(j:int,cmd:cstring): int {.stdcall, importc, dynlib: getDLLName().}

var jt = JInit()
echo "ptr: ", jt
echo JDo(jt, "a=:1+1")

