package main

/*
#include <stdlib.h>

typedef void (*DataCallback)(char* states);

static void InvokeCallback(DataCallback cb, char* states){
	if (cb != NULL) {
		cb(states);
	}
}
*/
import "C"

import (
	"math/rand"
	"time"
)

//export StartController
func StartController(port *C.char, configPath *C.char, callback C.DataCallback) C.int {
	controllerMu.Lock()
	defer controllerMu.Unlock()

	if controller != nil {
		return -1
	}

	rand.Seed(time.Now().UnixNano())

	c := &UnitController{
		configPath: C.GoString(configPath),
	}

	c.SetCallback(callback)

	if err := c.Start(C.GoString(port)); err != nil {
		return -1
	}

	controller = c
	return 0
}

//export StopController
func StopController() {
	controllerMu.Lock()
	defer controllerMu.Unlock()

	if controller != nil {
		controller.Stop()
		controller = nil
	}
}

//export GetUnitCount
func GetUnitCount() C.int {
	controllerMu.Lock()
	defer controllerMu.Unlock()

	if controller == nil {
		return 0
	}
	return C.int(controller.GetUnitCount())
}

//export IsControllerRunning
func IsControllerRunning() C.int {
	controllerMu.Lock()
	defer controllerMu.Unlock()

	if controller != nil && controller.server != nil && controller.server.IsRunning() {
		return 1
	}
	return 0
}

func main() {}
