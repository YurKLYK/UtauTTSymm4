package api

import (
	"net/http"

	"github.com/yh2237/UtauTTS/internal/engine"
)

// Readiness is deliberately separate from liveness and does not start helpers.
func (s *Server) handleReadiness(w http.ResponseWriter, r *http.Request) {
	rendererID := s.renderer
	availability := engine.Availability{Available: false}
	resolved, err := s.synthesisService().ResolveRenderer("")
	if err == nil {
		rendererID = string(resolved.PublicID())
		availability = resolved.Availability
		if resolved.Provider.ClassicTools {
			if _, err := s.synthesisService().ResolveClassicTools("", ""); err != nil {
				availability.Available = false
				availability.Issues = append(availability.Issues, engine.AvailabilityIssue{Message: err.Error()})
			}
		}
	} else {
		availability.Issues = []engine.AvailabilityIssue{{Message: err.Error()}}
	}
	// Begin only checks the Runtime's state; no synthesis resources are started.
	if s.runtime != nil {
		_, release, err := s.runtime.Begin(r.Context())
		if err != nil {
			availability.Available = false
			availability.Issues = append(availability.Issues, engine.AvailabilityIssue{Message: err.Error()})
		} else {
			release()
		}
	}
	count := len(s.voicebankList())
	ready := availability.Available && count > 0
	status, state := http.StatusOK, "ready"
	if !ready {
		status, state = http.StatusServiceUnavailable, "not_ready"
	}
	writeJSON(w, status, struct {
		Status         string              `json:"status"`
		RendererID     string              `json:"renderer_id"`
		Renderer       engine.Availability `json:"renderer"`
		VoicebankCount int                 `json:"voicebank_count"`
	}{state, rendererID, availability, count})
}
