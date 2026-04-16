package main

import "C"
import (
	"SocketProgrammingTMPLab5/dll/common/tcp"
	"fmt"
	"strings"
	"sync"
)

var (
	client   *FileClient
	clientMu sync.Mutex
)

type FileClient struct {
	tcpClient *tcp.Client
	drives    string
}

func (c *FileClient) Connect(address string) error {
	c.tcpClient = &tcp.Client{}
	if err := c.tcpClient.Connect(address); err != nil {
		return err
	}

	drivesLine, err := c.tcpClient.Receive()
	if err != nil {
		return fmt.Errorf("Error reading drives: %w", err)
	}

	if !strings.HasPrefix(drivesLine, "DRIVES: ") {
		return fmt.Errorf("Error, Recieved %s", drivesLine)
	}
	c.drives = strings.TrimPrefix(drivesLine, "DRIVES: ")
	return nil
}

func (c *FileClient) GetDrives() string {
	return c.drives
}

func (c *FileClient) GetDirectoryContent(path string) (string, error) {
	if err := c.tcpClient.Send("LIST_DIR:" + path); err != nil {
		return "", err
	}

	status, err := c.tcpClient.Receive()
	if err != nil {
		return "", err
	}

	if strings.HasPrefix(status, "ERROR:") {
		return "", fmt.Errorf(strings.TrimPrefix(status, "ERROR:"))
	}

	return c.tcpClient.ReceiveUntil("END")
}

func (c *FileClient) GetFileContent(path string) (string, error) {
	if err := c.tcpClient.Send("GET_FILE:" + path); err != nil {
		return "", err
	}

	status, err := c.tcpClient.Receive()
	if err != nil {
		return "", err
	}

	if strings.HasPrefix(status, "ERROR:") {
		return "", fmt.Errorf(strings.TrimPrefix(status, "ERROR:"))
	}

	return c.tcpClient.ReceiveUntil("END")
}

func (c *FileClient) Disconnect() {
	if c.tcpClient != nil {
		c.tcpClient.Send("QUIT")
		c.tcpClient.Disconnect()
		c.tcpClient = nil
	}
}
