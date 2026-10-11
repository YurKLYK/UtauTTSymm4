package prosody

import (
	"encoding/json"
	"os"
	"path/filepath"
	"testing"
)

// 同梱のF0ヘッドつきモデルJSON（v12）が、埋め込んだ基準モデルとF0・エネルギーヘッドを合わせて読めることを確認する。
func TestBundledF0HeadModelLoads(t *testing.T) {
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
	if head.F0Scale() != 0 || len(head.Phones()) != 40 || head.Mels() != 80 {
		t.Fatalf("f0 scale %v phones %d mels %d", head.F0Scale(), len(head.Phones()), head.Mels())
	}
	if len(head.PosVocab()) != 11 || len(head.PosGroup1Vocab()) != 28 {
		t.Fatalf("pos vocab %d/%d", len(head.PosVocab()), len(head.PosGroup1Vocab()))
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

// 既定の日本語抑揚モデルv12は、基準モデルを混ぜず、全体を-50cent下げ、エネルギーヘッドでモーラの音量も変える。
func TestBundledV12UsesHeadOnly(t *testing.T) {
	model, err := LoadModel(filepath.Join("..", "..", "models", "frame-intonation-tcn-v12.json"))
	if err != nil {
		t.Fatal(err)
	}
	if model.F0Head == nil || model.F0HeadBaseBlend != 0 || model.F0HeadPitchOffsetCents != -50 || !model.F0HeadEnergy || model.DefaultPriority <= 0 {
		t.Fatalf("v12: head %v blend %v offset %v energy %v priority %d", model.F0Head != nil, model.F0HeadBaseBlend, model.F0HeadPitchOffsetCents, model.F0HeadEnergy, model.DefaultPriority)
	}
	if model.F0Head.F0Scale() != 0 {
		t.Fatalf("v12 F0 head must use the natural scale, got f0_scale %v", model.F0Head.F0Scale())
	}
}
