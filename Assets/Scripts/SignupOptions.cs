using System.Collections.Generic;
using Supabase.Gotrue;

internal class SignupOptions : SignUpOptions
{
    public Dictionary<string, object> Data { get; set; }
}