namespace AegiDocs.Infrastructure.Storage.Paths;

public interface IReparsePointPolicy
{
    bool IsReparsePoint(string canonicalPath);
    bool IsFinalHandleUnderRoot(string canonicalRoot, string finalHandlePath);
}

public static class ProjectPathPolicy
{
    public static bool TryValidateRelativePath(string path, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.Contains('\\') || path.Contains("..", StringComparison.Ordinal) || path.Contains("//", StringComparison.Ordinal) || path.Length > 180) return false;
        var segments = path.Split('/');
        if (segments.Length is 0 or > 4 || segments.Any(segment => segment.Length is 0 or > 80 || segment == "." || segment.Any(char.IsControl) || segment.Contains(':'))) return false;
        canonical = string.Join('/', segments);
        return true;
    }
    public static bool IsSafeUnderRoot(string canonicalRoot, string finalHandlePath, IReparsePointPolicy policy) =>
        !policy.IsReparsePoint(canonicalRoot) && !policy.IsReparsePoint(finalHandlePath) && policy.IsFinalHandleUnderRoot(canonicalRoot, finalHandlePath);
}
