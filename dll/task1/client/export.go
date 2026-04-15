package client

import "C"

//export ConnectToServer
func ConnectToServer(address *C.char) *C.char {

}

//export Disconnect
func DisconnectFromServer() {

}

//export GetDirectoryContent
func GetDirectoryContent(path *C.char) *C.char {

}

//export GetFileContent
func GetFileContent(path *C.char) *C.char {

}
