namespace Common.Domain;

public interface IRepository<TAggregateRoot> 
    where TAggregateRoot : IAggregateRoot;