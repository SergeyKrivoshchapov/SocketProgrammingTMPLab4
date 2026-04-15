package task2_controller

import (
	"net"
	"sync"
)

var (
	controller   *Controller
	controllerMu sync.Mutex
)

type Controller struct {
	listener net.Listener
	running  bool
	stopChan chan struct{}
	port     string
}

func (c *Controller) Start(port string) error {
	listener, err := net.Listen("tcp", ":"+port)
	if err != nil {
		return err
	}

	c.listener = listener
	c.running = true
	c.stopChan = make(chan struct{})
	c.port = port

	go c.acceptLoop()
	return nil
}

func (c *Controller) acceptLoop() {

}
