using System;
using System.ComponentModel.DataAnnotations;
namespace samane.Models
{
    public class DevelopmentLoginInput
    {
        [Required, StringLength(256)]
        public string Login { get; set; }
        [Required, StringLength(1024)]
        public string Password { get; set; }
    }
    internal class DevelopmentAccount
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}
