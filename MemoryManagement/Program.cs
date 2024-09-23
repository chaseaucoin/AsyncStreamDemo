using Bogus;
using System.IO.Compression;
using System.Text.Json;

//await CreateDataFile();

await GetAverageCustomerIncomeFromList();

//await GetAverageCustomerIncomeFromEnumerable();

Console.ReadKey();

static async Task GetAverageCustomerIncomeFromList()
{
    using var fileStream = File.OpenRead("customers.json.gz");
    using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

    //Deserialize data from file as list
    var customers = await JsonSerializer.DeserializeAsync<List<Customer>>(gzipStream);
    var income = customers?.Average(c => c.AnnualIncome) ?? 0;

    Console.WriteLine($"Average income: $ {income:N0}\nTotal Customers: {customers?.Count:N0}");
}

static async Task GetAverageCustomerIncomeFromEnumerable()
{
    using var fileStream = File.OpenRead("customers.json.gz");
    using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

    //Deserialize data from file as enumerable
    var customers = JsonSerializer.DeserializeAsyncEnumerable<Customer>(gzipStream);

    long totalIncome = 0;
    var totalCustomers = 0;
    var stateIncomeStatistics = new HashSet<Statistic>();

    //Calculate average income
    await foreach (var customer in customers)
    {
        totalIncome += customer?.AnnualIncome ?? 0;
        totalCustomers++;

        //Calculate statistics for state
        var type = "State";
        var state = customer.State;

        var stateIncome = stateIncomeStatistics.FirstOrDefault(s => s.Type == type && s.Key == state);

        if (stateIncome == null)
        {
            stateIncome = new Statistic(type, state);
            stateIncomeStatistics.Add(stateIncome);
        }

        stateIncome.Update(customer.AnnualIncome);
    }

    var income = totalIncome / totalCustomers;
    Console.WriteLine($"Average income: $ {income:N0}\nTotal Customers: {totalCustomers:N0}");

    //print statistics for each state
    foreach (var stateIncome in stateIncomeStatistics)
    {
        //write statistics for this state
        Console.WriteLine($"{stateIncome.Key}: $ {stateIncome.Mean:N0}");
    }
}

static async Task CreateDataFile()
{
    var filePath = "customers.json.gz";
    var count = 10_000_000;

    using var fileStream = File.Create(filePath);
    using var gzipStream = new GZipStream(fileStream, CompressionLevel.Fastest);

    var data = GenmerateData(count);

    await JsonSerializer.SerializeAsync(gzipStream, data);
}

static IEnumerable<Customer> GenmerateData(int count)
{
    var faker = new Faker<Customer>()
        .RuleFor(c => c.Name, f => f.Name.FullName())
        .RuleFor(c => c.Birthday, f => f.Date.Past(80, DateTime.Today.AddYears(-18))) // Age between 18 and 98
        .RuleFor(c => c.State, f => f.Address.StateAbbr())
        .RuleFor(c => c.AnnualIncome, f => f.Random.Int(20_000, 200_000)); // Income between $20k and $200k

    for (int i = 0; i < count; i++)
    {
        yield return faker.Generate();
    }
}

public class Statistic : IEquatable<Statistic>
{
    private Statistic()
    {
    }

    public Statistic(string type, string key)
    {
        Type = type;
        Key = key;
    }

    public string Type { get; private set; }

    public string Key { get; private set; }

    public double Count { get; set; }

    public double Sum { get; set; }

    //We want to calulcate stream so we need the values for on the fly calculation of variance
    public double Mean { get; set; }

    //M2 is the sum of the squares of the differences between each data point and the mean
    public double M2 { get; set; }

    public double Min { get; set; }

    public double Max { get; set; }

    public double Variance => M2 / (Count - 1);

    public double StandardDeviation => Math.Sqrt(Variance);

    public double Skewness => Math.Sqrt(Count) * M2 / Math.Pow(Variance, 1.5);

    public double Kurtosis => Count * M2 / Math.Pow(Variance, 2);

    public double MeanDeviation => Sum / Count;

    public void Update(double value)
    {
        Count++;
        Sum += value;
        var delta = value - Mean;
        Mean += delta / Count;
        M2 += delta * (value - Mean);
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
    }

    //IEquatable<Statistic> implementation
    public bool Equals(Statistic other)
    {
        if (other is null)
            return false;

        return Type == other.Type && Key == other.Key;
    }

    public override bool Equals(object obj) => Equals(obj as Statistic);

    public override int GetHashCode() => HashCode.Combine(Type, Key);

    public static bool operator ==(Statistic left, Statistic right) => EqualityComparer<Statistic>.Default.Equals(left, right);

    public static bool operator !=(Statistic left, Statistic right) => !(left == right);

    public override string ToString() => $"{Type} {Key} Count: {Count} Sum: {Sum} Mean: {Mean} Variance: {Variance} StdDev: {StandardDeviation} Skewness: {Skewness} Kurtosis: {Kurtosis} MeanDev: {MeanDeviation}";

    public Statistic Clone() => new Statistic
    {
        Type = Type,
        Key = Key,
        Count = Count,
        Sum = Sum,
        Mean = Mean,
        M2 = M2,
        Min = Min,
        Max = Max
    };

    //Start with a clone of this to preserve the original values 
    //then merge in the new values
    public Statistic MergeNew(Statistic other)
    {
        var merged = Clone();
        merged.Count += other.Count;
        merged.Sum += other.Sum;
        merged.Mean += other.Mean;
        merged.M2 += other.M2;
        merged.Min = Math.Min(Min, other.Min);
        merged.Max = Math.Max(Max, other.Max);
        return merged;
    }
}

public class Customer
{
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    public string State { get; set; }
    public int AnnualIncome { get; set; }
}