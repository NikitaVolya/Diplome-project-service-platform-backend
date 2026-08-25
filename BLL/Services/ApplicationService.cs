using BLL.Services.Interfaces;
using DAL.UnitOfWork.Interfaces;
using Domain.Models;

namespace BLL.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ApplicationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Application?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Applications.GetWithDetailsByIdAsync(id);
        }

        public async Task<IEnumerable<Application>> GetByOrderIdAsync(int orderId)
        {
            return await _unitOfWork.Applications.GetByOrderIdAsync(orderId);
        }

        public async Task<IEnumerable<Application>> GetByExecutorIdAsync(string executorId)
        {
            return await _unitOfWork.Applications.GetByExecutorIdAsync(executorId);
        }

        public async Task<Application> CreateAsync(int orderId, string executorId, decimal proposedPrice, string? comment)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with id {orderId} not found.");
            }

            if (order.CustomerId == executorId)
            {
                throw new InvalidOperationException("Customer cannot apply to their own order.");
            }

            var hasApplied = await _unitOfWork.Applications.HasUserAppliedAsync(orderId, executorId);
            if (hasApplied)
            {
                throw new InvalidOperationException("User has already applied to this order.");
            }

            var application = new Application
            {
                OrderId = orderId,
                ExecutorId = executorId,
                ProposedPrice = proposedPrice,
                Comment = comment,
                CreatedAt = DateTime.UtcNow,
                Status = ApplicationStatus.Pending
            };
            await _unitOfWork.Applications.AddAsync(application);
            await _unitOfWork.SaveChangesAsync();

            return application;
        }

        public async Task AcceptApplicationAsync(int applicationId, string currentUserId)
        {
            var application = await _unitOfWork.Applications.GetWithDetailsByIdAsync(applicationId);
            if (application == null)
            {
                throw new ArgumentException($"Application with id {applicationId} not found.");
            }

            if (application.Order.CustomerId != currentUserId)
            {
                throw new UnauthorizedAccessException("Only the customer who created the order can accept applications.");
            }

            if (application.Status != ApplicationStatus.Pending)
            {
                throw new InvalidOperationException("Only pending applications can be accepted.");
            }
            
            await _unitOfWork.Applications.UpdateStatusAsync(applicationId, ApplicationStatus.Accepted);

            application.Order.ExecutorId = application.ExecutorId;
            application.Order.Status = OrderStatus.InProgress;

            var otherApplications = await _unitOfWork.Applications.GetByOrderIdAsync(application.OrderId);
            foreach (var otherApplication in otherApplications)
            {
                if (otherApplication.Id != applicationId && otherApplication.Status == ApplicationStatus.Pending)
                {
                    await _unitOfWork.Applications.UpdateStatusAsync(otherApplication.Id, ApplicationStatus.Rejected);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RejectApplicationAsync(int applicationId, string currentUserId)
        {
            var application = await _unitOfWork.Applications.GetWithDetailsByIdAsync(applicationId);
            if (application == null)
            {
                throw new ArgumentException($"Application with id {applicationId} not found.");
            }
            if (application.Order.CustomerId != currentUserId)
            {
                throw new UnauthorizedAccessException("Only the customer who created the order can reject applications.");
            }
            if (application.Status != ApplicationStatus.Pending)
            {
                throw new InvalidOperationException("Only pending applications can be rejected.");
            }
            await _unitOfWork.Applications.UpdateStatusAsync(applicationId, ApplicationStatus.Rejected);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int applicationId, string currentUserId)
        {
            var application = await _unitOfWork.Applications.GetWithDetailsByIdAsync(applicationId);
            if (application == null)
            {
                throw new ArgumentException($"Application with id {applicationId} not found.");
            }
            if (application.ExecutorId != currentUserId)
            {
                throw new UnauthorizedAccessException("Only the executor who created the application can delete it.");
            }
            if (application.Status != ApplicationStatus.Pending)
            {
                throw new InvalidOperationException("Only pending applications can be deleted.");
            }
            await _unitOfWork.Applications.DeleteAsync(applicationId);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}