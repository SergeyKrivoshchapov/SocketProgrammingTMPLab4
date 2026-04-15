package task1_server

import (
	"os"
	"runtime"
)

func GetLogicalDrives() []string {
	if runtime.GOOS == "windows" {
		var drives []string
		for _, drive := range "ABCDEFGHIJKLMNOPQRSTUVWXYZ" {
			path := string(drive) + ":\\"
			if _, err := os.Stat(path); err == nil {
				drives = append(drives, string(drive)+":")
			}
		}
		return drives
	}
	return []string{"/"}
}
