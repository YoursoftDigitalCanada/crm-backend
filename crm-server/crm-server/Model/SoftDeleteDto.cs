namespace crm_server.Model
{
    public class SoftDeleteDto
    {
        public required Guid UserId { get; set; }
        public required string AccessToken { get; set; }
    }
}
