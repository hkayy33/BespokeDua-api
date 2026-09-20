using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BespokeDuaApi.Auth;

public static class SupabaseAuthExtensions
{
    public const string SchemeName = JwtBearerDefaults.AuthenticationScheme;
}
