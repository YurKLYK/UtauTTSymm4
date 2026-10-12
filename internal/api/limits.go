package api

import (
	"context"
	"errors"
	"fmt"
	"net/http"
	"time"
)

type Limits struct {
	MaxJSONRequestBytes    int64         `json:"max_json_request_bytes"`
	MaxTextRunes           int           `json:"max_text_runes"`
	MaxBatchItems          int           `json:"max_batch_items"`
	MaxBatchWAVBytes       int64         `json:"max_batch_wav_bytes"`
	MaxManualPitchPoints   int           `json:"max_manual_pitch_points"`
	MaxConcurrentSynthesis int           `json:"max_concurrent_synthesis"`
	MaxConcurrentBatches   int           `json:"max_concurrent_batches"`
	QueueTimeout           time.Duration `json:"-"`
	SynthesisTimeout       time.Duration `json:"-"`
}

func DefaultLimits() Limits {
	return Limits{
		MaxJSONRequestBytes: maxJSONRequestBytes, MaxTextRunes: maxTextRunes,
		MaxBatchItems: maxBatchItems, MaxBatchWAVBytes: maxBatchWAVBytes,
		MaxManualPitchPoints:   maxManualPitchPoints,
		MaxConcurrentSynthesis: maxConcurrentSynthesis, MaxConcurrentBatches: maxConcurrentBatches,
	}
}

// Zero size/count values use existing defaults. Zero timeouts remain unlimited.
func (limits Limits) normalized() (Limits, error) {
	defaults := DefaultLimits()
	for _, value := range []struct {
		name     string
		target   *int
		fallback int
	}{
		{"max_text_runes", &limits.MaxTextRunes, defaults.MaxTextRunes},
		{"max_batch_items", &limits.MaxBatchItems, defaults.MaxBatchItems},
		{"max_manual_pitch_points", &limits.MaxManualPitchPoints, defaults.MaxManualPitchPoints},
		{"max_concurrent_synthesis", &limits.MaxConcurrentSynthesis, defaults.MaxConcurrentSynthesis},
		{"max_concurrent_batches", &limits.MaxConcurrentBatches, defaults.MaxConcurrentBatches},
	} {
		if *value.target < 0 {
			return Limits{}, fmt.Errorf("%s must not be negative", value.name)
		}
		if *value.target == 0 {
			*value.target = value.fallback
		}
	}
	for _, value := range []struct {
		name     string
		target   *int64
		fallback int64
	}{
		{"max_json_request_bytes", &limits.MaxJSONRequestBytes, defaults.MaxJSONRequestBytes},
		{"max_batch_wav_bytes", &limits.MaxBatchWAVBytes, defaults.MaxBatchWAVBytes},
	} {
		if *value.target < 0 {
			return Limits{}, fmt.Errorf("%s must not be negative", value.name)
		}
		if *value.target == 0 {
			*value.target = value.fallback
		}
	}
	if limits.QueueTimeout < 0 || limits.SynthesisTimeout < 0 {
		return Limits{}, errors.New("queue_timeout and synthesis_timeout must not be negative")
	}
	return limits, nil
}

func (s *Server) effectiveLimits() Limits {
	if s.limits.MaxJSONRequestBytes == 0 {
		return DefaultLimits()
	}
	return s.limits
}

var errQueueTimeout = errors.New("queue wait timed out")

func (s *Server) acquire(ctx context.Context, sem chan struct{}) (func(), error) {
	if err := ctx.Err(); err != nil {
		return nil, err
	}
	if sem == nil {
		return func() {}, nil
	}
	wait := ctx
	if timeout := s.effectiveLimits().QueueTimeout; timeout > 0 {
		var cancel context.CancelFunc
		wait, cancel = context.WithTimeout(ctx, timeout)
		defer cancel()
	}
	select {
	case sem <- struct{}{}:
		if wait.Err() != nil {
			<-sem
			if err := ctx.Err(); err != nil {
				return nil, err
			}
			return nil, errQueueTimeout
		}
		return func() { <-sem }, nil
	case <-wait.Done():
		if err := ctx.Err(); err != nil {
			return nil, err
		}
		return nil, errQueueTimeout
	}
}

func (s *Server) processingContext(ctx context.Context) (context.Context, context.CancelFunc) {
	if timeout := s.effectiveLimits().SynthesisTimeout; timeout > 0 {
		return context.WithTimeout(ctx, timeout)
	}
	return ctx, func() {}
}

func (s *Server) handleLimits(w http.ResponseWriter, _ *http.Request) {
	limits := s.effectiveLimits()
	writeJSON(w, http.StatusOK, struct {
		Limits
		QueueTimeout     string `json:"queue_timeout"`
		SynthesisTimeout string `json:"synthesis_timeout"`
	}{limits, limits.QueueTimeout.String(), limits.SynthesisTimeout.String()})
}
