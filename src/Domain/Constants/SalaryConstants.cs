namespace Domain.Constants.EntityNames;

public static class SalaryConstants
{
    public const int Kpi = 30; // common field for calculating salary of consultant and technician

    // Consultant
    public const int ConsultantBaseSalary = 300000; // vnd
    public const decimal ConsultRewardedRate = 2.0m;

    // Technician
    public const int TechnicianBaseSalary = 333333; // vnd
    public const decimal TechnicianRewardedRate = 2.0m;
}
