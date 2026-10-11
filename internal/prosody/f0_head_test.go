package prosody

import (
	"encoding/json"
	"os"
	"path/filepath"
	"testing"
)

func TestBundledV12LoadsBaseAndUsesHeadOnly(t *testing.T) {
	path := filepath.Join("..", "..", "models", "frame-intonation-tcn-v12.json")
	model, err := LoadModel(path)
	if err != nil {
		t.Fatal(err)
	}
	if model.ID != "frame-intonation-tcn-v12" || model.Language != "ja" {
		t.Fatalf("identity = %q %q %d", model.ID, model.Language, model.DefaultPriority)
	}
	if !model.HasFrameContour() || !model.RequiresExternalFeatures() {
		t.Fatal("base model features were not inherited")
	}
	head := model.F0Head
	if head == nil || !head.HasF0Head() || !head.HasEnergyHead() {
		t.Fatal("F0 head is missing")
	}
	if head.F0Scale() != 0 || model.F0HeadBaseBlend != 0 || model.F0HeadPitchOffsetCents != -50 || !model.F0HeadEnergy || model.DefaultPriority <= 0 {
		t.Fatalf("v12: scale %v blend %v offset %v energy %v priority %d", head.F0Scale(), model.F0HeadBaseBlend, model.F0HeadPitchOffsetCents, model.F0HeadEnergy, model.DefaultPriority)
	}
}

func TestF0HeadModelRejectsMissingHead(t *testing.T) {
	data, err := os.ReadFile(filepath.Join("..", "..", "models", "frame-intonation-tcn-v12.json"))
	if err != nil {
		t.Fatal(err)
	}
	var bundled struct {
		BaseModelData json.RawMessage `json:"base_model_data"`
	}
	if err := json.Unmarshal(data, &bundled); err != nil || len(bundled.BaseModelData) == 0 {
		t.Fatalf("bundled model has no embedded base: %v", err)
	}
	manifest, err := json.Marshal(map[string]any{"id": "broken", "display_name": "Broken", "base_model_data": bundled.BaseModelData, "f0_head": ""})
	if err != nil {
		t.Fatal(err)
	}
	path := filepath.Join(t.TempDir(), "broken.json")
	if err := os.WriteFile(path, manifest, 0o644); err != nil {
		t.Fatal(err)
	}
	if _, err := LoadModel(path); err == nil {
		t.Fatal("manifest without F0 head weights must fail")
	}
}
