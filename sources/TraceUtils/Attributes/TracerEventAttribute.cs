namespace TraceUtils.Attributes;

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TracerEventAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string OperationName { get; }

    public TracerEventAttribute(string operationName)
    {
        OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
    }
}
