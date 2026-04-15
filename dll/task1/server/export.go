package task1_server

import "C"

//export StartServer
func StartServer(port *C.char) C.int {
	serverMu.Lock()
	defer serverMu.Unlock()

	if server != nil {
		return -1
	}

	s := &Server{}
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
