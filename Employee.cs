using System;
using System.Collections.Generic;
using System.Text;

namespace Redistest
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        public Employee(int employeeId, string Name, int Age)
        {
            this.Id = employeeId;
            this.Name = Name;
            this.Age = Age;
        }

        public static void PlayGames(IEnumerable<Employee> teams)
        {
            // Simple random generation of statistics.
            Random r = new Random();

            foreach (var t in teams)
            {
                t.Age = r.Next(33);
                t.Name = t.Name + r.Next(33);
            }
        }

        public static List<Employee> Seed()
        {
            var teams = new List<Employee>
            {
                new Employee(1, "Adventure Works Cycles", 20),
                new Employee(2, "Alpine Ski House", 25),
                new Employee(3, "Blue Yonder Airlines", 27),
                new Employee(4, "Coho Vineyard", 51)
            };

            PlayGames(teams);

            return teams;
        }
    }
}
