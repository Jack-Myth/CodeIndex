﻿using System;
using System.Collections.Generic;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using Microsoft.Extensions.Logging;

namespace CodeIndex.Search
{
    public class SearchService
    {
        public SearchService(CodeIndexConfiguration codeIndexConfiguration, ILogger<SearchService> log, CodeIndexSearcher codeIndexSearcher)
        {
            CodeIndexConfiguration = codeIndexConfiguration;
            Log = log;
            CodeIndexSearcher = codeIndexSearcher;
        }

        CodeIndexConfiguration CodeIndexConfiguration { get; }
        ILogger<SearchService> Log { get; }
        CodeIndexSearcher CodeIndexSearcher { get; }

        public FetchResult<IEnumerable<CodeSource>> GetCodeSources(SearchRequest searchRequest)
        {
            FetchResult<IEnumerable<CodeSource>> result;

            try
            {
                searchRequest.RequireNotNull(nameof(searchRequest));

                result = new FetchResult<IEnumerable<CodeSource>>
                {
                    Result = SearchCodeSource(searchRequest, out _),
                    Status = new Status
                    {
                        Success = true
                    }
                };

                if (searchRequest.Preview)
                {
                    foreach (var item in result.Result)
                    {
                        item.Content = CodeIndexSearcher.GenerateHtmlPreviewText(searchRequest.Content, item.Content, 30, searchRequest.IndexPk, caseSensitive: searchRequest.CaseSensitive);
                    }
                }
                else
                {
                    foreach (var item in result.Result)
                    {
                        item.Content = CodeIndexSearcher.GenerateHtmlPreviewText(searchRequest.Content, item.Content, int.MaxValue, searchRequest.IndexPk, returnRawContentWhenResultIsEmpty: true, caseSensitive: searchRequest.CaseSensitive);
                    }
                }

                Log.LogDebug($"GetCodeSources {searchRequest} finished");
            }
            catch (Exception ex)
            {
                result = new FetchResult<IEnumerable<CodeSource>>
                {
                    Status = new Status
                    {
                        Success = false,
                        StatusDesc = ex.ToString()
                    }
                };

                Log.LogError(ex, $"GetCodeSources {searchRequest} failed");
            }

            return result;
        }

        public FetchResult<IEnumerable<CodeSourceWithMatchedLine>> GetCodeSourcesWithMatchedLine(SearchRequest searchRequest)
        {
            FetchResult<IEnumerable<CodeSourceWithMatchedLine>> result;

            try
            {
                searchRequest.RequireNotNull(nameof(searchRequest));

                var codeSources = SearchCodeSource(searchRequest, out var showResults);

                var queryForContent = CodeIndexSearcher.GetContentQueryFromStr(searchRequest.Content, searchRequest.IndexPk, searchRequest.CaseSensitive);

                var codeSourceWithMatchedLineList = new List<CodeSourceWithMatchedLine>();

                result = new FetchResult<IEnumerable<CodeSourceWithMatchedLine>>
                {
                    Result = codeSourceWithMatchedLineList,
                    Status = new Status
                    {
                        Success = true
                    }
                };

                if (queryForContent != null)
                {
                    var totalResult = 0;

                    foreach (var codeSource in codeSources)
                    {
                        var matchedLines = CodeIndexSearcher.GeneratePreviewTextWithLineNumber(queryForContent, codeSource.Content, int.MaxValue, showResults - totalResult, searchRequest.IndexPk, forWeb: searchRequest.ForWeb, needReplaceSuffixAndPrefix: searchRequest.NeedReplaceSuffixAndPrefix, caseSensitive: searchRequest.CaseSensitive);
                        codeSource.Content = string.Empty; // Empty content to reduce response size

                        foreach (var matchedLine in matchedLines)
                        {
                            totalResult++;

                            codeSourceWithMatchedLineList.Add(new CodeSourceWithMatchedLine(codeSource, matchedLine.LineNumber, matchedLine.MatchedLineContent));
                        }
                    }
                }
                else
                {
                    codeSourceWithMatchedLineList.AddRange(codeSources.Select(u =>
                    {
                        u.Content = string.Empty; // Empty content to reduce response size
                        return new CodeSourceWithMatchedLine(u, 1, string.Empty);
                    }));
                }

                Log.LogDebug($"GetCodeSources {searchRequest} successful");
            }
            catch (Exception ex)
            {
                result = new FetchResult<IEnumerable<CodeSourceWithMatchedLine>>
                {
                    Status = new Status
                    {
                        Success = false,
                        StatusDesc = ex.ToString()
                    }
                };

                Log.LogError(ex, $"GetCodeSources {searchRequest} failed");
            }

            return result;
        }

        public FetchResult<IEnumerable<string>> GetHints(string word, Guid indexPk)
        {
            FetchResult<IEnumerable<string>> result;
            try
            {
                word.RequireNotNullOrEmpty(nameof(word));

                result = new FetchResult<IEnumerable<string>>
                {
                    Result = CodeIndexSearcher.GetHints(word, indexPk),
                    Status = new Status
                    {
                        Success = true
                    }
                };

                Log.LogDebug($"Get Hints For '{word}' successful");
            }
            catch (Exception ex)
            {
                result = new FetchResult<IEnumerable<string>>
                {
                    Status = new Status
                    {
                        Success = false,
                        StatusDesc = ex.ToString()
                    }
                };

                Log.LogDebug(ex, $"Get Hints For '{word}' failed");
            }

            return result;
        }

        IEnumerable<CodeSource> SearchCodeSource(SearchRequest searchRequest, out int showResults)
        {
            showResults = searchRequest.ShowResults.HasValue && searchRequest.ShowResults.Value <= CodeIndexConfiguration.MaximumResults && searchRequest.ShowResults.Value > 0 ? searchRequest.ShowResults.Value : 100;

            return CodeIndexSearcher.SearchCode(QueryGenerator.GetSearchStr(searchRequest.FileName, searchRequest.Content, searchRequest.FileExtension, searchRequest.FilePath, searchRequest.CaseSensitive), out _, showResults, searchRequest.IndexPk);
        }
    }
}
