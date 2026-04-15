package main

import (
	"bufio"
	"fmt"
	"net"
	"strings"
)

func main() {
	listener, err := net.Listen("tcp", ":8080")
	if err != nil {
	}
	defer listener.Close()

	fmt.Println("Listening on :8080")

	connection, err := listener.Accept()
	if err != nil {

	}
	defer connection.Close()
	fmt.Printf("Client connected: %s\n", connection.RemoteAddr())
	reader := bufio.NewReader(connection)
	writer := bufio.NewWriter(connection)

	for {
		msg, err := reader.ReadString('\n')
		if err != nil {
			fmt.Println("Client disconnected")
			break
		}
		msg = strings.TrimSpace(msg)
		fmt.Printf("Recieved: %s\n", msg)

		if msg == "QUIT" {
			break
		}

		writer.WriteString("ECHO: " + msg + "\n")
		writer.Flush()
	}
}
