
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentService.Models
{
    [Table("investments")]
    public class Investment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int investmentid { get; set; }

        [Required]
        public int userid { get; set; }

        [Required]
        public int projectid { get; set; }

        [Required]
        public decimal amount { get; set; }

        [Required]
        public string status { get; set; } = "Pending";

        [Required]
        public DateTime createdat { get; set; } = DateTime.UtcNow;

        [ForeignKey("projectid")]
        public Project? Project { get; set; }
    }
}