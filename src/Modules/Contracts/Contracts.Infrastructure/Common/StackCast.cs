namespace Contracts.Infrastructure.Common;

internal static class StackCast
{
    extension(object[] stack)
    {
        public T? TryGetStruct<T>(int index) where T : struct
        {
            if (index < 0 || index >= stack.Length)
                return null;

            return stack[index] is T value ? value : null;
        }

        public T? TryGetClass<T>(int index) where T : class
        {
            if (index < 0 || index >= stack.Length)
                return null;

            return stack[index] as T;
        }
    }
}
