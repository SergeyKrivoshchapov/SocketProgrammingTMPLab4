package task2_dispatcher

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
	"SocketProgrammingTMPLab5/dll/common/tcp"
	"strconv"
	"strings"
	"sync"
)

var (
	dispatcher   *Dispatcher
	dispatcherMu sync.Mutex
)

type Dispatcher struct {
	tcpClient *tcp.Client
	callback  C.DataCallback
	running   bool
	stopChan  chan struct{}
}

func (d *Dispatcher) Connect(address string) error {
	d.tcpClient = &tcp.Client{}
	return d.tcpClient.Connect(address)
}

func (d *Dispatcher) StartReceiving() {
	for d.running {
		select {
		case <-d.stopChan:
			return
		default:
			line, err := d.tcpClient.Receive()
			if err != nil {
				d.running = false
				return
			}

			parts := strings.Split(line, ";")
			if len(parts) != 2 {
				continue
			}

			temp, err1 := strconv.ParseFloat(parts[0], 64)
			press, err2 := strconv.ParseFloat(parts[1], 64)

			if err1 == nil && err2 == nil && d.callback != nil {
				C.invokeCallback(d.callback, C.doubke(temp), C.double(press))
			}
		}
	}
}

func (d *Dispatcher) SetCallback(cb C.DataCallback) {
	d.callback = cb
}

func (d *Dispatcher) Disconnect() {
	d.running = false
	if d.stopChan != nil {
		close(d.stopChan)
	}
	if d.tcpClient != nil {
		d.tcpClient.Disconnect()
		d.tcpClient = nil
	}
}
