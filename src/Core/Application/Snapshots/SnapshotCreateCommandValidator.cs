using FluentValidation;

namespace Core.Application.Snapshots;

public sealed class SnapshotCreateCommandValidator : AbstractValidator<SnapshotCreateCommand>
{
    public SnapshotCreateCommandValidator()
    {
        RuleFor(request => request.Repositories)
            .Custom((repositories, context) =>
            {
                if (repositories is null || repositories.Count == 0)
                {
                    context.AddFailure("repositories", "At least one repository is required.");
                    return;
                }

                var rankSet = new HashSet<int>();
                var repoIdSet = new HashSet<string>(StringComparer.Ordinal);
                var maxRank = 0;

                foreach (var repository in repositories)
                {
                    if (repository.Rank < 1)
                    {
                        context.AddFailure("repositories.rank", "Rank must be >= 1.");
                    }

                    if (!rankSet.Add(repository.Rank))
                    {
                        context.AddFailure("repositories.rank", "Rank must be unique within the snapshot.");
                    }

                    if (!repoIdSet.Add(repository.RepoId))
                    {
                        context.AddFailure("repositories.repoId", "repoId must be unique within the snapshot.");
                    }

                    if (repository.Stars < 0)
                    {
                        context.AddFailure("repositories.stars", "Stars must be >= 0.");
                    }

                    if (repository.Forks < 0)
                    {
                        context.AddFailure("repositories.forks", "Forks must be >= 0.");
                    }

                    maxRank = Math.Max(maxRank, repository.Rank);
                }

                if (rankSet.Count == repositories.Count && maxRank != repositories.Count)
                {
                    context.AddFailure("repositories.rank", "Rank values must start at 1 and be contiguous.");
                }
            });
    }
}
