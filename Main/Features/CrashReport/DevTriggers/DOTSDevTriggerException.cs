using System;

namespace DOTS.Features.CrashReport.DevTriggers;

// Tagged exception type so post-mortem scripts can distinguish manufactured QA
// crashes from real player crashes. Always thrown via CrashReportDevTrigger.
public sealed class DotsDevTriggerException : Exception
{
    public DotsDevTriggerException(string message) : base(message) { }
}
