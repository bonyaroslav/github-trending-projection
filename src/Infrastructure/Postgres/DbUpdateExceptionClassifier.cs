using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Postgres;

public static class DbUpdateExceptionClassifier
{
    public static bool IsUniqueViolation(DbUpdateException exception)
    {
        var current = exception.InnerException;
        while (current is not null)
        {
            var sqlState = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
            if (string.Equals(sqlState, "23505", StringComparison.Ordinal))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
