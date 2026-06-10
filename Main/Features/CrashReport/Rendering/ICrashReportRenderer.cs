using DOTS.Features.CrashReport.Domain;

namespace DOTS.Features.CrashReport.Rendering;

public interface ICrashReportRenderer
{
    string Render(ExceptionContext context);
}
