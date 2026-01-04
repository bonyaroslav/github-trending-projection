using Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Unit.Infrastructure;

public sealed class DbUpdateExceptionClassifierTests
{
    [Fact]
    public void IsUniqueViolation_ReturnsTrue_WhenSqlStateIsUniqueViolation()
    {
        var exception = new DbUpdateException("boom", new FakeSqlStateException("23505"));

        var result = DbUpdateExceptionClassifier.IsUniqueViolation(exception);

        Assert.True(result);
    }

    [Fact]
    public void IsUniqueViolation_ReturnsFalse_WhenSqlStateIsDifferent()
    {
        var exception = new DbUpdateException("boom", new FakeSqlStateException("99999"));

        var result = DbUpdateExceptionClassifier.IsUniqueViolation(exception);

        Assert.False(result);
    }

    [Fact]
    public void IsUniqueViolation_ReturnsFalse_WhenNoInnerException()
    {
        var exception = new DbUpdateException("boom");

        var result = DbUpdateExceptionClassifier.IsUniqueViolation(exception);

        Assert.False(result);
    }

    private sealed class FakeSqlStateException : Exception
    {
        public FakeSqlStateException(string? sqlState)
        {
            SqlState = sqlState;
        }

        public string? SqlState { get; }
    }
}
