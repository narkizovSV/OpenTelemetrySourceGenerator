using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using TraceUtils.SourceGenerator.Models;

namespace TraceUtils.SourceGenerator.Generation;

/// <summary>
/// Преобразует символы Roslyn в модель сигнатуры метода для последующей генерации кода.
/// </summary>
internal static class TelemetryMethodSignatureTransformer
{
    public static bool Predicate(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is MethodDeclarationSyntax;
    }

    public static MethodSignatureInfo Transform(GeneratorAttributeSyntaxContext attrSyntaxContext, CancellationToken cancellationToken)
    {
        var methodSymbol = (IMethodSymbol)attrSyntaxContext.TargetSymbol;
        var interfaceSymbol = (INamedTypeSymbol)methodSymbol.ContainingType!;

        var tracerEventAttr = attrSyntaxContext.Attributes
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == TelemetryGenerationConstants.TraceEventAttributeName);

        var operationName = tracerEventAttr is not null &&
                            tracerEventAttr.ConstructorArguments.Length > 0 &&
                            tracerEventAttr.ConstructorArguments[0].Value is string configuredOperationName
            ? configuredOperationName
            : methodSymbol.Name;

        var containingType = interfaceSymbol.Name;
        var containingNamespace = interfaceSymbol.ContainingNamespace.ToDisplayString();

        var parameters = ImmutableArray.CreateBuilder<ParameterInfo>();
        foreach (var param in methodSymbol.Parameters)
        {
            var tracerPropertyAttr = param.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == TelemetryGenerationConstants.TracerPropertyAttributeName);

            var hasTracerPropertyAttribute = tracerPropertyAttr != null;
            var shouldAutoTagArrayLike = ShouldAutoTagArrayLikeParameter(param);
            var hasTracerProperty = hasTracerPropertyAttribute || shouldAutoTagArrayLike;

            var tagName = hasTracerPropertyAttribute
                ? tracerPropertyAttr?.ConstructorArguments[0].Value?.ToString() ?? param.Name
                : param.Name;
            var tagInfos = hasTracerProperty
                ? BuildParameterTagInfos(param, tagName!)
                : ImmutableArray<ParameterTagInfo>.Empty;

