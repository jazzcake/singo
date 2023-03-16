package handler

import (
	"encoding/json"

	"github.com/gorilla/websocket"
	"github.com/tockn/singo/model"
)

// HandleMessageはWebsocketで来たメッセージのTypeを元に適切なHandle関数を実行する
// HandleMessage는 Websocket에서 온 메시지의 Type을 바탕으로 적절한 Handle 함수를 실행한다.
func (h *Handler) HandleReceiveMessage(c *model.Client, conn *websocket.Conn) {
	defer func() {
		_ = conn.Close()
	}()
	for {
		_, msg, err := conn.ReadMessage()
		if err != nil {
			return
		}
		// Unmarshalして適切なmethod実行
		// Unmarshal하여 적절한 method 실행
		var req ReceiveMessage
		if err := json.Unmarshal(msg, &req); err != nil {
			// send error message
			continue
		}

		var resp *model.Message
		switch req.Type {
		case ReceiveMessageTypeJoinRoom:
			resp = h.handleJoinRoom(c, req.Payload)
		case ReceiveMessageTypeOffer:
			resp = h.handleSDPOffer(c, req.Payload)
		case ReceiveMessageTypeAnswer:
			resp = h.handleSDPAnswer(c, req.Payload)
		case ReceiveMessageTypeIceCandidate:
			resp = h.handleIceCandidate(c, req.Payload)
		default:
			// send bad request
			// invalid type
			resp = newErrorMessage(ErrMsgInvalidType)
		}

		if resp == nil {
			continue
		}
		respMsg, err := json.Marshal(resp)
		if err != nil {
			continue
		}
		if err := sendMessage(conn, respMsg); err != nil {
			continue
		}
	}
}

type MessageJoinRoom struct {
	RoomID string `json:"room_id"`
}

// room_id를 받아서 join한다.
func (h *Handler) handleJoinRoom(c *model.Client, msg []byte) *model.Message {
	var req MessageJoinRoom
	if err := json.Unmarshal(msg, &req); err != nil {
		return newErrorMessage(ErrMsgInvalidPayload)
	}
	if err := h.manager.JoinRoom(c, req.RoomID); err != nil {
		return newErrorMessage(ErrMsgInvalidPayload)
	}
	return nil
}

type MessageSDPOffer struct {
	SDP      *model.SDP `json:"sdp"`
	ClientID string     `json:"client_id"`
}

// SDP offerが来た時に呼ばれる。Room IDの
// SDP offer가 왔을 때 불린다.Room IDの
func (h *Handler) handleSDPOffer(c *model.Client, msg []byte) *model.Message {
	var req MessageSDPOffer
	if err := json.Unmarshal(msg, &req); err != nil {
		return newErrorMessage(ErrMsgInvalidPayload)
	}
	if err := h.manager.TransferSDPOffer(c, req.SDP, req.ClientID); err != nil {
		return newErrorMessage(ErrMsgInternalError)
	}
	return nil
}

type MessageSDPAnswer struct {
	SDP      *model.SDP `json:"sdp"`
	ClientID string     `json:"client_id"`
}

func (h *Handler) handleSDPAnswer(c *model.Client, msg []byte) *model.Message {
	var req MessageSDPAnswer
	if err := json.Unmarshal(msg, &req); err != nil {
		return newErrorMessage(ErrMsgInvalidPayload)
	}
	if err := h.manager.TransferSDPAnswer(c, req.SDP, req.ClientID); err != nil {
		return newErrorMessage(ErrMsgInternalError)
	}
	return nil
}

type MessageIceCandidate struct {
	ClientID string     `json:"client_id"`
	Candidate string 	`json:"candidate"`
	SdpMid string 		`json:"sdp_mid"`
	SdpIndex int 		`json:"sdp_index"`
}

func (h *Handler) handleIceCandidate(c *model.Client, msg []byte) *model.Message {
	var req MessageIceCandidate
	if err := json.Unmarshal(msg, &req); err != nil {
		return newErrorMessage(ErrMsgInvalidPayload)
	}
	if err := h.manager.TransferICECandidate(c, req.Candidate, req.SdpMid, req.SdpIndex, req.ClientID); err != nil {
		return newErrorMessage(ErrMsgInternalError)
	}
	return nil
}