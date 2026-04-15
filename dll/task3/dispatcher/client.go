package dispatcher

import "C"
import (
	"SocketProgrammingTMPLab5/dll/common/tcp"
	"fmt"
	"strconv"
	"strings"
	"sync"
	"unsafe"
)

var (
	dispatcher   *UnitDispatcher
	dispatcherMu sync.Mutex
)

type UnitDispatcher struct {
	tcpClient *tcp.Client
	callback  C.StatesCallback
	unitCount int
	running   bool
	stopChan  chan struct{}
}

func (d *UnitDispatcher) Connect(address string) (int, error) {
	d.tcpClient = &tcp.Client{}
	if err := d.tcpClient.Connect(address); err != nil {
		return 0, err
	}

	countLine, err := d.tcpClient.Receive()
	if err != nil {
		return 0, fmt.Errorf("ошибка получения COUNT: %w", err)
	}

	if !strings.HasPrefix(countLine, "COUNT:") {
		return 0, fmt.Errorf("ожидался COUNT, получено: %s", countLine)
	}

	count, err := strconv.Atoi(strings.TrimPrefix(countLine, "COUNT:"))
	if err != nil {
		return 0, fmt.Errorf("неверный формат COUNT: %w", err)
	}

	d.unitCount = count

	if err := d.tcpClient.Send("READY"); err != nil {
		return 0, fmt.Errorf("ошибка отправки READY: %w", err)
	}

	return count, nil
}

func (d *UnitDispatcher) StartReceiving() {
	for d.running {
		select {
		case <-d.stopChan:
			return
		default:
			line, err := d.tcpClient.Receive()
			if err != nil {
				d.running = false
				return
			}

			if d.callback != nil {
				cStates := C.CString(line)
				C.invokeCallback(d.callback, cStates)
				C.free(unsafe.Pointer(cStates))
			}
		}
	}
}

func (d *UnitDispatcher) SetCallback(cb C.StatesCallback) {
	d.callback = cb
}

func (d *UnitDispatcher) GetUnitCount() int {
	return d.unitCount
}

func (d *UnitDispatcher) Disconnect() {
	d.running = false
	if d.stopChan != nil {
		close(d.stopChan)
	}
	if d.tcpClient != nil {
		d.tcpClient.Disconnect()
		d.tcpClient = nil
	}
}
