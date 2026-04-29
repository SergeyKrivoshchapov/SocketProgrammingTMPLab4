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
	"bufio"
	"fmt"
	"net"
	"os"
	"strings"
	"sync"
	"time"
	"unsafe"
)

var (
	server   *Server
	serverMu sync.Mutex
)

type Server struct {
	listener  net.Listener
	running   bool
	stopChan  chan struct{}
	callback  C.DataCallback
	clients   map[net.Conn]*bufio.Writer
	clientsMu sync.Mutex
}

func ts() string {
	return time.Now().Format("02.01.2006 15:04:05")
}

func (s *Server) log(format string, args ...interface{}) {
	if s.callback == nil {
		return
	}

	message := fmt.Sprintf(format, args...)

	cMsg := C.CString(message)
	C.invokeCallback(s.callback, cMsg)
	C.free(unsafe.Pointer(cMsg))
}

func (s *Server) Start(port string) error {
	listener, err := net.Listen("tcp", ":"+port)
	if err != nil {
		return err
	}

	s.listener = listener
	s.running = true
	s.stopChan = make(chan struct{})
	s.clients = make(map[net.Conn]*bufio.Writer)

	s.log("Сервер включён %s", ts())

	go s.acceptLoop()
	return nil
}

func (s *Server) acceptLoop() {
	for s.running {
		conn, err := s.listener.Accept()
		if err != nil {
			if s.running {
				s.log("Ошибка при приёме соединения: %v", err)
				continue
			}
			return
		}

		remoteAddr := conn.RemoteAddr().String()
		s.log("Клиент соединился %s с адреса %s", ts(), remoteAddr)
		go s.handleClient(conn)
	}
}

func (s *Server) handleClient(conn net.Conn) {
	remoteAddr := conn.RemoteAddr().String()
	writer := bufio.NewWriter(conn)

	s.clientsMu.Lock()
	s.clients[conn] = writer
	s.clientsMu.Unlock()

	defer func() {
		s.clientsMu.Lock()
		delete(s.clients, conn)
		s.clientsMu.Unlock()
		conn.Close()
	}()

	reader := bufio.NewReader(conn)

	drives := getLogicalDrives()
	writer.WriteString("DRIVES:" + strings.Join(drives, ",") + "\n")
	writer.Flush()

	for {
		line, err := reader.ReadString('\n')
		if err != nil {
			s.log("Клиент %s отключился", remoteAddr)
			return
		}

		line = strings.TrimSpace(line)

		if line == "QUIT" {
			s.log("Клиент %s отключился", remoteAddr)
			return
		}

		if strings.HasPrefix(line, "LIST_DIR:") {
			path := strings.TrimPrefix(line, "LIST_DIR:")
			s.log("Сервер получил %s %s", ts(), path)
			handleListDir(writer, path, s, remoteAddr)
		} else if strings.HasPrefix(line, "GET_FILE:") {
			path := strings.TrimPrefix(line, "GET_FILE:")
			s.log("Сервер получил %s %s", ts(), path)
			handleGetFile(writer, path, s, remoteAddr)
		}
	}
}

func handleListDir(writer *bufio.Writer, path string, s *Server, clientAddr string) {
	entries, err := os.ReadDir(path)
	if err != nil {
		errMsg := fmt.Sprintf("ERROR:%s\n", err.Error())
		writer.WriteString(errMsg)
		writer.Flush()
		s.log("Ошибка чтения директории %s: %v", path, err)
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
	s.log("Отправлено содержимое директории %s клиенту с адресом %s", path, clientAddr)
}

func handleGetFile(writer *bufio.Writer, path string, s *Server, clientAddr string) {
	data, err := os.ReadFile(path)
	if err != nil {
		errMsg := fmt.Sprintf("ERROR:%s\n", err.Error())
		writer.WriteString(errMsg)
		writer.Flush()
		s.log("Ошибка чтения файла %s, %v", path, err)
		return
	}

	writer.WriteString("OK\n")
	writer.Write(data)
	writer.WriteString("\nEND\n")
	writer.Flush()
}

func (s *Server) Stop() {
	if s.running {
		s.log("Сервер остановлен")
	}
	s.running = false
	if s.listener != nil {
		s.listener.Close()
	}
}

func (s *Server) SetCallback(cb C.DataCallback) {
	s.callback = cb
}

func (s *Server) TransferToClient() {
	drives := getLogicalDrives()
	formattedDrives := strings.Join(drives, "")
	formattedDrives = strings.ReplaceAll(formattedDrives, ":", ":\\")
	msg := "PUSH:" + formattedDrives + "\n"

	s.clientsMu.Lock()
	for _, writer := range s.clients {
		writer.WriteString(msg)
		writer.Flush()
	}
	s.clientsMu.Unlock()
}
