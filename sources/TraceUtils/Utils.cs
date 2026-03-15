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
    public static string ServiceName => Assembly.GetEntryAssembly()?.EntryPoint?.DeclaringType?.Namespace ?? "DefaultProjectName";

    /// <summary>
    /// Версия сервиса
    /// </summary>
    public static string ServiceVersion => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0.0";

    /// <summary>
    /// Источник активностей для сервиса. Публичный для регистрации в OpenTelemetry (AddSource).
    /// </summary>
    public static readonly ActivitySource ActivitySource =
        new(ServiceName, ServiceVersion);

    /// <summary>
    /// Создать новый дочерний span (Activity) с указанным именем
    /// </summary>
    /// <param name="spanName">Имя span</param>
    /// <param name="activityKind"></param>
    public static Activity? StartActivity(string spanName, ActivityKind activityKind) =>
        ActivitySource?.StartActivity(spanName, activityKind);

    /// <summary>
    /// Возвращает true, если данные активности запрашиваются (теги и события будут экспортироваться).
    /// Использует активность, созданную из ActivitySource; когда false — достаточно вызвать метод напрямую.
    /// </summary>
    public static bool ShouldRecordData(Activity? activity) => activity is { IsAllDataRequested: true };

    /// <summary>
    /// Добавить событие в текущий Activity (если есть)
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="tags"></param>
    public static void AddEventToCurrentActivity(string eventName, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        ActivityTagsCollection? eventTags = null;
        if (tags is not null)
        {
            eventTags = new ActivityTagsCollection();
            foreach (var kv in tags)
            {
                eventTags.Add(kv);
            }
        }

        var activityEvent = eventTags is null
            ? new ActivityEvent(eventName)
            : new ActivityEvent(eventName, DateTimeOffset.UtcNow, eventTags);

        activity.AddEvent(activityEvent);
    }

    /// <summary>
    /// Добавить событие в указанный Activity
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="eventName"></param>
    /// <param name="tags"></param>
    public static void AddEvent(Activity activity, string eventName, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        if (activity is null)
        {
            return;
        }

        ActivityTagsCollection? eventTags = null;
        if (tags is not null)
        {
            eventTags = new ActivityTagsCollection();
            foreach (var kv in tags)
            {
                eventTags.Add(kv);
            }
        }

        var activityEvent = eventTags is null
            ? new ActivityEvent(eventName)
            : new ActivityEvent(eventName, DateTimeOffset.UtcNow, eventTags);

        activity.AddEvent(activityEvent);
    }
}