            parameters.Add(new ParameterInfo(
                Name: param.Name,
                Type: param.Type.ToDisplayString(TelemetryGenerationConstants.ParameterTypeDisplayFormat),
                Modifier: GetParameterModifier(param),
                IsParams: param.IsParams,
                HasDefaultValue: param.HasExplicitDefaultValue,
                DefaultValueExpression: FormatDefaultValue(param),
                HasTracerProperty: hasTracerProperty,
                TagInfos: tagInfos,
                TagName: tagName
            ));
        }

        var returnType = methodSymbol.ReturnType;
        var isAsync = returnType.Name == "Task" || returnType.Name == "ValueTask";
        var returnTypeString = returnType.ToDisplayString(TelemetryGenerationConstants.ReturnTypeDisplayFormat);

        var isGeneric = methodSymbol.IsGenericMethod;
        var genericConstraints = isGeneric ? BuildGenericConstraints(methodSymbol) : null;
        var genericTypeParameters = isGeneric
            ? methodSymbol.TypeParameters.Select(tp => tp.Name).ToImmutableArray()
            : ImmutableArray<string>.Empty;

        return new MethodSignatureInfo(
            ContainingNamespace: containingNamespace,
            ContainingType: containingType,
            MethodName: methodSymbol.Name,
            ReturnType: returnTypeString,
            OperationName: operationName,
            IsAsync: isAsync,
            IsGeneric: isGeneric,
            GenericConstraints: genericConstraints,
            Parameters: parameters.ToImmutable(),
            GenericTypeParameters: genericTypeParameters
        );
    }

    private static string BuildGenericConstraints(IMethodSymbol methodSymbol)
    {
        var constraints = new StringBuilder();

        foreach (var typeParam in methodSymbol.TypeParameters)
        {
            var paramConstraints = new List<string>();
            string? kindConstraint = null;

            if (typeParam.HasUnmanagedTypeConstraint)
                kindConstraint = "unmanaged";
            else if (typeParam.HasReferenceTypeConstraint)
                kindConstraint = "class";
            else if (typeParam.HasValueTypeConstraint)
                kindConstraint = "struct";
            else if (typeParam.HasNotNullConstraint)
                kindConstraint = "notnull";

            if (kindConstraint is not null)
                paramConstraints.Add(kindConstraint);

            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                paramConstraints.Add(constraintType.ToDisplayString());
            }

            if (typeParam.HasConstructorConstraint)
                paramConstraints.Add("new()");

            if (paramConstraints.Count > 0)
            {
                constraints.Append($" where {typeParam.Name} : {string.Join(", ", paramConstraints)}");
            }
        }

        return constraints.ToString();
    }

    private static string GetParameterModifier(IParameterSymbol parameter)
    {
        return parameter.RefKind switch
        {
            RefKind.Ref => "ref",
            RefKind.Out => "out",
            RefKind.In => "in",
            _ => string.Empty
        };
    }

    private static ImmutableArray<ParameterTagInfo> BuildParameterTagInfos(IParameterSymbol parameter, string tagPrefix)
    {
        var type = UnwrapNullableType(parameter.Type);

        if (ShouldFlattenType(type))
        {
            var propertyTags = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(IsFlattenableProperty)
                .Select(p => BuildTagInfoForProperty(parameter, tagPrefix, p))
                .ToImmutableArray();

            if (!propertyTags.IsDefaultOrEmpty)
                return propertyTags;
        }

        return [new ParameterTagInfo(
            TagName: tagPrefix,
            ValueExpression: parameter.Name,
            ShouldSerializeValue: ShouldSerializeValue(parameter.Type))];
    }

    private static bool ShouldFlattenType(ITypeSymbol parameterType)
    {
        var type = UnwrapNullableType(parameterType);

        if (type.TypeKind == TypeKind.Enum || IsSimpleType(type))
            return false;

        if (IsCollectionType(type))
            return false;

        if (ShouldNeverFlattenType(type))
            return false;

        return type.TypeKind is TypeKind.Class or TypeKind.Struct;
    }

    private static bool IsFlattenableProperty(IPropertySymbol property)
    {
        return !property.IsStatic &&
               !property.IsIndexer &&
               property.GetMethod is not null &&
               property.GetMethod.DeclaredAccessibility == Accessibility.Public;
    }

    private static ParameterTagInfo BuildTagInfoForProperty(IParameterSymbol parameter, string tagPrefix, IPropertySymbol property)
    {
        var memberAccess = RequiresConditionalAccess(parameter.Type)
            ? $"{parameter.Name}?.{property.Name}"
            : $"{parameter.Name}.{property.Name}";

        return new ParameterTagInfo(
            TagName: $"{tagPrefix}.{property.Name}",
            ValueExpression: memberAccess,
            ShouldSerializeValue: ShouldSerializeValue(property.Type));
    }

    private static bool ShouldSerializeValue(ITypeSymbol type)
    {
        var unwrappedType = UnwrapNullableType(type);

        if (unwrappedType is ITypeParameterSymbol)
            return true;

        if (unwrappedType is IArrayTypeSymbol)
            return true;

        if (IsCollectionType(unwrappedType))
            return true;

        if (ShouldNeverFlattenType(unwrappedType))
            return false;

        if (unwrappedType.TypeKind == TypeKind.Enum || IsSimpleType(unwrappedType))
            return false;

        return unwrappedType.TypeKind is TypeKind.Class or TypeKind.Struct;
    }

    private static bool ShouldAutoTagArrayLikeParameter(IParameterSymbol parameter)
    {
        var type = UnwrapNullableType(parameter.Type);
        if (type is not IArrayTypeSymbol arrayType)
            return false;

        if (parameter.IsParams)
            return true;

        return IsSimpleType(arrayType.ElementType) || arrayType.ElementType.TypeKind == TypeKind.Enum;
    }

    private static ITypeSymbol UnwrapNullableType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }

        return type;
    }

    private static bool RequiresConditionalAccess(ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated && type.IsReferenceType)
            return true;

        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        return false;
    }

    private static bool IsSimpleType(ITypeSymbol type)
    {
        if (type.SpecialType != SpecialType.None)
        {
            return type.SpecialType is
                SpecialType.System_Boolean or
                SpecialType.System_Byte or
                SpecialType.System_SByte or
                SpecialType.System_Char or
                SpecialType.System_Int16 or
                SpecialType.System_UInt16 or
                SpecialType.System_Int32 or
                SpecialType.System_UInt32 or
                SpecialType.System_Int64 or
                SpecialType.System_UInt64 or
                SpecialType.System_Single or
                SpecialType.System_Double or
                SpecialType.System_Decimal or
                SpecialType.System_String or
                SpecialType.System_DateTime;
        }

        var fullyQualifiedTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return fullyQualifiedTypeName is
            "global::System.Guid" or
            "global::System.DateTime" or
            "global::System.DateTimeOffset" or
            "global::System.DateOnly" or
            "global::System.TimeOnly" or
            "global::System.TimeSpan" or
            "global::System.Threading.CancellationToken";
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        var unwrapped = UnwrapNullableType(type);
        if (unwrapped is IArrayTypeSymbol)
            return true;

        if (unwrapped.SpecialType == SpecialType.System_String)
            return false;

        if (unwrapped is not INamedTypeSymbol namedType)
            return false;

        if (IsCollectionSymbol(namedType))
            return true;

        if (namedType.AllInterfaces.Any(IsCollectionSymbol))
            return true;

        var displayName = namedType.ToDisplayString();
        return displayName.Contains("IEnumerable<", StringComparison.Ordinal) ||
               displayName.Contains("ICollection<", StringComparison.Ordinal) ||
               displayName.Contains("IReadOnlyCollection<", StringComparison.Ordinal) ||
               displayName.Contains("IList<", StringComparison.Ordinal) ||
               displayName.Contains("IReadOnlyList<", StringComparison.Ordinal) ||
               displayName.Contains("IDictionary<", StringComparison.Ordinal) ||
               displayName.Contains("IReadOnlyDictionary<", StringComparison.Ordinal) ||
               displayName.Contains("List<", StringComparison.Ordinal) ||
               displayName.Contains("Dictionary<", StringComparison.Ordinal);
    }

    private static bool IsCollectionSymbol(INamedTypeSymbol symbol)
    {
        var namespaceName = symbol.ContainingNamespace.ToDisplayString();
        if (!namespaceName.StartsWith("System.Collections", StringComparison.Ordinal))
            return false;

        return symbol.MetadataName is
            "IEnumerable" or
            "IEnumerable`1" or
            "ICollection`1" or
            "IReadOnlyCollection`1" or
            "IList`1" or
            "IReadOnlyList`1" or
            "IDictionary`2" or
            "IReadOnlyDictionary`2";
    }

    private static bool IsOrInheritsFrom(ITypeSymbol type, string fullyQualifiedTypeName)
    {
        var current = type;
        while (current is not null)
        {
            if (string.Equals(
                    current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    fullyQualifiedTypeName,
                    StringComparison.Ordinal))
            {
                return true;
            }

            current = (current as INamedTypeSymbol)?.BaseType;
        }

        return false;
    }

    private static bool ShouldNeverFlattenType(ITypeSymbol type)
    {
        if (IsOrInheritsFrom(type, "global::System.IO.Stream"))
            return true;

        if (LooksLikeStreamBySymbolName(type))
            return true;

        var hasReadTimeout = type.GetMembers().OfType<IPropertySymbol>().Any(p => p.Name == "ReadTimeout");
        var hasWriteTimeout = type.GetMembers().OfType<IPropertySymbol>().Any(p => p.Name == "WriteTimeout");
        return hasReadTimeout && hasWriteTimeout;
    }

    private static bool LooksLikeStreamBySymbolName(ITypeSymbol type)
    {
        var current = type as INamedTypeSymbol;
        while (current is not null)
        {
            var namespaceName = current.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (namespaceName == "System.IO" && current.Name == "Stream")
                return true;

            current = current.BaseType;
        }

        var display = type.ToDisplayString();
        return display.EndsWith(".Stream", StringComparison.Ordinal) ||
               display == "Stream";
    }

    private static string? FormatDefaultValue(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
            return null;

        var value = parameter.ExplicitDefaultValue;

        if (value is null)
        {
            if (parameter.Type.IsValueType)
                return "default";
            return "null";
        }

        if (parameter.Type.TypeKind == TypeKind.Enum)
        {
            var enumMemberName = GetEnumMemberName(parameter.Type, value);
            if (enumMemberName is not null)
            {
                var enumType = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"{enumType}.{enumMemberName}";
            }

            var enumTypeFallback = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"{enumTypeFallback}({value})";
        }

        return value switch
        {
            string s => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
            char c => $"'{c}'",
            bool b => b ? "true" : "false",
            float f => f.ToString(CultureInfo.InvariantCulture) + "F",
            double d => d.ToString(CultureInfo.InvariantCulture) + "D",
            decimal m => m.ToString(CultureInfo.InvariantCulture) + "M",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    private static string? GetEnumMemberName(ITypeSymbol enumType, object value)
    {
        if (enumType is not INamedTypeSymbol namedEnum)
            return null;

        foreach (var member in namedEnum.GetMembers())
        {
            if (member is IFieldSymbol field &&
                field.HasConstantValue &&
                Equals(field.ConstantValue, value))
            {
                return field.Name;
            }
        }

        return null;
    }
}
