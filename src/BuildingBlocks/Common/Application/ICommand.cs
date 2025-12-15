using Ardalis.Result;
using MediatR;

namespace Common.Application;

public interface ICommand 
    : IRequest<Result>, IBaseCommand;

public interface ICommand<TResponse> 
    : IRequest<Result<TResponse>>, IBaseCommand;

public interface IBaseCommand;