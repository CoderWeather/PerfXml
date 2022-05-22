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

	public ReadOnlySpan<char> GetString() {
		return enumerator.MoveNext()
			? str[enumerator.Current]
			: default;
	}

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

	// public static bool InterpretBool(ReadOnlySpan<char> val) {
	// 	switch (val[0]) {
	// 		case '0': return false;
	// 		case '1': return true;
	// 	}
	//
	// 	if (val.StartsWith("no", StringComparison.InvariantCultureIgnoreCase))
	// 		return false;
	// 	if (val.StartsWith("yes", StringComparison.InvariantCultureIgnoreCase))
	// 		return true;
	//
	// 	if (val.StartsWith("false", StringComparison.InvariantCultureIgnoreCase))
	// 		return false;
	// 	if (val.StartsWith("true", StringComparison.InvariantCultureIgnoreCase))
	// 		return true;
	//
	// 	throw new InvalidDataException($"unknown boolean \"{val.ToString()}\"");
	// }
}
