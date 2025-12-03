# Middleware Implementation Proposals

This document outlines future opportunities for middleware implementation and improvements to the OakIdeas.GenericRepository library.

## Overview

The middleware infrastructure has been successfully implemented with core components and standard middleware. This document identifies further opportunities for enhancement and optimization.

## Completed Components

### Core Infrastructure
- ✅ `IRepositoryMiddleware<TEntity, TKey>` - Core middleware interface
- ✅ `RepositoryMiddlewareBase<TEntity, TKey>` - Base class for middleware
- ✅ `MiddlewareRepository<TEntity, TKey>` - Repository wrapper with middleware pipeline

### Standard Middleware
- ✅ `LoggingMiddleware` - Operation logging with performance metrics
- ✅ `ValidationMiddleware` - DataAnnotations validation
- ✅ `PerformanceMiddleware` - Performance monitoring and slow operation detection
- ✅ `AuditMiddleware` - Audit trail for data modifications

## Proposed Additional Middleware

### 1. CachingMiddleware

**Purpose**: Cache frequently accessed entities to improve read performance.

**Features**:
- Configurable cache duration
- Cache invalidation on updates/deletes
- Support for distributed caching (Redis, etc.)
- Query result caching
- Entity-level caching

**Implementation Priority**: High

**Example Usage**:
```csharp
var cachingMiddleware = new CachingMiddleware<Customer, int>(
    memoryCache,
    cacheDuration: TimeSpan.FromMinutes(5),
    invalidateOnWrite: true
);
```

**Benefits**:
- Reduces database queries
- Improves response times
- Configurable per-entity type
- Easy to enable/disable

### 2. RetryMiddleware

**Purpose**: Automatically retry failed operations with transient errors.

**Features**:
- Configurable retry count
- Exponential backoff
- Retry only on specific exceptions
- Circuit breaker pattern
- Retry policy configuration

**Implementation Priority**: Medium

**Example Usage**:
```csharp
var retryMiddleware = new RetryMiddleware<Customer, int>(
    maxRetries: 3,
    retryDelay: TimeSpan.FromMilliseconds(500),
    retryOn: new[] { typeof(TimeoutException), typeof(DbUpdateException) }
);
```

**Benefits**:
- Improves reliability
- Handles transient failures
- Reduces manual error handling

### 3. AuthorizationMiddleware

**Purpose**: Check permissions before allowing repository operations.

**Features**:
- Role-based access control
- Claims-based authorization
- Operation-level permissions (Create, Read, Update, Delete)
- Entity-level security
- Custom authorization logic

**Implementation Priority**: High

**Example Usage**:
```csharp
var authMiddleware = new AuthorizationMiddleware<Customer, int>(
    currentUser,
    requiredPermission: "Customer.Manage"
);
```

**Benefits**:
- Centralized authorization
- Consistent security enforcement
- Easy to audit

### 4. RateLimitingMiddleware

**Purpose**: Limit the rate of operations to prevent abuse.

**Features**:
- Configurable rate limits
- Per-user rate limiting
- Per-operation rate limiting
- Sliding window algorithm
- Integration with ASP.NET Core rate limiting

**Implementation Priority**: Medium

**Example Usage**:
```csharp
var rateLimitMiddleware = new RateLimitingMiddleware<Customer, int>(
    maxOperationsPerMinute: 100,
    keyProvider: () => currentUser.Id
);
```

### 5. TransactionMiddleware

**Purpose**: Manage database transactions across repository operations.

**Features**:
- Automatic transaction management
- Rollback on errors
- Nested transaction support
- Transaction scope configuration
- Distributed transaction support

**Implementation Priority**: Medium

**Example Usage**:
```csharp
var transactionMiddleware = new TransactionMiddleware<Customer, int>(
    dbContext,
    isolationLevel: IsolationLevel.ReadCommitted
);
```

### 6. EncryptionMiddleware

**Purpose**: Automatically encrypt/decrypt sensitive entity properties.

**Features**:
- Property-level encryption
- Transparent encryption/decryption
- Multiple encryption algorithms
- Key management integration
- Attribute-based configuration

**Implementation Priority**: Low

**Example Usage**:
```csharp
var encryptionMiddleware = new EncryptionMiddleware<Customer, int>(
    encryptionService,
    encryptedProperties: new[] { nameof(Customer.CreditCard) }
);
```

### 7. NotificationMiddleware

**Purpose**: Send notifications when entities are modified.

**Features**:
- Event-based notifications
- Multiple notification channels (email, SMS, webhooks)
- Batch notifications
- Configurable event types
- Async notification delivery

**Implementation Priority**: Low

**Example Usage**:
```csharp
var notificationMiddleware = new NotificationMiddleware<Customer, int>(
    notificationService,
    notifyOn: new[] { OperationType.Insert, OperationType.Delete }
);
```

### 8. CompressionMiddleware

**Purpose**: Compress large text properties before storage.

**Features**:
- Automatic compression/decompression
- Configurable compression threshold
- Multiple compression algorithms
- Property-level configuration

**Implementation Priority**: Low

### 9. TimeZoneMiddleware

**Purpose**: Handle timezone conversions for DateTime properties.

**Features**:
- Automatic UTC conversion
- Configurable target timezone
- Property-level configuration
- Supports DateTimeOffset

**Implementation Priority**: Low

### 10. SanitizationMiddleware

**Purpose**: Sanitize input data to prevent XSS and injection attacks.

**Features**:
- HTML sanitization
- SQL injection prevention
- Script removal
- Configurable sanitization rules

**Implementation Priority**: Medium

## Refactoring Opportunities

### SoftDelete Implementation

**Current Approach**: Inheritance-based with `SoftDeleteMemoryRepository` and `SoftDeleteEntityFrameworkCoreRepository`

