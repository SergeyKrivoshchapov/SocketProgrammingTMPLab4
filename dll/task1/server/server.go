package main

import (
	"bufio"
	"fmt"
	"net"
	"os"
	"strings"
	"sync"
)

var (
	server   *Server
	serverMu sync.Mutex
)

type Server struct {
	listener net.Listener
	running  bool
	stopChan chan struct{}
}

func (s *Server) Start(port string) error {
	listener, err := net.Listen("tcp", ":"+port)
	if err != nil {
		return err
	}

	s.listener = listener
	s.running = true
	s.stopChan = make(chan struct{})

	go s.acceptLoop()
	return nil
}

func (s *Server) acceptLoop() {
	for s.running {
		conn, err := s.listener.Accept()
		if err != nil {
			continue
		}
		go s.handleClient(conn)
	}
}

func (s *Server) handleClient(conn net.Conn) {
	defer conn.Close()

	reader := bufio.NewReader(conn)
	writer := bufio.NewWriter(conn)

	drives := getLogicalDrives()
	writer.WriteString("DRIVES:" + strings.Join(drives, ",") + "\n")
	writer.Flush()

	for {
		line, err := reader.ReadString('\n')
		if err != nil {
			return
		}

		line = strings.TrimSpace(line)

		if line == "QUIT" {
			return
		}

		if strings.HasPrefix(line, "LIST_DIR:") {
			path := strings.TrimPrefix(line, "LIST_DIR:")
			handleListDir(writer, path)
		}

		if strings.HasPrefix(line, "GET_FILE:") {
			path := strings.TrimPrefix(line, "GET_FILE:")
			handleGetFile(writer, path)
		}
	}
}

func handleListDir(writer *bufio.Writer, path string) {
	entries, err := os.ReadDir(path)
	if err != nil {
		writer.WriteString("ERROR:" + err.Error() + "\n")
		writer.Flush()
		return
	}

	writer.WriteString("OK\n")

	for _, entry := range entries {
		entryType := "F"
		if entry.IsDir() {
			entryType = "D"
		}
		writer.WriteString(fmt.Sprintf("%s|%s\n", entryType, entry.Name()))
	}

	writer.WriteString("END\n")
	writer.Flush()
}

func handleGetFile(writer *bufio.Writer, path string) {
	data, err := os.ReadFile(path)
	if err != nil {
		writer.WriteString("ERROR:" + err.Error() + "\n")
		writer.Flush()
		return
	}

	writer.WriteString("OK\n")
	writer.Write(data)
	writer.WriteString("\nEOF\n")
	writer.Flush()
}

func getLogicalDrives() []string {
	var drives []string
	for _, d := range "ABCDEFGHIJKLMNOPQRSTUVWXYZ" {
		path := string(d) + ":\\"
		if _, err := os.Stat(path); err == nil {
			drives = append(drives, string(d)+":")
		}
	}
	return drives
}

func (s *Server) Stop() {
	s.running = false
	if s.listener != nil {
		s.listener.Close()
	}
}
