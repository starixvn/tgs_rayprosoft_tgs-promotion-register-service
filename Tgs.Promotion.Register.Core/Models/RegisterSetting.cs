namespace Tgs.Promotion.Register.Core.Models;

public class RegisterSetting
{
	public decimal BonusAmount {get; set;}
	public decimal Percentage {get; set;}
	public decimal BonusAmountLimit { get; set; }
	public decimal TurnoverMultiple { get; set; }
}