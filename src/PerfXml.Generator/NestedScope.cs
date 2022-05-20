namespace PerfXml.Generator;

internal sealed class NestedScope : IDisposable {
	private NestedScope(IndentedTextWriter writer) => this.writer = writer;
	private readonly IndentedTextWriter writer;
	private static readonly Stack<NestedScope> Stack = new();
	private bool shouldCloseOnDispose = true;

	public void Dispose() {
		if (shouldCloseOnDispose) {
			Close();
		}

		Stack.Pop();
	}

	public void Close() {
		writer.Indent--;
		writer.WriteLine('}');
		shouldCloseOnDispose = false;
	}

	public static void CloseLast() {
		var scope = Stack.Peek();
		scope.Close();
	}

	public static NestedScope Start(IndentedTextWriter writer, string? scopeName = null) {
		var scope = new NestedScope(writer);
		Stack.Push(scope);
		if (scopeName is not null) {
			writer.WriteLine(scopeName);
		}

		writer.WriteLine('{');
		writer.Indent++;
		return scope;
	}
}

/// <summary>Helper class for generating partial parts of nested types</summary>
internal sealed class NestedClassScope : IDisposable {
	private readonly List<string> containingClasses = new(0);
	private readonly IndentedTextWriter writer;

	private NestedClassScope(IndentedTextWriter writer, ISymbol classSymbol) {
		this.writer = writer;
		var containingSymbol = classSymbol.ContainingSymbol;
		while (containingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default) is false) {
			var containingNamedType = (INamedTypeSymbol)containingSymbol;
			containingClasses.Add(GetClsString(containingNamedType));
			containingSymbol = containingSymbol.ContainingSymbol;
		}

		containingClasses.Reverse();
	}

	public static NestedClassScope Start(IndentedTextWriter writer,
		ITypeSymbol cls,
		bool implementsSerializationInterface = true
	) {
		var scope = new NestedClassScope(writer, cls);
		foreach (var containingClass in scope.containingClasses) {
			writer.WriteLine($"{containingClass}");
			writer.WriteLine("{");
			writer.Indent++;
		}

		writer.WriteLine($"{GetClsString(cls)}{(implementsSerializationInterface ? " : IXmlSerialization" : null)}");
		writer.WriteLine('{');
		writer.Indent++;

		return scope;
	}

	private static string TypeKindToStr(TypeKind kind) {
		return kind switch {
			TypeKind.Class  => "class",
			TypeKind.Struct => "struct",
			_               => throw new($"Unhandled kind {kind} in {nameof(TypeKindToStr)}")
		};
	}

	private static string GetClsString(ITypeSymbol namedTypeSymbol) {
		// {public/private...} {ref} partial {class/struct} {name}
		const string f = "{0} {1}partial {2} {3}";
		var symbolStr = namedTypeSymbol.ToString()!;
		var str = string.Format(f,
			namedTypeSymbol.DeclaredAccessibility.ToString().ToLowerInvariant(),
			namedTypeSymbol.IsRefLikeType ? "ref " : string.Empty,
			TypeKindToStr(namedTypeSymbol.TypeKind),
			symbolStr[(symbolStr.LastIndexOf('.') + 1)..]
		);
		return str;
	}

	public void Dispose() {
		writer.Indent--;
		writer.WriteLine('}');
		foreach (var _ in containingClasses) {
			writer.Indent--;
			writer.WriteLine("}"); // end container
		}
	}
}
