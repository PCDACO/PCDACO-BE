using System.Linq.Expressions;
using System.Reflection;

namespace UseCases.UnitTests.Extensions;

public static class IQueryableExtensions
{
    public static Task<int> ExecuteUpdateAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, T>> updateFactory,
        CancellationToken cancellationToken = default
    )
    {
        // Simulate the update operation
        foreach (var item in source.ToList())
        {
            var updatedItem = updateFactory.Compile().Invoke(item);
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var newValue = property.GetValue(updatedItem);
                property.SetValue(item, newValue);
            }
        }
        return Task.FromResult(source.Count());
    }
}
