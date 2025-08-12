using SynapseHealth.Core.Models;

namespace SynapseHealth.Core.Services
{
    public interface IOrderSubmissionService
    {
        Task SubmitOrderAsync(OrderDetails orderDetails);
    }
}

