using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models.AI;

/// <summary>Tenant-owned metadata for a private knowledge source. Raw content is never stored in this row.</summary>
public sealed class KnowledgeDocument
{
    [Key] public Guid Id { get; set; }
    public int TenantId { get; set; }
    [Required, StringLength(256)] public string Title { get; set; } = null!;
    public int OwnerSystemUserId { get; set; }
    [Required, StringLength(4000)] public string AccessPrincipalsJson { get; set; } = null!;
    public long AccessPolicyVersion { get; set; } = 1;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<KnowledgeDocumentVersion> Versions { get; set; } = new List<KnowledgeDocumentVersion>();
}

/// <summary>Immutable file version metadata. The source payload remains in the configured private object store.</summary>
public sealed class KnowledgeDocumentVersion
{
    [Key] public Guid Id { get; set; }
    public int TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public KnowledgeDocument Document { get; set; } = null!;
    public int VersionNumber { get; set; }
    [Required, StringLength(64)] public string ContentSha256 { get; set; } = null!;
    [Required, StringLength(2048)] public string SourceBlobUri { get; set; } = null!;
    [Required, StringLength(255)] public string OriginalFileName { get; set; } = null!;
    [Required, StringLength(128)] public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    [Required, StringLength(24)] public string Status { get; set; } = "Stored";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<KnowledgeChunk> Chunks { get; set; } = new List<KnowledgeChunk>();
    public ICollection<DocumentIngestionJob> IngestionJobs { get; set; } = new List<DocumentIngestionJob>();
}

/// <summary>Trace metadata for an indexed chunk. Chunk text remains in private storage/search, not SQL.</summary>
public sealed class KnowledgeChunk
{
    [Key] public Guid Id { get; set; }
    public int TenantId { get; set; }
    public Guid DocumentVersionId { get; set; }
    public KnowledgeDocumentVersion DocumentVersion { get; set; } = null!;
    [Required, StringLength(128)] public string PipelineVersion { get; set; } = null!;
    public long AccessPolicyVersion { get; set; }
    public int Ordinal { get; set; }
    [Required, StringLength(64)] public string ContentSha256 { get; set; } = null!;
    [Required, StringLength(2048)] public string ContentBlobUri { get; set; } = null!;
    [Required, StringLength(256)] public string SearchIndexKey { get; set; } = null!;
    public int? Page { get; set; }
    [StringLength(256)] public string? Section { get; set; }
    public int TokenCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Durable metadata-only transport for MinerU, embedding, and search indexing.</summary>
public sealed class DocumentIngestionJob
{
    [Key] public Guid Id { get; set; }
    public int TenantId { get; set; }
    public Guid DocumentVersionId { get; set; }
    public KnowledgeDocumentVersion DocumentVersion { get; set; } = null!;
    [Required, StringLength(16)] public string Operation { get; set; } = "Index";
    [Required, StringLength(128)] public string PipelineVersion { get; set; } = null!;
    public long AccessPolicyVersion { get; set; }
    public int? RequestedBySystemUserId { get; set; }
    [Required, StringLength(24)] public string State { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset AvailableAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid? LeaseId { get; set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    [StringLength(200)] public string? MinerUJobId { get; set; }
    [StringLength(2048)] public string? ParserResultBlobUri { get; set; }
    [StringLength(64)] public string? LastFailureCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
