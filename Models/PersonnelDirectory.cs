using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace samane.Models
{
    public class DirectoryQuery
    {
        [Range(1, 100000)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        [StringLength(100)]
        public string Search { get; set; }

        [StringLength(6000)]
        public string FiltersJson { get; set; }
        public string Sort { get; set; } = "fullName";
        public bool Desc { get; set; }
        public string TextMatch { get; set; } = "contains";
    }

    public class DirectoryInput
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; }

        [Required, StringLength(100)]
        public string LastName { get; set; }

        [Required, StringLength(20)]
        public string InternalPhone { get; set; }

        [StringLength(128)]
        public string Version { get; set; }
    }

    public class DirectoryRow
    {
        public long RowNumber { get; set; }
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => (FirstName + " " + LastName).Trim();
        public string InternalPhone { get; set; }
        public string Version { get; set; }
        public string[] AllowedActions { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DirectoryHistory
    {
        public string Action { get; set; }
        public string ActorId { get; set; }
        public string ActorName { get; set; }
        public DateTime ChangedAt { get; set; }
        public DirectoryRow Before { get; set; }
        public DirectoryRow After { get; set; }
    }

    public class DirectoryDetails
    {
        public DirectoryRow User { get; set; }
        public IList<DirectoryHistory> History { get; set; }
    }
}
