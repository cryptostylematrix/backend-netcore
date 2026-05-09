using System.Reflection;

namespace Marketing.Application;

public static class ApplicationReference
{
    public static readonly Assembly Assembly = typeof(ApplicationReference).Assembly;
}