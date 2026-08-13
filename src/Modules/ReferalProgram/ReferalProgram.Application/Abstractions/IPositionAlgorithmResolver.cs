namespace ReferalProgram.Application.Abstractions;

public interface IPositionAlgorithmResolver
{
    IPositionAlgorithmStrategy Resolve(string name);
}
