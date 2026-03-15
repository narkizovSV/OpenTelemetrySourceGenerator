namespace TraceUtils;

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class ActivityOperationAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// 
    /// </summary>
    public ActivityType ActivityType { get; }

    public ActivityOperationAttribute(string operationName, ActivityType activityType)
    {
        OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        ActivityType = activityType;
    }
}