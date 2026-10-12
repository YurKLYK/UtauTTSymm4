// Package utautts provides speech synthesis without GUI dependencies.
// Runtime helpers and models are discovered at initialization; subprocesses
// start on demand and are released by Engine.Close.
package utautts

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"strings"

	"github.com/yh2237/audiodsp/wav"

	"github.com/yh2237/UtauTTS/internal/lifecycle"
	"github.com/yh2237/UtauTTS/internal/synth"
	"github.com/yh2237/UtauTTS/internal/voicebank"
)

var (
	ErrClosed         = lifecycle.ErrClosed
	ErrUnavailable    = synth.ErrUnavailable
	ErrInvalidRequest = errors.New("invalid synthesis request")
)

// Config describes deployment resources, not individual utterances.
// Empty paths use the application's existing discovery rules.
type Config struct {
	VoiceDirectory      string
	Renderer            string
	WorldlineBridgePath string
	OpenJTalkPath       string
	OpenJTalkDictionary string
	RendererDirectories []string
	ModelDirectories    []string
}

type Engine struct {
	runtime *synth.Runtime
	voices  *voicebank.Library
}

type voiceResolver struct{ library *voicebank.Library }

func (r voiceResolver) Resolve(id string) (string, bool) {
	summary, ok := r.library.Resolve(id)
	return summary.Path, ok
}

// New discovers catalogs and voicebanks. Missing helpers are reported when a
// renderer or analysis operation needs them, rather than starting subprocesses.
func New(config Config) (*Engine, error) {
	voices := voicebank.NewLibrary(config.VoiceDirectory)
	runtime, err := synth.NewRuntime(synth.RuntimeConfig{
		Renderer: config.Renderer, WorldlineBridgePath: config.WorldlineBridgePath,
		OpenJTalkPath: config.OpenJTalkPath, OpenJTalkDictionary: config.OpenJTalkDictionary,
		RendererDirectories: append([]string(nil), config.RendererDirectories...),
		ModelDirectories:    append([]string(nil), config.ModelDirectories...),
	}, voiceResolver{voices})
	if err != nil {
		return nil, err
	}
	if err := voices.Reload(); err != nil {
		return nil, errors.Join(err, runtime.Close())
	}
	return &Engine{runtime: runtime, voices: voices}, nil
}

// Close cancels this engine's active requests, waits for them, and releases its
// caches and subprocesses. Repeated and concurrent calls return the same error.
func (e *Engine) Close() error { return e.runtime.Close() }

// Reload waits for active requests and reloads this engine's voicebank library.
// Its caches and subprocesses are discarded. Other engines remain usable.
// Renderer and model catalogs are fixed at New; recreate the engine to rediscover them.
func (e *Engine) Reload() error { return e.runtime.Reload(e.voices.Reload) }

type Voicebank struct {
	ID, Name, Path, Kind string
}

func (e *Engine) Voicebanks(ctx context.Context) ([]Voicebank, error) {
	_, release, err := e.runtime.Begin(ctx)
	if err != nil {
		return nil, err
	}
	defer release()
	items := e.voices.List()
	voices := make([]Voicebank, len(items))
	for i, item := range items {
		voices[i] = Voicebank{item.ID, item.Summary.Name, item.Summary.Path, item.Summary.Kind}
	}
	return voices, nil
}

type Renderer struct {
	ID, Name  string
	Available bool
	Problems  []string
}

func (e *Engine) Renderers(ctx context.Context) ([]Renderer, error) {
	_, release, err := e.runtime.Begin(ctx)
	if err != nil {
		return nil, err
	}
	defer release()
	availability := e.runtime.Service.RendererAvailability()
	items := make([]Renderer, 0, len(e.runtime.Catalog.Renderers))
	for _, item := range e.runtime.Catalog.Renderers {
		status := availability[item.ID]
		renderer := Renderer{ID: item.ID, Name: item.DisplayName, Available: status.Available}
		for _, issue := range status.Issues {
			renderer.Problems = append(renderer.Problems, issue.Message)
		}
		items = append(items, renderer)
	}
	return items, nil
}

type Model struct {
	ID, Name, Language string
}

