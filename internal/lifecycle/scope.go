package lifecycle

import (
	"context"
	"errors"
	"sync"
)

var ErrClosed = errors.New("synthesis runtime is closed")

type contextKey struct{}
type requestKey struct{}

type resource struct {
	value any
	close func() error
}

// Scopeは要求の終了を待って、自分が作った資源だけを解放する。
type Scope struct {
	access sync.RWMutex
	mu     sync.Mutex
	ctx    context.Context
	cancel context.CancelFunc
	closed bool
	done   chan struct{}
	err    error
	items  map[string]resource
	order  []string
}

func New() *Scope {
	ctx, cancel := context.WithCancel(context.Background())
	return &Scope{ctx: ctx, cancel: cancel, done: make(chan struct{}), items: make(map[string]resource)}
}

func FromContext(ctx context.Context) *Scope {
	if ctx == nil {
		return nil
	}
	scope, _ := ctx.Value(contextKey{}).(*Scope)
	return scope
}

func (s *Scope) Bind(ctx context.Context) context.Context {
	if ctx == nil {
		ctx = s.ctx
	}
	return context.WithValue(ctx, contextKey{}, s)
}

func (s *Scope) Begin(ctx context.Context) (context.Context, func(), error) {
	if ctx == nil {
		ctx = context.Background()
	}
	if err := ctx.Err(); err != nil {
		return nil, nil, err
	}
	if active, _ := ctx.Value(requestKey{}).(*Scope); active == s {
		return ctx, func() {}, nil
	}
	s.access.RLock()
	s.mu.Lock()
	closed := s.closed
	s.mu.Unlock()
	if closed {
		s.access.RUnlock()
		return nil, nil, ErrClosed
	}
	if err := ctx.Err(); err != nil {
		s.access.RUnlock()
		return nil, nil, err
	}
	request, cancel := context.WithCancel(ctx)
	stop := context.AfterFunc(s.ctx, cancel)
	var once sync.Once
	release := func() {
		once.Do(func() {
			stop()
			cancel()
			s.access.RUnlock()
		})
	}
	return context.WithValue(s.Bind(request), requestKey{}, s), release, nil
}

// Resourceのfactoryは状態だけを作る。プロセス起動やI/Oは取得後に行う。
func Resource[T any](ctx context.Context, key string, fallback T, factory func() T, close func(T) error) (T, error) {
	var zero T
	if ctx != nil && ctx.Err() != nil {
		return zero, ctx.Err()
	}
	s := FromContext(ctx)
	if s == nil {
		return fallback, nil
	}
	s.mu.Lock()
	defer s.mu.Unlock()
	if s.closed {
		return zero, ErrClosed
	}
	if item, ok := s.items[key]; ok {
		return item.value.(T), nil
	}
	value := factory()
	item := resource{value: value}
	if close != nil {
		item.close = func() error { return close(value) }
	}
	s.items[key] = item
	s.order = append(s.order, key)
	return value, nil
}

func (s *Scope) releaseResources() error {
	s.mu.Lock()
	items, order := s.items, s.order
	s.items, s.order = make(map[string]resource), nil
	s.mu.Unlock()
	var err error
	for index := len(order) - 1; index >= 0; index-- {
		if close := items[order[index]].close; close != nil {
			err = errors.Join(err, close())
		}
	}
	return err
}

func (s *Scope) Reset(reload func() error) error {
	s.access.Lock()
	defer s.access.Unlock()
	s.mu.Lock()
	closed := s.closed
	s.mu.Unlock()
	if closed {
		return ErrClosed
	}
	err := s.releaseResources()
	if reload != nil {
		err = errors.Join(err, reload())
	}
	return err
}

func (s *Scope) Close() error {
	s.mu.Lock()
	if s.closed {
		s.mu.Unlock()
		<-s.done
		return s.err
	}
	s.closed = true
	s.cancel()
	s.mu.Unlock()
	s.access.Lock()
	err := s.releaseResources()
	s.access.Unlock()
	s.mu.Lock()
	s.err = err
	close(s.done)
	s.mu.Unlock()
	return err
}
