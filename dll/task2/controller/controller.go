package main

/*
#include <stdlib.h>
*/
import "C"
import (
	"SocketProgrammingTMPLab5/dll/common/tcp"
	"fmt"
	"math/rand"
	"net"
	"sync"
	"time"
)

var (
	controller   *Controller
	controllerMu sync.Mutex
)

type Controller struct {
	server *tcp.Server
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
	}
}

func (c *Controller) Stop() {
	if c.server != nil {
		c.server.Stop()
	}
}
