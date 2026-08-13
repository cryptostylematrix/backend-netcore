namespace ReferalProgram.Application.Abstractions;

public interface ITonAddressComparer
{
    bool AreEqual(string? left, string? right);
}
