namespace Matching.Application.Helpers;

/// <summary>
/// Helper hỗ trợ làm tròn và xử lý tiền tệ theo chuẩn Việt Nam Đồng (VND).
/// </summary>
public static class CurrencyHelper
{
    /// <summary>
    /// Làm tròn số tiền VND theo đơn vị làm tròn (mặc định làm tròn đến 1.000 VNĐ gần nhất).
    /// Ví dụ: 67.891đ -> 68.000đ.
    /// </summary>
    /// <param name="amount">Số tiền cần làm tròn.</param>
    /// <param name="roundStep">Bước làm tròn (mặc định 1.000đ).</param>
    /// <returns>Số tiền sau khi làm tròn.</returns>
    public static double RoundVnd(this double amount, double roundStep = 1000.0)
    {
        if (amount <= 0 || roundStep <= 0)
        {
            return 0.0;
        }

        return Math.Round(amount / roundStep, MidpointRounding.AwayFromZero) * roundStep;
    }
}
