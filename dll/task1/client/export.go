package main

/*
#include <stdlib.h>
typedef struct {
	int status;
	char* msg;
} Reply;
*/
import "C"
import "unsafe"

func NewSuccess(msg string) *C.Reply {
	reply := (*C.Reply)(C.malloc(C.size_t(unsafe.Sizeof(C.Reply{}))))
	reply.status = C.int(0)
	reply.msg = C.CString(msg)
	return reply
}

func NewError(err string) *C.Reply {
	reply := (*C.Reply)(C.malloc(C.size_t(unsafe.Sizeof(C.Reply{}))))
	reply.status = C.int(-1)
	reply.msg = C.CString(err)
	return reply
}

func NewErrorFromErr(err error) *C.Reply {
	if err == nil {
		return NewSuccess("")
	}
	return NewError(err.Error())
}

func Free(reply *C.Reply) {
	if reply != nil {
		C.free(unsafe.Pointer(reply.msg))
		C.free(unsafe.Pointer(reply))
	}
}

//export ConnectToServer
func ConnectToServer(address *C.char) *C.Reply {
	clientMu.Lock()
	defer clientMu.Unlock()
	if client != nil {
		return NewError("client already connected")
	}
	c := &FileClient{}
	if err := c.Connect(C.GoString(address)); err != nil {
		return NewErrorFromErr(err)
	}

	client = c
	return NewSuccess(c.GetDrives())
}

//export DisconnectFromServer
func DisconnectFromServer() {
	clientMu.Lock()
	defer clientMu.Unlock()

	if client != nil {
		client.Disconnect()
		client = nil
	}
}

//export GetDirectoryContent
func GetDirectoryContent(path *C.char) *C.Reply {
	clientMu.Lock()
	defer clientMu.Unlock()

	if client == nil {
		return NewError("client not connected")
	}

	content, err := client.GetDirectoryContent(C.GoString(path))
	if err != nil {
		return NewErrorFromErr(err)
	}

	return NewSuccess(content)
}

//export GetFileContent
func GetFileContent(path *C.char) *C.Reply {
	clientMu.Lock()
	defer clientMu.Unlock()

	if client == nil {
		return NewError("client not connected")
	}

	content, err := client.GetFileContent(C.GoString(path))
	if err != nil {
		return NewErrorFromErr(err)
	}
	return NewSuccess(content)
}

//export FreeReply
func FreeReply(r *C.Reply) {
	Free(r)
}

func main() {}
