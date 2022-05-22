namespace PerfXml.Formatters;

public sealed class NullableStructFormatter<T> : IXmlFormatter<T?>
	where T : struct {
	public static readonly NullableStructFormatter<T> Instance = new();

	public bool TryWriteTo(Span<char> span, T? value, out int charsWritten, IXmlFormatterResolver resolver) {
		if (value.HasValue) {
			return resolver.TryWriteTo(span, value.Value, out charsWritten);
		}

		charsWritten = 0;
		return true;
	}

	public T? Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		if (span.Length > 0) {
			var f = resolver.GetRequiredFormatter<T>();
			return f.Parse(span, resolver);
		}

		return default;
	}
}
