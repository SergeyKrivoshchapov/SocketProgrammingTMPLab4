package main

import "C"
import (
	"bufio"
	"fmt"
	"os"
	"strings"
	"unsafe"
)

func main() {
	address := "localhost:8081"
	if len(os.Args) > 1 {
		address = os.Args[1]
	}

	cAddr := C.CString(address)
	defer C.free(unsafe.Pointer(cAddr))
	reply := C.ConnectToServer(cAddr)
	defer C.FreeReply(reply)

	if reply.status != 0 {
		fmt.Printf("Connection error %s.\n", C.GoString(reply.msg))
		return
	}

	fmt.Printf("File client connnected to address %s.\n", address)
	fmt.Printf("Disks: %s\n", C.GoString(reply.msg))

	console := bufio.NewReader(os.Stdin)
	printMenu()

	for {
		fmt.Print("> ")
		input, _ := console.ReadString('\n')
		input = strings.TrimSpace(input)

		switch input {
		case "2":
			fmt.Print("Enter directory path: ")
			path, _ := console.ReadString('\n')
			path = strings.TrimSpace(path)

			cPath := C.CString(path)
			reply := C.GetDirectoryContent(cPath)
			C.free(unsafe.Pointer(cPath))

			if reply.status != 0 {
				fmt.Printf("Error: %s\n", C.GoString(reply.msg))
			} else {
				fmt.Printf("Directory content:")
				fmt.Printf(C.GoString(reply.msg))
			}
			C.FreeReply(reply)

		case "3":
			fmt.Print("Enter file name: ")
			path, _ := console.ReadString('\n')
			path = strings.TrimSpace(path)

			cPath := C.CString(path)
			reply := C.GetFileContent(cPath)
			C.free(unsafe.Pointer(cPath))

			if reply.status != 0 {
				fmt.Printf("Error: %s\n", C.GoString(reply.msg))
			} else {
				fmt.Printf("File content:")
				fmt.Printf(C.GoString(reply.msg))
			}
			C.FreeReply(reply)

		case "4":
			C.DisconnectFromServer()
			fmt.Println("Connection disconnected.")
			return

		default:
			printMenu()
		}
		fmt.Println()
	}
}

func printMenu() {
	fmt.Println("Menu: ")
	fmt.Println("2 - directory list")
	fmt.Println("3 - file")
	fmt.Println("4 - exit")
}
