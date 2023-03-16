package handler

import (
	"context"
	"encoding/json"
	"net/http"

	"github.com/tockn/singo/model"
)

type SendClientID struct {
	ClientID string `json:"client_id"`
}

// CreateConnectionでwebsocketコネクションを確立
// Clientを作成してレスポンスとして通知する
// CreateConnection에서 websocket커넥션 확립
// Client를 작성하여 응답으로 통지하다
func (h *Handler) CreateConnection(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		return
	}
	c := model.NewClient("name")
	ctx := context.Background()
	go h.HandleSendMessage(ctx, c, conn)
	go h.HandleReceiveMessage(c, conn)
	resp := &SendClientID{ClientID: c.ID}
	payload, _ := json.Marshal(resp)
	body, _ := json.Marshal(model.Message{
		Type:    model.MessageTypeNotifyClientID,
		Payload: payload, // 이렇게 json.Marshal ( json.Marshal ) 하면 BASE64로 묶어버린다.
	})
	sendMessage(conn, body)
}
