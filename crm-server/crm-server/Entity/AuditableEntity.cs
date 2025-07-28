namespace crm_server.Entity
{
    public abstract class AuditableEntity
    {   
        // it is added as inherited for user table 
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

}
