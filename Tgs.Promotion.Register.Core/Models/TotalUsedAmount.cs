namespace Tgs.Promotion.Register.Core.Models;

public class TotalUsedAmount
{
    public Guid PromotionId { get; set; }
    public Guid SnapshotId { get; set; }
    public decimal TotalAmount { get; set; }
}