using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentService.Models
{
    [Table("projects")]
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int projectid { get; set; }

        [Required]
        public int ownerid { get; set; }

        [Required]
        [MaxLength(200)]
        public string title { get; set; } = string.Empty;

        [Required]
        public string description { get; set; } = string.Empty;

        [Required]
        public decimal targetamount { get; set; }

        [Required]
        public decimal fundedamount { get; set; } = 0;

        [Required]
        public DateTime createdat { get; set; } = DateTime.UtcNow;
    }
}