using System.Diagnostics;
using System.Net;

namespace PerfXml;

public delegate bool UnsafeReadAction(Span<char> span, out uint charsWritten);

/// <summary>Stack based XML deserializer</summary>
public ref struct XmlReadBuffer {
	internal const string CommentStart = "<!--";
	internal const string CommentEnd = "-->";
	internal const string DeclarationEnd = "?>";
	internal const string CDataStart = "<![CDATA[";
	internal const string CDataEnd = "]]>";

	// /// <summary>Abort parsing immediately</summary>
	// private bool abort;

	// /// <summary>Type of text blocks to deserialize</summary>
	// private CDataMode cdataMode;

	/// <summary>Current depth of calls to <see cref="ReadInto{T}"/></summary>
	private int depth;

	/// <summary>
	/// Maximum depth depth that calls to <see cref="ReadInto{T}"/> can happen before an exception will be thrown to
	/// protect the application
	/// </summary>
	private const int MaxDepth = 50;

	/// <summary>
	/// Parses XML node attributes
	/// </summary>
	/// <param name="currSpan">Text span</param>
	/// <param name="closeBraceIdx">Index in <param name="currSpan"/> which is at the end of the node declaration</param>
	/// <param name="position">Starting position within <param name="currSpan"/></param>
	/// <param name="obj">Object to receive parsed data</param>
	/// <param name="resolver"></param>
	/// <exception cref="InvalidDataException">Unable to parse data</exception>
	/// <returns>Position within <param name="currSpan"/> which is at the end of the attribute list</returns>
	private int DeserializeAttributes<T>(ReadOnlySpan<char> currSpan,
		int closeBraceIdx,
		int position,
		T obj,
		IXmlFormatterResolver resolver
	) where T : IXmlSerialization {
		while (position < closeBraceIdx) {
			var spaceSpan = currSpan.Slice(position, closeBraceIdx - position);
			if (spaceSpan[0] == ' ') {
				position++;
				continue;
			}

			var eqIdx = spaceSpan.IndexOf('=');
			if (eqIdx == -1) {
				break;
			}

			var attributeName = spaceSpan[..eqIdx];

			var quoteType = spaceSpan[eqIdx + 1];
			if (quoteType != '\'' && quoteType != '\"') {
				throw new InvalidDataException($"invalid quote char {quoteType}");
			}

			var attributeValueSpan = spaceSpan[(eqIdx + 2)..];
			var quoteEndIdx = attributeValueSpan.IndexOf(quoteType);
			if (quoteEndIdx == -1) {
				throw new InvalidDataException("unable to find pair end quote");
			}

			var attributeValue = attributeValueSpan[..quoteEndIdx];
			var attributeValueDecoded = DecodeText(attributeValue);

			var nameHash = IXmlSerialization.HashName(attributeName);
			var assigned = obj.ParseAttribute(ref this, nameHash, attributeValueDecoded, resolver);
			// if (abort)
			// 	return -1;
			if (!assigned) {
				Debug.Print("[XmlReadBuffer]: unhandled attribute {0} on {1}. \"{2}\"",
					attributeName.ToString(),
					obj.GetType(),
					attributeValue.ToString());
			}

			position += attributeName.Length + attributeValue.Length + 2 + 1; // ='' -- 3 chars
		}

		return position;
	}

	/// <summary>Parse an XML node and children into structured class <param name="obj"/></summary>
	/// <param name="span">Text to parse</param>
	/// <param name="obj">Object to receive parsed data</param>
	/// <param name="resolver"></param>
	/// <returns>Position within <param name="span"/> that the node ends at</returns>
	/// <exception cref="InvalidDataException">Unable to parse data</exception>
	/// <exception cref="Exception">Internal error</exception>
	private int ReadInto<T>(ReadOnlySpan<char> span, T obj, IXmlFormatterResolver? resolver = null)
		where T : IXmlSerialization {
		resolver ??= Xml.DefaultResolver;
		depth++;
		if (depth >= MaxDepth) {
			throw new($"maximum depth {MaxDepth} reached");
		}

		var primary = true;
		for (var i = 0; i < span.Length;) {
			var currSpan = span[i..];

			if (currSpan[0] != '<') {
				var idxOfAngleBracket = currSpan.IndexOf('<');
				if (idxOfAngleBracket == -1) {
					break;
				}

				i += idxOfAngleBracket;
				continue;
			}

			if (currSpan.Length > 1) {
				switch (currSpan[1]) {
					// no need to check length here.. name has to be at least 1 char lol
					case '/':
						// current block has ended
						depth--;
						return i + 2; // todo: hmm. this make caller responsible for aligning again
					case '?': {
						// skip xml declaration
						// e.g <?xml version='1.0'?>

						var declarationEnd = currSpan.IndexOf(DeclarationEnd);
						if (declarationEnd == -1) {
							throw new InvalidDataException("where is declaration end");
						}

						i += declarationEnd + DeclarationEnd.Length;
						continue;
					}
				}

				if (currSpan.StartsWith(CommentStart)) {
					var commentEnd = currSpan.IndexOf(CommentEnd);
					if (commentEnd == -1) {
						throw new InvalidDataException("where is comment end");
					}

					i += commentEnd + CommentEnd.Length;
					continue;
				}

				if (currSpan[1] == '!') {
					throw new("xml data type definitions are not supported");
				}
			}

			var closeBraceIdx = currSpan.IndexOf('>');
			var spaceIdx = currSpan.IndexOf(' ');
			if (spaceIdx > closeBraceIdx) {
				spaceIdx = -1; // we are looking for a space in the node declaration
			}

			var nameEndIdx = Math.Min(closeBraceIdx, spaceIdx);
			if (nameEndIdx == -1) {
				nameEndIdx = closeBraceIdx; // todo min of 1 and -1 is -1
			}

			if (nameEndIdx == -1) {
				throw new InvalidDataException("unable to find end of node name");
			}

			var noBody = false;
			if (currSpan[nameEndIdx - 1] == '/') {
				// <lightning/>
				noBody = true;
				nameEndIdx -= 1;
			}

			var nodeName = currSpan.Slice(1, nameEndIdx - 1);

			const int unassignedIdx = int.MinValue;

			if (primary) {
				// read actual node

				int afterAttrs;

				if (spaceIdx != -1) {
					afterAttrs = spaceIdx + 1; // skip space
					afterAttrs = DeserializeAttributes(currSpan, closeBraceIdx, afterAttrs, obj, resolver);
					// if (abort) {
					// 	depth--;
					// 	return -1;
					// }
				}
				else {
					afterAttrs = closeBraceIdx;
				}

				var afterAttrsChar = currSpan[afterAttrs];

				if (noBody || afterAttrsChar == '/') {
					// no body
					depth--;
					return i + closeBraceIdx + 1;
				}

				primary = false;

				if (afterAttrsChar != '>') {
					throw new InvalidDataException(
						"char after attributes should have been the end of the node, but it isn't");
				}

				var bodySpan = currSpan[(closeBraceIdx + 1)..];

				var endIdx = unassignedIdx;

				var handled = obj.ParseFullBody(ref this, bodySpan, ref endIdx, resolver);
				// if (abort) {
				// 	depth--;
				// 	return -1;
				// }

				if (handled) {
					if (endIdx == unassignedIdx) {
						throw new("endIdx should be set if returning true from ParseFullBody");
					}

					var fullSpanIdx = afterAttrs + 1 + endIdx;

					// should be </nodeName>
					if (currSpan[fullSpanIdx] != '<'
					 || currSpan[fullSpanIdx + 1] != '/'
					 || !currSpan.Slice(fullSpanIdx + 2, nodeName.Length).SequenceEqual(nodeName)
					 || currSpan[fullSpanIdx + 2 + nodeName.Length] != '>') {
						throw new InvalidDataException("Unexpected data after handling full body");
					}

					i += fullSpanIdx + 2 + nodeName.Length;
					continue;
				}

				i += closeBraceIdx + 1;
			}
			else {
				// read child nodes

				var endIdx = unassignedIdx;
				var endInnerIdx = unassignedIdx;

				var innerBodySpan = currSpan[(closeBraceIdx + 1)..];
				var nodeNameHash = IXmlSerialization.HashName(nodeName);
				var parsedSub = obj.ParseSubBody(ref this,
					nodeNameHash,
					currSpan,
					innerBodySpan,
					ref endIdx,
					ref endInnerIdx,
					resolver);
				if (parsedSub is false) {
					parsedSub = obj.ParseSubBody(ref this,
						nodeName,
						currSpan,
						innerBodySpan,
						ref endIdx,
						ref endInnerIdx,
						resolver);
				}

				if (parsedSub is false) {
					// Full node skip if not found member to read
					var i1 = 0;
					var i2 = 1;

					while (i1 < innerBodySpan.Length) {
						var ch1 = innerBodySpan[i1];
						var ch2 = innerBodySpan[i2];
						if (ch1 is '<' && ch2 is '/') {
							// (1 + nodeName.Length + 1) = "/name>"
							endInnerIdx = i + 1 + nodeName.Length + 1;
							break;
						}

						i1++;
						i2++;
					}
				}

				if (endIdx != unassignedIdx) {
					i += endIdx;
					continue;
				}

				if (endInnerIdx != unassignedIdx) {
					// (3 + nodeName.Length) = "</name>"
					i += closeBraceIdx + 1 + endInnerIdx + 3 + nodeName.Length;
					continue;
				}

				throw new("one of endIdx or endInnerIdx should be set if returning true from ParseSubBody");

				// throw new InvalidDataException($"Unknown sub body {nodeName.ToString()} on {obj.GetType()}");
			}
		}

		depth--;
		return span.Length;
	}

	public T Deserialize<T>(ReadOnlySpan<char> span, IXmlFormatterResolver resolver) {
		var f = resolver.GetRequiredFormatter<T>();
		return f.Parse(span, resolver);
	}

	private static ReadOnlySpan<char> DeserializeElementRawInnerText(ReadOnlySpan<char> span, out int endEndIdx) {
		endEndIdx = span.IndexOf('<'); // find start of next node
		if (endEndIdx == -1) {
			throw new InvalidDataException("unable to find end of text");
		}

		var textSlice = span[..endEndIdx];
		return DecodeText(textSlice);
	}

	/// <summary>Decode XML encoded text</summary>
	/// <param name="input"></param>
	/// <returns>Decoded text</returns>
	private static ReadOnlySpan<char> DecodeText(ReadOnlySpan<char> input) {
		var andIndex = input.IndexOf('&');
		if (andIndex == -1)
			// no need to decode :)
		{
			return input;
		}

		return WebUtility.HtmlDecode(input.ToString()); // todo: allocates input as string, gross
	}

	/*/// <summary>
	/// Deserialize XML element inner text. Switches between CDATA and raw text on <see cref="cdataMode"/>
	/// </summary>
	/// <param name="span">Span at the beginning of the element's inner text</param>
	/// <param name="endEndIdx">The index of the end of the text within <see cref="span"/></param>
	/// <returns>Deserialized inner text data</returns>
	/// <exception cref="InvalidDataException">The bounds of the text could not be determined</exception>
	public ReadOnlySpan<char> DeserializeString(ReadOnlySpan<char> span, out int endEndIdx) {
		if (cdataMode == CDataMode.Off)
			return DeserializeElementRawInnerText(span, out endEndIdx);
		// todo: CDATA cannot contain the string "]]>" anywhere in the XML document.

		if (!span.StartsWith(CDataStart))
			throw new InvalidDataException("invalid cdata start");

		var endIdx = span.IndexOf(CDataEnd);
		if (endIdx == -1)
			throw new InvalidDataException("unable to find end of cdata");

		endEndIdx = CDataEnd.Length + endIdx;

		var stringData = span.Slice(CDataStart.Length, endIdx - CDataStart.Length);
		return cdataMode == CDataMode.OnEncode
			? DecodeText(stringData)
			: stringData;
	}*/

	public ReadOnlySpan<char> ReadNodeValue(ReadOnlySpan<char> span, out int endEndIdx) {
		endEndIdx = span.IndexOf('<'); // find start of next node
		if (endEndIdx is -1) {
			throw new InvalidDataException("unable to find end of node value");
		}

		var slice = span[..endEndIdx];
		return slice;
	}

	/// <summary>
	/// Create a new instance of <typeparam name="T"/> and parse into it
	/// </summary>
	/// <param name="span">Text to parse</param>
	/// <param name="end">Index into <param name="span"/> that is at the end of the node</param>
	/// <param name="resolver"></param>
	/// <typeparam name="T">Type to parse</typeparam>
	/// <returns>The created instance</returns>
	public T Read<T>(ReadOnlySpan<char> span, out int end, IXmlFormatterResolver? resolver = null)
		where T : IXmlSerialization, new() {
		var t = new T();
		end = ReadInto(span, t);
		return t;
	}

	/// <summary>
	/// The same as <see cref="Read{T}(System.ReadOnlySpan{char},out int,PerfXml.IXmlFormatterResolver?)"/> but without the `end` out parameter
	/// </summary>
	/// <param name="span">Text to parse</param>
	/// <param name="resolver"></param>
	/// <typeparam name="T">Type to parse</typeparam>
	/// <returns>The created instance</returns>
	public T Read<T>(ReadOnlySpan<char> span, IXmlFormatterResolver? resolver = null)
		where T : IXmlSerialization, new() =>
		Read<T>(span, out _);
}
