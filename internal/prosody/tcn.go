package prosody

import "math"

// tcnForwardはdilation付き残差TCNを順伝播する。sameGroupが非nilなら参照元を同じgroupに限定する。
func tcnForward(state [][]float64, layers []SequencePitchLayer, sameGroup func(source, position int) bool) [][]float64 {
	if len(state) == 0 {
		return state
	}
	for _, layer := range layers {
		hidden := len(layer.Bias)
		next := make([][]float64, len(state))
		values := make([]float64, len(state)*hidden)
		for position := range state {
			next[position] = values[position*hidden : (position+1)*hidden]
			var sources [3][]float64
			for kernel := range sources {
				source := position + (kernel-1)*layer.Dilation
				if source >= 0 && source < len(state) && (sameGroup == nil || sameGroup(source, position)) {
					sources[kernel] = state[source]
				}
			}
			for output := 0; output < hidden; output++ {
				value := state[position][output] + layer.Bias[output]
				for input := 0; input < hidden; input++ {
					for kernel, source := range sources {
						if source != nil {
							value += layer.Weights[output][input][kernel] * source[input]
						}
					}
				}
				next[position][output] = math.Tanh(value)
			}
		}
		state = next
	}
	return state
}
