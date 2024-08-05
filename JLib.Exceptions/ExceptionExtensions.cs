using JLib.Helper;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JLib.Exceptions;
public static class ExceptionExtensions
{
    /// <summary>
    /// throws a <see cref="JLibAggregateException"/> with the given <paramref name="message"/> containing all <paramref name="exceptions"/> if <paramref name="exceptions"/> is not empty.
    /// </summary>
    /// <typeparam name="T">the exception type</typeparam>
    /// <param name="exceptions"></param>
    /// <param name="message"><see cref="JLibAggregateException.Message"/> of the to-be thrown exception</param>
    public static void ThrowExceptionIfNotEmpty<T>(this IEnumerable<T> exceptions, string message)
        where T : Exception
    {
        var mat = exceptions.Cast<Exception>().ToArray();

        if (!mat.Any())
            return;

        var e = new JLibAggregateException(message, mat);
        throw e;
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="exceptions"></param>
    /// <param name="message"><see cref="JLibAggregateException.Message"/> of the to-be thrown exception</param>
    /// <returns>a <see cref="JLibAggregateException"/> containing all <paramref name="exceptions"/> if <paramref name="exceptions"/> is not empty, otherwise it returns null</returns>
    public static Exception? GetExceptionIfNotEmpty<T>(this IEnumerable<T> exceptions, string message)
        where T : Exception
    {
        var mat = exceptions.Cast<Exception>().ToArray();
        return mat.Any()
            ? new JLibAggregateException(message, mat)
            : null;
    }
    /// <summary>
    /// adds all <paramref name="errors"/> bundled as <see cref="JLibAggregateException"/> to the <see cref="masterExceptionList"/> if <paramref name="errors"/> is not empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="errors"></param>
    /// <param name="message"><see cref="JLibAggregateException.Message"/> of the to-be thrown exception</param>
    /// <param name="masterExceptionList">the list the generated <see cref="JLibAggregateException"/> should be added to</param>
    public static void AddIfNotEmpty<T>(this IEnumerable<T> errors, string message,
        IList<Exception> masterExceptionList)
        where T : Exception
    {
        var mat = errors.Cast<Exception>().ToArray();
        if (mat.Any())
        {
            masterExceptionList.Add(new JLibAggregateException(message, mat));
        }
    }

    /// <summary>
    /// returns a string which visualized the exception as grouped tree
    /// </summary>
    public static string GetHierarchyInfo(this AggregateException exception)
    {
        var sb = new StringBuilder();
        CreateExceptionInfo(sb, exception);
        return sb.ToString();
    }


    private static void CreateExceptionInfo(StringBuilder sb, Exception ex, int indentCount = 0)
    {
        const string indentTemplate = "│  ";
        const string branchTemplate = "├─ ";

        /******************************\
        |           Message            |
        \******************************/
        var lineIndex = 0;
        foreach (var line in (ex is JLibAggregateException jex ? jex.UserMessage : ex.Message)
                 .ReplaceLineEndings()
                 .Split(Environment.NewLine))
        {
            if (indentCount >= 2)
                sb.AppendMultiple(indentTemplate, indentCount - (lineIndex == 0 ? 1 : 0));
            if (indentCount >= 1 && lineIndex == 0)
                sb.Append(branchTemplate);
            sb.AppendLine(line);
            lineIndex++;
        }

        /******************************\
        |  Aggregate Inner Exceptions  |
        \******************************/
        if (ex is AggregateException aggEx && aggEx.InnerExceptions.Any())
        {
            sb.AppendMultiple(indentTemplate, indentCount)
                .Append(branchTemplate)
                .AppendLine("Inner Exceptions");
            foreach (var exg in aggEx.InnerExceptions
                         .GroupBy(iex => iex.GetType()))
            {
                sb.AppendMultiple(indentTemplate, indentCount + 1)
                    .Append(branchTemplate)
                    .Append(exg.Count())
                    .Append(' ')
                    .AppendLine(exg.Key.FullName());
                foreach (var iex in exg)
                    CreateExceptionInfo(sb, iex, indentCount + 3);
            }
        }

        /******************************\
        |         Inner Exception      |
        \******************************/
        else if (ex is not AggregateException && ex.InnerException is not null)
        {
            sb.AppendMultiple(indentTemplate, indentCount)
                .Append(branchTemplate)
                .AppendLine("Inner Exception");
            CreateExceptionInfo(sb, ex.InnerException, indentCount + 2);
        }

        /******************************\
        |        new line at end       |
        \******************************/
        sb.AppendMultiple(indentTemplate, indentCount)
            .AppendLine();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true, 
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Converts the <paramref name="exception"/> object to a JSON string representation.
    /// The content is optimized to be quickly readable by humans, and has no stable schema.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> object to convert.</param>
    /// <param name="options"></param>
    /// <returns>A JSON string representation of the <paramref name="exception"/>.</returns>
    public static string GetHierarchyInfoJson(this Exception exception, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(exception.ToHumanOptimizedJsonObject(), options ?? JsonOptions);

    /// <summary>
    /// Converts the <see cref="Exception"/> object to a <see cref="JsonObject"/> representation.<br/>
    /// The content is optimized to be quickly readable by humans, and has no stable schema.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> object to convert.</param>
    /// <param name="includeType">If false, the exception type will not be included in the resulting json (top level only)</param>
    /// <returns>A <see cref="JsonObject"/> representation of the <paramref name="exception"/>.</returns>
    public static JsonObject ToHumanOptimizedJsonObject(this Exception exception, bool includeType = true)
    {
        var json = new JsonObject();
        json.Add("Message", GetJsonString(exception is JLibAggregateException jLibAgg ? jLibAgg.UserMessage : exception.Message));
        if (includeType && new[] { typeof(Exception), typeof(JLibAggregateException), typeof(AggregateException) }.Contains(exception.GetType()) == false)
            json.Add("Type", exception.GetType().Name);

        if (exception is AggregateException aggregate)
        {

            foreach (var exGroup in aggregate.InnerExceptions.GroupBy(ex => ex.GetType()))
            {
                var exLst = new JsonArray();
                Action<JsonNode?> add;
                if (exGroup.Count() == 1)
                    add = node => json.Add(exGroup.Key.FullName(), node);
                else
                {
                    json.Add($"{exGroup.Count()} {exGroup.Key.FullName()}", exLst);
                    add = node => exLst.Add(node);
                }

                foreach (var ex in exGroup)
                {
                    add(ex.InnerException is null
                        ? GetJsonString(ex.Message)
                        : ex.ToHumanOptimizedJsonObject(false)
                        );
                }
            }

        }
        else
        {
            if (exception.InnerException is null) { }
            else if (exception.InnerException.GetType() == typeof(Exception) && exception.InnerException.InnerException == null)
                json.Add("InnerException", GetJsonString(exception.InnerException.Message));
            else
                json.Add("InnerException", exception.InnerException.ToHumanOptimizedJsonObject());
        }

        JsonNode? GetJsonString(string? text)
        {
            if (text is null)
                return null;

            if (!text.Contains(Environment.NewLine))
                return JsonValue.Create(text);

            var msg = new JsonArray();
            foreach (var line in text.Split(Environment.NewLine))
                msg.Add(line);
            return msg;

        }

        return json;
    }
}
