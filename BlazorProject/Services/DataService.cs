using Bogus;
using SuperChargedStreams.Utils;
using System.Diagnostics;

namespace SuperChargedStreams.Services
{
    public class DataService
    {
        private readonly FileCache _cache;

        public DataService(FileCache cache)
        {
            _cache = cache;
        }

        public async IAsyncEnumerable<Customer> GetDataAsync(int count, string? nameFilter = null)
        {
            string cacheKey = $"CustomerData_{count}";
            if (!_cache.Exists(cacheKey))
            {
                Console.WriteLine("Generating customer data...");
                var stopwatch = Stopwatch.StartNew();

                // Generate customer data
                var data = GenerateCustomerDataAsync(count);

                // Save data to cache
                await _cache.SaveAsync(cacheKey, data);

                stopwatch.Stop();
                Console.WriteLine($"Generated and cached data in {stopwatch.ElapsedMilliseconds} ms");

                await foreach (var item in _cache.LoadAsync<Customer>(cacheKey))
                {
                    if (string.IsNullOrEmpty(nameFilter) || item.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return item;
                    }
                }
            }
            else
            {
                Console.WriteLine("Loading customer data from cache...");
                var stopwatch = Stopwatch.StartNew();

                // Load data from cache
                await foreach (var item in _cache.LoadAsync<Customer>(cacheKey))
                {
                    if (string.IsNullOrEmpty(nameFilter) || item.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return item;
                    }
                }

                stopwatch.Stop();
                Console.WriteLine($"Loaded data from cache in {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        private async IAsyncEnumerable<Customer> GenerateCustomerDataAsync(int count)
        {
            var faker = new Faker<Customer>()
                .RuleFor(c => c.Name, f => f.Name.FullName())
                .RuleFor(c => c.Birthday, f => f.Date.Past(80, DateTime.Today.AddYears(-18))) // Age between 18 and 98
                .RuleFor(c => c.State, f => f.Address.StateAbbr())
                .RuleFor(c => c.AnnualIncome, f => f.Random.Int(20_000, 200_000)); // Income between $20k and $200k

            for (int i = 0; i < count; i++)
            {
                yield return faker.Generate();
                await Task.Yield(); // Yield control to allow streaming
            }
        }
    }

    public class Customer
    {
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
        public string State { get; set; }
        public int AnnualIncome { get; set; }
    }

}
