using MediaHub.Core;
using Microsoft.EntityFrameworkCore;

namespace MediaHub.Behaviors
{
    /// <summary>
    /// Pipeline behavior to handle transactions
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly DbContext _dbContext;

        public TransactionBehavior(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Only apply transaction to commands (not queries)
            if (!IsQuery(request))
            {
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                    
                    var response = await next();
                    
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    
                    return response;
                });
            }
            
            return await next();
        }

        private bool IsQuery(TRequest request)
        {
            return request.GetType().Name.EndsWith("Query");
        }
    }
}