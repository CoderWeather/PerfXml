using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PerfXml;

/// <summary>Stack based XML serializer</summary>
public ref struct XmlWriteBuffer {
	/// <summary>Internal char buffer</summary>
	private char[] buffer;

	/// <summary>Span over the internal char buffer</summary>
	private Span<char> bufferSpan;

	/// <summary>Current write offset within <see cref="buffer"/></summary>
	private int currentOffset;

	/// <summary>Whether or not a node head is currently open (&gt; hasn't been written)</summary>
	private bool pendingNodeHeadClose;

	/// <summary>Type of text blocks to serialize</summary>
	public CDataMode CdataMode;

	/// <summary>Span representing the tail of the internal buffer</summary>
	private Span<char> WriteSpan => bufferSpan[currentOffset..];

	/// <summary>
	/// Create a new XmlWriteBuffer
	/// </summary>
	/// <returns>XmlWriteBuffer instance</returns>
	public static XmlWriteBuffer Create() => new(0);

	/// <summary>
	/// Actual XmlWriteBuffer constructor
	/// </summary>
	/// <param name="_">blank parameter</param>
	// ReSharper disable once UnusedParameter.Local
	private XmlWriteBuffer(int _ = 0) {
		pendingNodeHeadClose = false;
		buffer = ArrayPool<char>.Shared.Rent(1024);
		bufferSpan = buffer;
		currentOffset = 0;

		CdataMode = CDataMode.On;
	}

	/// <summary>Resize internal char buffer (<see cref="buffer"/>)</summary>
	private void Resize() {
		var newBuffer = ArrayPool<char>.Shared.Rent(buffer.Length * 2); // double size
		var newBufferSpan = new Span<char>(newBuffer);

		var usedBufferSpan = bufferSpan[..currentOffset];
		usedBufferSpan.CopyTo(newBufferSpan);

		ArrayPool<char>.Shared.Return(buffer);
		buffer = newBuffer;
		bufferSpan = newBufferSpan;
	}

	/// <summary>Record of a node that is currently being written into the buffer</summary>
	public readonly ref struct NodeRecord {
		public readonly ReadOnlySpan<char> Name;

		public NodeRecord(ReadOnlySpan<char> name) {
			Name = name;
		}
	}

	/// <summary>
	/// Puts a "&gt;" character to signify the end of the current node head ("&lt;name&gt;") if it hasn't been already done
	/// </summary>
	private void CloseNodeHeadForBodyIfOpen() {
		if (pendingNodeHeadClose is false)
			return;
		PutChar('>');
		pendingNodeHeadClose = false;
	}

	/// <summary>Start an XML node</summary>
	/// <param name="name">Name of the node</param>
	/// <returns>Record describing the node</returns>
	public NodeRecord StartNodeHead(ReadOnlySpan<char> name) {
		CloseNodeHeadForBodyIfOpen();

		PutChar('<');
		PutString(name);
		pendingNodeHeadClose = true;
		return new(name);
	}

	/// <summary>End an XML node</summary>
	/// <param name="record">Record describing the open node</param>
	public void EndNode(ref NodeRecord record) {
		if (pendingNodeHeadClose is false) {
			PutString("</");
			PutString(record.Name);
			PutChar('>');
		}
		else {
			PutString("/>");
			pendingNodeHeadClose = false;
		}
	}

	/// <summary>Escape and put text into the buffer</summary>
	/// <param name="text">The raw text to write</param>
	public void PutCData(ReadOnlySpan<char> text) {
		CloseNodeHeadForBodyIfOpen();
		if (CdataMode == CDataMode.Off) {
			EncodeText(text);
		}
		else {
			PutString(XmlReadBuffer.CDataStart);
			if (CdataMode == CDataMode.OnEncode)
				EncodeText(text);
			else
				PutString(text); // CDataMode.On
			PutString(XmlReadBuffer.CDataEnd);
		}
	}

	public void PutAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value) {
		StartAttrCommon(name);
		EncodeText(value, true);
		EndAttrCommon();
	}

	public void PutAttributeValue<T>(ReadOnlySpan<char> name, T value)
		where T : struct {
		StartAttrCommon(name);
		PutValue(value);
		EndAttrCommon();
	}

	public void PutAttributeInt(ReadOnlySpan<char> name, int value) {
		StartAttrCommon(name);
		PutInt(value);
		EndAttrCommon();
	}

	public void PutAttributeUInt(ReadOnlySpan<char> name, uint value) {
		StartAttrCommon(name);
		PutUInt(value);
		EndAttrCommon();
	}

	public void PutAttributeLong(ReadOnlySpan<char> name, long value) {
		StartAttrCommon(name);
		PutLong(value);
		EndAttrCommon();
	}

	public void PutAttributeDouble(ReadOnlySpan<char> name, double value) {
		StartAttrCommon(name);
		PutDouble(value);
		EndAttrCommon();
	}

	public void PutAttributeBoolean(ReadOnlySpan<char> name, bool value) {
		StartAttrCommon(name);
		PutChar(value ? '1' : '0');
		EndAttrCommon();
	}

	public void PutAttributeByte(ReadOnlySpan<char> name, byte value) {
		StartAttrCommon(name);
		PutUInt(value); // todo: hmm
		EndAttrCommon();
	}

	public void PutAttributeGuid(ReadOnlySpan<char> name, Guid value) {
		StartAttrCommon(name);
		PutString(value.ToString());
		EndAttrCommon();
	}

	/// <summary>Write the starting characters for an attribute (" name=''")</summary>
	/// <param name="name">Name of the attribute</param>
	private void StartAttrCommon(ReadOnlySpan<char> name) {
		Debug.Assert(pendingNodeHeadClose);
		PutChar(' ');
		PutString(name);
		PutString("='");
	}

	/// <summary>End an attribute</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // don't bother calling this
	private void EndAttrCommon() {
		PutChar('\'');
	}

	public void PutValue<T>(T value)
		where T : struct {
		CloseNodeHeadForBodyIfOpen();
		switch (value) {
			case int i:
				PutInt(i);
				break;
			case uint i:
				PutUInt(i);
				break;
			case long l:
				PutLong(l);
				break;
			case double d:
				PutDouble(d);
				break;
			case decimal d:
				PutDecimal(d);
				break;
			case char c:
				PutChar(c);
				break;
			case Guid g:
				PutGuid(g);
				break;
			case bool b:
				PutChar(b ? '1' : '0');
				break;
			default:
				PutString(value.ToString());
				break;
		}
	}

	/// <summary>Format a <see cref="Int32"/> into the buffer as text</summary>
	/// <param name="value">The value to write</param>
	public void PutInt(int value) {
		int charsWritten;
		while (value.TryFormat(WriteSpan, out charsWritten, default, CultureInfo.InvariantCulture) is false) {
			Resize();
		}

		currentOffset += charsWritten;
	}

	public void PutLong(long value) {
		int charsWritten;
		while (value.TryFormat(WriteSpan, out charsWritten, default, CultureInfo.InvariantCulture) is false) {
			Resize();
		}

		currentOffset += charsWritten;
	}

	/// <summary>Format a <see cref="UInt32"/> into the buffer as text</summary>
	/// <param name="value">The value to write</param>
	public void PutUInt(uint value) {
		int charsWritten;
		while (value.TryFormat(WriteSpan, out charsWritten, default, CultureInfo.InvariantCulture) is false) {
			Resize();
		}

		currentOffset += charsWritten;
	}

	/// <summary>Format a <see cref="Double"/> into the buffer as text</summary>
	/// <param name="value">The value to write</param>
	public void PutDouble(double value) {
		int charsWritten;
		while (value.TryFormat(WriteSpan, out charsWritten, default, CultureInfo.InvariantCulture) is false) {
			Resize();
		}

		currentOffset += charsWritten;
	}

	public void PutDecimal(decimal value) {
		int charsWritten;
		while (value.TryFormat(WriteSpan, out charsWritten) is false) {
			Resize();
		}

		currentOffset += charsWritten;
	}

	public void PutGuid(Guid value) {
		int charsWritten;
		while (value.TryFormat(WriteSpan, out charsWritten) is false) {
			Resize();
		}

		currentOffset += charsWritten;
	}

	/// <summary>Put a raw <see cref="ReadOnlySpan{T}"/> into the buffer</summary>
	/// <param name="chars">The span of text to write</param>
	public void PutString(ReadOnlySpan<char> chars) {
		if (chars.Length == 0)
			return;

		while (chars.TryCopyTo(WriteSpan) is false)
			Resize();
		currentOffset += chars.Length;
	}

	/// <summary>Put a raw <see cref="Char"/> into the buffer</summary>
	/// <param name="c">The character to write</param>
	public void PutChar(char c) {
		if (WriteSpan.Length == 0)
			Resize();

		WriteSpan[0] = c;
		currentOffset++;
	}

	/// <summary>
	/// Get <see cref="ReadOnlySpan{Char}"/> of used portion of the internal buffer containing serialized XML data
	/// </summary>
	/// <returns>Serialized XML data</returns>
	public ReadOnlySpan<char> ToSpan() {
		var fullSpan = new ReadOnlySpan<char>(buffer, 0, currentOffset);
		return fullSpan;
	}

	/// <summary>Release internal buffer</summary>
	public void Dispose() {
		if (buffer is not null) {
			var b = buffer;
			ArrayPool<char>.Shared.Return(b);
		}
	}

	/// <summary>
	/// Serialize a baseclass of <see cref="IXmlSerialization"/> to XML text
	/// </summary>
	/// <param name="obj">The object to serialize</param>
	/// <param name="cdataMode">Should text be written as CDATA</param>
	/// <returns>Serialized XML</returns>
	public static ReadOnlySpan<char> SerializeStatic<T>(T obj, CDataMode cdataMode = CDataMode.Off)
		where T : IXmlSerialization {
		if (obj == null)
			throw new ArgumentNullException(nameof(obj));
		var writer = new XmlWriteBuffer {
			CdataMode = cdataMode
		};
		try {
			obj.Serialize(ref writer);
			var span = writer.ToSpan();
			Span<char> result = new char[span.Length];
			span.CopyTo(result);
			return result;
		}
		finally {
			writer.Dispose();
		}
	}

	public static void SerializeStatic<T>(T obj,
		Span<char> span,
		out int writtenChars,
		CDataMode cdataMode = CDataMode.Off)
		where T : IXmlSerialization {
		if (obj == null)
			throw new ArgumentNullException(nameof(obj));
		var writer = new XmlWriteBuffer {
			CdataMode = cdataMode
		};
		try {
			obj.Serialize(ref writer);
			var resultSpan = writer.ToSpan();
			resultSpan.CopyTo(span);
			writtenChars = resultSpan.Length;
		}
		finally {
			writer.Dispose();
		}
	}


	private static readonly char[] EscapeChars = {
		'<', '>', '&'
	};

	private static readonly char[] EscapeCharsAttribute = {
		'<', '>', '&', '\'', '\"', '\n', '\r', '\t'
	};

	/// <summary>Encode unescaped text into the buffer</summary>
	/// <param name="input">Unescaped text</param>
	/// <param name="attribute">True if text is for an attribute, false for an element</param>
	public void EncodeText(ReadOnlySpan<char> input, bool attribute = false) {
		var escapeChars = new ReadOnlySpan<char>(attribute ? EscapeCharsAttribute : EscapeChars);

		var currentInput = input;
		while (true) {
			var escapeCharIdx = currentInput.IndexOfAny(escapeChars);
			if (escapeCharIdx == -1) {
				PutString(currentInput);
				return;
			}

			PutString(currentInput[..escapeCharIdx]);

			var charToEncode = currentInput[escapeCharIdx];
			PutString(charToEncode switch {
				'<'  => "&lt;",
				'>'  => "&gt;",
				'&'  => "&amp;",
				'\'' => "&apos;",
				'\"' => "&quot;",
				'\n' => "&#xA;",
				'\r' => "&#xD;",
				'\t' => "&#x9;",
				_    => throw new($"unknown escape char \"{charToEncode}\". how did we get here")
			});
			currentInput = currentInput[(escapeCharIdx + 1)..];
		}
	}
}
