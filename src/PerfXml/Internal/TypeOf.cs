namespace PerfXml.Internal;

public abstract record TypeOf {
	public abstract Type Type { get; }

	public static TypeOf Get<T>() => Cache<T>.Instance;

	private static class Cache<T> {
		public static readonly TypeOf Instance = new TypeOfImplementation<T>();
	}

	private sealed record TypeOfImplementation<T> : TypeOf {
		public override Type Type { get; } = typeof(T);
	}
}
