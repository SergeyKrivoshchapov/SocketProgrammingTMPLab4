package main

/*
#include <stdlib.h>

typedef void (*DataCallback)(char* msg);

static void invokeCallback(DataCallback cb, char* msg) {
		if (cb != NULL) {
			cb(msg);
		}
}
*/
import "C"

// ServerStart starts the server with a callback for log messages
//
//export StartServer
func StartServer(port *C.char, callback C.DataCallback) C.int {
	serverMu.Lock()
	defer serverMu.Unlock()

	if server != nil {
		return -1
	}

	s := &Server{}
	s.SetCallback(callback)
	if err := s.Start(C.GoString(port)); err != nil {
		return -1
	}

	server = s
	return 0
}

//export StopServer
func StopServer() {
	serverMu.Lock()
	defer serverMu.Unlock()

	if server != nil {
		server.Stop()
		server = nil
	}
}

func main() {}
