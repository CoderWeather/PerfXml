using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PerfXml.Generator;

internal sealed class ClassGenInfo {
	public readonly INamedTypeSymbol Symbol;
	public readonly List<BaseMemberGenInfo> XmlAttributes = new();
	public readonly List<BaseMemberGenInfo> XmlBodies = new();
	public bool InheritedClassName = false;
	public bool InheritedFromSerializable = false;

	public string? ClassName;

	public ClassGenInfo(INamedTypeSymbol symbol) {
		Symbol = symbol;
	}

	public string? AdditionalInheritanceMethodModifiers =>
		InheritedFromSerializable
			? " override"
			: Symbol.IsSealed is false || Symbol.IsAbstract
				? " virtual"
				: null;
}

internal abstract class BaseMemberGenInfo {
	public ISymbol Symbol { get; }
	public ITypeSymbol OriginalType { get; }
	public ITypeSymbol Type { get; }

	public string TypeName =>
		Type switch {
			INamedTypeSymbol nts => nts.IsGenericType ? nts.ToString() : nts.Name,
			ITypeParameterSymbol => Type.Name,
			_                    => Type.ToString()
		};

	public string? XmlName;
	public char? SplitChar;
	public bool TypeIsSerializable;

	protected BaseMemberGenInfo(ISymbol symbol, ITypeSymbol type) {
		Symbol = symbol;
		Type = type;
		OriginalType = Type.IsDefinition ? Type : Type.OriginalDefinition;
	}
}

internal sealed class FieldGenInfo : BaseMemberGenInfo {
	public new IFieldSymbol Symbol { get; }

	public FieldGenInfo(IFieldSymbol fieldSymbol) :
		base(fieldSymbol, fieldSymbol.Type) {
		Symbol = fieldSymbol;
	}
}

internal sealed class PropertyGenInfo : BaseMemberGenInfo {
	public new IPropertySymbol Symbol { get; }

	public PropertyGenInfo(IPropertySymbol propertySymbol) :
		base(propertySymbol, propertySymbol.Type) {
		Symbol = propertySymbol;
	}
}
