package task1_client

import "C"
import (
	"bufio"
	"fmt"
	"net"
	"strings"
	"sync"
	"unsafe"
)

/*
#include <stdlib.h>
typedef struct {
	int status;
	char* msg;
} Reply;
*/

var (
	client   *Client
	clientMu sync.Mutex
)

type Client struct {
	conn   net.Conn
	reader *bufio.Reader
	writer *bufio.Writer
	drives string
}

func NewClient() *Client {
	return &Client{}
}

func (c *Client) Connect(address string) error {
	conn, err := net.Dial("tcp", address)
	if err != nil {
		return err
	}
	c.conn = conn
	c.reader = bufio.NewReader(conn)
	c.writer = bufio.NewWriter(conn)

	drivesLine, err := c.reader.ReadString('\n')
	if err != nil {
		return fmt.Errorf("Error reading drives: %w", err)
	}

	drivesLine = strings.TrimSpace(drivesLine)
	if !strings.HasPrefix(drivesLine, "DRIVES: ") {
		return fmt.Errorf("Error, Recieved %d", drivesLine)
	}
	c.drives = strings.TrimPrefix(drivesLine, "DRIVES: ")
	return nil
}

func (c *Client) Disconnect() {
	if c.conn != nil {
		c.writer.WriteString("QUIT\n")
		c.writer.Flush()
		c.conn.Close()
		c.conn = nil
	}
}

func (c *Client) GetDrives() string {
	return c.drives
}

func (c *Client) GetDirectoryContent(path string) (string, error) {
	if c.writer == nil {
		return "", fmt.Errorf("Not connected to server")
	}

	_, err := c.writer.WriteString("LIST_DIR:" + path + "\n")
	if err != nil {
		return "", err
	}
	c.writer.Flush()

	statusLine, err := c.reader.ReadString('\n')
	if err != nil {
		return "", err
	}
	statusLine = strings.TrimSpace(statusLine)

	if strings.HasPrefix(statusLine, "ERROR") {
		return "", fmt.Errorf(strings.TrimPrefix(statusLine, "ERROR:"))
	}

	var builder strings.Builder
	for {
		line, err := c.reader.ReadString('\n')
		if err != nil {
			return "", err
		}

		line = strings.TrimSpace(line)
		if line == "END" {
			break
		}
		builder.WriteString(line)
		builder.WriteString("\n")
	}

	return builder.String(), nil
}

func (c *Client) GetFileContent(path string) (string, error) {
	if c.writer == nil {
		return "", fmt.Errorf("Not connected to server")
	}
	_, err := c.writer.WriteString("GET_FILE:" + path + "\n")
	if err != nil {
		return "", err
	}

	c.writer.Flush()

	statusLine, err := c.reader.ReadString('\n')
	if err != nil {
		return "", err
	}
	statusLine = strings.TrimSpace(statusLine)
	if strings.HasPrefix(statusLine, "ERROR:") {
		return "", fmt.Errorf(strings.TrimPrefix(statusLine, "ERROR:"))
	}

	var builder strings.Builder
	for {
		line, err := c.reader.ReadString('\n')
		if err != nil {
			return "", err
		}

		line = strings.TrimSpace(line)
		if line == "EOF" {
			break
		}
		builder.WriteString(line)
		builder.WriteString("\n")
	}

	return builder.String(), nil
}

func newReply(status int, msg string) *C.Reply {
	reply := (*C.Reply)(C.malloc(C.size_t(unsafe.Sizeof(C.Reply{}))))
	reply.status = C.int(status)
	reply.msg = C.CString(msg)
	return reply
}

func newErrorReply(err error) *C.Reply {
	return newReply(-1, err.Error())
}

func newSuccessReply(data string) *C.Reply {
	return newReply(0, data)
}
