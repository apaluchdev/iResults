using iResults;

public class Program
{
    private static async Task Main(string[] args)
    {
        var client = new iResultsClient();
        await client.Start();

        Console.WriteLine("Success");
        Console.ReadLine();
    }
}