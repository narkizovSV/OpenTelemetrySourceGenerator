namespace TraceUtils.Attributes;

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class TracerPropertyAttribute : Attribute
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
    public TracerPropertyAttribute(string tagName)
    {
        TagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
    }
}
