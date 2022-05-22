using PerfXml.Generator.Internal;

namespace PerfXml.Generator;

internal partial class XmlGenerator {
	private void WriteParseBody(IndentedTextWriter writer, ClassGenInfo cls) {
		var needsInlineBody = false;
		var needsSubBody = false;

		foreach (var body in cls.XmlBodies)
			if (body.OriginalType.IsPrimitive() && body.XmlName is null)
				needsInlineBody = true;
			else
				needsSubBody = true;

		if (needsInlineBody && needsSubBody)
			throw new($"{cls.Symbol.Name} needs inline body and sub body");

		if (needsInlineBody) {
			writer.WriteLine(
				$"public{cls.AdditionalMethodsModifiers} bool ParseFullBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> bodySpan, ref int end, IXmlFormatterResolver resolver)"
			);
			using (NestedScope.Start(writer)) {
				foreach (var body in cls.XmlBodies)
					if (body.OriginalType.Name == "String")
						writer.WriteLines(
							"var nodeSpan = buffer.ReadNodeValue(innerBodySpan, out endInner);",
							$"this.{body.Symbol.Name} = resolver.Parse<{body.Type}>(nodeSpan);"
						);
					else
						throw new(
							$"Xml:WriteParseBodyMethods: how to inline body {body.OriginalType.IsNativeIntegerType}");

				writer.WriteLine("return true;");
			}

			WriteEmptyParseSubBody(writer, cls);
			WriteEmptyParseSubBodyByNames(writer, cls);
		}
		else {
			if (cls.InheritedFromSerializable is false)
				WriteEmptyParseBody(writer, cls);

			if (needsSubBody) {
				WriteParseSubBody(writer, cls);
				WriteParseSubBodyByNames(writer, cls);
			}
			else if (cls.InheritedFromSerializable is false) {
				WriteEmptyParseSubBody(writer, cls);
				WriteEmptyParseSubBodyByNames(writer, cls);
			}
		}
	}

	private void WriteParseSubBody(IndentedTextWriter writer, ClassGenInfo cls) {
		var xmlBodies = cls.XmlBodies
		   .Where(x => x is not PropertyGenInfo prop || prop.Symbol.IsReadOnly is false)
		   .Where(x => x is not FieldGenInfo field || field.Symbol.IsReadOnly is false)
		   .Where(x => x.OriginalType is not ITypeParameterSymbol)
		   .ToArray();

		if (xmlBodies.Any() is false && cls.InheritedFromSerializable)
			return;

		writer.WriteLine(
			$"public{cls.AdditionalMethodsModifiers} bool ParseSubBody(ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver)"
		);
		using (NestedScope.Start(writer)) {
			if (xmlBodies.Any() is false) {
				writer.WriteLine("return default;");
				NestedScope.CloseLast();
				return;
			}

			if (cls.InheritedFromSerializable)
				writer.WriteLine(
					"if (base.ParseSubBody(ref buffer, hash, bodySpan, innerBodySpan, ref end, ref endInner, resolver)) return true;"
				);

			writer.WriteLine("switch (hash)");
			using (NestedScope.Start(writer)) {
				foreach (var body in xmlBodies) {
					var isList = body.OriginalType.IsList();

					ClassGenInfo? classToParse = null;
					if (body.OriginalType.IsPrimitive() is false) {
						var type = isList
							? ((INamedTypeSymbol)body.OriginalType).TypeArguments[0]
							: body.OriginalType;

						classToParse = classes[type];
					}

					var nameToCheck =
						body.XmlName
					 ?? classToParse?.ClassName
					 ?? throw new InvalidDataException("no body name??");

					writer.WriteLine($"case {HashName(nameToCheck)}:");
					using (NestedScope.Start(writer)) {
						if (classToParse is not null) {
							if (isList) {
								writer.WriteLine(
									$"this.{body.Symbol.Name} ??= new();");
								writer.WriteLine(
									$"this.{body.Symbol.Name}.Add(buffer.Read<{classToParse.Symbol}>(bodySpan, out end, resolver));");
							}
							else {
								writer.WriteLines(
									$"if (this.{body.Symbol.Name} is not null) throw new InvalidDataException(\"duplicate non-list body {body.Symbol.Name}\");",
									$"this.{body.Symbol.Name} = buffer.Read<{classToParse.Symbol}>(bodySpan, out end, resolver);"
								);
							}
						}
						else if (true) {
							writer.WriteLines(
								"var nodeSpan = buffer.ReadNodeValue(innerBodySpan, out endInner);",
								$"this.{body.Symbol.Name} = resolver.Parse<{body.Type}>(nodeSpan);"
							);
						}

						writer.WriteLine("return true;");
					}
				}
			}

			writer.WriteLine("return false;");
		}
	}

