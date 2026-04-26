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
	"fmt"
	"math/rand"
	"net"
	"sync"
	"time"

	"SocketProgrammingTMPLab5/dll/common/tcp"
)

var (
	controller   *Controller
	controllerMu sync.Mutex
)

type Controller struct {
	server   *tcp.Server
	callback C.DataCallback
}

func (c *Controller) Start(port string) error {
	c.server = tcp.NewServer(c.handleDispatcher)
	return c.server.Start(port)
}

func (c *Controller) handleDispatcher(conn net.Conn) {
	defer conn.Close()

	ticker := time.NewTicker(1 * time.Second)
	defer ticker.Stop()

	for c.server.IsRunning() {
		<-ticker.C

		temp := rand.Float64() * 100
		press := rand.Float64() * 6

		msg := fmt.Sprintf("%.2f;%.2f\n", temp, press)

		if _, err := conn.Write([]byte(msg)); err != nil {
			return
		}

		if c.callback != nil {
			C.invokeCallback(c.callback, C.double(temp), C.double(press))
		}
	}
}

func (c *Controller) Stop() {
	if c.server != nil {
		c.server.Stop()
	}
}

func (c *Controller) SetCallback(cb C.DataCallback) {
	c.callback = cb
}
