using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Redistest
{
    class Program
    {
        private static Lazy<ConnectionMultiplexer> __lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            var cacheConnection = "danson.redis.cache.windows.net,abortConnect=false,ssl=true,password=QSBiha8+aGikk56uTZ16x6n9UlrWdM6azQLSHoYuO5Q=";
            //cacheConnection = "clj-lc-qa-snt01:26379,serviceName=lc-redis,abortConnect=true,ssl=true,password=PASS1234";//26380 clj-lc-qa-snt01:26379 lc-redis
            //cacheConnection = "clj-lc-qa-snt01:26380,serviceName=cc-redis,syncTimeout=5000,password=PASS1234";

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

                //Parallel.For(0, 3, i =>
                //{
                //    cache.LockTake("teamsList", Guid.Empty.ToString(), TimeSpan.FromMilliseconds(30000));
                //    serializedTeams = cache.StringGet("teamsList");
                //    Console.WriteLine($"locked teamsList {serializedTeams}");
                //    teams = JsonConvert.DeserializeObject<List<Employee>>(serializedTeams);
                //    Employee.PlayGames(teams);
                //    teams.RemoveAll(e => e.Id == 1);
                //    serializedTeams = JsonConvert.SerializeObject(teams);
                //    cache.StringSet("teamsList", serializedTeams);
                //    cache.LockRelease("teamsList", Guid.Empty.ToString());
                //});


                cache.LockTake("teamsList", Guid.Empty.ToString(), TimeSpan.FromMilliseconds(30000));
                Console.WriteLine($"lock taken by {Process.GetCurrentProcess().Id}");
                Thread.Sleep(10000);
                serializedTeams = cache.StringGet("teamsList");
                Console.WriteLine($"locked teamsList {serializedTeams}");
                teams = JsonConvert.DeserializeObject<List<Employee>>(serializedTeams);
                Employee.PlayGames(teams);
                teams.RemoveAll(e => e.Id == 1);
                serializedTeams = JsonConvert.SerializeObject(teams);
                cache.StringSet("teamsList", serializedTeams);
                Console.WriteLine($"lock released by {Process.GetCurrentProcess().Id}");
                cache.LockRelease("teamsList", Guid.Empty.ToString());
            }
        }
    }
}
