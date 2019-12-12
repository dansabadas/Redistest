using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            //return ConnectionMultiplexer.Connect(cacheConnection);

            var options = new ConfigurationOptions()
            {
                CommandMap = CommandMap.Sentinel,
                EndPoints = { { "clj-lc-qa-snt01", 26379 } },
                AllowAdmin = true,
                TieBreaker = "",
                ServiceName = "lc-redis",
                SyncTimeout = 5000,
                //Password= "PASS1234",
            };



            //reference page for Sentinel: https://redis.io/topics/sentinel



            var connection = ConnectionMultiplexer.Connect(options);
            //need to get the master node
            // option 1:
            IDatabase cache = connection.GetDatabase();
            //RedisResult[] result = (RedisResult[])cache.Execute("SENTINEL", "get-master-addr-by-name", "lc-redis");
            //Console.WriteLine($"Cache response : {result[0]}:{result[1]}");
            //connection.GetSubscriber().Subscribe()



            //option 2:
            var masters = connection.GetServer("clj-lc-qa-snt01", 26379).SentinelMasters();

            options = new ConfigurationOptions()
            {
                EndPoints = { { masters.First().Single(x => x.Key == "ip").Value, int.Parse(masters.First().Single(x => x.Key == "port").Value) } },
                AllowAdmin = true,
                TieBreaker = "",
                ServiceName = "lc-redis",
                SyncTimeout = 5000,
                Password = "PASS1234",
            };
            connection = ConnectionMultiplexer.Connect(options);
            return connection;
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
