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

    /// <summary>
    /// Имя поля/тега для записи входных параметров метода.
    /// </summary>
    public string InputParametersName { get; set; } = "input.parameters";

    /// <summary>
    /// Имя поля/тега для записи выходных данных метода.
    /// </summary>
    public string OutputParametersName { get; set; } = "output.parameters";

    public ActivityOperationAttribute(
        string operationName,
        ActivityType activityType,
        string inputParametersName = "input.parameters",
        string outputParametersName = "output.parameters")
    {
        OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        ActivityType = activityType;
        InputParametersName = inputParametersName;
        OutputParametersName = outputParametersName;
    }
}