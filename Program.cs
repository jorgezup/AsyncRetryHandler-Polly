using System.Diagnostics;
using Polly;
using Refit;

// Assuming the ICustomerApi is in the same namespace as CustomerController
public interface ICustomerApi
{
    [Get("/customer/{customerId}")]
    Task<Customer> GetCustomer(string customerId, CancellationToken ct);
}

// public class Customer
// {
//     public string Id { get; set; }
//     public string Name { get; set; }
// }

class Program
{
    static async Task Main(string[] args)
    {
        var customerId = "12345";
        var cts = new CancellationTokenSource();

        var stopwatchRetry = new Stopwatch();
        var retryPolicy = Policy.Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<OperationCanceledException>()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromMilliseconds(200),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Stopwatch inside onRetry: {stopwatchRetry.ElapsedMilliseconds}ms");
                    Console.WriteLine($"Retrying due to: {exception.Message}. Retry attempt {retryCount}. Sleeping for {timeSpan.TotalMilliseconds}ms");
                });

        // Create HttpClient and configure Polly policies
        var clientHandler = new HttpClientHandler();
        var httpClient = new HttpClient(clientHandler)
        {
            BaseAddress = new Uri("http://localhost:5000"),
            Timeout = TimeSpan.FromSeconds(7)
        };


        // var api = RestService.For<ICustomerApi>($"http://localhost:5000");

        var api = RestService.For<ICustomerApi>(httpClient);
        // var task = combinedPolicy.ExecuteAsync(
        //     async ct => await api.GetCustomer(customerId, ct), cts.Token);



        var task = api.GetCustomer(customerId, cts.Token);

        // Simule delay
        // await Task.Delay(1000, cts.Token);

        var stopwatch = new Stopwatch();
        try
        {
            stopwatch.Start();
            stopwatchRetry.Start();
            var customer = await retryPolicy.ExecuteAsync(async () => await api.GetCustomer(customerId, cts.Token));
            // var customer = await task;
            Console.WriteLine($"Customer received: {customer.Name}");
            Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Request was cancelled");
            Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds}ms");

        }
    }
}
