using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.HomePageModels
{
    public class AlertsModel
    {       
        public List<UrgentAssessmentModel> UrgentAssessments { get; set; } = new();
        public List<UpcomingAssessmentModel> DueThisWeek { get; set; } = new();
    }
}
