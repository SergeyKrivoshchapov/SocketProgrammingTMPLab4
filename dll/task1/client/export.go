package task1_client

import "C"
import (
	"fmt"
	"unsafe"
)

/*
typedef struct {
	int status;
	char* msg;
} Reply;
*/

//export ConnectToServer
func ConnectToServer(address *C.char) *C.Reply {
	clientMu.Lock()
	defer clientMu.Unlock()
	if client != nil {
		return newErrorReply(fmt.Errorf("client already connected"))
	}
	c := &Client{}
	if err := c.Connect(C.GoString(address)); err != nil {
		return newErrorReply(err)
	}

	client = c
	return newSuccessReply(c.GetDrives())
}

//export Disconnect
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
		return newErrorReply(fmt.Errorf("client not connected"))
	}

	content, err := client.GetDirectoryContent(C.GoString(path))
	if err != nil {
		return newErrorReply(err)
	}

	return newSuccessReply(content)

}

//export GetFileContent
func GetFileContent(path *C.char) *C.Reply {
	clientMu.Lock()
	defer clientMu.Unlock()

	if client == nil {
		return newErrorReply(fmt.Errorf("client not connected"))
	}

	content, err := client.GetFileContent(C.GoString(path))
	if err != nil {
		return newErrorReply(err)
	}
	return newSuccessReply(content)
}

//export FreeReply
func FreeReply(reply *C.Reply) {
	if reply != nil {
		C.free(unsafe.Pointer(reply.msg))
		C.free(unsafe.Pointer(reply))
	}
}
