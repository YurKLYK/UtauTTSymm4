//go:build windows

package main

import "github.com/yh2237/UtauTTS/internal/worldffi"

func worldF0(r record, step float64, path string) ([]float64, error) {
	return worldffi.AnalyzeF0(r.AudioPath, step, path)
}
