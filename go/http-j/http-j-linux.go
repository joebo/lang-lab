package main

import (
	"net/http"
	"runtime"
        "fmt"
        "syscall"
	"unsafe"
	"io/ioutil"
)
/*
#cgo LDFLAGS: -L.  -lcallj -ldl
void* jlib_call(char *url, char *body, int *outputLen);
void jlib_freemem(char *ptr);
*/
import "C"

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

	var buffer bytes.Buffer

	for _, c := range r.Cookies() {
		buffer.WriteString("COOKIE: ")
		buffer.WriteString(c.Name)
		buffer.WriteString(" ")
		buffer.WriteString(c.Value)
		buffer.WriteString("\n")
   	}
	buffer.WriteString("\x1FBODY: ")
	buffer.WriteString(string(body[:]))

	bodyBytes, _ := syscall.BytePtrFromString(string(body[:]))
	var bodyPtr = unsafe.Pointer(bodyBytes)
	
	var responseLen uint32 = 0;
	//ret, _, _ := syscall.Syscall6(uintptr(callJ), 3, uintptr(urlPtr), uintptr(bodyPtr), uintptr(unsafe.Pointer(&responseLen)), 0,0,0)
	ret, _ :=  C.jlib_call((*C.char)(urlPtr), (*C.char)(bodyPtr), (*C.int)(unsafe.Pointer(&responseLen)));


	//TODO: http://stackoverflow.com/questions/19060015/integrating-existing-c-code-to-go-convert-unsigned-char-poiner-result-to-byte
	var output = bytePtrToString((uintptr)(unsafe.Pointer(ret)), int(responseLen))

	var headerEnd = strings.Index(output, "\n\n");

	if headerEnd == -1 {
		w.Header().Set("Content-Type", "text/html")
		fmt.Fprint(w, output)
	} else {
		var headers = strings.Split(output[:headerEnd], "\n")
		var outputBody = output[headerEnd+1:]

		fmt.Printf("body is %s\n",outputBody)
		for _, vv := range headers {
			var header = strings.Split(vv, ":")
			w.Header().Set(header[0], strings.Trim(header[1], " "))	
		}
		if len(headers) > 0 && strings.Contains(headers[0], "Location:") {
			var url = strings.Split(headers[0], ":")
			http.Redirect(w, r, strings.Trim(url[1], " "), http.StatusFound)
		}

		fmt.Fprint(w, outputBody)
	}


	C.jlib_freemem((*C.char)(ret))

	/*
	w.Header().Set("Content-Type", "text/html")
	fmt.Fprintf(w, "hello world %s %d", body, urlPtr, bodyPtr, responseLen, ret);
	*/
}

