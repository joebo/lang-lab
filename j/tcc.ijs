load 'dll'
load 'task'
coclass 'tcc'


TCCPATH=:(jpath,'~bin'), '/tcc'
DLL=:TCCPATH,'/libtcc_shim.dll'
RunC=:'"',DLL,'" runc + x *c *c x x x'
InitC=:'"',DLL,'" tcc_init + x *c'
CompileC=:'"',DLL,'" tcc_compile + x *c'
ExecInt =: '"',DLL,'" tcc_exec_int + x *c x x x'
ExecIntOutInt =: '"',DLL,'" tcc_exec_int_long + x *c x x x *x'
FreeTCC=:'"',DLL,'" tcc_free + n'

SHIMCODE =: 0 : 0
#include <windows.h>
#include <stdio.h>
#include "libtcc.h"

#define DLL_EXPORT __declspec(dllexport)

static TCCState *_state;

DLL_EXPORT void* tcc_init (char*tcc_path) {
    _state = tcc_new();
    if (!_state) {
        return -1;
    }
    tcc_set_lib_path(_state, tcc_path);
    tcc_add_sysinclude_path(_state, tcc_path);

    tcc_set_output_type(_state, TCC_OUTPUT_MEMORY);
    return 1;
}

DLL_EXPORT int tcc_compile (char*prog) {
    if (tcc_compile_string(_state, prog) == -1) {
       fprintf(stderr, "Compilation error\n");
       fprintf(stderr, "prog: %s\n", prog);
       fflush(stderr);
       return -1;
    }

    if (tcc_relocate(_state, TCC_RELOCATE_AUTO) < 0)
        return -1;

}

DLL_EXPORT int tcc_exec_int (char *funcName, long mem, int len, int arg) {
    int (*func)(int,long, int);
    func = tcc_get_symbol(_state, funcName);
    if (!func)
        return -1;

    return func(mem,len, arg);
}

DLL_EXPORT int tcc_exec_int_long (char *funcName, long mem, long len, int arg, long long *out) {
    int (*func)(int,long, int, long*);
    func = tcc_get_symbol(_state, funcName);
    if (!func)
        return -1;

    return func(mem,len, arg, out);
}


DLL_EXPORT tcc_free () {
     tcc_delete(_state);
}

//runs a string and returns the result
DLL_EXPORT int runc (char *prog, char*tcc_path, long mem, int len, int arg)
{
    TCCState *s;
    int (*func)(int,long, int);

    s = tcc_new();
    if (!s) {
        return -1;
    }

    tcc_set_lib_path(s, tcc_path);
    tcc_add_sysinclude_path(s, tcc_path);

    tcc_set_output_type(s, TCC_OUTPUT_MEMORY);

    //printf("prog: %s\n", prog);

    if (tcc_compile_string(s, prog) == -1) {
       fprintf(stderr, "Compilation error\n");
       fprintf(stderr, "prog: %s\n", prog);
       fflush(stderr);
       return -1;
    }

    /* as a test, we add a symbol that the compiled program can use.
       You may also open a dll with tcc_add_dll() and use symbols from that */
    //tcc_add_symbol(s, "add", add);

    
    if (tcc_relocate(s, TCC_RELOCATE_AUTO) < 0)
        return -1;

    
    func = tcc_get_symbol(s, "func");
    if (!func)
        return -1;

    
    return func(mem,len, arg);

    
    tcc_delete(s);

}
)

checkInstall =: 3 : 0
if. 0=fexist(TCCPATH,'/lib/libtcc1.a') do.
smoutput 'tcc is not install propertly into ', TCCPATH
end.
)

validateShim =: 3 : 0
if. 0=fexist(TCCPATH,'/libtcc_shim.dll') do.
    SHIMCODE fwrite (TCCPATH,'/libtcc_shim.c')
    NB. compiles the shim using TCC.
    NB. first changes working directory to path for relative paths to work
    smoutput 'building tcc'
    spawn_jtask_ ('cmd.exe /c "cd "', TCCPATH, '"" && tcc libtcc_shim.c -o libtcc_shim.dll -shared -I libtcc libtcc\libtcc.def')
end.
)

checkInstall''
validateShim''

runc=: 3 : 0
if. 4=#y do.
'code mem size arg' =. y
else.
'code mem size' =. y
arg=.0
end.
RunC cd code;TCCPATH;mem;size;arg
)

init =: 3 : 0
ret=.InitC cd <TCCPATH
)
compile =: 3 : 'CompileC cd y'

