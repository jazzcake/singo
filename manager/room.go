package manager

import (
	"encoding/json"
	"log"

	"github.com/tockn/singo/model"
	"github.com/tockn/singo/repository"
)

type RoomMgr struct {
	roomRepo repository.RoomRepo
}

func NewManager(roomRepo repository.RoomRepo) *RoomMgr {
	return &RoomMgr{roomRepo: roomRepo}
}

func (rm *RoomMgr) CreateRoom(name string) (*model.Room, error) {
	r := model.NewRoom(name)
	return rm.roomRepo.Create(r)
}

func (rm *RoomMgr) JoinRoom(c *model.Client, roomID string) error {
	r, err := rm.roomRepo.Get(roomID)
	if err == repository.ErrNotFound {
		r = model.NewRoom(roomID)
		if _, err := rm.roomRepo.Create(r); err != nil {
			return err
		}
	} else if err != nil {
		return err
	}
	r.Clients[c.ID] = c
	if _, err := rm.roomRepo.Update(r); err != nil {
		return err
	}
	log.Printf("joined! clientID:%s, roomID: %s\n", c.ID, roomID)
	return rm.notifyNewClient(roomID, c)
}

func (rm *RoomMgr) LeaveRoom(c *model.Client) error {
	r, err := rm.roomRepo.GetByClientID(c.ID)
	if err != nil {
		return err
	}
	go rm.notifyLeaveClient(r.ID, c)
	delete(r.Clients, c.ID)
	if _, err := rm.roomRepo.Update(r); err != nil {
		return err
	}
	return nil
}

type NewClientPayload struct {
	ClientID string `json:"client_id"`
}

func (rm *RoomMgr) notifyNewClient(roomID string, nc *model.Client) error {
	r, err := rm.roomRepo.Get(roomID)
	if err != nil {
		return err
	}

	// 이렇게 json.Marshal ( json.Marshal ) 하면 BASE64로 묶어버린다.
	payload, _ := json.Marshal(NewClientPayload{ClientID: nc.ID})
	msg := &model.Message{
		Type:    model.MessageTypeNewClient,
		Payload: payload,
	}
	for _, c := range r.Clients {
		// 자신에게도 오게 한다. 이 경우 join 프로세스가 종료된 것이다.
		// if c.ID == nc.ID {
		// 	continue
		// }
		c.SendChan <- msg
	}
	return nil
}

type LeaveClientPayload struct {
	ClientID string `json:"client_id"`
}

func (rm *RoomMgr) notifyLeaveClient(roomID string, nc *model.Client) error {
	r, err := rm.roomRepo.Get(roomID)
	if err != nil {
		return err
	}

	// 이렇게 json.Marshal ( json.Marshal ) 하면 BASE64로 묶어버린다.
	payload, _ := json.Marshal(LeaveClientPayload{ClientID: nc.ID})
	msg := &model.Message{
		Type:    model.MessageTypeLeaveClient,
		Payload: payload,
	}
	for _, c := range r.Clients {
		if c.ID == nc.ID {
			continue
		}
		c.SendChan <- msg
	}
	return nil
}

type SDPOfferPayload struct {
	ClientID string     `json:"client_id"`
	SDP      *model.SDP `json:"sdp"`
}

func (rm *RoomMgr) TransferSDPOffer(senderClient *model.Client, sdp *model.SDP, clientID string) error {
	r, err := rm.roomRepo.GetByClientID(senderClient.ID)
	if err != nil {
		return err
	}

	// 이렇게 json.Marshal ( json.Marshal ) 하면 BASE64로 묶어버린다.
	payload, _ := json.Marshal(SDPOfferPayload{ClientID: senderClient.ID, SDP: sdp})
	msg := &model.Message{
		Type:    model.MessageTypeSDPOffer,
		Payload: payload,
	}
	for _, c := range r.Clients {
		if c.ID != clientID {
			continue
		}
		c.SendChan <- msg
	}
	return nil
}

type SDPAnswerPayload struct {
	ClientID string     `json:"client_id"`
	SDP      *model.SDP `json:"sdp"`
}

func (rm *RoomMgr) TransferSDPAnswer(senderClient *model.Client, sdp *model.SDP, clientID string) error {
	r, err := rm.roomRepo.GetByClientID(senderClient.ID)
	if err != nil {
		return err
	}

	// 이렇게 json.Marshal ( json.Marshal ) 하면 BASE64로 묶어버린다.
	payload, _ := json.Marshal(SDPAnswerPayload{ClientID: senderClient.ID, SDP: sdp})
	msg := &model.Message{
		Type:    model.MessageTypeSDPAnswer,
		Payload: payload,
	}
	for _, c := range r.Clients {
		if c.ID != clientID {
			continue
		}
		c.SendChan <- msg
	}
	return nil
}

type SDPICECandidatePayload struct {
	ClientID string     `json:"client_id"`
	Candidate string 	`json:"candidate"`
	SdpMid string 		`json:"sdp_mid"`
	SdpIndex int 		`json:"sdp_index"`
}

func (rm *RoomMgr) TransferICECandidate(senderClient *model.Client, candidate string, sdp_mid string, sdp_index int, clientID string) error {
	r, err := rm.roomRepo.GetByClientID(senderClient.ID)
	if err != nil {
		return err
	}

	// 이렇게 json.Marshal ( json.Marshal ) 하면 BASE64로 묶어버린다.
	payload, _ := json.Marshal(SDPICECandidatePayload{ClientID: senderClient.ID, Candidate: candidate, SdpMid: sdp_mid, SdpIndex: sdp_index})
	msg := &model.Message{
		Type:    model.MessageTypeICECandidate, // .MessageTypeSDPAnswer, answer는 아니지 않나?
		Payload: payload,
	}
	for _, c := range r.Clients {
		if c.ID != clientID {
			continue
		}
		c.SendChan <- msg
	}
	return nil
}
