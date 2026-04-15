package tcp

import "C"
import (
	"bufio"
	"net"
	"strings"
)

type Client struct {
	conn   net.Conn
	reader *bufio.Reader
	writer *bufio.Writer
}

func (c *Client) Connect(address string) error {
	conn, err := net.Dial("tcp", address)
	if err != nil {
		return err
	}

	c.conn = conn
	c.reader = bufio.NewReader(conn)
	c.writer = bufio.NewWriter(conn)

	return nil
}

func (c *Client) Disconnect() {
	if c.conn != nil {
		c.conn.Close()
		c.conn = nil
	}
}

func (c *Client) Send(msg string) error {
	_, err := c.writer.WriteString(msg + "\n")
	if err != nil {
		return err
	}

	return c.writer.Flush()
}

func (c *Client) Receive() (string, error) {
	line, err := c.reader.ReadString('\n')
	if err != nil {
		return "", err
	}
	return strings.TrimSpace(line), nil
}

func (c *Client) ReceiveUntil(stopWord string) (string, error) {
	var builder strings.Builder

	for {
		line, err := c.Receive()
		if err != nil {
			return "", err
		}

		if line == stopWord {
			break
		}

		builder.WriteString(line)
		builder.WriteString("\n")
	}

	return builder.String(), nil
}

func (c *Client) IsConnected() bool {
	return c.conn != nil
}
