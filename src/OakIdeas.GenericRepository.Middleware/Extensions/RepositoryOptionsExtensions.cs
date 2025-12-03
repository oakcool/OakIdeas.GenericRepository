using System;
using OakIdeas.GenericRepository.Core;

namespace OakIdeas.GenericRepository.Middleware.Extensions;

/// <summary>
/// Extension methods for configuring repository options with middleware.
/// </summary>
public static class RepositoryOptionsExtensions
{
    /// <summary>
    /// Configures repository options using a fluent builder pattern.
    /// </summary>
    /// <param name="options">The repository options to configure</param>
    /// <param name="configure">The configuration action</param>
    /// <returns>The configured repository options</returns>
    public static RepositoryOptions Configure(this RepositoryOptions options, Action<RepositoryOptions> configure)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        configure(options);
        return options;
    }

    /// <summary>
    /// Enables logging for repository operations.
    /// </summary>
    /// <param name="options">The repository options</param>
    /// <param name="enabled">Whether to enable logging</param>
    /// <returns>The repository options for chaining</returns>
    public static RepositoryOptions WithLogging(this RepositoryOptions options, bool enabled = true)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.EnableLogging = enabled;
        return options;
    }

    /// <summary>
    /// Enables performance tracking for repository operations.
    /// </summary>
    /// <param name="options">The repository options</param>
    /// <param name="enabled">Whether to enable performance tracking</param>
    /// <returns>The repository options for chaining</returns>
    public static RepositoryOptions WithPerformanceTracking(this RepositoryOptions options, bool enabled = true)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.EnablePerformanceTracking = enabled;
        return options;
    }

    /// <summary>
    /// Enables validation for repository operations.
    /// </summary>
    /// <param name="options">The repository options</param>
    /// <param name="enabled">Whether to enable validation</param>
    /// <returns>The repository options for chaining</returns>
    public static RepositoryOptions WithValidation(this RepositoryOptions options, bool enabled = true)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.EnableValidation = enabled;
        return options;
    }

    /// <summary>
    /// Enables or disables the middleware pipeline.
    /// </summary>
    /// <param name="options">The repository options</param>
    /// <param name="enabled">Whether to enable middleware</param>
    /// <returns>The repository options for chaining</returns>
    public static RepositoryOptions WithMiddleware(this RepositoryOptions options, bool enabled = true)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.EnableMiddleware = enabled;
        return options;
    }

    /// <summary>
    /// Sets a custom setting value.
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="options">The repository options</param>
    /// <param name="key">The setting key</param>
    /// <param name="value">The setting value</param>
    /// <returns>The repository options for chaining</returns>
    public static RepositoryOptions WithSetting<T>(this RepositoryOptions options, string key, T value)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.SetSetting(key, value);
        return options;
    }
}
