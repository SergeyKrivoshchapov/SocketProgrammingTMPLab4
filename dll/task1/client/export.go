package task1_client

import "C"
import (
	"SocketProgrammingTMPLab5/dll/common/reply"
)

//export ConnectToServer
func ConnectToServer(address *C.char) *C.Reply {
	clientMu.Lock()
	defer clientMu.Unlock()
	if client != nil {
		return reply.NewError("client already connected")
	}
	c := &FileClient{}
	if err := c.Connect(C.GoString(address)); err != nil {
		return reply.NewErrorFromErr(err)
	}

	client = c
	return reply.NewSuccess(c.GetDrives())
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
		return reply.NewError("client not connected")
	}

	content, err := client.GetDirectoryContent(C.GoString(path))
	if err != nil {
		return reply.NewErrorFromErr(err)
	}

	return reply.NewSuccess(content)

}

//export GetFileContent
func GetFileContent(path *C.char) *C.Reply {
	clientMu.Lock()
	defer clientMu.Unlock()

	if client == nil {
		return reply.NewError("client not connected")
	}

	content, err := client.GetFileContent(C.GoString(path))
	if err != nil {
		return reply.NewErrorFromErr(err)
	}
	return reply.NewSuccess(content)
}

//export FreeReply
func FreeReply(r *C.Reply) {
	reply.Free(r)
}
