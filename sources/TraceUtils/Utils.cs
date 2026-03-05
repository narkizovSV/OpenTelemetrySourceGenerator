using System.Diagnostics;
using System.Reflection;

namespace TraceUtils;

/// <summary>
/// Вспомогательные методы для работы с open telemetry
/// </summary>
public static class Utils
{
    /// <summary>
    /// Имя сервиса
    /// </summary>
    public static string ServiceName => Assembly.GetEntryAssembly()?.EntryPoint?.DeclaringType?.Namespace ?? "MedControl";

    /// <summary>
    /// Версия сервиса
    /// </summary>
    public static string ServiceVersion => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0.0";

    /// <summary>
    /// Источник активностей для сервиса
    /// </summary>
    private static readonly ActivitySource ActivitySource =
        new(ServiceName, ServiceVersion);

    /// <summary>
    /// Создать новый дочерний span (Activity) с указанным именем
    /// </summary>
    /// <param name="spanName">Имя span</param>
    /// <param name="activityKind"></param>
    public static Activity? StartActivity(string spanName, ActivityKind activityKind) =>
        ActivitySource?.StartActivity(spanName, activityKind);

}
