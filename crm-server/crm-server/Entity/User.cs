namespace crm_server.Entity
{
    public class User : AuditableEntity //find deleted field here
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty ;
        public string Role { get; set; } = string.Empty;
        public string? RefreshTokens { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
