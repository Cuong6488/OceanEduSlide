using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{
    public class CallLog
    {
        public int Id { get; set; }
        [StringLength(100)]
        public string UniqueId { get; set; }
        public DateTime CallDate { get; set; }
        public string CallDateString { get; set; }
        public int UserId { get; set; }
        public int? HistoryUserId { get; set; }
        [StringLength(20)]
        public string Phone { get; set; }
        [StringLength(20)]
        public string Exten { get; set; }
        public int Duration { get; set; }
        public int BillSec { get; set; }
        [StringLength(20)]
        public string Disposition { get; set; }
        [StringLength(300)]
        public string RecordingFile { get; set; }
        [StringLength(50)]
        public string CNam { get; set; }
        [StringLength(10)]
        public string Type { get; set; }
        public virtual User User { get; set; }
        public virtual HistoryUser HistoryUser { get; set; }

    }
}