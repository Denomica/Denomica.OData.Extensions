using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OData.Tests
{
    public class Person : Document
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public DateTime DateOfBirth { get; set; }

        public string? MobilePhone { get; set; }

        public string? Hometown { get; set; }

    }

    public class Employee : Person
    {
        public DateTime? LastDayOfEmployment { get; set; }

        public DateTime FirstDayOfEmployment { get; set; }

        public Employee Manager { get; set; } = null!;

        public Person? EmergencyContact { get; set; }

        public TimeSpan? DailyWorkingTime { get; set; }

    }

    public class Document
    {
        public Document()
        {
            this.Id = Guid.NewGuid().ToString();
            this.Partition = this.GetType().Name;
        }

        public string Id { get; set; } = null!;

        public string? Partition { get; set; }

    }
}
