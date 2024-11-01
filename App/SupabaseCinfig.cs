namespace DotNet.Docker;

public static class SupabaseConfig
{
    public static Supabase.Client CreateClient()
    {
        return new Supabase.Client("https://your-project-id.supabase.co", "your-anon-key");
    }
}