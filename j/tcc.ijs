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

#define LONG long long

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

DLL_EXPORT int tcc_exec_int_long (char *funcName, void*mem, LONG len, LONG arg, LONG *out) {
    int (*func)(int,LONG, LONG, LONG*);
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

long infixslow(long pmem, long len, int infixLen, long *out) {
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

testInfix =: 3 : 0
txt=. 'abcdefghijklmnopqrstuvwxyz'
infix txt
)

threadTest_code=: 0 : 0
#include <stdio.h>
#include <windows.h>
#include <process.h>

void loop(void *arg) {
printf ("Thread #: %d\n", arg);
fflush(stdout);
return 0;
}

int func(long pmem, long len) {
  printf("hello\n");
  fflush(stdout);
  _beginthread( loop, 1, 1 );
  return 1;
}
)

testThread =: 3 : 0
ret=.runc_tcc_ threadTest_code;0;0
)

testInfixThreaded_code=: 0 : 0
#include <stdio.h>
#include <windows.h>
#include <process.h>

#define LONG long long

const int THREADS = 4;
void free(void* ptr) {
 //free wont work since we use VirtualAlloc
 //free(ptr);
 VirtualFree(ptr, 0, MEM_RELEASE);
}


typedef struct ThreadData ThreadData;
struct ThreadData {
       void *mem;
       int threadId;
       LONG d;
       int infixLen;
       void *newMem;
       LONG len;
};

int  infixThreaded(void *threadData) {
    ThreadData *args = (ThreadData*)threadData;
    LONG infixLen = args->infixLen;
    LONG blockSize = (args->d+(THREADS-1))/THREADS;
    LONG d = args->d;
    
    char *x=(char*)args->newMem;
    char *y=args->mem;
    LONG ctr = 0;

    for(LONG i=0;i<args->threadId*blockSize;i++) {
        x+=infixLen;
        y+=1;
        ctr+=1;
    }


    for(LONG i=0;i<=blockSize && ctr<=d;i++) {
        memcpy(x,y,infixLen);
        //printf("start: %d on thread %d, blockSize : %d\n", ctr, args->threadId, blockSize);
        x+=infixLen;
        y+=1;
        ctr++;
    }

    return 0;
}


int infix(void* pmem, LONG len, LONG infixLen, LONG *out) {
    LONG allocLen = len*sizeof(char)*infixLen;
        
    //malloc fails on larger than 2gb allocations
    //char *newMem = (char*)malloc(allocLen);
    char *newMem = VirtualAlloc(0, allocLen, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    if (NULL == newMem) {
       printf("could not allocate\n");
       fflush(stdout);
       return -1;
    }

    char *mem = (char*)pmem;
    LONG d = 1+len-infixLen;


    //skip threads for small args
    if (d < 1000000) {
        char *x=newMem;
        char *y=mem;
        for(int i=0;i<d;i++) {
            memcpy(x,y,infixLen);
            x+=infixLen;
            y+=1;
        }
    } else {

        HANDLE* threads = (HANDLE*)malloc(sizeof(HANDLE)*THREADS);
        ThreadData **args = (ThreadData**)malloc(sizeof(ThreadData)*THREADS);
        for(int i=0;i<THREADS;i++) {
            ThreadData *arg = (ThreadData*)malloc(sizeof(ThreadData));
            arg->mem = pmem;
            arg->threadId = i;
            arg->d = d;
            arg->infixLen = infixLen;
            arg->newMem = newMem;
            arg->len = len;
            args[i] = arg;
            threads[i] = _beginthreadex(NULL, 0, &infixThreaded, arg, 0, NULL);
        }
        WaitForMultipleObjects(THREADS, threads, 1, INFINITE);
        for(int i = 0; i<THREADS; i++) { CloseHandle(threads[i]); free(args[i]); }
        free(threads);
    }

    printf("done\n");

    fflush(stdout);
    *out = (d*infixLen);
    return newMem;
}

)

testinfixThreaded=: 3 : 0
txt=.y
addr=. mema 4*(#txt)
txt memw addr,0,(#txt)
infixSize=.9
tcc=:init_tcc_''
compile_tcc_ < testInfixThreaded_code
ret=:execIntOutInt_tcc_ 'infix';addr;(#txt);infixSize
NB. smoutput ret
'memPtr size'=: ret
output =: memr memPtr,0,size
execInt_tcc_ 'free';memPtr;0
smoutput 'done'
free_tcc_''
ret
NB. output
)


NB. test1''
NB. test1a''
NB. test2''
NB. testInfix''
NB. testThread''
abc=. 'abcdefghijklmnopqrstuvwxyz'
NB. testinfixThreaded abc

txt=:fread 'c:/users/joe bogner/downloads/chr2.fa'
txt=:(txt #~ -. LF = txt)






  
  
NB. cdf''

NB. exit''

NB. testinfixThreaded txt
6!:2 'testinfixThreaded abc'