
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models
{
        [Table("wallets")]
        public class Wallets{
        
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int user_id { get; set; }
        public decimal balance { get; set; }
        public DateTime created_at { get; set; }
    }

    [Table("transactions")]
    public class Transactions{
        
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int user_id { get; set; }
        public string transaction_type { get; set; }
        public decimal amount { get; set; }
        public DateTime created_at { get; set; }
    }


    public enum TransactionType
    {
        Topup,
        Investment
    }

}