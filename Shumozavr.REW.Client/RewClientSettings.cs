using System.ComponentModel.DataAnnotations;
using Shumozavr.Common;

namespace Shumozavr.REW.Client;

public class RewClientSettings : IOptionsValue
{
    public static string OptionsKey => "RewClient";

    [Url]
    [Required(AllowEmptyStrings = false)]
    public string BaseAddress { get; set; } = null!;
}