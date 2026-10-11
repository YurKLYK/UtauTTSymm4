package audio

import (
	"bytes"
	"os"
	"path/filepath"
	"reflect"
	"testing"
)

func TestWavFileAndMemoryAPIsPreservePCM(t *testing.T) {
	pcm := &PCM{SampleRate: 48000, Channels: 2, Data: []int16{-32768, -1, 0, 32767}}
	path := filepath.Join(t.TempDir(), "output.wav")
	if err := WriteWav(path, pcm); err != nil {
		t.Fatal(err)
	}
	data, err := os.ReadFile(path)
	if err != nil {
		t.Fatal(err)
	}
	if !bytes.Equal(data, PCMToWavBytes(pcm)) {
		t.Fatal("file and memory WAV output differ")
	}
	got, err := ReadWav(path)
	if err != nil || !reflect.DeepEqual(got, pcm) {
		t.Fatalf("read PCM = %+v, %v", got, err)
	}
}

func TestWriteWavRejectsEmptyPCMWithoutReplacingFile(t *testing.T) {
	path := filepath.Join(t.TempDir(), "output.wav")
	if err := os.WriteFile(path, []byte("existing"), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := WriteWav(path, &PCM{SampleRate: 16000, Channels: 1}); err == nil {
		t.Fatal("empty PCM accepted")
	}
	data, err := os.ReadFile(path)
	if err != nil || string(data) != "existing" {
		t.Fatalf("failed write replaced the file: %q, %v", data, err)
	}
}
