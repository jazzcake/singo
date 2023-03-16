package handler

import (
	"net/http"

	"github.com/tockn/singo/manager"

	"github.com/gorilla/websocket"
)

type Handler struct {
	manager *manager.RoomMgr
}

func NewHandler(man *manager.RoomMgr) *Handler {
	return &Handler{manager: man}
}

var upgrader = websocket.Upgrader{
	ReadBufferSize:  1024,
	WriteBufferSize: 1024,
	CheckOrigin: func(r *http.Request) bool {
		return true
	},
}
