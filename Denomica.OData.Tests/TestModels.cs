using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OData.Tests
{
    public enum Gender
    {
        Unspecified = -1,
        Male = 1,
        Femaile = 2,
        Both = 3,
        None = 0
    }

    public class Person : Document
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public DateTime DateOfBirth { get; set; }

        public string? MobilePhone { get; set; }

        public string? Hometown { get; set; }

        public Gender Gender { get; set; }
    }

    public class Employee : Person
    {
        public DateTime? LastDayOfEmployment { get; set; }

        public DateTime FirstDayOfEmployment { get; set; }

        public Employee Manager { get; set; } = null!;

        public Person? EmergencyContact { get; set; }

        public TimeSpan? DailyWorkingTime { get; set; }

    }

    public enum VersionState
    {
        Draft,
        Current,
        Archived
    }

    public class VersionedDocument : Document
    {
        public VersionedDocument? PreviousVersion { get; set; }

        public VersionState State { get; set; }
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
