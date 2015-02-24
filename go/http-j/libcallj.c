//"c:\Users\Joe Bogner\Downloads\tcc\tcc" callj.c j.def -shared -o ..\..\go\http-j\callj.dll

//gcc -shared -fPIC callj.c -L libj.so -o callj.so

#if _WIN64
#include <windows.h>
#define DLL_EXPORT __declspec(dllexport)
#else
#define DLL_EXPORT
#endif

typedef long long I;

#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#if _WIN32

void jlib_bind() { }
void jlib_close() { }
#else

#include <dlfcn.h>
typedef void* (*JInitType)();
typedef int (*JDoType)(void*, char*);
typedef int (*JSetMType)(void*, char*, I*, I*, I*, I*);
typedef int (*JGetMType)(void*, char*, I*, I*, I*, I*);
typedef void* (*JFreeType)(void*);
typedef void* (*dllquitType) (void*);

static JInitType JInit = NULL;
static JDoType JDo = NULL;
static JSetMType JSetM = NULL;
static JGetMType JGetM = NULL;
static JFreeType JFree = NULL;
static dllquitType dllquit = NULL;

//may need to call dlclose, but not worried about it now
void* jlib_bind() {
  void *handle = dlopen("libj.so", RTLD_LAZY);
  if(!handle) {
    fputs(dlerror(), stderr);
    exit(1);
  }
  JInit = dlsym(handle, "JInit");
  JDo = dlsym(handle, "JDo");
  JSetM = dlsym(handle, "JSetM");
  JGetM = dlsym(handle, "JGetM");
  JFree = dlsym(handle, "JFree");
  dllquit = dlsym(handle, "dllquit");

  return handle;
}

#endif

DLL_EXPORT char* jlib_call(char *url, char *body, int *outputLen) {

#if _WIN32
#else
  if (JInit == NULL) { jlib_bind(); }
#endif

  void* j  = (void*)JInit();

  
  I typei=2;
  I ranki=1;
  I shapei[1];
  shapei[0] = (int)strlen(url);
  void *pshapei = (void*)shapei;
  void *pdata = (void*)url;

  int ret = JSetM(j, "url_request_", &typei, &ranki, &pshapei, &pdata);
    
  shapei[0] = strlen(body);
  pdata=body;
  ret = JSetM(j, "body_request_", &typei, &ranki, &pshapei, &pdata);
  ret = JDo(j, "0!:0 <'server.ijs'");
  printf("ret: %d\n", ret);
  if (ret!=0) {
      char error[] = "parsing error";
      int len = strlen(error);
      char *output = (char*)malloc(sizeof(char)*len);
      strcpy(output, error);
      *outputLen = len;
      JFree(j);
      return output;
  }
  //printf("ret: %d\n", ret);

    
  int type = 0;
  int *v = &type;
  int rank = 0;
  long *shape;
  void *pvals = 0;
  ret = JGetM(j, "response_request_", v, &rank, &shape, &pvals);
  //printf("ret: %d, type: %d, rank: %d, shape: %d", ret, type, rank, shape[0]);
    
  int len = shape[0];
  char *output = (char*)malloc(sizeof(char)*(len+1));
  strcpy(output, (char*)pvals);
  *outputLen = len;
  printf("\nresponse is: %s\n", output);

  JFree(j);

  return output;
    
}


DLL_EXPORT void jlib_freemem(char *ptr) {
  free(ptr);
}

