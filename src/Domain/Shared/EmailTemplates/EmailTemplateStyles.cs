namespace Domain.Shared.EmailTemplates;

public static class EmailTemplateStyles
{
    public static string ContainerStyle =>
        "font-family: Roboto, Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333; line-height: 1.6;";

    public static string HeaderStyle(string backgroundColor) =>
        $"background-color: {backgroundColor}; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;";

    public static string BodyStyle =>
        "padding: 20px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;";

    public static string DetailBoxStyle(string backgroundColor) =>
        $"background-color: {backgroundColor}; padding: 20px; border-radius: 8px; margin: 20px 0;";

    public static string FooterStyle => "text-align: center; color: #666; margin-top: 30px;";
}
