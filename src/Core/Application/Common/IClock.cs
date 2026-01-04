using System;

namespace Core.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
