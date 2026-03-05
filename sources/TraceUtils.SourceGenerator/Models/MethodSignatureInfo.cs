using System.Collections.Immutable;

namespace TraceUtils.SourceGenerator.Models;

/// <summary>
/// Информация о сигнатуре метода для генерации
/// </summary>
/// <param name="ContainingNamespace">Пространство имён типа, где объявлен исходный метод.</param>
/// <param name="ContainingType">Имя типа, где объявлен исходный метод.</param>
/// <param name="MethodName">Имя исходного метода.</param>
/// <param name="ReturnType">Строковое представление возвращаемого типа.</param>
/// <param name="OperationName">Имя операции для телеметрии.</param>
/// <param name="IsAsync">Признак того, что метод возвращает `Task`/`ValueTask`.</param>
/// <param name="IsGeneric">Признак generic-метода.</param>
/// <param name="GenericConstraints">Строка ограничений generic-параметров.</param>
/// <param name="Parameters">Параметры метода в нормализованном виде.</param>
/// <param name="GenericTypeParameters">Имена generic-параметров метода.</param>
public record struct MethodSignatureInfo(
    string ContainingNamespace,
    string ContainingType,
    string MethodName,
    string ReturnType,
    string OperationName,
    bool IsAsync,
    bool IsGeneric,
    string? GenericConstraints,
    ImmutableArray<ParameterInfo> Parameters,
    ImmutableArray<string> GenericTypeParameters
);
