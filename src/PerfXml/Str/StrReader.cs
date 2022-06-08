namespace PerfXml.Str;

public ref struct StrReader {
	private ReadOnlySpan<char> str;
	private SpanSplitEnumerator<char> enumerator;

	public StrReader(ReadOnlySpan<char> str, char separator) : this(str,
		new SpanSplitEnumerator<char>(str, separator)) { }

	public StrReader(ReadOnlySpan<char> str, SpanSplitEnumerator<char> enumerator) {
		this.str = str;
		this.enumerator = enumerator;
	}

	public ReadOnlySpan<char> GetString() =>
		enumerator.MoveNext()
			? str[enumerator.Current]
			: default;

	public T ReadAndParse<T>(IXmlFormatterResolver resolver) => resolver.Parse<T>(GetString());

	public IReadOnlyList<string> ReadToEnd() {
		var lst = new List<string>();
		while (HasRemaining()) {
			var span = GetString();
			lst.Add(span.ToString());
		}

		return lst;
	}

	public bool HasRemaining() => enumerator.CanMoveNext();
}
