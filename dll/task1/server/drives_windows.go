//go:build windows

package main

import (
	"fmt"
	"syscall"
)

var (
	kernel32             = syscall.NewLazyDLL("kernel32.dll")
	procGetLogicalDrives = kernel32.NewProc("GetLogicalDrives")
)

func getLogicalDrives() []string {
	maskRaw, _, _ := procGetLogicalDrives.Call()
	mask := uint32(maskRaw)

	drives := make([]string, 0, 26)
	for i := 0; i < 26; i++ {
		if mask&(1<<uint(i)) != 0 {
			drives = append(drives, fmt.Sprintf("%c:", 'A'+i))
		}
	}

	return drives
}
