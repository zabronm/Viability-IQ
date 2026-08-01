using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Projections
{

    /// Centralized state management for financial projections
    /// Handles caching, dependency tracking, and change notifications

    public class ProjectionStateManager : IProjectionStateManager
    {
        #region Private Fields

        private readonly IFinancialCalculationsEngine _financialCalculationsEngine;
        private readonly ILogger<ProjectionStateManager> _logger;

        // Cache for calculation results
        private readonly Dictionary<string, CachedProjection> _projectionCache = new();

        // Dependency graph: tracks which calculations depend on which data
        private readonly Dictionary<string, HashSet<string>> _dependencyGraph = new();

        // Event publishers for reactive updates
        private event EventHandler<ProjectionChangedEventArgs> _projectionChanged;

        #endregion

        #region Public Events

        public event EventHandler<ProjectionChangedEventArgs> ProjectionChanged
        {
            add { _projectionChanged += value; }
            remove { _projectionChanged -= value; }
        }

        #endregion

        #region Constructor

        public ProjectionStateManager(
            IFinancialCalculationsEngine calculationEngine,
            ILogger<ProjectionStateManager> logger)
        {
            _financialCalculationsEngine = calculationEngine ?? throw new ArgumentNullException(nameof(calculationEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Public Methods        
        /// Gets or calculates loan repayment projection with caching        
        public async Task<List<AssessmentLoanRepayment>> GetLoanRepaymentProjectionAsync(
            AssessmentLoan loan, LoanCalculationMethodsEnums method, bool forceRecalculate = false)
        {
            try
            {
                var cacheKey = GenerateCacheKey(loan.AssessmentLoanId, method);

                // Return cached result if available and not forced to recalculate
                if (_projectionCache.TryGetValue(cacheKey, out var cached) && !forceRecalculate)
                {
                    if (!cached.IsExpired())
                    {
                        _logger.LogDebug("Returning cached projection for loan {LoanId}", loan.AssessmentLoanId);
                        return await Task.FromResult(cached.RepaymentData);
                    }
                }

                // Calculate fresh projection
                _logger.LogInformation("Calculating fresh projection for loan {LoanId}", loan.AssessmentLoanId);
                var result = _financialCalculationsEngine.BuildRepaymentRecords(loan, method);

                if (result == null)
                {
                    _logger.LogWarning("Calculation returned null for loan {LoanId}", loan.AssessmentLoanId);
                    return new List<AssessmentLoanRepayment>();
                }

                // Cache the result
                _projectionCache[cacheKey] = new CachedProjection
                {
                    RepaymentData = result,
                    CachedAt = DateTime.UtcNow,
                    CacheDurationSeconds = 300 // 5 minutes default
                };

                // Record dependencies
                RecordDependencies(cacheKey, new[]
                {
                    GetDataKey(loan.AssessmentId, "loan", loan.AssessmentLoanId)
                });

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting loan repayment projection for loan {LoanId}", loan.AssessmentLoanId);
                throw;
            }
        }


        /// Notifies the state manager that data has changed
        /// Invalidates affected caches and triggers recalculation        
        public async Task InvalidateDataAsync(string dataType, long entityId, long assessmentId)  // ✅ Changed int to long
        {
            try
            {
                _logger.LogInformation("Invalidating cache for {DataType} {EntityId} in Assessment {AssessmentId}",
                    dataType, entityId, assessmentId);

                var dataKey = GetDataKey(assessmentId, dataType, entityId);

                // Find all affected projections
                var affectedProjections = FindAffectedProjections(dataKey);

                // Invalidate those projections
                foreach (var projectionKey in affectedProjections)
                {
                    if (_projectionCache.TryGetValue(projectionKey, out var cached))
                    {
                        cached.Invalidate();
                        _logger.LogDebug("Invalidated projection: {ProjectionKey}", projectionKey);
                    }
                }

                // Publish change notification
                PublishChange(dataType, entityId, assessmentId, affectedProjections);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for {DataType}", dataType);
                throw;
            }
        }


        /// Gets all cached projections for an assessment

        public Dictionary<string, CachedProjection> GetAssessmentCache(long assessmentId)  // ✅ Changed int to long
        {
            return _projectionCache
                .Where(kvp => kvp.Key.Contains($"_assessment{assessmentId}_"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }


        /// Clears all projections for an assessment

        public async Task ClearAssessmentCacheAsync(long assessmentId)  // ✅ Changed int to long
        {
            try
            {
                _logger.LogInformation("Clearing all projections for assessment {AssessmentId}", assessmentId);

                var keysToRemove = _projectionCache
                    .Keys
                    .Where(k => k.Contains($"_assessment{assessmentId}_"))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _projectionCache.Remove(key);
                }

                _logger.LogInformation("Cleared {Count} projections for assessment {AssessmentId}", keysToRemove.Count, assessmentId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }


        /// Gets cache statistics for monitoring

        public ProjectionCacheStats GetCacheStats()
        {
            var validCount = _projectionCache.Count(kvp => !kvp.Value.IsExpired());
            var expiredCount = _projectionCache.Count - validCount;

            return new ProjectionCacheStats
            {
                TotalCachedItems = _projectionCache.Count,
                ValidCacheItems = validCount,
                ExpiredCacheItems = expiredCount,
                CacheHitRate = CalculateCacheHitRate()
            };
        }
        #endregion

        #region Private Helper Methods
        private string GenerateCacheKey(long loanId, LoanCalculationMethodsEnums method)
        {
            return $"loan_repayment_{loanId}_{method}";
        }

        private string GetDataKey(long assessmentId, string dataType, long entityId)  // ✅ All long
        {
            return $"assessment{assessmentId}_{dataType}_{entityId}";
        }

        private void RecordDependencies(string projectionKey, string[] dataKeys)
        {
            foreach (var dataKey in dataKeys)
            {
                if (!_dependencyGraph.ContainsKey(dataKey))
                {
                    _dependencyGraph[dataKey] = new HashSet<string>();
                }

                _dependencyGraph[dataKey].Add(projectionKey);
                _logger.LogDebug("Recorded dependency: {DataKey} -> {ProjectionKey}", dataKey, projectionKey);
            }
        }

        private HashSet<string> FindAffectedProjections(string dataKey)
        {
            var affected = new HashSet<string>();

            if (_dependencyGraph.TryGetValue(dataKey, out var dependencies))
            {
                affected.UnionWith(dependencies);
            }

            return affected;
        }

        private void PublishChange(string dataType, long entityId, long assessmentId, HashSet<string> affectedProjections)  // ✅ Changed int to long
        {
            var args = new ProjectionChangedEventArgs
            {
                DataType = dataType,
                EntityId = entityId,
                AssessmentId = assessmentId,
                AffectedProjections = affectedProjections,
                ChangedAt = DateTime.UtcNow
            };

            _projectionChanged?.Invoke(this, args);
            _logger.LogInformation("Published projection change event: {DataType} affecting {Count} projections", dataType, affectedProjections.Count);
        }

        private double CalculateCacheHitRate()
        {
            // TODO: Implement tracking of cache hits vs misses
            return 0.0;
        }
        #endregion

        #region Inner Classes

        /// Represents a cached projection with metadata        
        public class CachedProjection
        {
            public List<AssessmentLoanRepayment> RepaymentData { get; set; }
            public DateTime CachedAt { get; set; }
            public int CacheDurationSeconds { get; set; }
            public bool IsInvalidated { get; set; }

            public bool IsExpired()
            {
                if (IsInvalidated) return true;
                return DateTime.UtcNow.Subtract(CachedAt).TotalSeconds > CacheDurationSeconds;
            }

            public void Invalidate()
            {
                IsInvalidated = true;
            }
        }

        #endregion
    }
}