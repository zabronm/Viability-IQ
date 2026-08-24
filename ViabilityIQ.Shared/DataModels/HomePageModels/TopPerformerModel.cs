using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels.HomePageModels
{    
    /// Represents a top performer on the leaderboard    
    public class TopPerformerModel
    {        
          
        public long userId { get; set; }  /// User ID    
        public string Name { get; set; }    /// Full name of the user       
        public int CompletedCount { get; set; } /// Number of assessments completed this month        


        /// Average rating or score (optional)
        public decimal Score { get; set; }

        
        /// Rank position (1, 2, 3, etc.)        
        public int Rank { get; set; }
    }
}