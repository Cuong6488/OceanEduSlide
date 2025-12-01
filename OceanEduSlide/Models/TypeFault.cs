using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class TypeFault
    {
        public int Id { get; set; }
        [StringLength(100), Display(Name = "Nội dung")]
        public string Content { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        public int Sort { get; set; }
        public virtual User User { get; set; }
        public virtual Office Office { get; set; }
        public TypeFault()
        {
            Active = true;
        }
    }
}