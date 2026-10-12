package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"os"
	"strings"
	"unicode/utf8"

	"github.com/yh2237/UtauTTS/internal/entryinput"
)

const maxUtteranceBytes = 1 << 20

func readUtteranceFile(path string, stdin io.Reader) (string, error) {
	input := stdin
	if path != "-" {
		file, err := os.Open(path)
		if err != nil {
			return "", err
		}
		defer file.Close()
		input = file
	}
	data, err := io.ReadAll(io.LimitReader(input, maxUtteranceBytes+1))
	if err != nil {
		return "", err
	}
	if len(data) > maxUtteranceBytes {
		return "", fmt.Errorf("input is limited to %d bytes", maxUtteranceBytes)
	}
	data = bytes.TrimPrefix(data, []byte{0xef, 0xbb, 0xbf})
	if !utf8.Valid(data) {
		return "", fmt.Errorf("input must be UTF-8")
	}
	value := strings.TrimRight(string(data), "\r\n")
	if strings.TrimSpace(value) == "" {
		return "", fmt.Errorf("input is empty")
	}
	return value, nil
}

func readRequestFile(path string, stdin io.Reader) (*entryinput.JSONRequest, error) {
	data, err := readUtteranceFile(path, stdin)
	if err != nil {
		return nil, err
	}
	var request *entryinput.JSONRequest
	if err := json.Unmarshal([]byte(data), &request); err != nil {
		return nil, err
	}
	if request == nil {
		return nil, fmt.Errorf("request must be a JSON object")
	}
	return request, nil
}
