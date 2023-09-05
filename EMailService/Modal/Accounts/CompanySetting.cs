using System;
using System.Collections.Generic;

namespace ModalLayer.Modal.Accounts
{
    public class CompanySetting
    {
        public CompanySetting()
        {
            SetupRecommandedWeekEnds();
        }

        private void SetupRecommandedWeekEnds()
        {
            OfficialWeekOffDays = new List<DayOfWeek>();
            OfficialWeekOffDays.Add(DayOfWeek.Saturday);
            OfficialWeekOffDays.Add(DayOfWeek.Sunday);
        }

        public int SettingId { set; get; }
        public int CompanyId { set; get; }
        public int ProbationPeriodInDays { set; get; }
        public int NoticePeriodInDays { set; get; }
        public int PayrollCycleMonthlyRunDay { set; get; } = DateTime.Now.Day;
        public int FinancialYear { set; get; }
        public int DeclarationStartMonth { set; get; }
        public int DeclarationEndMonth { set; get; }
        public bool IsPrimary { set; get; }
        public int WorkingDaysInAWeek { set; get; } = 5;
        // This value will come from database and filled by admin using page.
        public int EveryMonthLastDayOfDeclaration { set; get; }
        public bool IsUseInternationalWeekDays { set; get; } = true;
        public List<DayOfWeek> OfficialWeekOffDays { set; get; }
        public bool IsAccrualLeaveForNoticePeriodOnly { set; get; } // override all rule and allow leave for 2 or 3 months (define as per rule) leaves only.
        public bool IsAccrualLeaveForProbationPeriondOnly { set; get; } // override all rule and allow leave for 2 or 3 months (define as per rule) leaves only.
        public int AttendanceSubmissionLimit { set; get; } = 2;
        public int LeaveAccrualRunCronDayOfMonth { set; get; }
        public string TimezoneName { set; get; }
        public bool IsJoiningBarrierDayPassed { set; get; }
    }
}
