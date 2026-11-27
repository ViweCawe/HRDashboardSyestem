namespace HrDashboard.Components.Shared;

public static class EmployeeInitials
{
    public static string For(string displayName)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p is not ("Mr" or "Mrs" or "Ms" or "Dr" or "Mr." or "Mrs." or "Ms." or "Dr.")).ToArray();
        return parts.Length == 0 ? "HR" : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }
}