	private void WriteParseSubBodyByNames(IndentedTextWriter writer, ClassGenInfo cls) {
		var xmlBodies = cls.XmlBodies
		   .Where(x => x is not PropertyGenInfo prop || prop.Symbol.IsReadOnly is false)
		   .Where(x => x is not FieldGenInfo field || field.Symbol.IsReadOnly is false)
		   .Where(x => x.OriginalType is ITypeParameterSymbol)
		   .ToArray();

		writer.WriteLine(
			$"public{cls.AdditionalMethodsModifiers} bool ParseSubBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> nodeName, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver)"
		);
		using (NestedScope.Start(writer)) {
			if (xmlBodies.Any() is false) {
				writer.WriteLine("return default;");
				NestedScope.CloseLast();
				return;
			}

			if (cls.InheritedFromSerializable)
				writer.WriteLine(
					"if (base.ParseSubBody(ref buffer, hash, bodySpan, innerBodySpan, ref end, ref endInner, resolver)) return true;"
				);

			var isFirst = true;
			foreach (var body in xmlBodies) {
				if (isFirst is false)
					writer.Write("else ");
				else
					isFirst = false;

				writer.WriteLine(
					$"if (nodeName.Equals(NodeNamesCollector.GetFor<{body.OriginalType}>(), StringComparison.Ordinal))"
				);
				using (NestedScope.Start(writer)) {
					writer.WriteLines(
						$"if (this.{body.Symbol.Name} is not null) throw new InvalidDataException(\"duplicate non-list body this.{body.Symbol.Name}\");",
						$"this.{body.Symbol.Name} = buffer.Read<{body.OriginalType}>(bodySpan, out end, resolver);",
						"return true;"
					);
				}
			}

			writer.WriteLine("return false;");
		}
	}

	private void WriteParseAttribute(IndentedTextWriter writer, ClassGenInfo cls) {
		var xmlAttrs = cls.XmlAttributes
		   .Where(x => x.XmlName is not null)
		   .Where(x => x is not PropertyGenInfo prop || prop.Symbol.IsReadOnly is false)
		   .OrderBy(x => x.XmlName!.Length)
		   .ToArray();

		if (xmlAttrs.Any() is false && cls.InheritedFromSerializable)
			return;

		writer.WriteLine(
			$"public{cls.AdditionalMethodsModifiers} bool ParseAttribute(ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> value, IXmlFormatterResolver resolver)"
		);
		using (NestedScope.Start(writer)) {
			if (xmlAttrs.Any() is false) {
				writer.WriteLine("return default;");
				NestedScope.CloseLast();
				return;
			}

			if (cls.InheritedFromSerializable)
				writer.WriteLine("if (base.ParseAttribute(ref buffer, hash, value, resolver)) return true;");

			writer.WriteLine("switch (hash)");
			using (NestedScope.Start(writer)) {
				foreach (var attr in xmlAttrs) {
					writer.WriteLine($"case {HashName(attr.XmlName!)}:");
					using (NestedScope.Start(writer)) {
						if (attr.SplitChar is not null) {
							var namedType = (INamedTypeSymbol)attr.OriginalType;
							var typeToRead = namedType.TypeArguments[0].Name;

							writer.WriteLine($"var lst = new List<{typeToRead}>();");
							writer.WriteLine($"var reader = new StrReader(value, '{attr.SplitChar}');");

							writer.WriteLine("while (reader.HasRemaining())");
							using (NestedScope.Start(writer)) {
								writer.WriteLines(
									$"var val = reader.ReadAndParse<{attr.Type}()>;",
									"lst.Add(val);"
								);
							}

							writer.WriteLine($"this.{attr.Symbol.Name} = lst;");
						}
						else {
							writer.WriteLine($"this.{attr.Symbol.Name} = resolver.Parse<{attr.Type}>(value);");
						}

						writer.WriteLine("return true;");
					}
				}
			}

			writer.WriteLine("return false;");
		}
	}

