using System.Collections.Immutable;
using System.Text;
using TraceUtils.SourceGenerator.Models;

namespace TraceUtils.SourceGenerator.Generation;

/// <summary>
/// Формирует текст тела extension-метода на основе собранной сигнатуры.
/// </summary>
internal static class TelemetryExtensionMethodRenderer
{
    public static string GenerateExtensionMethodBody(MethodSignatureInfo methodInfo)
    {
        var genericParams = methodInfo.IsGeneric && methodInfo.GenericTypeParameters.Length > 0
            ? "<" + string.Join(", ", methodInfo.GenericTypeParameters) + ">"
            : string.Empty;

        var parameters = string.Join(", ", methodInfo.Parameters.Select(BuildParameterDeclaration));
        var parametersWithComma = methodInfo.Parameters.Any()
            ? ", " + parameters
            : string.Empty;

        var isTaskLike =
            methodInfo.ReturnType.Contains("System.Threading.Tasks.Task", StringComparison.Ordinal) ||
            methodInfo.ReturnType.Contains("Task", StringComparison.Ordinal) ||
            methodInfo.ReturnType.Contains("global::System.Threading.Tasks.Task", StringComparison.Ordinal) ||
            methodInfo.ReturnType.Contains("System.Threading.Tasks.ValueTask", StringComparison.Ordinal) ||
            methodInfo.ReturnType.Contains("ValueTask", StringComparison.Ordinal) ||
            methodInfo.ReturnType.Contains("global::System.Threading.Tasks.ValueTask", StringComparison.Ordinal);

        var asyncKeyword = isTaskLike ? "async " : string.Empty;
        var awaitKeyword = isTaskLike ? "await " : string.Empty;
        var generatedMethodName = BuildGeneratedMethodName(methodInfo.MethodName, isTaskLike);

        var preCallTagLines = new List<string>();
        var postCallTagLines = new List<string>();
        foreach (var param in methodInfo.Parameters.Where(p => p.HasTracerProperty))
        {
            foreach (var tagInfo in param.TagInfos)
            {
                var tagValueExpression = tagInfo.ShouldSerializeValue
                    ? $"System.Text.Json.JsonSerializer.Serialize({tagInfo.ValueExpression})"
                    : tagInfo.ValueExpression;

                var tagLine = $"activity?.SetTag(\"{tagInfo.TagName}\", {tagValueExpression});";
                if (string.Equals(param.Modifier, "out", StringComparison.Ordinal))
                {
                    postCallTagLines.Add(tagLine);
                }
                else
                {
                    preCallTagLines.Add(tagLine);
                }
            }
        }
        var hasPostCallTags = postCallTagLines.Count > 0;

        var callExpression =
            $"instance.{methodInfo.MethodName}{genericParams}({string.Join(", ", methodInfo.Parameters.Select(BuildArgumentExpression))})";
        var returnValueVariableName = BuildReturnValueVariableName(methodInfo.Parameters);

        string callStatement;
        var isNonGenericTaskLike = methodInfo.ReturnType == "System.Threading.Tasks.Task" ||
                                   methodInfo.ReturnType == "Task" ||
                                   methodInfo.ReturnType == "global::System.Threading.Tasks.Task" ||
                                   methodInfo.ReturnType == "System.Threading.Tasks.ValueTask" ||
                                   methodInfo.ReturnType == "ValueTask" ||
                                   methodInfo.ReturnType == "global::System.Threading.Tasks.ValueTask";

        if (isTaskLike && isNonGenericTaskLike)
        {
            callStatement = $"{awaitKeyword}{callExpression};";
        }
        else if (isTaskLike)
        {
            callStatement = hasPostCallTags
                ? $"var {returnValueVariableName} = {awaitKeyword}{callExpression};"
                : $"return {awaitKeyword}{callExpression};";
        }
        else if (methodInfo.ReturnType == "void")
        {
            callStatement = $"{callExpression};";
        }
        else
        {
            callStatement = hasPostCallTags
                ? $"var {returnValueVariableName} = {callExpression};"
                : $"return {callExpression};";
        }

        if (hasPostCallTags && (methodInfo.ReturnType != "void" && !isNonGenericTaskLike))
        {
            postCallTagLines.Add($"return {returnValueVariableName};");
        }

        var preCallTagsBlock = BuildTagBlock(preCallTagLines, 8);
        var postCallTagsBlock = BuildTagBlock(postCallTagLines, 16);

        var genericConstraints = string.IsNullOrEmpty(methodInfo.GenericConstraints)
            ? string.Empty
            : methodInfo.GenericConstraints;

        return TelemetryGenerationConstants.ExtensionMethodTemplate
            .Replace("{{Async}}", asyncKeyword)
            .Replace("{{ReturnType}}", methodInfo.ReturnType.Replace("global::", string.Empty))
            .Replace("{{GeneratedMethodName}}", generatedMethodName)
            .Replace("{{GenericParams}}", genericParams)
            .Replace("{{ContainingType}}", methodInfo.ContainingType)
            .Replace("{{ParametersWithComma}}", parametersWithComma)
            .Replace("{{GenericConstraints}}", genericConstraints)
            .Replace("{{OperationName}}", methodInfo.OperationName)
            .Replace("{{PreCallTags}}", preCallTagsBlock)
            .Replace("{{PostCallTags}}", postCallTagsBlock)
            .Replace("{{Call}}", callStatement)
            .Replace("global::", string.Empty);
    }

    private static string BuildParameterDeclaration(ParameterInfo parameter)
    {
        var prefix = new StringBuilder();
        if (parameter.IsParams)
            prefix.Append("params ");
        if (!string.IsNullOrEmpty(parameter.Modifier))
            prefix.Append(parameter.Modifier).Append(' ');

        var defaultValue = parameter.HasDefaultValue
            ? $" = {parameter.DefaultValueExpression ?? "default"}"
            : string.Empty;

        return $"{prefix}{parameter.Type} {parameter.Name}{defaultValue}";
    }

    private static string BuildArgumentExpression(ParameterInfo parameter)
    {
        return string.IsNullOrEmpty(parameter.Modifier)
            ? parameter.Name
            : $"{parameter.Modifier} {parameter.Name}";
    }

    private static string BuildReturnValueVariableName(ImmutableArray<ParameterInfo> parameters)
    {
        const string baseName = "__traceResult";
        var usedNames = new HashSet<string>(parameters.Select(p => p.Name), StringComparer.Ordinal);

        if (!usedNames.Contains(baseName))
            return baseName;

        var index = 1;
        while (usedNames.Contains($"{baseName}{index}"))
            index++;

        return $"{baseName}{index}";
    }

    private static string BuildTagBlock(IReadOnlyList<string> lines, int subsequentIndentSize)
    {
        if (lines.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        builder.Append(lines[0]);

        if (lines.Count == 1)
            return builder.ToString();

        var subsequentIndent = new string(' ', subsequentIndentSize);
        for (var i = 1; i < lines.Count; i++)
        {
            builder.Append('\n');
            builder.Append(subsequentIndent);
            builder.Append(lines[i]);
        }

        return builder.ToString();
    }

    private static string BuildGeneratedMethodName(string originalMethodName, bool isTaskLike)
    {
        if (!isTaskLike)
            return $"{originalMethodName}WithTrace";

        var baseName = originalMethodName.EndsWith("Async", StringComparison.Ordinal)
            ? originalMethodName.Substring(0, originalMethodName.Length - "Async".Length)
            : originalMethodName;

        return $"{baseName}WithTraceAsync";
    }
}
