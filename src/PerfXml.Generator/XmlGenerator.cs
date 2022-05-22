using PerfXml.Generator.Internal;

namespace PerfXml.Generator;

[Generator]
internal sealed partial class XmlGenerator : ISourceGenerator {
	private INamedTypeSymbol? bodyAttributeSymbol;
	private INamedTypeSymbol? fieldAttributeSymbol;
	private INamedTypeSymbol? classAttributeSymbol;
	private INamedTypeSymbol? splitStringAttributeSymbol;

	private readonly Dictionary<ITypeSymbol, ClassGenInfo> classes = new(SymbolEqualityComparer.Default);

	public void Initialize(GeneratorInitializationContext context) {
		context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
#if DEBUG
		if (Debugger.IsAttached is false) {
			Debugger.Launch();
		}
#endif
	}

	public void Execute(GeneratorExecutionContext context) {
		try {
			ExecuteInternal(context);
		}
		catch (Exception e) {
			var descriptor = new DiagnosticDescriptor(nameof(XmlGenerator),
				"Error",
				e.ToString(),
				"Error",
				DiagnosticSeverity.Error,
				true);
			var diagnostic = Diagnostic.Create(descriptor, Location.None);
			context.ReportDiagnostic(diagnostic);
		}
	}

	private void ExecuteInternal(GeneratorExecutionContext context) {
		if (context.SyntaxReceiver is not SyntaxReceiver receiver)
			return;

		var compilation = context.Compilation;

		bodyAttributeSymbol = compilation.GetTypeByMetadataName("PerfXml.XmlBodyAttribute");
		fieldAttributeSymbol = compilation.GetTypeByMetadataName("PerfXml.XmlFieldAttribute");
		classAttributeSymbol = compilation.GetTypeByMetadataName("PerfXml.XmlClsAttribute");
		splitStringAttributeSymbol = compilation.GetTypeByMetadataName("PerfXml.XmlSplitStrAttribute");

		foreach (var cl in receiver.Classes) {
			var model = compilation.GetSemanticModel(cl.SyntaxTree);
			var symbol = model.GetDeclaredSymbol(cl);

			var classAttr = symbol.TryGetAttribute(classAttributeSymbol);

			var havePartialKeyword = cl.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword));

			if (classAttr is null || symbol is null || havePartialKeyword is false)
				continue;

			var classGenInfo = new ClassGenInfo(symbol) {
				ClassName = classAttr.ConstructorArguments[0].AsString()
			};

			if (classGenInfo.ClassName is null) {
				var ancestor = symbol.GetAllAncestors()
				   .FirstOrDefault(t =>
						t.TryGetAttribute(classAttributeSymbol)?.ConstructorArguments[0].Value is not null);
				if (ancestor is not null) {
					classGenInfo.ClassName =
						ancestor.GetAttribute(classAttributeSymbol!).ConstructorArguments[0].Value!.ToString();
					classGenInfo.InheritedClassName = true;
				}
				else {
					classGenInfo.ClassName = symbol.Name;
				}
			}

			classGenInfo.AlreadyHasEmptyConstructor = symbol.InstanceConstructors
			   .Any(x => x.Parameters.IsDefaultOrEmpty);

