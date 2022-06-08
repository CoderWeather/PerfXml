namespace PerfXml.Formatters;

public sealed class EnumFormatter<T> : IXmlFormatter<T>
	where T : struct, Enum {
	public static readonly EnumFormatter<T> Instance = new();

	public bool TryWriteTo(Span<char> span, T value, out int charsWritten, IXmlFormatterResolver resolver) {
		var name = EnumCache.GetName(value).AsSpan();
		charsWritten = name.Length;
		return name.TryCopyTo(span);
	}

	public T Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) => EnumCache.ByName<T>(span.ToString());
}
