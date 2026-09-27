using System.Text.Json;

namespace Librago.Connectors.Nantes;

internal static class NantesCatalogueTitles
{
    private static readonly Action<ILogger, Exception?> LogFailure = LoggerMessage.Define(
        LogLevel.Warning, new EventId(1202), "A Nantes catalogue title batch could not be enriched; source titles were retained.");

    internal static async Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        HttpClient client, string token, string siteKey, string json, string identityField, string volumeField,
        IReadOnlySet<string> includedIds, ILogger logger, CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(json);
        var entries = new List<Entry>();
        foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
        {
            var data = item.GetProperty("data");
            var id = Text(data, identityField);
            if (id is null || !includedIds.Contains(id)) continue;
            var seq = Text(data, "seqNo") ?? Text(data, "seq_no");
            var bac = Text(data, "bacNo");
            var reference = seq is not null ? "p::usmarcdef_" + seq.PadLeft(10, '0')
                : bac is not null ? "p::usmarcdef_" + bac : null;
            if (reference is null || !TrySuffix(data, volumeField, out var suffix)) continue;
            entries.Add(new Entry(id, reference, seq ?? bac!, suffix));
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var batch in entries.DistinctBy(e => e.Reference).Chunk(100))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout ?? TimeSpan.FromSeconds(10));
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/in/rest/api/resolveBySeqNo");
                request.Headers.Add("X-InMedia-Authorization", $"Bearer {token} {siteKey}");
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["locale"] = "fr",
                    ["ids"] = string.Join(',', batch.Select(e => e.QueryId))
                });
                using var response = await client.SendAsync(request, deadline.Token);
                response.EnsureSuccessStatusCode();
                using var notices = JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
                if (!notices.RootElement.TryGetProperty("resultSet", out var set) || set.ValueKind != JsonValueKind.Array)
                    throw new JsonException();
                var titles = new Dictionary<string, string?>(StringComparer.Ordinal);
                foreach (var notice in set.EnumerateArray())
                {
                    var reference = FirstValue(notice, "id");
                    if (reference is null) continue;
                    var title = FirstValue(notice, "title");
                    if (!titles.TryAdd(reference, title)) titles[reference] = null;
                }
                var references = batch.Select(e => e.Reference).ToHashSet(StringComparer.Ordinal);
                foreach (var entry in entries.Where(e => references.Contains(e.Reference)))
                    if (titles.TryGetValue(entry.Reference, out var title) && title is not null)
                        result[entry.Id] = title + entry.Suffix;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException or JsonException or InvalidOperationException)
            {
                LogFailure(logger, null);
            }
        }
        return result;
    }

    private static string? FirstValue(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var values) ||
            values.ValueKind != JsonValueKind.Array || values.GetArrayLength() == 0 ||
            values[0].ValueKind != JsonValueKind.Object || !values[0].TryGetProperty("value", out var value) ||
            value.ValueKind != JsonValueKind.String) return null;
        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static string? Text(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value)) return null;
        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static bool TrySuffix(JsonElement data, string volumeField, out string suffix)
    {
        suffix = "";
        if (!TryFlag(data, "hasIssueCaption", out var issue)) return false;
        if (!issue)
        {
            if (!TryFlag(data, "hasVolume", out var volume)) return false;
            if (!volume) return true;
        }
        var text = Text(data, issue ? "issueCaption" : volumeField);
        if (text is null) return false;
        suffix = " - " + text;
        return true;
    }

    private static bool TryFlag(JsonElement data, string name, out bool flag)
    {
        flag = false;
        if (!data.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return true;
        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        flag = value.GetBoolean();
        return true;
    }

    private sealed record Entry(string Id, string Reference, string QueryId, string Suffix);
}
