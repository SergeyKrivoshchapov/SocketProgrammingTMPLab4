package task2_dispatcher

import "C"
import "unsafe"

//export ConnectToController
func ConnectToController(address *C.char, callback C.DataCallback) C.int {
	dispatcherMu.Lock()
	defer dispatcherMu.Unlock()

	if dispatcher != nil {
		return -1
	}

	d := &Dispatcher{
		stopChan: make(chan struct{}),
		running:  true,
	}
	d.SetCallback(callback)

	if err := d.Connect(C.GoString(address)); err != nil {
		return -1
	}

	dispatcher = d
	go dispatcher.StartReceiving()

	return 0
}

//export DisconnectFromController
func DisconnectFromController() {
	dispatcherMu.Lock()
	defer dispatcherMu.Unlock()

	if dispatcher != nil {
		dispatcher.Disconnect()
		dispatcher = nil
	}
}

//export FreeMemory
func FreeMemory(ptr *C.char) {
	C.free(unsafe.Pointer(ptr))
}
