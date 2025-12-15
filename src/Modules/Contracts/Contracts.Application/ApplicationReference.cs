using System.Reflection;

namespace Contracts.Application;

public static class ApplicationReference
{
    public static readonly Assembly Assembly = typeof(ApplicationReference).Assembly;
}