func (e *Engine) Models(ctx context.Context) ([]Model, error) {
	_, release, err := e.runtime.Begin(ctx)
	if err != nil {
		return nil, err
	}
	defer release()
	items := make([]Model, len(e.runtime.Catalog.Models))
	for i, item := range e.runtime.Catalog.Models {
		items[i] = Model{item.ID, item.DisplayName, item.Language}
	}
	return items, nil
}

// Request describes one utterance. Empty IDs select catalog defaults.
// Reading, when specified, takes precedence over Text for pronunciation.
type Request struct {
	Text, Reading, Language, Phonemizer string
	VoicebankID, RendererID, ModelID    string
	Tone, Color                         string
	Dictionary                          []DictionaryEntry
	Options                             Options
	// RendererSettings carries implementation-specific JSON values.
	// Common option IDs are rejected; use Options for those settings.
	RendererSettings map[string]json.RawMessage
}

type DictionaryEntry struct {
	Surface string `json:"surface"`
	Reading string `json:"reading"`
}

// Options uses nil for defaults. Non-nil values, including 0 and false, are
// explicit. MoraDurationMS must be positive; PauseDurationMS and ReleaseMS may be 0.
type Options struct {
	MoraDurationMS, PauseDurationMS, IntonationStrength, ReleaseMS *float64
	ApplyPitch, ContextDuration, BoundaryTone, StretchAdapt        *bool
	PauseContext, EnglishWeakForm                                  *bool
}

type Analysis struct {
	Reading string
	Morae   []Mora
}

type Mora struct {
	Text, Consonant, Vowel string
	Pause                  bool
}

func (e *Engine) Analyze(ctx context.Context, request Request) (*Analysis, error) {
	ctx, release, err := e.runtime.Begin(ctx)
	if err != nil {
		return nil, err
	}
	defer release()
	input, err := request.internal()
	if err != nil {
		return nil, err
	}
	preview, err := e.runtime.Service.AnalyzeContext(ctx, input)
	if err != nil {
		return nil, err
	}
	result := &Analysis{Reading: preview.Reading, Morae: make([]Mora, len(preview.Morae))}
	for i, mora := range preview.Morae {
		result.Morae[i] = Mora{mora.Text, mora.Consonant, mora.Vowel, mora.Pause}
	}
	return result, nil
}

// Audio contains interleaved signed 16-bit PCM owned by the caller.
type Audio struct {
	SampleRate, Channels int
	Samples              []int16
}

func (a Audio) WriteWAV(w io.Writer) error {
	return wav.Encode(w, &wav.PCM{SampleRate: a.SampleRate, Channels: a.Channels, Data: a.Samples})
}

// Phoneme times are relative to the start of the returned audio, in milliseconds.
// They are planned label boundaries, not a forced alignment of the waveform.
type Phoneme struct {
	Symbol         string
	StartMS, EndMS float64
}

type Diagnostic struct{ Severity, Code, Message string }

type Result struct {
	Audio               Audio
	Reading, RendererID string
	DurationMS          float64
	Phonemes            []Phoneme
	Diagnostics         []Diagnostic
}

func (e *Engine) Synthesize(ctx context.Context, request Request) (*Result, error) {
	ctx, release, err := e.runtime.Begin(ctx)
	if err != nil {
		return nil, err
	}
	defer release()
	input, err := request.internal()
	if err != nil {
		return nil, err
	}
	output, err := e.runtime.Service.SynthesizeContext(ctx, input)
	if err != nil {
		return nil, err
	}
	result := &Result{
		Audio:   Audio{output.Audio.SampleRate, output.Audio.Channels, output.Audio.Data},
		Reading: output.Plan.Reading, RendererID: output.RendererID, DurationMS: output.DurationMS,
	}
	labels := strings.NewReader(output.Lab)
	for {
		var start, end int64
		var symbol string
		if _, err := fmt.Fscan(labels, &start, &end, &symbol); err != nil {
			if err == io.EOF {
				break
			}
			return nil, fmt.Errorf("decode phoneme labels: %w", err)
		}
		result.Phonemes = append(result.Phonemes, Phoneme{symbol, float64(start) / 10000, float64(end) / 10000})
	}
	for _, diagnostic := range output.Diagnostics() {
		result.Diagnostics = append(result.Diagnostics, Diagnostic{diagnostic.Severity, diagnostic.Code, diagnostic.Message})
	}
	return result, nil
}
