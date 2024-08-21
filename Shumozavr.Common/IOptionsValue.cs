namespace Shumozavr.Common;

public interface IOptionsValue
{
    /// <summary>
    /// Key in configuration
    /// </summary>
    public static abstract string OptionsKey { get; }
}