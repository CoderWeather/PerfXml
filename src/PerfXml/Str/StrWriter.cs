using System.Buffers;
using System.Diagnostics;
using System.Globalization;

namespace PerfXml.Str;

public ref struct StrWriter {
	public readonly Span<char> Buffer;
	public readonly char Separator;
	public readonly bool SeparatorAtEnd;

	private char[]? backing;
	private int currIdx;
	private bool isFirst;

	public ReadOnlySpan<char> BuiltSpan => Buffer[..currIdx];

	public const int MaxSize = 256;

	public StrWriter(char separator) {
		backing = ArrayPool<char>.Shared.Rent(MaxSize);
		Buffer = new(backing);

		currIdx = 0;
		isFirst = true;

		Separator = separator;
		SeparatorAtEnd = false;
	}

	private void AssertWriteable() {
		if (backing == null) {
			throw new ObjectDisposedException("StrWriter");
		}
	}

	private void PutSeparator() {
		if (isFirst) {
			isFirst = false;
		}
		else {
			PutRaw(Separator);
		}
	}

	public void Write<T>(T value, IXmlFormatterResolver resolver) {
		PutSeparator();
		if (resolver.TryWriteTo(Buffer, value, out var charsWritten)) {
			currIdx += charsWritten;
		}
		else {
			throw new InvalidOperationException();
		}
	}

	public void PutRaw(char c) {
		AssertWriteable();
		Buffer[currIdx++] = c;
	}

	public void PutRaw(ReadOnlySpan<char> str) {
		AssertWriteable();
		str.CopyTo(Buffer[currIdx..]);
		currIdx += str.Length;
	}

	public void Finish(bool terminate) {
		if (SeparatorAtEnd) {
			PutRaw(Separator);
		}

		if (terminate) {
			PutRaw('\0');
		}
	}

	public ReadOnlySpan<char> AsSpan(bool terminate = false) {
		AssertWriteable();

		Finish(terminate);
		return BuiltSpan;
	}

	public override string ToString() {
		AssertWriteable();

		Finish(false);
		var str = BuiltSpan.ToString();
		Dispose();
		return str;
	}


	public void Dispose() {
		if (backing is not null) {
			ArrayPool<char>.Shared.Return(backing);
			backing = null;
		}
	}
}
