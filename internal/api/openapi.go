package api

import (
	_ "embed"
	"log"
	"net/http"
)

//go:embed openapi.yaml
var openAPISpec []byte

func (s *Server) handleOpenAPI(w http.ResponseWriter, _ *http.Request) {
	w.Header().Set("Content-Type", "application/yaml; charset=utf-8")
	w.WriteHeader(http.StatusOK)
	if _, err := w.Write(openAPISpec); err != nil {
		log.Printf("write OpenAPI response: %v", err)
	}
}
