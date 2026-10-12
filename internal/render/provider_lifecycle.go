package render

import (
	"errors"

	"github.com/yh2237/UtauTTS/internal/render/base"
)

// CloseProviderSessionsはアプリ終了時に常駐する外部Providerプロセスを解放する。
func CloseProviderSessions() error {
	return errors.Join(base.CloseRegistered(), externalProviderSessions.closeAll())
}
