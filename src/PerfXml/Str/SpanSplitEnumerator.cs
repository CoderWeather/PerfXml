namespace PerfXml.Str;

/// <summary>
/// <see cref="SpanSplitEnumerator{T}"/> allows for enumeration of each element within a <see cref="System.ReadOnlySpan{T}"/>
/// that has been split using a provided separator.
/// </summary>
public ref struct SpanSplitEnumerator<T> where T : IEquatable<T> {
	private readonly ReadOnlySpan<T> _buffer;

	private readonly ReadOnlySpan<T> _separators;
	private readonly T _separator;

	private readonly int _separatorLength;
	private readonly bool _splitOnSingleToken;

	private readonly bool _isInitialized;

	private int _startCurrent;
	private int _endCurrent;
	private int _startNext;

	/// <summary>
	/// Returns an enumerator that allows for iteration over the split span.
	/// </summary>
	/// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/> that can be used to iterate over the split span.</returns>
	public SpanSplitEnumerator<T> GetEnumerator() => this;

	/// <summary>
	/// Returns the current element of the enumeration.
	/// </summary>
	/// <returns>Returns a <see cref="System.Range"/> instance that indicates the bounds of the current element withing the source span.</returns>
	public Range Current => new(_startCurrent, _endCurrent);

	public SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separators) {
		_isInitialized = true;
		_buffer = span;
		_separators = separators;
		_separator = default!;
		_splitOnSingleToken = false;
		_separatorLength = _separators.Length != 0 ? _separators.Length : 1;
		_startCurrent = 0;
		_endCurrent = 0;
		_startNext = 0;
	}

	public SpanSplitEnumerator(ReadOnlySpan<T> span, T separator) {
		_isInitialized = true;
		_buffer = span;
		_separator = separator;
		_separators = default;
		_splitOnSingleToken = true;
		_separatorLength = 1;
		_startCurrent = 0;
		_endCurrent = 0;
		_startNext = 0;
	}

	/// <summary>
	/// Advances the enumerator to the next element of the enumeration.
	/// </summary>
	/// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the enumeration.</returns>
	public bool MoveNext() {
		if (!CanMoveNext()) {
			return false;
		}

		var slice = _buffer[_startNext..];
		_startCurrent = _startNext;

		var separatorIndex = _splitOnSingleToken ? slice.IndexOf(_separator) : slice.IndexOf(_separators);
		var elementLength = separatorIndex != -1 ? separatorIndex : slice.Length;

		_endCurrent = _startCurrent + elementLength;
		_startNext = _endCurrent + _separatorLength;
		return true;
	}

	public bool CanMoveNext() => _isInitialized && _startNext <= _buffer.Length;
}
