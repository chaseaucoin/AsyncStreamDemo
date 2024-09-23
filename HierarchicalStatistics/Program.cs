using Bogus;

// Now we will compute the statistics in a hierarchical manner
await CalculateStatistics();

Console.ReadKey();


static IEnumerable<List<Ticket>> GenerateSalesData(int numberOfStores, int years)
{
    var startTime = DateTime.UtcNow.AddYears(-years);
    var endTime = DateTime.UtcNow;
    var faker = new Faker();
    var menuItems = new[] { "Burger", "Fries", "Soda", "Shake", "Salad" };

    var currentTime = startTime;
    //seconds in between tickets
    var secondsPerHour = 3600;

    while (currentTime <= endTime)
    {
        // Generate data only for hours between 5 AM and 8 PM
        if (currentTime.Hour >= 5 && currentTime.Hour <= 20)
        {
            var ticketList = new List<Ticket>();

            for (int storeId = 1; storeId <= numberOfStores; storeId++)
            {
                var ticketsPerHour = faker.Random.Int(1, 20);

                for (int i = 0; i < ticketsPerHour; i++)
                {
                    var ticket = new Ticket
                    {
                        // Random timestamp within the hour
                        Timestamp = currentTime.AddSeconds(faker.Random.Int(0, secondsPerHour)),
                        StoreId = storeId,
                        Items = Enumerable.Range(1, faker.Random.Int(1, 5))
                            .Select(_ => new MenuItem
                            {
                                Name = faker.PickRandom(menuItems),
                                Price = faker.Random.Double(1, 2.5),
                                Quantity = faker.Random.Int(1, 3)
                            })
                            .ToList()
                    };

                    ticketList.Add(ticket);
                }
            }

            yield return ticketList;
        }
        // Move to the next hour
        currentTime = currentTime.AddHours(1);
    }
}

static async Task CalculateStatistics()
{
    RestaurantStats restaurantStats = new RestaurantStats()
    { Period = DateTime.UtcNow };

    foreach (var ticketBatch in GenerateSalesData(200,1))
    {
        foreach (var ticket in ticketBatch)
        {
            restaurantStats.Update(ticket);
        }

        Console.WriteLine(ticketBatch.First().Timestamp.ToString());
        restaurantStats.Display();
    }
}

public class MenuItem
{
    public string Name { get; set; }
    public double Price { get; set; }
    public int Quantity { get; set; }
}

public class Ticket
{
    public int StoreId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<MenuItem> Items { get; set; } = new List<MenuItem>();
}

public class Store
{
    public int StoreId { get; set; }
    public string Location { get; set; }
}

public class RestaurantStats
{
    public DateTime Period { get; set; }

    // Basic statistics
    public double TotalSales { get; private set; }
    public int TotalItemsSold { get; private set; }
    public int TotalTickets { get; private set; }
    public double AverageTicketPrice => TotalTickets == 0 ? 0 : TotalSales / TotalTickets;
    public double AverageItemsPerTicket => TotalTickets == 0 ? 0 : (double)TotalItemsSold / TotalTickets;

    // Statistics for ticket sales distribution
    public double MinTicketPrice { get; private set; } = double.MaxValue;
    public double MaxTicketPrice { get; private set; } = double.MinValue;

    // Update statistics with a new ticket
    public void Update(Ticket ticket)
    {
        double ticketTotal = ticket.Items.Sum(item => item.Price * item.Quantity);
        int itemsInTicket = ticket.Items.Sum(item => item.Quantity);

        // Update overall statistics
        TotalSales += ticketTotal;
        TotalItemsSold += itemsInTicket;
        TotalTickets++;

        MinTicketPrice = Math.Min(MinTicketPrice, ticketTotal);
        MaxTicketPrice = Math.Max(MaxTicketPrice, ticketTotal);
    }

    // Merge another RestaurantStats object into this one
    public RestaurantStats Merge(RestaurantStats other)
    {
        return new RestaurantStats {
            TotalSales = this.TotalSales + other.TotalSales,
            TotalItemsSold = this.TotalItemsSold + other.TotalItemsSold,
            TotalTickets = this.TotalTickets + other.TotalTickets,
            MinTicketPrice = Math.Min(this.MinTicketPrice, other.MinTicketPrice),
            MaxTicketPrice = Math.Max(this.MaxTicketPrice, other.MaxTicketPrice)
        };
    }

    //Add + operator to merge two RestaurantStats objects
    public static RestaurantStats operator +(RestaurantStats a, RestaurantStats b)
    {
        return a.Merge(b);
    }

    // Display the current statistics (for debugging purposes)
    public void Display()
    {
        Console.WriteLine($"Total Sales: $ {TotalSales:N2}");
        Console.WriteLine($"Total Items Sold: {TotalItemsSold}");
        Console.WriteLine($"Total Tickets: {TotalTickets}");
        Console.WriteLine($"Average Ticket Price: $ {AverageTicketPrice:N2}");
        Console.WriteLine($"Average Items per Ticket: {AverageItemsPerTicket:N2}");
        Console.WriteLine($"Min Ticket Price: $ {MinTicketPrice:N2}");
        Console.WriteLine($"Max Ticket Price: $ {MaxTicketPrice:N2}");
    }
}


// Step 2: Statistics Class to Handle Aggregation
public class DateStatistic : IEquatable<DateStatistic>
{
    private DateStatistic() { }

    public DateStatistic(DateTime key)
    {
        Key = key;
    }

    public DateTime Key { get; private set; }

    public double Count { get; set; }
    public double Sum { get; set; }
    public double Mean => Count == 0 ? 0 : Sum / Count;

    public double Min { get; set; } = double.MaxValue;
    public double Max { get; set; } = double.MinValue;

    // Updates the statistic with a new value
    public void Update(double value)
    {
        Count++;
        Sum += value;
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
    }

    // Merges another Statistic object into this one
    public DateStatistic Merge(DateStatistic other)
    {
        var merged = new DateStatistic(Key)
        {
            Count = this.Count + other.Count,
            Sum = this.Sum + other.Sum,
            Min = Math.Min(this.Min, other.Min),
            Max = Math.Max(this.Max, other.Max)
        };
        return merged;
    }

    public override bool Equals(object obj) => Equals(obj as DateStatistic);
    public bool Equals(DateStatistic other) => other != null && Key == other.Key;
    public override int GetHashCode() => Key.GetHashCode();
}