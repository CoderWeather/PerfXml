﻿namespace PerfXml.Formatters;

public sealed class DateOnlyFormatter : IXmlFormatter<DateOnly> {
	private readonly string format;
	public DateOnlyFormatter(string format = "yyyy-MM-dd") => this.format = format;
	public static readonly DateOnlyFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, DateOnly value, out int charsWritten, IXmlFormatterResolver resolver) {
		return value.TryFormat(span, out charsWritten, format);
	}

	public DateOnly Parse(ReadOnlySpan<char> span, IXmlFormatterResolver formatterResolver) {
		return DateOnly.ParseExact(span, format);
	}
}

public sealed class TimeOnlyFormatter : IXmlFormatter<TimeOnly> {
	private readonly string format;
	public TimeOnlyFormatter(string format = "HH:mm:ss") => this.format = format;
	public static readonly TimeOnlyFormatter Instance = new();

	public bool TryWriteTo(Span<char> span, TimeOnly value, out int charsWritten, IXmlFormatterResolver resolver) {
		return value.TryFormat(span, out charsWritten, format);
	}

	public TimeOnly Parse(ReadOnlySpan<char> span, IXmlFormatterResolver formatterResolver) {
		return TimeOnly.ParseExact(span, format);
	}
}
