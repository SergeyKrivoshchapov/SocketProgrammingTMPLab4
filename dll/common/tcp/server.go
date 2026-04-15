package tcp

import (
	"net"
	"sync"
)

type Server struct {
	listener net.Listener
	running  bool
	stopChan chan struct{}
	mu       sync.RWMutex
	handler  func(net.Conn)
}

func NewServer(handler func(net.Conn)) *Server {
	return &Server{
		handler:  handler,
		stopChan: make(chan struct{}),
	}
}

func (s *Server) Start(port string) error {
	listener, err := net.Listen("tcp", ":"+port)
	if err != nil {
		return err
	}

	s.mu.Lock()
	s.listener = listener
	s.running = true
	s.mu.Unlock()

	go s.acceptLoop()
	return nil
}

func (s *Server) acceptLoop() {
	for s.IsRunning() {
		conn, err := s.listener.Accept()
		if err != nil {
			if s.IsRunning() {
				continue
			}
			return
		}
		go s.handler(conn)
	}
}

func (s *Server) Stop() {
	s.running = false
	if s.listener != nil {
		s.listener.Close()
	}
}

func (s *Server) IsRunning() bool {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.running
}
