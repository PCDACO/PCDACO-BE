using Net.payOS.Types;

namespace UseCases.Services.PayOSService;

public record CreatePaymentRequest(
    long OrderCode,
    int Amount,
    string Description,
    string BuyerName,
    string ReturnUrl,
    string CancelUrl,
    IEnumerable<ItemData> Items
);
