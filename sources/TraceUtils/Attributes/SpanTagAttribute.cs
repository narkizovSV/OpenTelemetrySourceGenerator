namespace TraceUtils;

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class SpanTagAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string TagName { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tagName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SpanTagAttribute(string tagName)
    {
        TagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
    }
}
