package task1

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"strings"
)

const (
	RespOk     = "OK"
	RespError  = "ERROR"
	CmdListDir = "LIST_DIR"
	CmdGetFile = "GET_FILE"
	CmdQuit    = "QUIT"

	Delimiter = '\n'
)

type Message struct {
	Command string
	Payload string
}

func (m *Message) Encode() string {
	if m.Payload == "" {
		return m.Command + "\n"
	}
	return m.Command + ":" + m.Payload + "\n"
}

func GetDirectoryContent(path string) ([]string, error) {
	info, err := os.Stat(path)
	if err != nil {
		return nil, fmt.Errorf("path does not exist: %w", err)
	}
	if !info.IsDir() {
		return nil, fmt.Errorf("path is not a directory")
	}
	entries, err := os.ReadDir(path)
	if err != nil {
		return nil, fmt.Errorf("error reading directory: %w", err)
	}

	var result []string
	for _, entry := range entries {
		entryType := "F"
		if entry.IsDir() {
			entryType = "D"
		}

		size := int64(0)
		if !entry.IsDir() {
			if fileInfo, err := entry.Info(); err == nil {
				size = fileInfo.Size()
			}
		}
		result = append(result, fmt.Sprintf("%s|%s|%d", entryType, entry.Name(), size))
	}
	return result, nil
}

func GetFileContent(path string) (string, error) {
	info, err := os.Stat(path)
	if err != nil {
		return "", fmt.Errorf("file does not exist: %w", err)
	}

	if info.IsDir() {
		return "", fmt.Errorf("path is a directory")
	}

	if info.Size() > 10*1024*1024 {
		return "", fmt.Errorf("file is too large")
	}

	content, err := os.ReadFile(path)
	if err != nil {
		return "", fmt.Errorf("error reading file: %w", err)
	}

	if !isTextFile(content) {
		return "", fmt.Errorf("file is not a text file")
	}

	return string(content), nil
}

func isTextFile(content []byte) bool {
	if len(content) == 0 {
		return true
	}

	checkLen := len(content)
	if checkLen > 512 {
		checkLen = 512
	}

	for i := 0; i < checkLen; i++ {
		if content[i] == 0 {
			return false
		}
	}
	return true

}

func SendResponse(w *bufio.Writer, status, message string) error {
	_, err := w.WriteString(fmt.Sprintf("%s%s\n", status, message))
	return err
}

func SendData(w *bufio.Writer, data string) error {
	_, err := w.WriteString(data + "\n")
	return err
}

func ReadMessage(r *bufio.Reader) (*Message, error) {
	line, err := r.ReadString(Delimiter)
	if err != nil {
		if err == io.EOF {
			return nil, err
		}
		return nil, fmt.Errorf("error reading message: %w", err)
	}
	return DecodeMessage(line), nil
}
