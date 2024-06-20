using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Shared.Models;

public class AzureSearchService(SearchClient searchClient) : ISearchService
{
    public async Task<SupportingContentRecord[]> QueryDocumentsAsync(
        string? query = null,
        float[]? embedding = null,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        if (query is null && embedding is null)
        {
            throw new ArgumentException("Either query or embedding must be provided");
        }

        var documentContents = string.Empty;
        var top = overrides?.Top ?? 3;
        var exclude_category = overrides?.ExcludeCategory;
        var filter = exclude_category == null ? string.Empty : $"category ne '{exclude_category}'";
        var useSemanticRanker = overrides?.SemanticRanker ?? false;
        var useSemanticCaptions = overrides?.SemanticCaptions ?? false;

        SearchOptions searchOptions = useSemanticRanker
            ? new SearchOptions
            {
                Filter = filter,
                QueryType = SearchQueryType.Semantic,
                SemanticSearch = new()
                {
                    SemanticConfigurationName = "default",
                    QueryCaption = new(useSemanticCaptions
                        ? QueryCaptionType.Extractive
                        : QueryCaptionType.None),
                },
                // TODO: Find if these options are assignable
                //QueryLanguage = "en-us",
                //QuerySpeller = "lexicon",
                Size = top,
            }
            : new SearchOptions
            {
                Filter = filter,
                Size = top,
            };

        if (embedding != null && overrides?.RetrievalMode != RetrievalMode.Text)
        {
            var k = useSemanticRanker ? 50 : top;
            var vectorQuery = new VectorizedQuery(embedding)
            {
                // if semantic ranker is enabled, we need to set the rank to a large number to get more
                // candidates for semantic reranking
                KNearestNeighborsCount = useSemanticRanker ? 50 : top,
            };
            vectorQuery.Fields.Add("embedding");
            searchOptions.VectorSearch = new();
            searchOptions.VectorSearch.Queries.Add(vectorQuery);
        }

        var searchResultResponse = await searchClient.SearchAsync<SearchDocument>(
            query, searchOptions, cancellationToken);
        if (searchResultResponse.Value is null)
        {
            throw new InvalidOperationException("fail to get search result");
        }

        SearchResults<SearchDocument> searchResult = searchResultResponse.Value;

        // Assemble sources here.
        // Example output for each SearchDocument:
        // {
        //   "@search.score": 11.65396,
        //   "id": "Northwind_Standard_Benefits_Details_pdf-60",
        //   "content": "x-ray, lab, or imaging service, you will likely be responsible for paying a copayment or coinsurance. The exact amount you will be required to pay will depend on the type of service you receive. You can use the Northwind app or website to look up the cost of a particular service before you receive it.\nIn some cases, the Northwind Standard plan may exclude certain diagnostic x-ray, lab, and imaging services. For example, the plan does not cover any services related to cosmetic treatments or procedures. Additionally, the plan does not cover any services for which no diagnosis is provided.\nIt’s important to note that the Northwind Standard plan does not cover any services related to emergency care. This includes diagnostic x-ray, lab, and imaging services that are needed to diagnose an emergency condition. If you have an emergency condition, you will need to seek care at an emergency room or urgent care facility.\nFinally, if you receive diagnostic x-ray, lab, or imaging services from an out-of-network provider, you may be required to pay the full cost of the service. To ensure that you are receiving services from an in-network provider, you can use the Northwind provider search ",
        //   "category": null,
        //   "sourcepage": "Northwind_Standard_Benefits_Details-24.pdf",
        //   "sourcefile": "Northwind_Standard_Benefits_Details.pdf"
        // }
        var sb = new List<SupportingContentRecord>();
        foreach (var doc in searchResult.GetResults())
        {
            doc.Document.TryGetValue("sourcepage", out var sourcePageValue);
            string? contentValue;
            try
            {
                if (useSemanticCaptions)
                {
                    var docs = doc.SemanticSearch.Captions.Select(c => c.Text);
                    contentValue = string.Join(" . ", docs);
                }
                else
                {
                    doc.Document.TryGetValue("content", out var value);
                    contentValue = (string)value;
                }
            }
            catch (ArgumentNullException)
            {
                contentValue = null;
            }

            if (sourcePageValue is string sourcePage && contentValue is string content)
            {
                content = content.Replace('\r', ' ').Replace('\n', ' ');
                sb.Add(new SupportingContentRecord(sourcePage, content));
            }
        }

        return [.. sb];
    }

    /// <summary>
    /// query images.
    /// </summary>
    /// <param name="embedding">embedding for imageEmbedding</param>
    public async Task<SupportingImageRecord[]> QueryImagesAsync(
        string? query = null,
        float[]? embedding = null,
        RequestOverrides? overrides = null,
        CancellationToken cancellationToken = default)
    {
        var top = overrides?.Top ?? 3;
        var exclude_category = overrides?.ExcludeCategory;
        var filter = exclude_category == null ? string.Empty : $"category ne '{exclude_category}'";

        var searchOptions = new SearchOptions
        {
            Filter = filter,
            Size = top,
        };

        if (embedding != null)
        {
            var vectorQuery = new VectorizedQuery(embedding)
            {
                KNearestNeighborsCount = top,
            };
            vectorQuery.Fields.Add("imageEmbedding");
            searchOptions.VectorSearch = new();
            searchOptions.VectorSearch.Queries.Add(vectorQuery);
        }

        var searchResultResponse = await searchClient.SearchAsync<SearchDocument>(
                       query, searchOptions, cancellationToken);

        if (searchResultResponse.Value is null)
        {
            throw new InvalidOperationException("fail to get search result");
        }

        SearchResults<SearchDocument> searchResult = searchResultResponse.Value;
        var sb = new List<SupportingImageRecord>();

        foreach (var doc in searchResult.GetResults())
        {
            doc.Document.TryGetValue("sourcefile", out var sourceFileValue);
            doc.Document.TryGetValue("imageEmbedding", out var imageEmbeddingValue);
            doc.Document.TryGetValue("category", out var categoryValue);
            doc.Document.TryGetValue("content", out var imageName);
            if (sourceFileValue is string url &&
                imageName is string name &&
                categoryValue is string category &&
                category == "image")
            {
                sb.Add(new SupportingImageRecord(name, url));
            }
        }

        return [.. sb];
    }
}
