using Ardalis.Result;
using MediatR;

namespace Common.Application;

public interface IQuery<TResponse> 
    : IRequest<Result<TResponse>>;