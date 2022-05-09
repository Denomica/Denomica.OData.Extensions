using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OData.Tests
{
    public class Person
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public DateTime DateOfBirth { get; set; }

        public string? MobilePhone { get; set; }
    }

    public class Employee : Person
    {
        public DateTime? LastDayOfEmployment { get; set; }

        public Employee Manager { get; set; } = null!;

        public Person? EmergencyContact { get; set; }
    }
}
