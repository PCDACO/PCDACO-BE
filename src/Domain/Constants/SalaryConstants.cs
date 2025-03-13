namespace Domain.Constants.EntityNames;

public static class SalaryConstants
{
    public const int Kpi = 80; // common field for calculating salary of consultant and technician

    // Consultant
    public const int ConsultantBaseSalary = 50000; // vnd
    public const decimal ConsultRewardedRate = 1.2m;

    // Technician
    public const int TechnicianBaseSalary = 100000; // vnd
    public const decimal TechnicianRewardedRate = 1.5m;
}
