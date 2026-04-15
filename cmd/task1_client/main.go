package main

import (
	"bufio"
	"fmt"
	"log"
	"net"
	"os"
	"strings"
)

func main() {
	conn, err := net.Dial("tcp", "localhost:8080")
	if err != nil {
		log.Fatal(err)
	}

	defer conn.Close()
	fmt.Println("Connected to port 8080")

	reader := bufio.NewReader(conn)
	writer := bufio.NewWriter(conn)
	console := bufio.NewReader(os.Stdin)

	for {
		fmt.Print("> ")
		input, _ := console.ReadString('\n')
		input = strings.TrimSpace(input)
		writer.WriteString(input + "\n")
		writer.Flush()

		if input == "QUIT" {
			break
		}
		response, _ := reader.ReadString('\n')
		fmt.Print("Server: " + response)
	}
}
