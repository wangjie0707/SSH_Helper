namespace Tools;


/// <summary>
/// 对 string 的扩展方法。
/// </summary>
public static class ToolsExtension
{
    /// <summary>
    /// IsNullOrEmpty 自动Trim()
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty(this string value)
    {
        if (value == null)
        {
            return true;
        }

        return string.IsNullOrEmpty(value.Trim());
    }

}