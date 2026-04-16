package main

import "C"
import (
	"math/rand"
	"time"
)

//export StartController
func StartController(port *C.char) C.int {
	controllerMu.Lock()
	defer controllerMu.Unlock()

	if controller != nil {
		return -1
	}

	rand.Seed(time.Now().UnixNano())

	c := &Controller{}
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
