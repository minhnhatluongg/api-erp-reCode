using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.DTOs
{
    public class DashboardStatsDto
    {
        public int countAll { get; set; }
        public int countAllSum { get; set; }
        public int countAllDay { get; set; }
        public int countAllMonth { get; set; }
        public int countAllYear { get; set; }
        public int countAllhcs { get; set; }
        public int countSKHDThcs { get; set; }
        public int countAllhct { get; set; }
        public int count0 { get; set; }
        public int count0Sum { get; set; }
        public int count0Day { get; set; }
        public int count0Month { get; set; }
        public int count101Month { get; set; }
        public int count301Month { get; set; }
        public int count201Month { get; set; }
        public int countCloseMonth { get; set; }
        public int count101 { get; set; }
        public int count101Sum { get; set; }
        public int count301 { get; set; }
        public int count301Sum { get; set; }
        public int count201 { get; set; }
        public int count201Sum { get; set; }
        public int countBack { get; set; }
        public int countBack200 { get; set; }
        public int countBack300 { get; set; }
        public int countBack500 { get; set; }
        public int countKH { get; set; }
        public int countKHSum { get; set; }
        public int countPH { get; set; }
        public int countPHSum { get; set; }
        public int countEnd { get; set; }
        public int countEndSum { get; set; }
        public int countClose { get; set; }
        public int countCloseSum { get; set; }
        public int countKHSign { get; set; }
        public int countKHSignSum { get; set; }
        public int countCancelNotice { get; set; }
        public int countCancelNoticeCKS { get; set; }
        public decimal sumDT { get; set; } = 0;
        public int countAlluser { get; set; }
        public int count0user { get; set; }
        public int count_wait_KT_user { get; set; }
        public int count_wait_hcs_user { get; set; }
        public int count_complete_hcs_user { get; set; }
        public int count_complete_skh_user { get; set; }
        public int count_wait_KT2_user { get; set; }
        public int count_wait_hct_user { get; set; }
        public int count_complete_hct_user { get; set; }
        public int count_complete_user { get; set; }
        public int count_close_user { get; set; }
        public int count_wait_KT { get; set; }
        public int count_wait_hcs { get; set; }
        public int count_complete_hcs { get; set; }
        public int count_complete_skh { get; set; }
        public int count_wait_KT2 { get; set; }
        public int count_wait_hct { get; set; }
        public int count_complete_hct { get; set; }
        public int count_complete { get; set; }
        public int count_close { get; set; }
        public int counthcscompleteuser { get; set; }
        public int count101user { get; set; }
        public int count301user { get; set; }
        public int count201user { get; set; }
        public int countKHuser { get; set; }
        public int countPHuser { get; set; }
        public int countEnduser { get; set; }
        public int countCloseuser { get; set; }
        public int countKHSignuser { get; set; }
        public decimal sumDTuser { get; set; } = 0;
        public int countAllCKS { get; set; }
        public int count0CKS { get; set; }
        public int count101CKS { get; set; }
        public int count301CKS { get; set; }
        public int count201CKS { get; set; }
        public int countBackCKS { get; set; }
    }
}
