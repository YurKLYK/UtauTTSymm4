// Package entryinput converts application inputs without changing their legacy
// validation rules. Strict public API validation remains in the root package.
package entryinput

import (
	utautts "github.com/yh2237/UtauTTS"
	"github.com/yh2237/UtauTTS/internal/synth"
)

// Apply adds the normal utterance to an internal request carrying editing or
// evaluation settings. CLI and HTTP keep their existing zero-pause semantics.
func Apply(request utautts.Request, input synth.Request) synth.Request {
	input = synth.ApplyCommonOptions(input, synth.CommonOptions(request.Options))
	input.PauseDurationSet = false
	input.Text, input.Reading = request.Text, request.Reading
	input.Language, input.Phonemizer = request.Language, request.Phonemizer
	input.VoicebankID, input.Renderer, input.ModelID = request.VoicebankID, request.RendererID, request.ModelID
	input.Tone, input.Color = request.Tone, request.Color
	input.Dictionary = make([]synth.DictionaryEntry, len(request.Dictionary))
	for i, entry := range request.Dictionary {
		input.Dictionary[i] = synth.DictionaryEntry{Surface: entry.Surface, Reading: entry.Reading}
	}
	// These entries retain the existing priority over typed compatibility fields,
	// including common settings and values diagnosed by the renderer itself.
	input.RendererSettings = request.RendererSettings
	return input
}
