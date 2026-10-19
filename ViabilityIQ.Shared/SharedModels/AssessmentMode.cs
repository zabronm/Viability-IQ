namespace ViabilityIQ.Shared.SharedModels;

public enum AssessmentMode
{
    Trading = 1,
    Service = 2,
    // Retained for existing assessments. New manufacturing assessments use Manufacturing.
    ManufacturingSimplified = 3,
    Manufacturing = 4
}

public enum ManufacturingInputMethod
{
    MonthlyEstimates = 1,
    BillOfMaterials = 2,
    JobBased = 3
}
