namespace TraceUtils.SourceGenerator.Models;

/// <summary>
/// Информация об одном теге, который нужно проставить для параметра.
/// </summary>
/// <param name="TagName">Имя тега в активности.</param>
/// <param name="ValueExpression">C#-выражение, вычисляющее значение тега.</param>
/// <param name="ShouldSerializeValue">Нужно ли сериализовать значение в JSON перед установкой тега.</param>
public record struct ParameterTagInfo(
    string TagName,
    string ValueExpression,
    bool ShouldSerializeValue
);
