using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountService.Models
{
    [Table("users")] 
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int userid { get; set; }

        [Required]
        [MaxLength(100)]
        public string username { get; set; } = null!;

        [Required]
        public string passwordb { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string fullname { get; set; } = null!;

        [Required]
        public DateTime createdat { get; set; }
    }

    public class LoginModel{

    public required string username { get; set; }
    public required string password { get; set; }
    
    }
}