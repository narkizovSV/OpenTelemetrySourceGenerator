using Microsoft.CodeAnalysis;
using System.Globalization;
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
        var (call, returnStatement, hasResultVariable) = BuildMethodCall(methodSymbol);
        var inputTagBlock = BuildInputTagBlock(
            methodSymbol,
            context.InputParametersName,
            context.WriteTagsToDictionary);
        var outputTagBlock = BuildOutputTagBlock(
            methodSymbol,
            context.OutputParametersName,
            hasResultVariable,
            context.WriteTagsToDictionary);

        return TelemetryGenerationConstants.ExtensionMethodTemplate
            .Replace("{{signature}}", signature)
            .Replace("{{OperationName}}", context.OperationName)
            .Replace("{{ActivityType}}", context.ActivityType)
            .Replace("{{InputTagBlock}}", inputTagBlock)
            .Replace("{{Call}}", call)
            .Replace("{{OutputTagBlock}}", outputTagBlock)
            .Replace("{{ReturnStatement}}", returnStatement);
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

        var methodName = BuildGeneratedMethodName(methodSymbol);
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

    private static string BuildGeneratedMethodName(IMethodSymbol methodSymbol)
    {
        var methodName = methodSymbol.Name;
        if (!IsAsyncMethod(methodSymbol))
        {
            return methodName + GetTraceSuffix(methodSymbol);
        }

        // Keep a single Async marker only at the end: <Name>WithTraceAsync.
        var normalizedMethodName = methodName.Replace("Async", string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedMethodName))
        {
            normalizedMethodName = methodName;
        }

        return normalizedMethodName + GetTraceSuffix(methodSymbol);
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

        if (paramType.SpecialType == SpecialType.System_Single)
        {
            var floatValue = Convert.ToSingle(param.ExplicitDefaultValue, CultureInfo.InvariantCulture);
            return FormatFloatLiteral(floatValue);
        }

        if (paramType.SpecialType == SpecialType.System_Double)
        {
            var doubleValue = Convert.ToDouble(param.ExplicitDefaultValue, CultureInfo.InvariantCulture);
            return FormatDoubleLiteral(doubleValue);
        }

        return Convert.ToString(param.ExplicitDefaultValue, CultureInfo.InvariantCulture) ?? "null";
    }

    private static string FormatFloatLiteral(float value) =>
        value.ToString("R", CultureInfo.InvariantCulture) + "f";

    private static string FormatDoubleLiteral(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

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

    private static (string Call, string ReturnStatement, bool HasResultVariable) BuildMethodCall(IMethodSymbol methodSymbol)
    {
        var sb = new StringBuilder();
        var isAsync = IsAsyncMethod(methodSymbol);
        var hasReturnValue = HasReturnValue(methodSymbol);

        if (hasReturnValue)
        {
            sb.Append("var __result = ");
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

        var returnStatement = hasReturnValue ? "return __result;" : string.Empty;
        return (sb.ToString(), returnStatement, hasReturnValue);
    }

    private static readonly string PreCallTagIndent = new string(' ', 12);
    private static readonly string PostCallTagIndent = new string(' ', 16);

    private static string BuildInputTagBlock(
        IMethodSymbol methodSymbol,
        string inputParametersName,
        bool writeTagsToDictionary)
    {
        if (!HasInputTagEntries(methodSymbol))
            return string.Empty;

        if (writeTagsToDictionary)
            return BuildDictionaryInputTagBlock(methodSymbol);

        var sb = new StringBuilder();
        AppendInputTagEntries(sb, methodSymbol, PreCallTagIndent, individualTags: true);
        return sb.ToString();
    }

    private static string BuildDictionaryInputTagBlock(IMethodSymbol methodSymbol)
    {
        var sb = new StringBuilder();
        sb.Append("var __activityTags = new System.Collections.Generic.Dictionary<string, object?>();");
        AppendInputTagEntries(sb, methodSymbol, PreCallTagIndent, individualTags: false, "__activityTags");
        return sb.ToString();
    }

    private static string BuildOutputTagBlock(
        IMethodSymbol methodSymbol,
        string outputParametersName,
        bool hasResultVariable,
        bool writeTagsToDictionary)
    {
        var hasInputTagEntries = HasInputTagEntries(methodSymbol);
        var hasAnyOutputTags = HasOutputTagEntries(methodSymbol, hasResultVariable);

        if (!writeTagsToDictionary)
        {
            if (!hasAnyOutputTags)
                return string.Empty;

            var sb = new StringBuilder();
            AppendOutputTagEntries(sb, methodSymbol, outputParametersName, hasResultVariable, PostCallTagIndent, individualTags: true);
            return sb.ToString();
        }

        if (!hasInputTagEntries && !hasAnyOutputTags)
            return string.Empty;

        var merged = new StringBuilder();

        if (!hasInputTagEntries)
        {
            merged.Append("var __activityTags = new System.Collections.Generic.Dictionary<string, object?>();");
        }

        AppendOutputTagEntries(merged, methodSymbol, outputParametersName, hasResultVariable, PostCallTagIndent, individualTags: false, "__activityTags");

        merged.Append('\n')
              .Append(PostCallTagIndent)
              .Append($"activity?.SetTag(\"{EscapeString(outputParametersName)}\", System.Text.Json.JsonSerializer.Serialize(__activityTags, Utils.TelemetryJsonSerializerOptions));");

        return merged.ToString();
    }

    private static void AppendInputTagEntries(
        StringBuilder sb,
        IMethodSymbol methodSymbol,
        string indent,
        bool individualTags,
        string? dictionaryName = null)
    {
        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.RefKind == RefKind.Out)
                continue;

            var tagName = ExtractTagInfo(parameter);
            if (tagName == null)
                continue;

            var tagValue = GetTagValueExpression(parameter);
            if (tagValue == null)
                continue;

            if (individualTags)
            {
                AppendIndividualSetTag(sb, indent, tagName, tagValue, parameter.Type);
            }
            else
            {
                sb.Append('\n')
                  .Append(indent)
                  .Append($"{dictionaryName}[\"{EscapeString(tagName)}\"] = {tagValue};");
            }
        }
    }

    private static bool HasInputTagEntries(IMethodSymbol methodSymbol) =>
        methodSymbol.Parameters.Any(p =>
            p.RefKind != RefKind.Out &&
            ExtractTagInfo(p) != null &&
            GetTagValueExpression(p) != null);

    private static bool HasOutputTagEntries(IMethodSymbol methodSymbol, bool hasResultVariable)
    {
        if (hasResultVariable)
            return true;

        return methodSymbol.Parameters.Any(p =>
            p.RefKind == RefKind.Out &&
            GetTagValueExpression(p) != null);
    }

    private static void AppendOutputTagEntries(
        StringBuilder sb,
        IMethodSymbol methodSymbol,
        string resultTagName,
        bool hasResultVariable,
        string indent,
        bool individualTags,
        string? dictionaryName = null)
    {
        if (hasResultVariable)
        {
            var resultType = GetMethodResultType(methodSymbol);
            if (individualTags)
            {
                AppendIndividualSetTag(sb, indent, resultTagName, "__result", resultType);
            }
            else
            {
                sb.Append('\n')
                  .Append(indent)
                  .Append($"{dictionaryName}[\"{EscapeString(resultTagName)}\"] = {FormatTagValueExpression("__result", resultType)};");
            }
        }

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.RefKind != RefKind.Out)
                continue;

            var tagName = ExtractTagInfo(parameter) ?? parameter.Name;
            var tagValue = GetTagValueExpression(parameter);

            if (tagValue == null)
                continue;

            if (individualTags)
            {
                AppendIndividualSetTag(sb, indent, tagName, tagValue, parameter.Type);
            }
            else
            {
                sb.Append('\n')
                  .Append(indent)
                  .Append($"{dictionaryName}[\"{EscapeString(tagName)}\"] = {tagValue};");
            }
        }
    }

    private static void AppendIndividualSetTag(
        StringBuilder sb,
        string indent,
        string tagName,
        string valueExpression,
        ITypeSymbol typeSymbol)
    {
        sb.Append('\n')
          .Append(indent)
          .Append($"activity?.SetTag(\"{EscapeString(tagName)}\", {FormatTagValueExpression(valueExpression, typeSymbol)});");
    }

    private static string FormatTagValueExpression(string valueExpression, ITypeSymbol typeSymbol) =>
        ShouldSerializeTagValue(typeSymbol)
            ? $"System.Text.Json.JsonSerializer.Serialize({valueExpression}, Utils.TelemetryJsonSerializerOptions)"
            : valueExpression;

    private static ITypeSymbol GetMethodResultType(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        if (returnType is INamedTypeSymbol namedType && TryUnwrapAsyncReturnType(namedType, out var unwrapped))
        {
            return unwrapped;
        }

        return returnType;
    }

    private static bool TryUnwrapAsyncReturnType(INamedTypeSymbol namedType, out ITypeSymbol unwrapped)
    {
        var definition = namedType.OriginalDefinition;
        if (definition is { IsGenericType: true, Arity: 1 } &&
            definition.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks" &&
            definition.Name is "Task`1" or "ValueTask`1")
        {
            unwrapped = namedType.TypeArguments[0];
            return true;
        }

        unwrapped = namedType;
        return false;
    }

    private static bool IsTupleType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            if (namedType.IsTupleType)
                return true;

            var definitionName = namedType.OriginalDefinition.Name;
            if (definitionName.StartsWith("ValueTuple", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool ShouldSerializeTagValue(ITypeSymbol typeSymbol)
    {
        if (IsTupleType(typeSymbol))
            return true;

        if (IsPrimitiveType(typeSymbol) || IsDateTimeType(typeSymbol) || IsGuidType(typeSymbol))
            return false;

        var actualType = GetUnderlyingType(typeSymbol);
        return actualType.TypeKind != TypeKind.Enum && actualType.Name != "TimeSpan";
    }

    private static string? ExtractTagInfo(IParameterSymbol parameter)
    {
        foreach (var attr in parameter.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == TelemetryGenerationConstants.SpanTagAttributeName)
            {
                var tagName = attr.ConstructorArguments.Length > 0
                    ? attr.ConstructorArguments[0].Value?.ToString() ?? parameter.Name
                    : parameter.Name;

                return tagName;
            }
        }

        return null;
    }

    private static string? GetTagValueExpression(IParameterSymbol parameter)
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

        if (parameterType.TypeKind == TypeKind.Pointer ||
            parameterType.TypeKind == TypeKind.FunctionPointer ||
            parameterType.TypeKind == TypeKind.Dynamic)
        {
            return null;
        }

        return parameter.Name;
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

    private static bool HasReturnValue(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType.ToDisplayString();

        return !methodSymbol.ReturnsVoid &&
               returnType != "System.Threading.Tasks.Task" &&
               returnType != "System.Threading.Tasks.ValueTask";
    }

    private static string EscapeString(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

}
