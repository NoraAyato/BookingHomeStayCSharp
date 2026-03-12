using DoAnCs.Models;
using DoAnCs.Models.SearchModels;
using DoAnCs.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnCs.Services
{
    public interface IElasticsearchService
    {
        Task SeedDataAsync();
        Task<IEnumerable<KhuVucDocument>> SuggestKhuVucAsync(string query);
        Task<IEnumerable<TinTucDocument>> SuggestTinTucAsync(string query);
    }
}