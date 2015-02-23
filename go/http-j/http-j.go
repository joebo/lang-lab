package main

import (
	"net/http"
	"runtime"
        "fmt"
        "syscall"
	"unsafe"
	"io/ioutil"
)

var (
	calljdll, _        = syscall.LoadLibrary("callj.dll")
	callJ, _ = syscall.GetProcAddress(calljdll, "jlib_call")
	freeJMem, _ = syscall.GetProcAddress(calljdll, "jlib_freemem")
)        
func main() {
	runtime.GOMAXPROCS(runtime.NumCPU())

	http.HandleFunc("/", handler)
	http.ListenAndServe(":8080", nil)
}

// From: https://code.google.com/p/go/source/browse/src/pkg/net/interface_windows.go
func bytePtrToString(p uintptr) string {
    a := (*[10000]uint8)(unsafe.Pointer(p))
    i := 0
    for a[i] != 0 {
        i++
    }
    return string(a[:i])
}

func handler(w http.ResponseWriter, r *http.Request) {

	var url = r.URL.RequestURI();
	urlBytes, _ := syscall.BytePtrFromString(url)
	var urlPtr = unsafe.Pointer(urlBytes);

	body, _ := ioutil.ReadAll(r.Body)

	bodyBytes, _ := syscall.BytePtrFromString(string(body[:]))
	var bodyPtr = unsafe.Pointer(bodyBytes)
	
	//fmt.Println("GO URL:", r.URL.RequestURI());
	var responseLen uint32 = 0;
	ret, _, _ := syscall.Syscall6(uintptr(callJ), 3, uintptr(urlPtr), uintptr(bodyPtr), uintptr(unsafe.Pointer(&responseLen)), 0,0,0)
	//fmt.Println(responseLen)

	var output = bytePtrToString(ret)
	//fmt.Println(output)
	if ret == 0 {
		fmt.Println("error");
	}
	w.Header().Set("Content-Type", "text/html")
	fmt.Fprintf(w, output);
}

