using Microsoft.CodeAnalysis;
using System.Text;
using TraceUtils.SourceGenerator.Models;

namespace TraceUtils.SourceGenerator.Infrastructure;

/// <summary>
/// Генерирует тело extension-метода с телеметрией.
/// </summary>
internal static class TelemetryExtensionMethodRenderer
{
    private static readonly SymbolDisplayFormat TypeFormat =
        new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static string GenerateExtensionMethodBody(MethodContextInfo context)
    {
        var methodSymbol = context.MethodSymbol;
        var signature = BuildExtensionSignature(methodSymbol, context.ContainingTypeName);
        var call = BuildMethodCall(methodSymbol);
        var preCallTags = BuildPreCallTags(methodSymbol);
        var postCallTags = BuildPostCallTags(methodSymbol);

        return TelemetryGenerationConstants.ExtensionMethodTemplate
            .Replace("{{signature}}", signature)
            .Replace("{{OperationName}}", context.OperationName)
            .Replace("{{ActivityType}}", context.ActivityType)
            .Replace("{{PreCallTags}}", preCallTags)
            .Replace("{{Call}}", call)
            .Replace("{{PostCallTags}}", postCallTags);
    }

    private static string BuildExtensionSignature(IMethodSymbol methodSymbol, string containingTypeName)
    {
        var sb = new StringBuilder();

        sb.Append("public static ");

        if (IsAsyncMethod(methodSymbol))
        {
            sb.Append("async ");
        }

        var returnType = methodSymbol.ReturnType.ToDisplayString(TypeFormat);
        sb.Append(returnType);
        sb.Append(' ');

        var methodName = methodSymbol.Name + GetTraceSuffix(methodSymbol);
        if (methodSymbol.IsGenericMethod && methodSymbol.TypeParameters.Length > 0)
        {
            var typeParams = string.Join(", ", methodSymbol.TypeParameters.Select(tp => tp.Name));
            methodName += $"<{typeParams}>";
        }

        sb.Append(methodName);
        sb.Append('(');

        sb.Append("this ");
        sb.Append(containingTypeName);
        sb.Append(" instance");

        foreach (var param in methodSymbol.Parameters)
        {
            sb.Append(", ");

            var refKind = param.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => ""
            };

            sb.Append(refKind);
            sb.Append(param.Type.ToDisplayString(TypeFormat));
            sb.Append(' ');
            sb.Append(param.Name);

            if (param.HasExplicitDefaultValue)
            {
                sb.Append(" = ");
                sb.Append(FormatDefaultValue(param));
            }
        }

        sb.Append(')');

        if (methodSymbol.IsGenericMethod && methodSymbol.TypeParameters.Length > 0)
        {
            var constraints = BuildConstraints(methodSymbol.TypeParameters);
            if (!string.IsNullOrEmpty(constraints))
            {
                sb.AppendLine();
                sb.Append("        ");
                sb.Append(constraints);
            }
        }

