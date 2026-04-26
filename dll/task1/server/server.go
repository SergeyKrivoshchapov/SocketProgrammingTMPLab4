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
	listener net.Listener
	running  bool
	stopChan chan struct{}
	callback C.DataCallback
}

func (s *Server) log(format string, args ...interface{}) {
	if s.callback == nil {
		return
	}

	timestamp := time.Now().Format("02.01.2006 15:04:05")
	message := fmt.Sprintf(format, args...)
	fullMsg := fmt.Sprintf("%s %s", timestamp, message)

	cMsg := C.CString(fullMsg)
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

	s.log("Сервер включён")

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
		s.log(fmt.Sprintf("Клиент соединился с адреса %s", remoteAddr))
		go s.handleClient(conn)
	}
}

func (s *Server) handleClient(conn net.Conn) {
	defer conn.Close()
	remoteAddr := conn.RemoteAddr().String()
	reader := bufio.NewReader(conn)
	writer := bufio.NewWriter(conn)

	drives := getLogicalDrives()
	writer.WriteString("DRIVES:" + strings.Join(drives, ",") + "\n")
	writer.Flush()
	s.log("Отправлен список дисков клиенту %s", remoteAddr)

	for {
		line, err := reader.ReadString('\n')
		if err != nil {
			s.log("Клиент %s отключился", remoteAddr)
			return
		}

		line = strings.TrimSpace(line)
		s.log("Получена команда от %s: %s", remoteAddr, line)

		if line == "QUIT" {
			s.log("Клиент %s запросил отключение", remoteAddr)
			return
		}

		if strings.HasPrefix(line, "LIST_DIR:") {
			path := strings.TrimPrefix(line, "LIST_DIR:")
			s.log("Запрос списка директории: %s", path)
			handleListDir(writer, path, s, remoteAddr)
		}

		if strings.HasPrefix(line, "GET_FILE:") {
			path := strings.TrimPrefix(line, "GET_FILE:")
			s.log("Запрос файла: %s", path)
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
	writer.WriteString("\nEOF\n")
	writer.Flush()
	s.log(fmt.Sprintf("Файл %s отправлен клиенту", path, clientAddr))
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
