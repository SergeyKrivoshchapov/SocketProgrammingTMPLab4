package main

/*
#cgo LDFLAGS: -L../../bin -ltask1_server
#include <stdlib.h>
#include "../../bin/task1_server.h"
*/
import "C"
import (
	"bufio"
	"fmt"
	"os"
	"unsafe"
)

func main() {
	port := "8081"
	if len(os.Args) > 1 {
		port = os.Args[1]
	}
	cPort := C.CString(port)
	defer C.free(unsafe.Pointer(cPort))
	result := C.StartServer(cPort)

	if result != 0 {
		fmt.Println("Error starting server")
		return
	}

	fmt.Printf("File server started on port %s.\n Press Enter to exit.\n", port)

	bufio.NewReader(os.Stdin).ReadString('\n')
	C.StopServer()
	fmt.Printf("Server stopped on port %s.\n", port)
}
