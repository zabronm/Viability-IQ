using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ViabilityIQ.Shared.SharedModels
{

    //==================  DEFINES LOAN CALCULATION METHODS  ==========================
    public enum LoanCalculationMethodsEnums
    {
        ReducingBalance = 1,
        FlatRate = 2,
        InterestOnly = 3,
        FixedPrincipal = 4,
        Balloon = 5
    }
}
