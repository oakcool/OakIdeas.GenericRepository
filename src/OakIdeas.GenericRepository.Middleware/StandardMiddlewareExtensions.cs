using System;
using OakIdeas.GenericRepository.Middleware;
using OakIdeas.GenericRepository.Middleware.Standard;

namespace OakIdeas.GenericRepository;

/// <summary>
/// Convenience extension methods for registering standard middleware.
/// </summary>
public static class StandardMiddlewareExtensions
{
    /// <summary>
    /// Adds logging middleware to the repository.
    /// </summary>
    public static IGenericRepository<TEntity, TKey> WithLogging<TEntity, TKey>(
        this IGenericRepository<TEntity, TKey> repository,
        Action<string> logger,
        bool logPerformance = true)
        where TEntity : class
        where TKey : notnull
    {
        return repository.WithMiddleware(new LoggingMiddleware<TEntity, TKey>(logger, logPerformance));
    }

    /// <summary>
    /// Adds validation middleware to the repository.
    /// </summary>
    public static IGenericRepository<TEntity, TKey> WithValidation<TEntity, TKey>(
        this IGenericRepository<TEntity, TKey> repository,
        bool throwOnValidationError = true)
        where TEntity : class
        where TKey : notnull
    {
        return repository.WithMiddleware(new ValidationMiddleware<TEntity, TKey>(throwOnValidationError));
    }

    /// <summary>
    /// Adds performance monitoring middleware to the repository.
    /// </summary>
    public static IGenericRepository<TEntity, TKey> WithPerformanceMonitoring<TEntity, TKey>(
        this IGenericRepository<TEntity, TKey> repository,
        Action<string, long> performanceReporter,
        long slowOperationThresholdMs = 1000)
        where TEntity : class
        where TKey : notnull
    {
        return repository.WithMiddleware(
            new PerformanceMiddleware<TEntity, TKey>(performanceReporter, slowOperationThresholdMs));
    }

    /// <summary>
    /// Adds audit middleware to the repository.
    /// </summary>
    public static IGenericRepository<TEntity, TKey> WithAuditing<TEntity, TKey>(
        this IGenericRepository<TEntity, TKey> repository,
        Action<AuditEntry> auditLogger,
        Func<string>? userProvider = null)
        where TEntity : class
        where TKey : notnull
    {
        return repository.WithMiddleware(new AuditMiddleware<TEntity, TKey>(auditLogger, userProvider));
    }
}
