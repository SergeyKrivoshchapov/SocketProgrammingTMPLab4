package main

/*
#cgo LDFLAGS: -L../../bin -ltask1_client
#include <stdlib.h>
#include "../../bin/task1_client.h"
*/
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
        fmt.Printf("Error: %s\n", C.GoString(reply.msg))
        return
    }

    fmt.Printf("=== File Client ===\n")
    fmt.Printf("Connected to: %s\n", address)
    fmt.Printf("Drives: %s\n\n", C.GoString(reply.msg))

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
                fmt.Println("\nDirectory content:")
                fmt.Println(strings.Repeat("-", 50))
                fmt.Print(C.GoString(reply.msg))
                fmt.Println(strings.Repeat("-", 50))
            }
            C.FreeReply(reply)

        case "3":
            fmt.Print("Enter file path: ")
            path, _ := console.ReadString('\n')
            path = strings.TrimSpace(path)

            cPath := C.CString(path)
            reply := C.GetFileContent(cPath)
            C.free(unsafe.Pointer(cPath))

            if reply.status != 0 {
                fmt.Printf("Error: %s\n", C.GoString(reply.msg))
            } else {
                fmt.Println("\nFile content:")
                fmt.Println(strings.Repeat("-", 50))
                fmt.Print(C.GoString(reply.msg))
                fmt.Println(strings.Repeat("-", 50))
            }
            C.FreeReply(reply)

        case "4", "quit", "exit":
            C.DisconnectFromServer()
            fmt.Println("Disconnecting...")
            return

        default:
            printMenu()
        }
        fmt.Println()
    }
}

func printMenu() {
    fmt.Println("Menu:")
    fmt.Println("  2 - List directory")
    fmt.Println("  3 - View file")
    fmt.Println("  4 - Exit")
}