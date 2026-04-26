package main

/*
#include <stdlib.h>

typedef void (*DataCallback)(double temperature, double pressure);

static void invokeCallback(DataCallback cb, double temp, double press) {
	if (cb != NULL) {
		cb(temp, press);
	}
}
*/
import "C"

import (
	"math/rand"
	"time"
)

//export StartController
func StartController(port *C.char, callback C.DataCallback) C.int {
	controllerMu.Lock()
	defer controllerMu.Unlock()

	if controller != nil {
		return -1
	}

	rand.Seed(time.Now().UnixNano())

	c := &Controller{}
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

func main() {}
