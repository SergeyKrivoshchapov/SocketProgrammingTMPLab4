//go:build !windows

package main

import (
	"os"
)

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
