//"c:\Users\Joe Bogner\Downloads\tcc\tcc" callj.c j.def -shared -o ..\..\go\http-j\callj.dll

#include <windows.h>
#define DLL_EXPORT __declspec(dllexport)

#if _WIN64 || __amd64__
typedef long long I;
#else
typedef long I;
#endif

DLL_EXPORT char* jlib_call(char *url, char *body, int *outputLen) {
    int j = JInit();

    I typei=2;
    I ranki=1;
    I shapei[1];
    shapei[0] = (int)strlen(url);
    void *pshapei = (void*)shapei;
    void *pdata = (void*)url;
    //ret = JSetM(jt, "b", addr(t), addr(r), addr(sptr), addr(dataptr))
    int ret = JSetM(j, "url_request_", &typei, &ranki, &pshapei, &pdata);
    
    shapei[0] = strlen(body);
    ret = JSetM(j, "body_request_", &typei, &ranki, &pshapei, &pdata);
    ret = JDo(j, "0!:0 <'server.ijs'");
    //printf("ret: %d\n", ret);

    
    int type = 0;
    int *v = &type;
    int rank = 0;
    long *shape;
    void *pvals = 0;
    ret = JGetM(j, "response_request_", v, &rank, &shape, &pvals);
    //printf("ret: %d, type: %d, rank: %d, shape: %d", ret, type, rank, shape[0]);
    
    int len = shape[0];
    char *output = (char*)malloc(sizeof(char)*len);
    strcpy(output, (char*)pvals);
    *outputLen = len;
    printf("\n\nresponse is: %s\n", output);
    
    JFree(j);
    
    return output;
    
}

DLL_EXPORT void jlib_freemem(char *ptr) {
    free(ptr);
}

int main() {
    int outputLen = 0;
    jlib_call("foo", "foo", &outputLen);

    /*
    int j = JInit();

    long long rank = 1;
 
    long long shape[1];
    long long *pshape = shape;
    shape[0] = 2;

    long long vals[2];
    vals[0] = 2;
    vals[1] = 4;
    long long *pvals = vals;

    //long typei[] = { 2 };
    long long type = 2;
 
    int ret = JSetM(j, "a", &type, &rank, &pshape, &pvals);
    printf("ret: %d, type: %d, rank: %d, shape: %d", ret, type, rank);
    */
}