execInt=: 3 : 0
if. 4=#y do.
'func mem size arg' =. y
else.
'func mem size' =. y
arg=.0
end.
ExecInt cd func;mem;size;arg
)

mwrite=: 4 : 0
p=. mema l=. #y
y memw p,0,l,x
p
)

execIntOutInt=: 3 : 0
'func mem size arg' =. y
out=:4 mwrite 0
ret=:ExecIntOutInt cd func;mem;size;arg;(<,out)
(0{::ret);(5{::ret)
)


free=: 3 : ' FreeTCC cd '''''

coclass 'base'

test1_code=: 0 : 0
#include<stdio.h>
int func(int mem, int len) {
long long *p = (long long*)mem;

int sum=0;
for(int i=1;i<=len;i++&&p++) {
  //printf("value at arg: %d\n",*p);
  sum+=*p;
}

fflush(stdout);
return sum;
}
)

NB. test adding up a numeric array
test1=: 3 : 0
addr=. mema (32*8)
ii=.1+i.10
ii memw addr,0,10,4
ret=.runc_tcc_ test1_code;addr;10
memf addr
assert. ((+/ii)=(0{::ret))
)

NB. test adding up a numeric array using init
test1a=: 3 : 0
addr=. mema (32*8)
ii=.1+i.10
ii memw addr,0,10,4

tcc=:init_tcc_''
compile_tcc_ <test1_code
ret=.execInt_tcc_ 'func';addr;10
memf addr
assert. ((+/ii)=(0{::ret))
)


test2_code=: 0 : 0
void reverse(char s[], int length) {
  int c, i, j;

  for (i = 0, j = length - 1; i < j; i++, j--) {
    c = s[i];
    s[i] = s[j];
    s[j] = c;
  }
}

int func(int mem, int len) {
    char *p = (int*)mem;
    reverse(p,len);
}
)

NB. test reversing a string array
test2=: 3 : 0
NB. should dynamically allocate based on size
addr=. mema 100
txt=. 'hello world'
'hello world' memw addr,0,(#txt)
ret=.runc_tcc_ test2_code;addr;(#txt)
data=.memr addr,0,(#txt)
memf addr
assert. (|.txt)=data
)



testInfix_code=: 0 : 0
#include <stdio.h>

void free2(long ptr) {
 free(ptr);
}

long infixs(long pmem, long len, int infixLen, long *out) {
    long allocLen = len*sizeof(char)*infixLen;
    char *newMem = (char*)malloc(allocLen);
    char *mem = (char*)pmem;

    long offset = 0;
    long idx = 0;

    while(1) {
        for(int q=0;q<infixLen;q++) {
            newMem[idx++] = mem[q+offset];
        }
        offset++;
        if ((len-offset) <= infixLen) {
           break;
        }
    }
    *out = idx;
    return newMem;
}
long infix(long pmem, long len, int infixLen, long *out) {
    long allocLen = len*sizeof(char)*infixLen;
    char *newMem = (char*)malloc(allocLen);
    char *mem = (char*)pmem;

    int d = 1+len-infixLen;

    char *x=newMem;
    char *y=mem;
    for(int i=0;i<d;i++) {
        memcpy(x,y,infixLen);
        x+=infixLen;
        y+=1;
    }

    *out = (d*sizeof(char)*infixLen);
    return newMem;
}

)

infix=: 3 : 0
txt=.y
addr=. mema 4*(#txt)
txt memw addr,0,(#txt)
infixSize=.9
tcc=:init_tcc_''
compile_tcc_ < testInfix_code
ret=.execIntOutInt_tcc_ 'infix';addr;(#txt);infixSize
smoutput ret
'memPtr size'=: ret
output =: memr memPtr,0,size
execInt_tcc_ 'free';memPtr;0
NB. free_tcc_''
output
)

infixf=: 3 : 0
txt=:y
addr=. mema 4*(#txt)
txt memw addr,0,(#txt)
infixSize=.9
ret=:execIntOutInt_tcc_ 'infix';addr;(#txt);infixSize
smoutput ret
'memPtr size'=: ret
output =: memr memPtr,0,size
execInt_tcc_ 'free';memPtr;0
NB. free_tcc_''
NB. output
)


testInfix =: 3 : 0
txt=. 'abcdefghijklmnopqrstuvwxyz'
infix txt
)
test1''
test1a''
test2''
testInfix''
NB. cdf''

NB. exit''

