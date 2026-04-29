package main

/*
#include <stdlib.h>

typedef void (*DataCallback)(char* msg);

static void invokeCallback(DataCallback cb, char* msg) {
	if (cb != NULL) {
		cb(msg);
	}
}
*/
import "C"
import (
	"fmt"
	"strings"
	"sync"
	"time"
	"unsafe"

	"SocketProgrammingTMPLab5/dll/common/tcp"
)

var (
	client   *FileClient
	clientMu sync.Mutex
)

type FileClient struct {
	tcpClient *tcp.Client
	drives    string
	callback  C.DataCallback
	respChan  chan string
	running   bool
}

func ts() string {
	return time.Now().Format("02.01.2006 15:04:05")
}

func (c *FileClient) log(format string, args ...interface{}) {
	if c.callback == nil {
		return
	}
	message := fmt.Sprintf(format, args...)
	cMsg := C.CString(message)
	C.invokeCallback(c.callback, cMsg)
	C.free(unsafe.Pointer(cMsg))
}

func (c *FileClient) SetCallback(cb C.DataCallback) {
	c.callback = cb
}

func (c *FileClient) Connect(address string) error {
	c.tcpClient = &tcp.Client{}
	if err := c.tcpClient.Connect(address); err != nil {
		return err
	}

	c.respChan = make(chan string, 100)
	c.running = true
	go c.readLoop()

	drivesLine := <-c.respChan

	if !strings.HasPrefix(drivesLine, "DRIVES:") {
		return fmt.Errorf("Error, Recieved %s", drivesLine)
	}
	c.drives = strings.TrimPrefix(drivesLine, "DRIVES:")

	formattedDrives := strings.ReplaceAll(c.drives, ",", ":\\")
	if formattedDrives != "" {
		formattedDrives += ":\\"
	}

	c.log("Клиент получил %s\n%s", ts(), formattedDrives)
	return nil
}

func (c *FileClient) readLoop() {
	for c.running {
		line, err := c.tcpClient.Receive()
		if err != nil {
			if c.running {
				c.log("Соединение разорвано: %v", err)
			}
			go c.Disconnect()
			return
		}
		if strings.HasPrefix(line, "PUSH:") {
			c.log("Клиент получил %s\n%s", ts(), strings.TrimPrefix(line, "PUSH:"))
		} else {
			c.respChan <- line
		}
	}
}

func (c *FileClient) GetDrives() string {
	return c.drives
}

func (c *FileClient) GetDirectoryContent(path string) (string, error) {
	if err := c.tcpClient.Send("LIST_DIR:" + path); err != nil {
		return "", err
	}

	status := <-c.respChan

	if strings.HasPrefix(status, "ERROR:") {
		return "", fmt.Errorf("%s", strings.TrimPrefix(status, "ERROR:"))
	}

	var builder strings.Builder
	var logNames []string
	for {
		line := <-c.respChan
		if line == "END" {
			break
		}
		builder.WriteString(line)
		builder.WriteString("\n")

		parts := strings.SplitN(line, "|", 2)
		if len(parts) == 2 {
			logNames = append(logNames, parts[1])
		}
	}

	c.log("Клиент получил %s\n%s", ts(), strings.Join(logNames, ","))
	return builder.String(), nil
}

func (c *FileClient) GetFileContent(path string) (string, error) {
	if err := c.tcpClient.Send("GET_FILE:" + path); err != nil {
		return "", err
	}

	status := <-c.respChan

	if strings.HasPrefix(status, "ERROR:") {
		return "", fmt.Errorf("%s", strings.TrimPrefix(status, "ERROR:"))
	}

	var builder strings.Builder
	for {
		line := <-c.respChan
		if line == "END" {
			break
		}
		builder.WriteString(line)
		builder.WriteString("\n")
	}

	res := builder.String()
	c.log("Клиент получил %s\n%s", ts(), res)
	return res, nil
}

func (c *FileClient) Disconnect() {
	if c.tcpClient != nil {
		c.running = false
		c.tcpClient.Send("QUIT")
		c.tcpClient.Disconnect()
		c.tcpClient = nil
		c.log("Отключено от сервера")
	}
}
