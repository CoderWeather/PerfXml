namespace PerfXml.Generator.Internal;

internal static class SymbolsExtensions {
	#region Types

	public static bool IsPrimitive(this ITypeSymbol type) {
		return type.IsValueType
		 || type.IsValueNullable()
		 || type.IsEnum()
		 || type.Name is "String";
	}

	public static bool IsString(this ITypeSymbol type) {
		return type.Name is "String";
	}

	public static bool IsList(this ITypeSymbol type) {
		return type.Name is "List";
	}

	public static bool IsValueNullable(this ITypeSymbol type) {
		return type is INamedTypeSymbol { Name: "Nullable", IsValueType: true };
	}

	public static bool IsEnum(this ITypeSymbol type) {
		return type.IsValueType && type.TypeKind is TypeKind.Enum;
	}

	public static ITypeSymbol? IfValueNullableGetInnerType(this ITypeSymbol type) {
		return type.IsValueNullable() && type is INamedTypeSymbol nt
			? nt.TypeArguments[0]
			: null;
	}

	public static string? AsString(this TypedConstant tc) {
		return tc.Value as string;
	}

	public static T? As<T>(this TypedConstant tc) {
		return tc.Value is T v ? v : default;
	}

	#endregion

	#region Base Classes

	public static bool HasBaseClass(this ITypeSymbol type) {
		return type.BaseType?.Name is not "Object";
	}

	#endregion

	#region Attributes

	public static AttributeData? TryGetAttribute(this ISymbol? type, INamedTypeSymbol? attributeType) {
		return type?.GetAttributes()
		   .SingleOrDefault(a => a.AttributeClass?.Equals(attributeType, SymbolEqualityComparer.Default) ?? false);
	}

	public static AttributeData GetAttribute(this ISymbol type, INamedTypeSymbol attribute) {
		return type is not null
			? type.TryGetAttribute(attribute) ?? throw new($"{attribute} attribute not found for {type}")
			: throw new ArgumentNullException(nameof(type));
	}

	#endregion

	#region Ancestors

	private static IEnumerable<INamedTypeSymbol> GetAllAncestorsEnumerable(this ITypeSymbol type) {
		if (type.IsPrimitive())
			yield break;

		var baseType = type.BaseType;

		while (baseType is not null && baseType.Name is not "object") {
			yield return baseType;
			baseType = baseType.BaseType;
		}
	}

	private static readonly Dictionary<ITypeSymbol, IEnumerable<INamedTypeSymbol>> AncestorsCache =
		new(SymbolEqualityComparer.Default);

	public static IEnumerable<INamedTypeSymbol> GetAllAncestors(this ITypeSymbol type) {
		if (AncestorsCache.TryGetValue(type, out var ancestors)) {
			return ancestors;
		}

		AncestorsCache[type] = ancestors = type.GetAllAncestorsEnumerable().ToArray();

		return ancestors;
	}

	public static INamedTypeSymbol? FindOldestAncestor(this ITypeSymbol type, Func<ITypeSymbol?, bool> predicate) {
		return type.GetAllAncestors().LastOrDefault(predicate.Invoke);
	}

	#endregion
}
