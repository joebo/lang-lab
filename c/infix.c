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

//http://www.cse.yorku.ca/~oz/hash.html
unsigned long djb2(unsigned char *str, int len) {
    unsigned long hash = 5381;
    for(int i=0;i<len;i++)
        hash = ((hash << 5) + hash) + str[i]; /* hash * 33 + c */

    return hash;
}


unsigned int __stdcall  infixThreaded(void *threadData) {
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

    //unsigned long tableSize = 2*d;
    for(LONG i=0;i<=blockSize && ctr<=d;i++) {
        //unsigned long hash = djb2(x, infixLen) % tableSize;

        memcpy(x,y,infixLen);
        //printf("start: %d on thread %d, blockSize : %d\n", ctr, args->threadId, blockSize);
        x+=infixLen;
        y+=1;
        ctr++;
    }

    return 0;
}


int add1(int src)
{
    int dst;
     
     asm ("mov %0, %1\n\t"
         "add $1, %0"
         : "=r" (dst)
          : "r" (src)
          : "memory", "cc" );
     
     printf("add: %d\n", dst);
  return dst;
}

int hash2(char *pMem)
{
    printf("char: %d\n", *pMem);
    printf("ptr: %ld\n", pMem);
    int dst=0; 
    int infixSize=9;
    /*
    asm ("mov %%eax, 0x1505\n\t"
         "mov %%rcx, 0x21\n\t"
         "imul %%eax, %%rcx\n\t"
         "mov %1, %%eax"
         : "=r" (dst)
         : "r" (pMem)
         : "eax");
    */

    asm (

         "mov $0, %%edx \n"
         "movl $0x1505, %%eax \n"
         "1: \n"
         "imul $0x21, %%eax \n"
         //"mov $0, %%p \n"
         //"add %1, %%eax \n"
         // "movsx BYTE PTR [
         //"add $1, %%ecx \n"
         //"add %%rdx, %%eax \n"
         //"cmp %2, %%ecx \n"
         //"jl 1b \n"
         //"movl %2, %%esi \n"
         "mov $0, %0 \n"
         "movb (%%ebx), %%ax \n"
         "mov %%ax, %0 \n"
         //"movsw \n"
         : "=a" (dst)
         : "b" (pMem),"c" (infixSize)
         : "esi");


    printf("hash: %d\n", (char*)dst);
  return dst;
}


int count_unique_hash(void* newMem, LONG infixLen, LONG rowCt) {
    char *pMem = newMem;
    //http://www.jsoftware.com/papers/indexof/indexofscript.htm
    unsigned long tableSize = 2*rowCt;
    char**table = VirtualAlloc(0, tableSize * sizeof(char*), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

    //adds 2 seconds to initialize
    for(LONG i =0; i < tableSize; i++) {
        table[i] = 0;
    }
    
    LONG matches=0;
    LONG collisions=0;
    for(LONG i = 0; i < rowCt; i++) {

        
        unsigned long hash = 5381;
        
        for(int k=0;k<infixLen;k++)
            hash = (hash + pMem[k]) * 33; // ((hash << 5) + hash) + pMem[k]; 
        */
        unsigned long hash = 0;
        for(int k=0;k<infixLen;k++) 
            /* xor the bottom with the current octet */
            hash ^= pMem[k];
            hash *= 0x01000193;
        }
        
        //printf("%lu\n", hash);
        hash = hash % tableSize;
        //long hash = djb2(pMem, infixLen) % tableSize;
        if (table[hash] == 0) {
            table[hash] = pMem;
           matches++;
        }
        else {
            int match = memcmp(pMem, table[hash], infixLen); //strncmp(pMem, table[hash], infixLen);
           if (match !=0) {
              collisions++;
           }
           //collisions++;
            //matches++;
        }

        //printf("key: %d\n", hash);
        pMem+=infixLen;
    }
    free(table);
    printf("done, found %d possible matches, collisions: %d\n", matches, collisions);
    return matches;
}

int compare(const void* a, const void* b)
{
    return strncmp(a, b, 9);
}

int count_unique_sort(void* newMem, LONG infixLen, LONG rowCt) {
    long uniq = 0;
    qsort(newMem, rowCt, sizeof(char)*infixLen, compare);
    char *pMem = newMem;
    for(LONG i=0;i<rowCt;i++) {
        int match = strncmp(pMem, (pMem+infixLen), infixLen);
        if (match != 0) {
           uniq++;
        }
        pMem+=infixLen;
    }
    return uniq;
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


    int uniqCt = count_unique_hash(newMem, infixLen, d);
    printf("found %d unique values\n", uniqCt);

    //int uniqCt2 = count_unique_sort(newMem, infixLen, d);
    //printf("count_unique_sort found %d unique values\n", uniqCt2);

    fflush(stdout);
    *out = (d*infixLen);
    return newMem;
}

int main() {

    char fileName[] = "c:/users/joe bogner/downloads/chr2.fa";
    FILE *f = fopen(fileName, "rb");
    fseek(f,0,SEEK_END);
    long length=ftell(f);
    fseek(f, 0, SEEK_SET);
    char *mem = VirtualAlloc(0, length, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    fread(mem, sizeof(char), length, f);
    long count = 0;
    //length = 9;
    infix(mem, length, 9, &count);
    printf("count: %lld\n", count);
    //add1(10);
    //hash(mem);
    fclose(f);
}
