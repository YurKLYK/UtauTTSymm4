package main

import (
	"context"
	"errors"
	"flag"
	"fmt"
	"log"
	"net"
	"net/http"
	"os"
	"os/signal"
	"strings"
	"syscall"
	"time"

	"utautts/internal/api"
	"utautts/internal/appinfo"
)

func isLoopbackHost(host string) bool {
	switch strings.ToLower(host) {
	case "localhost", "127.0.0.1", "::1":
		return true
	}
	if ip := net.ParseIP(host); ip != nil && ip.IsLoopback() {
		return true
	}
	return false
}

func main() {
	if err := run(); err != nil {
		log.Print(err)
		os.Exit(1)
	}
}

func run() (runErr error) {
	var port int
	var host string
	var showVersion bool
	var config api.Config
	flag.BoolVar(&showVersion, "version", false, "print application version")
	flag.IntVar(&port, "port", 8080, "port")
	flag.StringVar(&host, "host", "127.0.0.1", "host")
	flag.StringVar(&config.VoiceDir, "voice-dir", "voice", "directory containing voicebanks")
	flag.StringVar(&config.Renderer, "renderer", "", "default renderer ID (default: highest configured priority)")
	flag.StringVar(&config.WorldlineBridgePath, "worldline-bridge", "", "path to worldline bridge")
	flag.StringVar(&config.OpenJTalkPath, "openjtalk-features", "", "path to Open JTalk feature helper")
	flag.StringVar(&config.OpenJTalkDictionary, "openjtalk-dictionary", "", "path to Open JTalk dictionary")
	flag.StringVar(&config.AuthToken, "auth-token", "", "optional local UI authentication token")
	flag.BoolVar(&config.AllowVoicebankRegistration, "allow-voicebank-registration", false, "allow registration of paths below voice-dir")
	flag.Func("renderer-dir", "renderer plugin directory (repeatable)", func(value string) error {
		config.RendererDirectories = append(config.RendererDirectories, value)
		return nil
	})
	flag.Func("model-dir", "prosody model directory (repeatable)", func(value string) error {
		config.ModelDirectories = append(config.ModelDirectories, value)
		return nil
	})
	flag.Parse()
	if showVersion {
		fmt.Printf("%s %s\n", appinfo.Name(), appinfo.Version())
		return nil
	}
	if config.AuthToken == "" && !isLoopbackHost(host) {
		log.Printf("warning: listening on %s without an auth token; all API endpoints are unauthenticated", host)
	}

	listener, err := net.Listen("tcp", fmt.Sprintf("%s:%d", host, port))
	if err != nil {
		return err
	}
	defer listener.Close()
	apiServer, err := api.New(config)
	if err != nil {
		return err
	}
	defer func() { runErr = errors.Join(runErr, apiServer.Close()) }()
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()
	url := "http://" + listener.Addr().String()
	fmt.Printf("UTAUTTS_READY=%s\n", url)
	log.Printf("listening on %s", url)
	server := &http.Server{Handler: apiServer.Handler(), ReadHeaderTimeout: 5 * time.Second, ReadTimeout: 30 * time.Second, WriteTimeout: 10 * time.Minute, IdleTimeout: 60 * time.Second}
	server.BaseContext = func(net.Listener) context.Context { return ctx }
	served := make(chan error, 1)
	go func() { served <- server.Serve(listener) }()
	select {
	case err := <-served:
		if !errors.Is(err, http.ErrServerClosed) {
			return err
		}
		return nil
	case <-ctx.Done():
		shutdown, cancel := context.WithTimeout(context.Background(), 10*time.Second)
		defer cancel()
		err := server.Shutdown(shutdown)
		if err != nil {
			err = errors.Join(err, server.Close())
		}
		<-served
		return err
	}
}