        return sb.ToString();
    }

    private static bool IsAsyncMethod(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType.ToDisplayString();
        return returnType.StartsWith("System.Threading.Tasks.Task") ||
               returnType.StartsWith("System.Threading.Tasks.ValueTask");
    }

    private static string GetTraceSuffix(IMethodSymbol methodSymbol)
    {
        return IsAsyncMethod(methodSymbol) ? "WithTraceAsync" : "WithTrace";
    }

    private static string FormatDefaultValue(IParameterSymbol param)
    {
        if (param.ExplicitDefaultValue is null)
        {
            if (param.Type.IsValueType && !param.Type.ToDisplayString().EndsWith("?"))
            {
                return "default";
            }
            return "null";
        }

        if (param.ExplicitDefaultValue is string str)
            return $"\"{str}\"";

        if (param.ExplicitDefaultValue is bool b)
            return b ? "true" : "false";

        var paramType = param.Type is INamedTypeSymbol nts && nts.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            ? nts.TypeArguments[0]
            : param.Type;
        if (paramType.TypeKind == TypeKind.Enum && param.ExplicitDefaultValue is not null)
        {
            var enumType = (INamedTypeSymbol)paramType;
            var rawValue = param.ExplicitDefaultValue;
            long valueLong = rawValue switch
            {
                int i => i,
                long l => l,
                byte by => by,
                sbyte sb => sb,
                short s => s,
                ushort us => us,
                uint u => u,
                ulong ul => (long)ul,
                _ => -1
            };
            foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
            {
                if (!member.IsConst || member.ConstantValue is null) continue;
                long memberLong = member.ConstantValue switch
                {
                    int i => i,
                    long l => l,
                    byte by => by,
                    sbyte sb => sb,
                    short s => s,
                    ushort us => us,
                    uint u => u,
                    ulong ul => (long)ul,
                    _ => -1
                };
                if (memberLong == valueLong)
                {
                    var typeName = enumType.ToDisplayString(TypeFormat);
                    return $"{typeName}.{member.Name}";
                }
            }
            var enumTypeName = enumType.ToDisplayString(TypeFormat);
            return $"({enumTypeName}){rawValue}";
        }

        return param.ExplicitDefaultValue!.ToString();
    }

    private static string BuildConstraints(System.Collections.Immutable.ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var sb = new StringBuilder();

        foreach (var tp in typeParameters)
        {
            var constraintParts = new List<string>();

            if (tp.HasReferenceTypeConstraint)
                constraintParts.Add("class");
            if (tp.HasValueTypeConstraint)
                constraintParts.Add("struct");
            if (tp.HasUnmanagedTypeConstraint)
                constraintParts.Add("unmanaged");
            if (tp.HasNotNullConstraint)
                constraintParts.Add("notnull");

            foreach (var t in tp.ConstraintTypes)
            {
                constraintParts.Add(t.ToDisplayString(TypeFormat));
            }

            if (tp.HasConstructorConstraint)
                constraintParts.Add("new()");

            if (constraintParts.Count == 0)
                continue;

            if (sb.Length > 0)
                sb.Append(" ");

            sb.Append("where ")
              .Append(tp.Name)
              .Append(" : ")
              .Append(string.Join(", ", constraintParts));
        }

        return sb.ToString();
    }

    private static string BuildMethodCall(IMethodSymbol methodSymbol)
    {
        var sb = new StringBuilder();
        var isAsync = IsAsyncMethod(methodSymbol);
        var returnType = methodSymbol.ReturnType.ToDisplayString();
        var hasReturnValue = !methodSymbol.ReturnsVoid &&
            returnType != "System.Threading.Tasks.Task" &&
            returnType != "System.Threading.Tasks.ValueTask";

        if (hasReturnValue)
        {
            sb.Append("return ");
        }

        if (isAsync)
        {
            sb.Append("await ");
        }

        sb.Append("instance.");
        sb.Append(methodSymbol.Name);

        if (methodSymbol.IsGenericMethod && methodSymbol.TypeParameters.Length > 0)
        {
            var typeParams = string.Join(", ", methodSymbol.TypeParameters.Select(tp => tp.Name));
            sb.Append($"<{typeParams}>");
        }

        sb.Append('(');

        var paramCalls = methodSymbol.Parameters.Select(p =>
        {
            var refKind = p.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => ""
            };
            return $"{refKind}{p.Name}";
        });

        sb.Append(string.Join(", ", paramCalls));
        sb.Append(");");

        return sb.ToString();
    }

    private static readonly string PreCallTagIndent = new string(' ', 12);

    private static string BuildPreCallTags(IMethodSymbol methodSymbol)
    {
        var sb = new StringBuilder();
        var first = true;

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.RefKind == RefKind.Out)
                continue;

            var tagInfo = ExtractTagInfo(parameter);
            if (tagInfo == null)
                continue;

            var (tagName, shouldSerialize) = tagInfo.Value;
            var tagValue = GetTagValueExpression(parameter, shouldSerialize);

            if (tagValue == null)
                continue;

            if (!first)
                sb.Append('\n').Append(PreCallTagIndent);
            sb.Append($"activity?.SetTag(\"{tagName}\", {tagValue});");
            first = false;
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildPostCallTags(IMethodSymbol methodSymbol)
    {
        var sb = new StringBuilder();

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.RefKind != RefKind.Out)
                continue;

            var tagInfo = ExtractTagInfo(parameter);
            if (tagInfo == null)
                continue;

            var (tagName, shouldSerialize) = tagInfo.Value;
            var tagValue = GetTagValueExpression(parameter, shouldSerialize);
            if (tagValue == null)
                continue;

            if (sb.Length > 0)
                sb.Append('\n').Append(PreCallTagIndent);
            sb.Append($"activity?.SetTag(\"{tagName}\", {tagValue});");
        }

        return sb.ToString();
    }

    private static (string TagName, bool ShouldSerialize)? ExtractTagInfo(IParameterSymbol parameter)
    {
        foreach (var attr in parameter.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == TelemetryGenerationConstants.SpanTagAttributeName)
            {
                var tagName = attr.ConstructorArguments.Length > 0
                    ? attr.ConstructorArguments[0].Value?.ToString() ?? parameter.Name
                    : parameter.Name;

                var shouldSerialize = attr.ConstructorArguments.Length > 1
                    && attr.ConstructorArguments[1].Value is bool serialize
                        ? serialize
                        : true;

                return (tagName, shouldSerialize);
            }
        }

        return null;
    }

    private static string? GetTagValueExpression(IParameterSymbol parameter, bool shouldSerialize)
    {
        var parameterType = parameter.Type;
        var isNullable = IsNullableType(parameterType);
        var nullableAccess = isNullable ? "?" : "";

        if (IsDateTimeType(parameterType))
        {
            return $"{parameter.Name}{nullableAccess}.ToString(\"O\")";
        }

        if (IsGuidType(parameterType))
        {
            return $"{parameter.Name}{nullableAccess}.ToString()";
        }

        if (IsPrimitiveType(parameterType))
        {
            return parameter.Name;
        }

        if (IsArrayOrGenericType(parameterType))
        {
            return shouldSerialize
                ? $"System.Text.Json.JsonSerializer.Serialize({parameter.Name})"
                : null;
        }

        if (shouldSerialize)
        {
            return $"System.Text.Json.JsonSerializer.Serialize({parameter.Name})";
        }

        return null;
    }

    private static bool IsNullableType(ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol namedType &&
               namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    private static bool IsGuidType(ITypeSymbol typeSymbol)
    {
        var actualType = GetUnderlyingType(typeSymbol);
        return actualType.Name == "Guid";
    }

    private static bool IsArrayOrGenericType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol)
            return true;

        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            return true;

        return false;
    }

    private static bool IsPrimitiveType(ITypeSymbol typeSymbol)
    {
        var actualType = GetUnderlyingType(typeSymbol);

        if (actualType.SpecialType is
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
            SpecialType.System_String)
        {
            return true;
        }

        if (actualType.TypeKind == TypeKind.Enum)
            return true;

        return actualType.Name == "TimeSpan";
    }

    private static bool IsDateTimeType(ITypeSymbol typeSymbol)
    {
        var actualType = GetUnderlyingType(typeSymbol);

        if (actualType.SpecialType == SpecialType.System_DateTime)
            return true;

        var typeName = actualType.Name;
        return typeName is "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly";
    }

    private static ITypeSymbol GetUnderlyingType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0];
        }
        return typeSymbol;
    }

}
