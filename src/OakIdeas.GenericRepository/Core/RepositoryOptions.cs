using System;
using System.Collections.Generic;

namespace OakIdeas.GenericRepository.Core;

/// <summary>
/// Configuration options for repository instances.
/// Provides a central place to configure repository behavior including middleware.
/// </summary>
public class RepositoryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging.
    /// </summary>
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable performance tracking.
    /// </summary>
    public bool EnablePerformanceTracking { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable validation.
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable middleware pipeline.
    /// </summary>
    public bool EnableMiddleware { get; set; } = true;

    /// <summary>
    /// Gets the list of middleware configurations.
    /// </summary>
    internal List<MiddlewareRegistration> Middlewares { get; } = new();

    /// <summary>
    /// Gets or sets custom configuration values.
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; } = new();

    /// <summary>
    /// Creates a new instance of RepositoryOptions with default settings.
    /// </summary>
    public RepositoryOptions()
    {
    }

    /// <summary>
    /// Gets a custom setting value by key.
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="defaultValue">Default value if key not found</param>
    /// <returns>The setting value or default</returns>
    public T GetSetting<T>(string key, T defaultValue = default!)
    {
        if (CustomSettings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Sets a custom setting value.
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="value">The setting value</param>
    public void SetSetting<T>(string key, T value)
    {
        CustomSettings[key] = value!;
    }
}

/// <summary>
/// Internal class to store middleware registration information.
/// </summary>
internal class MiddlewareRegistration
{
    public Type MiddlewareType { get; set; } = null!;
    public object? Configuration { get; set; }
    public int Order { get; set; }
}
