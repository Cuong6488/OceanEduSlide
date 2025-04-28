using System;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class MemberCredential
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [StringLength(500)]
        public string CredentialId { get; set; }
        public string PublicKey { get; set; }
        public long SignatureCounter { get; set; }
        [StringLength(512)]
        public string UserHandle { get; set; }
        public Guid AuthenticatorAttestationGuid { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [StringLength(255)]
        public string DeviceName { get; set; }
        [StringLength(100)]
        public string Platform { get; set; }

        public virtual User User { get; set; }
    }
}