			classes[symbol] = classGenInfo;
		}

		foreach (var field in receiver.Fields) {
			var model = compilation.GetSemanticModel(field.SyntaxTree);
			foreach (var variable in field.Declaration.Variables) {
				if (ModelExtensions.GetDeclaredSymbol(model, variable) is not IFieldSymbol symbol)
					continue;

				var fieldAttr = symbol.TryGetAttribute(fieldAttributeSymbol);
				var bodyAttr = symbol.TryGetAttribute(bodyAttributeSymbol);
				var splitAttr = symbol.TryGetAttribute(splitStringAttributeSymbol);

				if (fieldAttr is null && bodyAttr is null)
					continue;

				if (classes.TryGetValue(symbol.ContainingType, out var classInfo) is false) {
					classInfo = new(symbol.ContainingType);
					classes[symbol.ContainingType] = classInfo;
				}

				var fieldInfo = new FieldGenInfo(symbol);
				if (fieldAttr is not null) {
					fieldInfo.XmlName = fieldAttr.ConstructorArguments[0].AsString() ?? symbol.Name;
					classInfo.XmlAttributes.Add(fieldInfo);
				}

				if (bodyAttr is not null) {
					fieldInfo.XmlName = bodyAttr.ConstructorArguments[0].AsString() ?? symbol.Name;
					classInfo.XmlBodies.Add(fieldInfo);
				}

				if (splitAttr?.ConstructorArguments[0].As<char>() is { } ch)
					fieldInfo.SplitChar = ch;
			}
		}

		foreach (var prop in receiver.Properties) {
			var model = compilation.GetSemanticModel(prop.SyntaxTree);
			if (ModelExtensions.GetDeclaredSymbol(model, prop) is not IPropertySymbol symbol || symbol.IsAbstract)
				continue;

			var fieldAttr = symbol.TryGetAttribute(fieldAttributeSymbol);
			var bodyAttr = symbol.TryGetAttribute(bodyAttributeSymbol);
			var splitAttr = symbol.TryGetAttribute(splitStringAttributeSymbol);

			if (fieldAttr is null && bodyAttr is null)
				continue;

			if (classes.TryGetValue(symbol.ContainingType, out var classInfo) is false) {
				classInfo = new(symbol.ContainingType);
				classes[symbol.ContainingType] = classInfo;
			}

			var propInfo = new PropertyGenInfo(symbol);

			if (fieldAttr is not null) {
				propInfo.XmlName = fieldAttr.ConstructorArguments[0].AsString() ?? symbol.Name;
				classInfo.XmlAttributes.Add(propInfo);
			}

			if (bodyAttr is not null) {
				var takeNameFromType = bodyAttr.ConstructorArguments[0].Value is true;
				if (takeNameFromType) {
					var fieldType = symbol.Type;
					propInfo.XmlName = fieldType switch {
						INamedTypeSymbol nt => classes.TryGetValue(nt, out var ci)
							? ci.ClassName
							: nt.GetAttribute(classAttributeSymbol!).ConstructorArguments[0].AsString()
						 ?? nt.GetAllAncestors()
							   .Select(x => x.TryGetAttribute(classAttributeSymbol))
							   .FirstOrDefault(x => x?.ConstructorArguments[0].Value is not null)
							  ?.ConstructorArguments[0]
							   .AsString(),
						ITypeParameterSymbol => throw new(
							"Cannot set name of sub-body entry with name of base of generic type parameter type"),
						_ => null
					};
				}
				else {
					propInfo.XmlName = bodyAttr.ConstructorArguments[0].AsString();
				}

				classInfo.XmlBodies.Add(propInfo);
			}

			if (splitAttr?.ConstructorArguments[0].As<char>() is { } ch)
				propInfo.SplitChar = ch;
		}

		foreach (var (_, v) in classes
					.Where(x => x.Value.AlreadyHasEmptyConstructor)
					.ToArray()) {
			v.XmlAttributes.RemoveAll(x => x.OriginalType.IsList());
			v.XmlBodies.RemoveAll(x => x.OriginalType.IsList());
		}

		foreach (var (_, v) in classes) {
			if (v.Symbol.HasBaseClass() && classes.ContainsKey(v.Symbol.BaseType!.OriginalDefinition))
				v.InheritedFromSerializable = true;

			if (v.XmlAttributes.Any(x => x.OriginalType is ITypeParameterSymbol)
			 || v.XmlBodies.Any(x => x.OriginalType is ITypeParameterSymbol))
				v.HaveGenericElements = true;
		}

		foreach (var group in classes
					.GroupBy(x => x.Value.Symbol.ContainingNamespace,
						 x => x.Value,
						 SymbolEqualityComparer.Default)) {
			var ns = group.Key!.ToString()!;
			var source = ProcessClasses(ns, group);
			if (source is null)
				continue;

			context.AddSource($"{nameof(XmlGenerator)}_{ns}.cs", SourceText.From(source, Encoding.UTF8));
		}
	}

	private string? ProcessClasses(string containingNamespace, IEnumerable<ClassGenInfo> enumerable) {
		var writer = new IndentedTextWriter(new StringWriter(), "  ");
		writer.WriteLine("using System;");
		writer.WriteLine("using System.IO;");
		writer.WriteLine("using System.Collections.Generic;");
		writer.WriteLine("using PerfXml;");
		writer.WriteLine("using PerfXml.Str;");
		writer.WriteLine();
		writer.WriteLine($"namespace {containingNamespace};");
		writer.WriteLine();

		foreach (var cls in enumerable)
			using (NestedClassScope.Start(writer, cls.Symbol, cls.InheritedFromSerializable is false)) {
				if (cls.Symbol.IsAbstract is false) {
					writer.WriteLine($"static {cls.Symbol.Name}()");
					using (NestedScope.Start(writer)) {
						var name = cls.Symbol.Name;
						if (cls.Symbol.IsGenericType) {
							var t = cls.Symbol.ToString()!;
							name = t[(t.LastIndexOf('.') + 1)..];
						}

						writer.WriteLine($"NodeNamesCollector.RegisterFor<{name}>(\"{cls.ClassName}\");");
					}
				}

				if (cls.InheritedClassName is false)
					writer.WriteLine("public{0} ReadOnlySpan<char> GetNodeName() => \"{1}\";",
						cls.AdditionalMethodsModifiers,
						cls.ClassName
					);

				WriteParseBody(writer, cls);
				WriteParseAttribute(writer, cls);
				WriteSerializeBody(writer, cls);
				WriteSerializeAttributes(writer, cls);
				WriteSerialize(writer, cls);
			}

		var resultStr = writer.InnerWriter.ToString();
		return resultStr;
	}

	private string GetPutAttributeAction(BaseMemberGenInfo m) {
		var type = m.Type;
		var name = m.Symbol.Name;
		string? preCheck = null;
		if (m.Type.IfValueNullableGetInnerType() is { } t) {
			type = t;
			preCheck = $"if ({name}.HasValue) ";
			name += ".Value";
		}

		if (type.IsEnum())
			return $"{preCheck}buffer.PutEnumValue(\"{m.XmlName}\", {name})";

		var writerAction = type.Name switch {
			"String" => $"buffer.PutAttribute(\"{m.XmlName}\", {name});",
			"Byte"
				or "Int16"
				or "Int32"
				or "UInt32"
				or "Int64"
				or "Double"
				or "Decimal"
				or "Char"
				or "Boolean"
				or "Guid"
				or "DateOnly"
				or "TimeOnly"
				or "DateTime" => $"buffer.PutAttributeValue(\"{m.XmlName}\", {name});",
			_ => throw new($"no attribute writer for type {type}")
		};
		return $"{preCheck}{writerAction}";
	}

	// private string GetParseAction(BaseMemberGenInfo m) {
	// 	var type = m.Type;
	// 	if (m.Type.IfValueNullableGetInnerType() is { } t) {
	// 		type = t;
	// 	}
	//
	// 	if (type.IsEnum()) {
	// 		return $"StrReader.ParseEnum<{type.Name}>(value)";
	// 	}
	//
	// 	var readCommand = type.Name switch {
	// 		"String"   => "value.ToString()",
	// 		"Byte"     => "StrReader.ParseByte(value)",
	// 		"Int16"    => "StrReader.ParseShort(value)",
	// 		"Int32"    => "StrReader.ParseInt(value)",
	// 		"UInt32"   => "StrReader.ParseUInt(value)",
	// 		"Int64"    => "StrReader.ParseLong(value)",
	// 		"Double"   => "StrReader.ParseDouble(value)",
	// 		"Decimal"  => "StrReader.ParseDecimal(value)",
	// 		"Char"     => "StrReader.ParseChar(value)",
	// 		"Boolean"  => "StrReader.InterpretBool(value)",
	// 		"Guid"     => "StrReader.ParseGuid(value)",
	// 		"DateOnly" => "StrReader.ParseDateOnly(value)",
	// 		"TimeOnly" => "StrReader.ParseTimeOnly(value)",
	// 		"DateTime" => "StrReader.ParseDateTime(value)",
	// 		_          => throw new($"no attribute reader for {type}")
	// 	};
	// 	return readCommand;
	// }

	// public static string GetWriterForType(ITypeSymbol type, string toWrite) {
	// 	var t = type;
	// 	if (type.IfValueNullableGetInnerType() is { } inner) {
	// 		t = inner;
	// 		toWrite += ".GetValueOrDefault()";
	// 	}
	//
	// 	var result = t.Name switch {
	// 		"String"  => $"writer.PutString({toWrite})",
	// 		"ReadOnlySpan<char>" => $"writer.PutString({toWrite})",
	// 		"Byte"
	// 			or "Int16"
	// 			or "Int32"
	// 			or "UInt32"
	// 			or "Int64"
	// 			or "Double"
	// 			or "Decimal"
	// 			or "Char"
	// 			or "Boolean"
	// 			or "Guid"
	// 			or "DateOnly"
	// 			or "TimeOnly"
	// 			or "DateTime" => $"writer.PutValue({toWrite});",
	// 		_ => throw new($"GetWriterForType: {type} is missing")
	// 	};
	// 	return result;
	// }

	// public static string GetReaderForType(ITypeSymbol type) {
	// 	if (type.IsEnum()) {
	// 		return $"reader.GetEnumValue<{type.Name}>()";
	// 	}
	//
	// 	var result = type.Name switch {
	// 		"String"   => "reader.GetString().ToString()",
	// 		"ReadOnlySpan<char>"  => "reader.GetReadOnlySpan<char>ing()",
	// 		"Byte"     => "reader.GetByte()",
	// 		"Int16"    => "reader.GetShort()",
	// 		"Int32"    => "reader.GetInt()",
	// 		"UInt32"   => "reader.GetUInt()",
	// 		"Int64"    => "reader.GetLong()",
	// 		"Double"   => "reader.GetDouble()",
	// 		"Decimal"  => "reader.GetDecimal()",
	// 		"Char"     => "reader.GetChar()",
	// 		"Boolean"  => "reader.GetBoolean()",
	// 		"Guid"     => "reader.GetGuid()",
	// 		"DateOnly" => "reader.GetDateOnly()",
	// 		"TimeOnly" => "reader.GetTimeOnly()",
	// 		"DateTime" => "reader.GetDateTime()",
	// 		_          => throw new($"GetReaderForType: {type} is missing")
	// 	};
	// 	return result;
	// }

	private static ulong HashName(ReadOnlySpan<char> name) {
		var hashedValue = 0x2AAAAAAAAAAAAB67ul;
		for (var i = 0; i < name.Length; i++) {
			hashedValue += name[i];
			hashedValue *= 0x2AAAAAAAAAAAAB6Ful;
		}

		return hashedValue;
	}
}
