using Microsoft.EntityFrameworkCore;
using Nest;
using DoAnCs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoAnCs.Models.SearchModels;
using DoAnCs.Models.ViewModels;

namespace DoAnCs.Services
{
    public class ElasticsearchService : IElasticsearchService
    {
        private readonly IElasticClient _client;
        private readonly string _defaultIndex;
        private readonly ApplicationDbContext _dbContext;

        public ElasticsearchService(IElasticClient elasticClient, IConfiguration configuration, ApplicationDbContext dbContext)
        {
            _client = elasticClient;
            _defaultIndex = configuration.GetSection("Elasticsearch")["IndexName"];
            _dbContext = dbContext;
        }

        public async Task SeedDataAsync()
        {
            const int maxRetries = 3;
            int retries = 0;
            bool indexCreated = false;

            // Chỉ mục cho khu vực
            string khuVucIndex = _defaultIndex + "_khuvuc";
            // Chỉ mục cho tin tức
            string tinTucIndex = _defaultIndex + "_tintuc";

            // Tạo chỉ mục cho KhuVuc
            while (retries < maxRetries && !indexCreated)
            {
                try
                {
                    var indexExistsResponse = await _client.Indices.ExistsAsync(khuVucIndex);
                    if (!indexExistsResponse.Exists)
                    {
                        Console.WriteLine($"Creating index {khuVucIndex}...");
                        var createIndexResponse = await _client.Indices.CreateAsync(khuVucIndex, c => c
                            .Settings(s => s
                                .Analysis(a => a
                                    .Analyzers(analyzer => analyzer
                                        .Custom("vi_analyzer", ca => ca
                                            .Tokenizer("icu_tokenizer")
                                            .Filters("icu_folding", "icu_normalizer")
                                        )
                                    )
                                )
                            )
                            .Map<KhuVucDocument>(m => m
                                .Properties(p => p
                                    .Keyword(k => k.Name(name => name.Ma_KV))
                                    .Text(t => t.Name(name => name.Ten_KV).Analyzer("vi_analyzer"))
                                    .Completion(c => c.Name("suggest").Analyzer("vi_analyzer"))
                                )
                            )
                        );

                        if (!createIndexResponse.IsValid)
                        {
                            var errorMessage = createIndexResponse.OriginalException?.Message ?? createIndexResponse.ServerError?.ToString();
                            Console.WriteLine($"Failed to create index: {errorMessage}");
                            throw new Exception($"Failed to create index: {errorMessage}");
                        }
                        Console.WriteLine($"Index {khuVucIndex} created successfully.");
                        indexCreated = true;
                    }
                    else
                    {
                        Console.WriteLine($"Index {khuVucIndex} already exists.");
                        indexCreated = true;
                    }
                }
                catch (Exception ex)
                {
                    retries++;
                    Console.WriteLine($"Attempt {retries}/{maxRetries} failed: {ex.Message}");
                    if (retries == maxRetries)
                    {
                        throw new Exception($"Failed to create index after {maxRetries} attempts: {ex.Message}");
                    }
                    await Task.Delay(2000);
                }
            }

            // Tạo chỉ mục cho TinTuc
            retries = 0;
            indexCreated = false;
            while (retries < maxRetries && !indexCreated)
            {
                try
                {
                    var indexExistsResponse = await _client.Indices.ExistsAsync(tinTucIndex);
                    if (!indexExistsResponse.Exists)
                    {
                        Console.WriteLine($"Creating index {tinTucIndex}...");
                        var createIndexResponse = await _client.Indices.CreateAsync(tinTucIndex, c => c
                            .Settings(s => s
                                .Analysis(a => a
                                    .Analyzers(analyzer => analyzer
                                        .Custom("vi_analyzer", ca => ca
                                            .Tokenizer("icu_tokenizer")
                                            .Filters("icu_folding", "icu_normalizer")
                                        )
                                    )
                                )
                            )
                            .Map<TinTucDocument>(m => m
                                .Properties(p => p
                                    .Keyword(k => k.Name(name => name.Ma_TinTuc))
                                    .Text(t => t.Name(name => name.TieuDe).Analyzer("vi_analyzer"))
                                    .Text(t => t.Name(name => name.NoiDung).Analyzer("vi_analyzer"))
                                    .Keyword(k => k.Name(name => name.ID_ChuDe))
                                    .Text(t => t.Name(name => name.TenChuDe).Analyzer("vi_analyzer"))
                                    .Keyword(k => k.Name(name => name.TacGia))
                                    .Date(d => d.Name(name => name.NgayDang))
                                    .Completion(c => c.Name("suggest").Analyzer("vi_analyzer"))
                                )
                            )
                        );

                        if (!createIndexResponse.IsValid)
                        {
                            var errorMessage = createIndexResponse.OriginalException?.Message ?? createIndexResponse.ServerError?.ToString();
                            Console.WriteLine($"Failed to create index: {errorMessage}");
                            throw new Exception($"Failed to create index: {errorMessage}");
                        }
                        Console.WriteLine($"Index {tinTucIndex} created successfully.");
                        indexCreated = true;
                    }
                    else
                    {
                        Console.WriteLine($"Index {tinTucIndex} already exists.");
                        indexCreated = true;
                    }
                }
                catch (Exception ex)
                {
                    retries++;
                    Console.WriteLine($"Attempt {retries}/{maxRetries} failed: {ex.Message}");
                    if (retries == maxRetries)
                    {
                        throw new Exception($"Failed to create index after {maxRetries} attempts: {ex.Message}");
                    }
                    await Task.Delay(2000);
                }
            }

            // Lấy dữ liệu KhuVuc từ database và lập chỉ mục
            var khuVucList = await _dbContext.KhuVucs
                .AsNoTracking()
                .ToListAsync();

            var khuVucDocuments = khuVucList.Select(kv => new KhuVucDocument
            {
                Ma_KV = kv.Ma_KV,
                Ten_KV = kv.Ten_KV,
                Suggest = new CompletionField { Input = new[] { kv.Ten_KV } }
            }).ToList();

            foreach (var doc in khuVucDocuments)
            {
                retries = 0;
                bool indexed = false;

                while (retries < maxRetries && !indexed)
                {
                    try
                    {
                        var existsResponse = await _client.DocumentExistsAsync<KhuVucDocument>(doc.Ma_KV, d => d.Index(khuVucIndex));
                        if (!existsResponse.Exists)
                        {
                            var indexResponse = await _client.IndexAsync(doc, i => i.Index(khuVucIndex));
                            if (!indexResponse.IsValid)
                            {
                                Console.WriteLine($"Failed to index document {doc.Ma_KV}: {indexResponse.OriginalException?.Message}");
                            }
                            else
                            {
                                Console.WriteLine($"Indexed document {doc.Ma_KV} successfully.");
                                indexed = true;
                            }
                        }
                        else
                        {
                            indexed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        retries++;
                        Console.WriteLine($"Attempt {retries}/{maxRetries} failed for document {doc.Ma_KV}: {ex.Message}");
                        if (retries == maxRetries)
                        {
                            Console.WriteLine($"Failed to index document {doc.Ma_KV} after {maxRetries} attempts: {ex.Message}");
                        }
                        await Task.Delay(2000);
                    }
                }
            }

            // Lấy dữ liệu TinTuc từ database và lập chỉ mục
            var tinTucList = await _dbContext.TinTucs
                .Include(t => t.ChuDe)
                .AsNoTracking()
                .ToListAsync();

            var tinTucDocuments = tinTucList.Select(t => new TinTucDocument
            {
                Ma_TinTuc = t.Ma_TinTuc,
                TieuDe = t.TieuDe,
                NoiDung = t.NoiDung,
                ID_ChuDe = t.ID_ChuDe,
                TenChuDe = t.ChuDe?.TenChuDe,
                TacGia = t.TacGia,
                NgayDang = t.NgayDang,
                Suggest = new CompletionField { Input = new[] { t.TieuDe, t.NoiDung } }
            }).ToList();

            foreach (var doc in tinTucDocuments)
            {
                retries = 0;
                bool indexed = false;

                while (retries < maxRetries && !indexed)
                {
                    try
                    {
                        var existsResponse = await _client.DocumentExistsAsync<TinTucDocument>(doc.Ma_TinTuc, d => d.Index(tinTucIndex));
                        if (!existsResponse.Exists)
                        {
                            var indexResponse = await _client.IndexAsync(doc, i => i.Index(tinTucIndex));
                            if (!indexResponse.IsValid)
                            {
                                Console.WriteLine($"Failed to index document {doc.Ma_TinTuc}: {indexResponse.OriginalException?.Message}");
                            }
                            else
                            {
                                Console.WriteLine($"Indexed document {doc.Ma_TinTuc} successfully.");
                                indexed = true;
                            }
                        }
                        else
                        {
                            indexed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        retries++;
                        Console.WriteLine($"Attempt {retries}/{maxRetries} failed for document {doc.Ma_TinTuc}: {ex.Message}");
                        if (retries == maxRetries)
                        {
                            Console.WriteLine($"Failed to index document {doc.Ma_TinTuc} after {maxRetries} attempts: {ex.Message}");
                        }
                        await Task.Delay(2000);
                    }
                }
            }
        }

        public async Task<IEnumerable<KhuVucDocument>> SuggestKhuVucAsync(string query)
        {
            string khuVucIndex = _defaultIndex + "_khuvuc";

            if (string.IsNullOrEmpty(query))
                return Enumerable.Empty<KhuVucDocument>();

            var searchResponse = await _client.SearchAsync<KhuVucDocument>(s => s
                .Index(khuVucIndex)
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Ten_KV)
                        .Query(query)
                        .Fuzziness(Fuzziness.Auto)
                        .Analyzer("vi_analyzer")
                    )
                )
                .Size(10)
            );

