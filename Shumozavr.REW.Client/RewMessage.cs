using System.Threading.Channels;
using Shumozavr.REW.Client.Models;

namespace Shumozavr.REW.Client;

public record RewMessage(string Message, RewMessageSource Source);