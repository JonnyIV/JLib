using JLib.Exceptions;
using JLib.Helper;

namespace JLib.Testing;

public record struct ExceptionSnapshotInfo
    (string Type, IReadOnlyCollection<string> MessageLines, IReadOnlyCollection<ExceptionSnapshotInfo> InnerExceptions)
{
    public ExceptionSnapshotInfo(Exception exception) : this(
        exception.GetType().FullName(true),
        (exception.As<JLibAggregateException>()
             ?.UserMessage
         ?? exception.Message)
        .Split(Environment.NewLine),
        exception.As<AggregateException>()
            ?.InnerExceptions
            .Select(e => new ExceptionSnapshotInfo(e))
            .OrderBy(e => e.Type)
            .ThenBy(e => e.MessageLines.First())
            .ToArray()
        ?? Array.Empty<ExceptionSnapshotInfo>()
    )
    {
    }

}