            if (!searchResponse.IsValid)
            {
                throw new Exception($"Search failed: {searchResponse.OriginalException?.Message}");
            }

            return searchResponse.Documents
                .Where(d => !string.IsNullOrEmpty(d.Ma_KV) && !string.IsNullOrEmpty(d.Ten_KV))
                .DistinctBy(d => d.Ma_KV);
        }

        public async Task<IEnumerable<TinTucDocument>> SuggestTinTucAsync(string query)
        {
            string tinTucIndex = _defaultIndex + "_tintuc";

            if (string.IsNullOrEmpty(query))
                return Enumerable.Empty<TinTucDocument>();

            var searchResponse = await _client.SearchAsync<TinTucDocument>(s => s
                .Index(tinTucIndex)
                .Query(q => q
                    .MultiMatch(mm => mm
                        .Fields(f => f
                            .Field(ff => ff.TieuDe, 2.0) // Tăng trọng số cho tiêu đề
                            .Field(ff => ff.NoiDung))
                        .Query(query)
                        .Fuzziness(Fuzziness.Auto)
                        .Analyzer("vi_analyzer")
                    )
                )
                .Size(10)
            );

            if (!searchResponse.IsValid)
            {
                throw new Exception($"Search failed: {searchResponse.OriginalException?.Message}");
            }

            return searchResponse.Documents
                .Where(d => !string.IsNullOrEmpty(d.Ma_TinTuc) && !string.IsNullOrEmpty(d.TieuDe))
                .DistinctBy(d => d.Ma_TinTuc);
        }
    }   
}