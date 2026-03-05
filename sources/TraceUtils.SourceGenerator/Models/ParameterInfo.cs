using System.Collections.Immutable;

namespace TraceUtils.SourceGenerator.Models;

/// <summary>
/// Информация о параметре метода
/// </summary>
/// <param name="Name">Имя параметра исходного метода.</param>
/// <param name="Type">Строковое представление типа параметра для генерации кода.</param>
/// <param name="Modifier">Модификатор параметра (`ref`, `out`, `in`) или пустая строка.</param>
/// <param name="IsParams">Признак параметра с модификатором `params`.</param>
/// <param name="HasDefaultValue">Признак наличия значения по умолчанию.</param>
/// <param name="DefaultValueExpression">Выражение значения по умолчанию в формате C#.</param>
/// <param name="HasTracerProperty">Нужно ли создавать теги для параметра.</param>
/// <param name="TagInfos">Коллекция описаний тегов, которые будут записаны в активность.</param>
/// <param name="TagName">Базовое имя тега для параметра (если применимо).</param>
public record struct ParameterInfo(
    string Name,
    string Type,
    string Modifier,
    bool IsParams,
    bool HasDefaultValue,
    string? DefaultValueExpression,
    bool HasTracerProperty,
    ImmutableArray<ParameterTagInfo> TagInfos,
    string? TagName
);
