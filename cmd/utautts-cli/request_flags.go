package main

// Placement and output flags can accompany a JSON utterance. All synthesis,
// editing and evaluation flags belong in JSON or in the legacy flag mode.
var requestTransportFlags = map[string]bool{
	"request": true, "json": true, "version": true, "timeout": true,
	"voicebank": true, "renderer-dir": true, "model-dir": true,
	"worldline-bridge": true, "openjtalk-features": true, "openjtalk-dictionary": true,
	"out": true, "plan-out": true, "ustx-out": true,
	"write-text": true, "write-lab": true, "text-encoding": true,
}