**Proposed Approach**: Middleware-based implementation

**Benefits**:
- ✅ Reduces code duplication
- ✅ More flexible composition
- ✅ Easier to enable/disable
- ✅ Works with any repository implementation
- ✅ Can be combined with other middleware

**Migration Strategy**:
1. Create `SoftDeleteMiddleware<TEntity, TKey>`
2. Keep existing implementations for backward compatibility
3. Mark existing implementations as obsolete with migration guidance
4. Update documentation to recommend middleware approach
5. Remove old implementations in next major version

**Implementation Complexity**: Medium

**Example**:
```csharp
// Old approach
var repository = new SoftDeleteMemoryRepository<Customer>();

// New approach with middleware
var repository = new MiddlewareRepository<Customer>(
    new MemoryGenericRepository<Customer>(),
    new SoftDeleteMiddleware<Customer, int>()
);
```

### Timestamp/Audit Fields

**Current Approach**: Manual implementation in applications

**Proposed Approach**: TimestampMiddleware

**Features**:
- Automatic CreatedAt/UpdatedAt timestamps
- CreatedBy/UpdatedBy tracking
- Works with any entity implementing `ITimestamped` or `IAuditable`

### Tenant Isolation

**Current Approach**: Manual filtering in queries

**Proposed Approach**: TenantIsolationMiddleware

**Features**:
- Automatic tenant filtering on reads
- Automatic tenant assignment on writes
- Multi-tenant support
- Configurable tenant provider

## Architecture Improvements

### 1. Middleware Configuration Builder

**Purpose**: Fluent API for configuring middleware pipeline

**Example**:
```csharp
var repository = new RepositoryBuilder<Customer>()
    .UseMemoryRepository()
    .UseLogging(logger)
    .UseValidation()
    .UseCaching(cache, TimeSpan.FromMinutes(5))
    .UseAuditing(auditLog, userProvider)
    .Build();
```

**Benefits**:
- More readable configuration
- Type-safe middleware composition
- Built-in validation
- Convention-based defaults

**Implementation Priority**: Medium

### 2. Middleware Ordering Attributes

**Purpose**: Define middleware execution order via attributes

**Example**:
```csharp
[MiddlewareOrder(10)]
public class ValidationMiddleware { }

[MiddlewareOrder(20)]
public class LoggingMiddleware { }
```

**Benefits**:
- Declarative ordering
- Less error-prone
- Self-documenting

**Implementation Priority**: Low

### 3. Conditional Middleware

**Purpose**: Enable/disable middleware based on conditions

**Example**:
```csharp
.UseWhen(env.IsDevelopment(), builder => 
    builder.UseLogging(console))
```

**Benefits**:
- Environment-specific configuration
- Feature flags support
- A/B testing support

**Implementation Priority**: Low

## Performance Optimizations

### 1. Middleware Compilation

**Purpose**: Pre-compile middleware pipeline for better performance

**Benefits**:
- Eliminates reflection overhead
- Faster pipeline execution
- Lower memory allocations

**Implementation Priority**: Medium

### 2. Async Enumerable Support in Middleware

**Purpose**: Support middleware for streaming operations

**Current Limitation**: GetAsyncEnumerable bypasses middleware

**Proposed Solution**: Add middleware intercept points for streaming

**Implementation Priority**: Medium

### 3. Batch Middleware Optimization

**Purpose**: Optimize middleware for batch operations

**Features**:
- Single validation pass for batch
- Batch logging
- Bulk audit entries

**Implementation Priority**: Low

## Testing Enhancements

### 1. Middleware Testing Utilities

**Purpose**: Helper methods and classes for testing middleware

**Features**:
- Mock repository builders
- Middleware assertion helpers
- Test data generators

**Implementation Priority**: Medium

### 2. Integration Test Suite

**Purpose**: Comprehensive tests for middleware combinations

**Features**:
- Test common middleware combinations
- Performance benchmarks
- Edge case coverage

**Implementation Priority**: Medium

## Documentation Improvements

### 1. Video Tutorials

**Topics**:
- Getting started with middleware
- Creating custom middleware
- Common patterns and best practices

**Implementation Priority**: Low

### 2. Interactive Examples

**Purpose**: Live examples in documentation

**Platform**: .NET Fiddle or similar

**Implementation Priority**: Low

### 3. Migration Guides

**Purpose**: Help users migrate from old patterns to middleware

**Topics**:
- Decorator to middleware
- Inheritance to middleware
- Manual validation to ValidationMiddleware

**Implementation Priority**: High

## Community Contributions

### Desired Middleware from Community

- Database sharding middleware
- GraphQL integration middleware
- SignalR notification middleware
- Elasticsearch indexing middleware
- Message queue middleware
- Change tracking middleware
- Versioning middleware

## Roadmap

### Phase 1 (Current) - Foundation ✅
- Core middleware infrastructure
- Standard middleware (Logging, Validation, Audit, Performance)
- Basic documentation
- Test coverage

### Phase 2 - Enhancement
- CachingMiddleware
- AuthorizationMiddleware
- RetryMiddleware
- SoftDelete refactoring
- Configuration builder
- Enhanced documentation

### Phase 3 - Optimization
- Performance optimizations
- Async enumerable support
- Advanced middleware
- Community middleware templates

### Phase 4 - Ecosystem
- Middleware marketplace
- Community contributions
- Third-party integrations
- Advanced patterns

## Conclusion

The middleware infrastructure provides a solid foundation for extending repository functionality. The proposals outlined here will further enhance the library's capabilities while maintaining simplicity and flexibility.

Next steps:
1. Prioritize high-priority middleware (Caching, Authorization)
2. Begin SoftDelete refactoring
3. Implement configuration builder
4. Enhance documentation with migration guides
5. Gather community feedback on proposals
