/*
using System.ComponentModel;

namespace PerfXml.Str;

public readonly ref struct SpanStr {
	private readonly ReadOnlySpan<char> data;
	private readonly string? str;

	public int Length => str?.Length ?? data.Length;

	public SpanStr(ReadOnlySpan<char> data) {
		this.data = data;
		str = null;
	}

	public SpanStr(string str) {
		this.str = str;
		data = default;
	}

	public bool Contains(char c) {
		return ((ReadOnlySpan<char>)this).IndexOf(c) != -1;
	}

	public static bool operator ==(SpanStr left, SpanStr right) {
		return ((ReadOnlySpan<char>)left).SequenceEqual(right); // turn both into spans
	}

	public static bool operator !=(SpanStr left, SpanStr right) {
		return !(left == right);
	}

	public static bool operator ==(SpanStr left, string right) {
		return ((ReadOnlySpan<char>)left).SequenceEqual(right);
	}

	public static bool operator !=(SpanStr left, string right) {
		return !(left == right);
	}

	public char this[int index] => ((ReadOnlySpan<char>)this)[index];

	public static implicit operator ReadOnlySpan<char>(SpanStr str) {
		return str.str is not null
			? str.str.AsSpan()
			: str.data;
	}

	// explicit to prevent accidental allocs
	public static explicit operator string(SpanStr str) {
		return str.ToString();
	}

	public override string ToString() {
		return str ?? data.ToString();
	}

	/// <summary>
	/// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
	/// <exception cref="System.NotSupportedException">
	/// Always thrown by this method.
	/// </exception>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool Equals(object? obj) {
		throw new NotSupportedException();
	}

	/// <summary>
	/// This method is not supported as spans cannot be boxed.
	/// <exception cref="System.NotSupportedException">
	/// Always thrown by this method.
	/// </exception>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override int GetHashCode() {
		throw new NotSupportedException();
	}
}
*/



