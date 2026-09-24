using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Librago.Loans;

namespace Librago.Connectors.Nantes;

internal static class NantesLoanParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static NantesLoanPage Parse(string json, string borrower)
    {
        NantesLoansResponse? response;

        try
        {
            response = JsonSerializer.Deserialize<NantesLoansResponse>(json, JsonOptions);
        }
        catch (JsonException)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nantes loans response was not valid JSON in the expected format.");
        }

        if (response is null || response.Total is null or < 0)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nantes loans response was incomplete.");
        }

        if (response.Items is null)
        {
            throw new LibraryConnectorException(
                LibraryConnectorFailureKind.UnexpectedResponse,
                "The Nantes loans response did not contain its items.");
        }

        var loans = response.Items.Select(item => Map(item, borrower)).ToArray();
        return new NantesLoanPage(loans, response.Total.Value);
    }

    private static LoanSnapshot Map(NantesLoanItem item, string borrower)
    {
        var data = item.Data ?? throw new LibraryConnectorException(
            LibraryConnectorFailureKind.InvalidData,
            "A Nantes loan did not contain its data object.");
        var title = Required(data.Title, "title");
        var dueOn = ParseDate(Required(data.ReturnDate, "return date"), "return date");
        var borrowedOn = string.IsNullOrWhiteSpace(data.LoanDate)
            ? (DateOnly?)null
            : ParseDate(data.LoanDate, "loan date");
        var externalId = string.IsNullOrWhiteSpace(data.DocumentNumber)
            ? ExternalLoanId.FromFallback(data.Isbn13, data.Isbn, title, data.ReturnDate)
            : data.DocumentNumber.Trim();

        return new LoanSnapshot(
            externalId,
            Required(borrower, "configured borrower"),
            title,
            Clean(data.Author),
            Clean(data.CategoryLabel),
            Clean(data.Branch?.Description),
            borrowedOn,
            dueOn);
    }

    private static DateOnly ParseDate(string value, string field)
    {
        if (DateOnly.TryParseExact(
                value.Trim(),
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        throw new LibraryConnectorException(
            LibraryConnectorFailureKind.InvalidData,
            $"A Nantes loan contained an invalid {field}.");
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new LibraryConnectorException(
                LibraryConnectorFailureKind.InvalidData,
                $"A Nantes loan did not contain a {field}.")
            : value.Trim();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class NantesLoansResponse
    {
        [JsonPropertyName("items")]
        public List<NantesLoanItem>? Items { get; init; }

        [JsonPropertyName("total")]
        public int? Total { get; init; }
    }

    private sealed class NantesLoanItem
    {
        [JsonPropertyName("data")]
        public NantesLoanData? Data { get; init; }
    }

    private sealed class NantesLoanData
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("author")]
        public string? Author { get; init; }

        [JsonPropertyName("returnDate")]
        public string? ReturnDate { get; init; }

        [JsonPropertyName("loanDate")]
        public string? LoanDate { get; init; }

        [JsonPropertyName("documentNumber")]
        public string? DocumentNumber { get; init; }

        [JsonPropertyName("isbn")]
        public string? Isbn { get; init; }

        [JsonPropertyName("isbn13")]
        public string? Isbn13 { get; init; }

        [JsonPropertyName("categoryLabel")]
        public string? CategoryLabel { get; init; }

        [JsonPropertyName("branch")]
        public NantesBranch? Branch { get; init; }
    }

    private sealed class NantesBranch
    {
        [JsonPropertyName("desc")]
        public string? Description { get; init; }
    }
}

internal sealed record NantesLoanPage(IReadOnlyList<LoanSnapshot> Loans, int Total);
