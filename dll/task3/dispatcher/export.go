package main

/*
   #include <stdlib.h>

   typedef void (*StatesCallback)(char* states);
   static void invokeCallback(StatesCallback cb, char* states) {
       if (cb != NULL) {
           cb(states);
       }
   }
*/
import "C"
import "unsafe"

//export ConnectToController
func ConnectToController(address *C.char, callback C.StatesCallback) C.int {
	dispatcherMu.Lock()
	defer dispatcherMu.Unlock()

	if dispatcher != nil {
		return -1
	}

	d := &UnitDispatcher{
		stopChan: make(chan struct{}),
		running:  true,
	}
	d.SetCallback(callback)

	count, err := d.Connect(C.GoString(address))
	if err != nil {
		return -1
	}

	dispatcher = d
	go dispatcher.StartReceiving()

	return C.int(count)
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

//export GetConnectedUnitCount
func GetConnectedUnitCount() C.int {
	dispatcherMu.Lock()
	defer dispatcherMu.Unlock()

	if dispatcher == nil {
		return 0
	}
	return C.int(dispatcher.GetUnitCount())
}

//export FreeMemory
func FreeMemory(ptr *C.char) {
	C.free(unsafe.Pointer(ptr))
}

func main() {}
