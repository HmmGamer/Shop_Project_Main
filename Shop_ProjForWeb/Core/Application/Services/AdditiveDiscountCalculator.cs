using Shop_ProjForWeb.Core.Domain.Interfaces;

namespace Shop_ProjForWeb.Core.Application.Services;

public class AdditiveDiscountCalculator : IDiscountCalculator
{
    private readonly IVipStatusCalculator _vipStatusCalculator;

    public AdditiveDiscountCalculator(IVipStatusCalculator vipStatusCalculator)
    {
        _vipStatusCalculator = vipStatusCalculator;
    }

    public decimal CalculateFinalPrice(decimal basePrice, decimal productDiscountPercent, bool isVip)
    {
        var vipTier = isVip ? 1 : 0;
        return CalculateFinalPrice(basePrice, productDiscountPercent, vipTier);
    }

    public decimal CalculateFinalPrice(decimal basePrice, decimal productDiscountPercent, int vipTier)
    {
        ValidateInputs(basePrice, productDiscountPercent, vipTier);
        var vipDiscountPercent = _vipStatusCalculator.GetDiscountPercentForTier(vipTier);
        var productDiscountAmount = basePrice * (productDiscountPercent / 100m);
        var vipDiscountAmount = basePrice * (vipDiscountPercent / 100m);
        var totalDiscountAmount = Math.Min(productDiscountAmount + vipDiscountAmount, basePrice);
        var finalPrice = basePrice - totalDiscountAmount;
        return Math.Round(finalPrice, 2);
    }

    public DiscountBreakdown GetDiscountBreakdown(decimal basePrice, decimal productDiscountPercent, bool isVip)
    {
        var vipTier = isVip ? 1 : 0;
        return GetDiscountBreakdown(basePrice, productDiscountPercent, vipTier);
    }

    public DiscountBreakdown GetDiscountBreakdown(decimal basePrice, decimal productDiscountPercent, int vipTier)
    {
        ValidateInputs(basePrice, productDiscountPercent, vipTier);
        var vipDiscountPercent = _vipStatusCalculator.GetDiscountPercentForTier(vipTier);
        var productDiscountAmount = basePrice * (productDiscountPercent / 100m);
        var vipDiscountAmount = basePrice * (vipDiscountPercent / 100m);
        var totalDiscountAmount = Math.Min(productDiscountAmount + vipDiscountAmount, basePrice);
        var finalPrice = Math.Round(basePrice - totalDiscountAmount, 2);
        var effectiveDiscountPercent = basePrice > 0 ? totalDiscountAmount / basePrice * 100m : 0m;
        return new DiscountBreakdown
        {
            BasePrice = basePrice,
            ProductDiscountAmount = Math.Round(productDiscountAmount, 2),
            VipDiscountAmount = Math.Round(vipDiscountAmount, 2),
            TotalDiscountAmount = Math.Round(totalDiscountAmount, 2),
            FinalPrice = finalPrice,
            ProductDiscountPercent = productDiscountPercent,
            VipDiscountPercent = vipDiscountPercent,
            EffectiveDiscountPercent = Math.Round(effectiveDiscountPercent, 2),
            VipTier = vipTier
        };
    }

    private static void ValidateInputs(decimal basePrice, decimal productDiscountPercent, int vipTier)
    {
        if (basePrice < 0) throw new ArgumentException("Base price cannot be negative");
        if (productDiscountPercent < 0 || productDiscountPercent > 100) throw new ArgumentException("Product discount percent must be between 0 and 100");
        if (vipTier < 0 || vipTier > 3) throw new ArgumentException("VIP tier must be between 0 and 3");
    }
}
