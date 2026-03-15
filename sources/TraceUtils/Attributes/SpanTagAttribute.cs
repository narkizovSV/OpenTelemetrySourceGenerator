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
    public bool ShouldSerialize { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tagName"></param>
    /// <param name="shouldSerialize"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SpanTagAttribute(string tagName, bool shouldSerialize = false)
    {
        TagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
        ShouldSerialize = shouldSerialize;
    }
}
