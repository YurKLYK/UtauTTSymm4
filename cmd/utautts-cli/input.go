package main

import (
	"bytes"
	"fmt"
	"io"
	"os"
	"strings"
	"unicode/utf8"
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
