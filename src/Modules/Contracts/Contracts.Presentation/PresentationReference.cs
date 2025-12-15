using System.Reflection;

namespace Contracts.Presentation;

public static class PresentationReference
{
    public static readonly Assembly Assembly = typeof(PresentationReference).Assembly;
}