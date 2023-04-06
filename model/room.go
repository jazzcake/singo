package model

import "github.com/rs/xid"

type Room struct {
	ID      string
	Name    string
	Clients map[string]*Client
	Index   int
}

func NewRoom(name string) *Room {
	return &Room{
		ID:      name,
		Name:    name,
		Clients: make(map[string]*Client, 0),
		Index:   0,
	}
}

type Client struct {
	ID       string
	Name     string
	SendChan chan *Message
	Index    int
}

func NewClient(name string) *Client {
	return &Client{
		ID:       xid.New().String(),
		Name:     name,
		SendChan: make(chan *Message, 16),
		Index:    -1,
	}
}
