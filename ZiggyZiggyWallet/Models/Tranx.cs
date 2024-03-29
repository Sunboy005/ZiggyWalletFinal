﻿using System.ComponentModel.DataAnnotations;

namespace ZiggyZiggyWallet.Models
{
    public class Tranx:BaseEntity
    {
        [Required] 
        public float AmountSent { get; set; }
        public float AmountReceived { get; set; }

        public string Description { get; set; }


        public string SenderCurrency { get; set; }
        public string RecieverCurrency { get; set; }


        [Required]
        [MinLength(16, ErrorMessage = "SenderName should not be below 16 letters")]
        public string SenderWalletId { get; set; }

        [Required]
        [MinLength(16, ErrorMessage = "Reciepient Wallet should not be below 6 letters")]
        public string RecipientWalletId { get; set; }

        public string Status { get; set; }

        public string TranxType { get; set; }
    }
}
