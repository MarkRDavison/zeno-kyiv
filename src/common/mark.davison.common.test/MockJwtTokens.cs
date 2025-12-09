namespace mark.davison.common.test;

public static class MockJwtTokens
{
    public static string Issuer { get; set; } = Guid.NewGuid().ToString();
    public static SecurityKey SecurityKey { get; }
    public static SigningCredentials SigningCredentials { get; }

    private static readonly JwtSecurityTokenHandler s_tokenHandler = new JwtSecurityTokenHandler();
    private static readonly RandomNumberGenerator s_rng = RandomNumberGenerator.Create();
    private static readonly byte[] s_key = new byte[32];

    static MockJwtTokens()
    {
        s_rng.GetBytes(s_key);
        SecurityKey = new SymmetricSecurityKey(s_key) { KeyId = Guid.NewGuid().ToString() };
        SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
    }

    public static string GenerateJwtToken(IEnumerable<Claim> claims)
    {
        return GenerateJwtToken(claims, DateTime.UtcNow.AddMinutes(20));
    }

    public static string GenerateJwtToken(IEnumerable<Claim> claims, DateTime expiration)
    {
        var header = new JwtHeader(SigningCredentials);
        var payload = new JwtPayload(Issuer, null, null, null, expiration);
        payload.AddClaims(claims);
        return s_tokenHandler.WriteToken(new JwtSecurityToken(header, payload));
    }
}