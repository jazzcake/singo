package main

import (
	"flag"
	"fmt"
)

func main() {
	if err := run(); err != nil {
		panic(err)
	}
}

func run() error {
	var (
		withDemo = flag.Bool("demo", false, "serve with video chat system demo")
		addrFlag = flag.String("addr", "127.0.0.1", "addr")
		portFlag = flag.Int("port", 5000, "port")
	)
	flag.Parse()

	addr := fmt.Sprintf("%s:%d", *addrFlag, *portFlag)

	if *withDemo {
		return serveWithDemo(addr)
	}

	return serve(addr)
}
