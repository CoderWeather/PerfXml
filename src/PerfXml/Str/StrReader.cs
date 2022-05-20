using System.Globalization;

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

	public SpanStr GetSpanString() {
		if (enumerator.MoveNext() is false)
			return default;
		return new(str[enumerator.Current]);
	}

	public byte GetByte() {
		var span = GetString();
		return ParseByte(span);
	}

	public int GetInt() {
		var span = GetString();
		return ParseInt(span);
	}

	public uint GetUInt() {
		var span = GetString();
		return ParseUInt(span);
	}

	public long GetLong() {
		var span = GetString();
		return ParseLong(span);
	}

	public double GetDouble() {
		var span = GetString();
		return ParseDouble(span);
	}

	public decimal GetDecimal() {
		var span = GetString();
		return ParseDecimal(span);
	}

	public char GetChar() {
		var span = GetString();
		return ParseChar(span);
	}

	public bool GetBoolean() {
		var span = GetString();
		return InterpretBool(span);
	}

	public Guid GetGuid() {
		var span = GetString();
		return ParseGuid(span);
	}

	public IReadOnlyList<string> ReadToEnd() {
		var lst = new List<string>();
		while (HasRemaining()) {
			var span = GetString();
			lst.Add(span.ToString());
		}

		return lst;
	}

	public bool HasRemaining() {
		return enumerator.CanMoveNext();
	}

	public static bool InterpretBool(ReadOnlySpan<char> val) {
		switch (val[0]) {
			case '0': return false;
			case '1': return true;
		}

		if (val.StartsWith("no", StringComparison.InvariantCultureIgnoreCase))
			return false;
		if (val.StartsWith("yes", StringComparison.InvariantCultureIgnoreCase))
			return true;

		if (val.StartsWith("false", StringComparison.InvariantCultureIgnoreCase))
			return false;
		if (val.StartsWith("true", StringComparison.InvariantCultureIgnoreCase))
			return true;

		throw new InvalidDataException($"unknown boolean \"{val.ToString()}\"");
	}

	public static int ParseInt(ReadOnlySpan<char> span) {
		return int.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture);
	}

	public static uint ParseUInt(ReadOnlySpan<char> span) {
		return uint.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture);
	}

	public static long ParseLong(ReadOnlySpan<char> span) {
		return long.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture);
	}

	public static byte ParseByte(ReadOnlySpan<char> span) {
		return span.Length == 0
			? default
			: byte.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture);
	}

	public static double ParseDouble(ReadOnlySpan<char> span) {
		return double.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
	}

	public static decimal ParseDecimal(ReadOnlySpan<char> span) {
		return decimal.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
	}

	public static char ParseChar(ReadOnlySpan<char> span) {
		return char.Parse(span.ToString());
	}

	public static Guid ParseGuid(ReadOnlySpan<char> span) {
		return Guid.Parse(span);
	}
}
