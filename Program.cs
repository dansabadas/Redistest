using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Redistest
{
    class Program
    {
        private static Lazy<ConnectionMultiplexer> __lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            string cacheConnection = Configuration[SecretName];
            cacheConnection = "danson.redis.cache.windows.net,abortConnect=false,ssl=true,password=QSBiha8+aGikk56uTZ16x6n9UlrWdM6azQLSHoYuO5Q=";
            //cacheConnection = "clj-lc-qa-snt01:26379,abortConnect=false,ssl=true,password=PASS1234";

            return ConnectionMultiplexer.Connect(cacheConnection);
        });

        static void Main()
        {
            using (var multiplexer = __lazyConnection.Value)
            {
                IDatabase cache = multiplexer.GetDatabase();

                // Perform cache operations using the cache object...

                // Simple PING command
                string cacheCommand = "PING";
                Console.WriteLine($"\nCache command  : {cacheCommand}");
                Console.WriteLine($"Cache response : {cache.Execute(cacheCommand)}");

                // Simple get and put of integral data types into the cache
                cacheCommand = "GET Message";
                Console.WriteLine($"\nCache command  : {cacheCommand} or StringGet()");
                Console.WriteLine($"Cache response : {cache.StringGet("Message")}");

                cacheCommand = "SET Message \"Hello! The cache is working from a .NET Core console app!\"";
                Console.WriteLine($"\nCache command  : {cacheCommand} or StringSet()");
                Console.WriteLine(
                    $"Cache response : {cache.StringSet("Message", "Hello! The cache is working from a .NET Core console app!")}");

                // Demonstrate "SET Message" executed as expected...
                cacheCommand = "GET Message";
                Console.WriteLine($"\nCache command  : {cacheCommand} or StringGet()");
                Console.WriteLine($"Cache response : {cache.StringGet("Message")}");

                // Get the client list, useful to see if connection list is growing...
                cacheCommand = "CLIENT LIST";
                Console.WriteLine($"\nCache command  : {cacheCommand}");
                Console.WriteLine(
                    $"Cache response : \n{cache.Execute("CLIENT", "LIST").ToString().Replace("id=", "id=")}");

                // Store .NET object to cache
                var gotIt = cache.StringGet("e007");
                if (!gotIt.IsNull)
                {
                    Console.WriteLine("cache.KeyDelete : " + cache.KeyDelete("e007"));
                }

                Employee e007 = new Employee(7, "Davide Columbo", 100);
                Console.WriteLine("Cache response from storing Employee .NET object : " +
                                  cache.StringSet("e007", JsonConvert.SerializeObject(e007)));

                // Retrieve .NET object from cache
                gotIt = cache.StringGet("e008");
                if (gotIt.IsNull)
                {
                    gotIt = cache.StringGet("e007");
                }

                var e007FromCache = JsonConvert.DeserializeObject<Employee>(gotIt);
                Console.WriteLine("Deserialized Employee .NET object :\n");
                Console.WriteLine("\tEmployee.Name : " + e007FromCache.Name);
                Console.WriteLine("\tEmployee.Id   : " + e007FromCache.Id);
                Console.WriteLine("\tEmployee.Age  : " + e007FromCache.Age + "\n");

                string serializedTeams = cache.StringGet("teamsList");
                Console.WriteLine($"serializedTeams={serializedTeams}");
                var teams = Employee.Seed();
                cache.StringSet("teamsList", JsonConvert.SerializeObject(teams));

                Parallel.For(0, 3, i =>
                {
                    cache.LockTake("teamsList", Guid.Empty.ToString(), TimeSpan.FromMilliseconds(3000));
                    serializedTeams = cache.StringGet("teamsList");
                    teams = JsonConvert.DeserializeObject<List<Employee>>(serializedTeams);
                    Employee.PlayGames(teams);
                    cache.LockRelease("teamsList", Guid.Empty.ToString());
                });
            }
        }

        private static IConfigurationRoot Configuration { get; } = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        private const string SecretName = "CacheConnection";
    }
}
