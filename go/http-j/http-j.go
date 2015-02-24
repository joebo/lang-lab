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
	calljdll, _        = syscall.LoadLibrary("libcallj.dll")
	callJ, _ = syscall.GetProcAddress(calljdll, "jlib_call")
	freeJMem, _ = syscall.GetProcAddress(calljdll, "jlib_freemem")
)        
func main() {
	runtime.GOMAXPROCS(runtime.NumCPU())

	http.HandleFunc("/", handler)
	http.ListenAndServe(":8080", nil)
}


//hacky
func bytePtrToString(p uintptr, len int) string {
	a:=make([]uint8,len+1);
	for i := 0; i < len; i++ {
		a[i] = (*[1]uint8)(unsafe.Pointer(p+uintptr(i)))[0]
	}
	return string(a[:len])
}

func handler(w http.ResponseWriter, r *http.Request) {

	var url = r.URL.RequestURI();
	urlBytes, _ := syscall.BytePtrFromString(url)
	var urlPtr = unsafe.Pointer(urlBytes);

	body, _ := ioutil.ReadAll(r.Body)

	bodyBytes, _ := syscall.BytePtrFromString(string(body[:]))
	var bodyPtr = unsafe.Pointer(bodyBytes)
	
	var responseLen uint32 = 0;
	ret, _, _ := syscall.Syscall6(uintptr(callJ), 3, uintptr(urlPtr), uintptr(bodyPtr), uintptr(unsafe.Pointer(&responseLen)), 0,0,0)


	var output = bytePtrToString(ret, int(responseLen))

	if ret == 0 {
		fmt.Println("error");
	}
	w.Header().Set("Content-Type", "text/html")
	fmt.Fprintf(w, output);

	syscall.Syscall6(uintptr(freeJMem), 1, uintptr(ret),0,0,0,0,0);
}

