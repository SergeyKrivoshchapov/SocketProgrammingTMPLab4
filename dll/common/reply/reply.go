package reply

/*
#include <stdlib.h>

typedef struct {
	int status;
	char* msg;
} Reply;
*/
import "C"
import "unsafe"

func NewSuccess(msg string) *C.Reply {
	reply := (*C.Reply)(C.malloc(C.size_t(unsafe.Sizeof(C.Reply{}))))
	reply.status = C.int(0)
	reply.msg = C.CString(msg)
	return reply
}

func NewError(err string) *C.Reply {
	reply := (*C.Reply)(C.malloc(C.size_t(unsafe.Sizeof(C.Reply{}))))
	reply.status = C.int(-1)
	reply.msg = C.CString(err)
	return reply
}

func NewErrorFromErr(err error) *C.Reply {
	if err == nil {
		return NewSuccess("")
	}
	return NewError(err.Error())
}

func Free(reply *C.Reply) {
	if reply != nil {
		C.free(unsafe.Pointer(reply.msg))
		C.free(unsafe.Pointer(reply))
	}
}
