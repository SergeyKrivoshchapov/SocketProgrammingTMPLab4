package main

/*
#include <stdlib.h>

typedef void (*DataCallback)(char* states);

static void InvokeCallback(DataCallback cb, char* states) {
    if (cb != NULL) {
        cb(states);
    }
}
*/
import "C"

import (
	"bufio"
	"fmt"
	"math/rand"
	"net"
	"os"
	"strconv"
	"strings"
	"sync"
	"time"
	"unsafe"

	"SocketProgrammingTMPLab5/dll/common/tcp"
)

var (
	controller   *UnitController
	controllerMu sync.Mutex
)

type UnitController struct {
	server     *tcp.Server
	unitCount  int
	configPath string
	callback   C.DataCallback
}

func (c *UnitController) Start(port string) error {
	if err := c.loadConfig(); err != nil {
		return err
	}

	c.server = tcp.NewServer(c.handleDispatcher)
	return c.server.Start(port)
}

func (c *UnitController) loadConfig() error {
	data, err := os.ReadFile(c.configPath)
	if err != nil {
		c.unitCount = 10
		return nil
	}

	lines := strings.Split(string(data), "\n")
	for _, line := range lines {
		line = strings.TrimSpace(line)
		if strings.HasPrefix(line, "units=") {
			count, err := strconv.Atoi(strings.TrimPrefix(line, "units="))
			if err == nil && count > 0 {
				c.unitCount = count
				return nil
			}
		}
	}

	c.unitCount = 10
	return nil
}

func (c *UnitController) handleDispatcher(conn net.Conn) {
	defer conn.Close()

	reader := bufio.NewReader(conn)
	writer := bufio.NewWriter(conn)

	writer.WriteString(fmt.Sprintf("COUNT:%d\n", c.unitCount))
	writer.Flush()

	ready, err := reader.ReadString('\n')
	if err != nil {
		return
	}
	if strings.TrimSpace(ready) != "READY" {
		return
	}

	states := make([]int, c.unitCount)

	ticker := time.NewTicker(2 * time.Second)
	defer ticker.Stop()

	for c.server.IsRunning() {
		select {
		case <-ticker.C:
			if !c.sendStates(writer, states) {
				return
			}
			c.sendStatesToCallback(states)
			c.updateStates(states)
		}
	}
}

func (c *UnitController) sendStates(writer *bufio.Writer, states []int) bool {
	strStates := make([]string, len(states))
	for i, s := range states {
		strStates[i] = strconv.Itoa(s)
	}

	msg := strings.Join(strStates, ";") + "\n"
	_, err := writer.WriteString(msg)
	if err != nil {
		return false
	}
	return writer.Flush() == nil
}

func (c *UnitController) sendStatesToCallback(states []int) {
	if c.callback == nil {
		return
	}

	strStates := make([]string, len(states))
	for i, s := range states {
		strStates[i] = strconv.Itoa(s)
	}

	msg := strings.Join(strStates, ";")
	cMsg := C.CString(msg)
	C.InvokeCallback(c.callback, cMsg)
	C.free(unsafe.Pointer(cMsg))
}

func (c *UnitController) updateStates(states []int) {
	for i := range states {
		switch states[i] {
		case 0: // Работает
			if rand.Float64() < 0.2 {
				states[i] = 1 // Авария
			}
		case 1: // Авария
			states[i] = 2 // Ремонт
		case 2: // Ремонт
			if rand.Float64() < 0.5 {
				states[i] = 0 // Работает
			}
		}
	}
}

func (c *UnitController) Stop() {
	if c.server != nil {
		c.server.Stop()
	}
}

func (c *UnitController) GetUnitCount() int {
	return c.unitCount
}

func (c *UnitController) SetCallback(cb C.DataCallback) {
	c.callback = cb
}