	private void WriteSerializeBody(IndentedTextWriter writer, ClassGenInfo cls) {
		if (cls.XmlBodies.Any() is false && cls.InheritedFromSerializable)
			return;

		writer.WriteLine(
			$"public{cls.AdditionalMethodsModifiers} void SerializeBody(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver)"
		);
		using (NestedScope.Start(writer)) {
			if (cls.XmlBodies.Any() is false) {
				writer.WriteLine("return;");
				NestedScope.CloseLast();
				return;
			}

			if (cls.InheritedFromSerializable)
				writer.WriteLine("base.SerializeBody(ref buffer, resolver);");

			foreach (var body in cls.XmlBodies) {
				var isCanBeNull = body.OriginalType.IsReferenceType || body.Type.IsValueNullable();

				NestedScope? isNotNullScope = null;
				if (isCanBeNull) {
					writer.WriteLine($"if (this.{body.Symbol.Name} is not null)");
					isNotNullScope = NestedScope.Start(writer);
				}

				if (body.OriginalType.IsList()) {
					if (body.OriginalType.IsPrimitive() || classes.ContainsKey(body.OriginalType) is false)
						throw new("for xml body of type list<T>, T must be IXmlSerialization");

					writer.WriteLine($"foreach (var obj in this.{body.Symbol.Name})");
					using (NestedScope.Start(writer)) {
						writer.WriteLine("obj.Serialize(ref buffer, resolver);");
					}
				}
				// another IXmlSerialization
				else if (body.OriginalType.IsPrimitive() is false) {
					writer.WriteLine($"this.{body.Symbol.Name}.Serialize(ref buffer, resolver);");
				}
				else {
					NestedScope? nodeScope = null;
					if (body.XmlName is not null) {
						if (isCanBeNull is false)
							nodeScope = NestedScope.Start(writer);

						writer.WriteLine($"var node = buffer.StartNodeHead(\"{body.XmlName}\");");
					}

					writer.WriteLine($"buffer.Write(this.{body.Symbol.Name}, resolver);");

					if (body.XmlName is not null) {
						writer.WriteLine("buffer.EndNode(ref node);");
						nodeScope?.Close();
					}
				}

				isNotNullScope?.Close();
			}
		}
	}

	private void WriteSerializeAttributes(IndentedTextWriter writer, ClassGenInfo cls) {
		if (cls.XmlAttributes.Any() is false && cls.InheritedFromSerializable)
			return;

		writer.WriteLine(
			$"public{cls.AdditionalMethodsModifiers} void SerializeAttributes(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver)");
		using (NestedScope.Start(writer)) {
			if (cls.InheritedFromSerializable)
				writer.WriteLine("base.SerializeAttributes(ref buffer, resolver);");

			if (cls.XmlAttributes.Any() is false) {
				writer.WriteLine("return;");
				NestedScope.CloseLast();
				return;
			}

			foreach (var field in cls.XmlAttributes)
				if (field.SplitChar is not null) {
					// var namedType = (INamedTypeSymbol)field.OriginalType;
					// var typeToRead = namedType.TypeArguments[0];

					using (NestedScope.Start(writer)) {
						writer.WriteLines(
							$"using var writer = new StrWriter('{field.SplitChar}');",
							$"foreach (var val in {field.Symbol.Name})"
						);
						using (NestedScope.Start(writer)) {
							writer.WriteLine("writer.Write(val, resolver);");
						}

						writer.WriteLines(
							$"buffer.PutAttribute(\"{field.XmlName}\", writer.BuiltSpan);",
							"writer.Dispose();"
						);
					}
				}
				else {
					using (NestedScope.Start(writer)) { }

					var writerAction = GetPutAttributeAction(field);
					writer.WriteLine(writerAction);
				}
		}
	}

	private void WriteSerialize(IndentedTextWriter writer, ClassGenInfo cls) {
		if (cls.InheritedFromSerializable)
			return;

		writer.WriteLine("public void Serialize(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver)");
		using (NestedScope.Start(writer)) {
			writer.WriteLine("var node = buffer.StartNodeHead(GetNodeName());");
			writer.WriteLine("SerializeAttributes(ref buffer, resolver);");
			writer.WriteLine("SerializeBody(ref buffer, resolver);");
			writer.WriteLine("buffer.EndNode(ref node);");
		}
	}
}
