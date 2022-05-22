namespace PerfXml.Formatters;

public sealed class ByteFormatter : IXmlFormatter<byte> {
	private ByteFormatter() { }
	public static readonly ByteFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, byte value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public byte Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class Int16Formatter : IXmlFormatter<short> {
	private Int16Formatter() { }
	public static readonly Int16Formatter Instance = new();

	public bool TryWriteTo(Span<char> span, short value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public short Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class Int32Formatter : IXmlFormatter<int> {
	private Int32Formatter() { }
	public static readonly Int32Formatter Instance = new();

	public bool TryWriteTo(Span<char> span, int value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public int Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class UInt32Formatter : IXmlFormatter<uint> {
	private UInt32Formatter() { }
	public static readonly UInt32Formatter Instance = new();

	public bool TryWriteTo(Span<char> span, uint value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public uint Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class Int64Formatter : IXmlFormatter<long> {
	private Int64Formatter() { }
	public static readonly Int64Formatter Instance = new();

	public bool TryWriteTo(Span<char> span, long value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public long Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class DoubleFormatter : IXmlFormatter<double> {
	private DoubleFormatter() { }
	public static readonly DoubleFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, double value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> Serialize(double value, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public double Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class DecimalFormatter : IXmlFormatter<decimal> {
	private DecimalFormatter() { }
	public static readonly DecimalFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, decimal value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> Serialize(decimal value, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public decimal Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class StringFormatter : IXmlFormatter<string?> {
	private StringFormatter() { }
	public static readonly StringFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, string? value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> Serialize(string? value, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public string? Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class CharFormatter : IXmlFormatter<char> {
	private CharFormatter() { }
	public static readonly CharFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, char value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public char Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class BooleanFormatter : IXmlFormatter<bool> {
	private BooleanFormatter() { }
	public static readonly BooleanFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, bool value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public bool Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class GuidFormatter : IXmlFormatter<Guid> {
	private GuidFormatter() { }
	public static readonly GuidFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, Guid value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public Guid Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}

public sealed class DateTimeFormatter : IXmlFormatter<DateTime> {
	private DateTimeFormatter() { }
	public static readonly DateTimeFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, DateTime value, out int charsWritten, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}

	public DateTime Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		throw new NotImplementedException();
	}
}
