using BLL.Exceptions;
using BLL.Results;
using BLL.Services.Interfaces;
using DAL.UnitOfWork.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class OrderMessageService : IOrderMessageService
    {
        // Must match HasMaxLength in ApplicationDbContext.
        private const int MaxMessageLength = 1000;

        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public OrderMessageService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        private enum ChatAccess
        {
            /// <summary>This executor has nothing to do with the order: no access.</summary>
            None,

            /// <summary>History can be read, new messages cannot be sent.</summary>
            ReadOnly,

            /// <summary>Reading and writing are allowed.</summary>
            Active
        }

        // ------------------------------------------------------------------ send

        public async Task<OrderMessage> SendMessageAsync(
            int orderId,
            string senderId,
            bool senderIsExecutor,
            string? receiverId,
            string text)
        {
            var cleanText = NormalizeText(text);

            var order = await GetOrderAsync(orderId);

            var executorId = await ResolveExecutorAsync(
                order, senderId, senderIsExecutor, receiverId, forSending: true);

            // The receiver is always "the other one" of the (customer, executor) pair.
            var actualReceiverId = senderId == order.CustomerId ? executorId : order.CustomerId;

            var message = new OrderMessage
            {
                OrderId = orderId,
                SenderId = senderId,
                ReceiverId = actualReceiverId,
                Text = cleanText,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.OrderMessages.AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                userId: actualReceiverId,
                title: "New message in chat",
                message: cleanText.Length > 50 ? $"{cleanText.Substring(0, 47)}..." : cleanText
            );

            return message;
        }

        // ------------------------------------------------------------------ read one conversation

        public async Task<IEnumerable<OrderMessage>> GetConversationAsync(
            int orderId,
            string currentUserId,
            bool currentUserIsExecutor,
            string? participantId)
        {
            var order = await GetOrderAsync(orderId);

            var executorId = await ResolveExecutorAsync(
                order, currentUserId, currentUserIsExecutor, participantId, forSending: false);

            var counterpartId = currentUserId == order.CustomerId ? executorId : order.CustomerId;

            // Only what the counterpart sent to me in THIS conversation becomes "read".
            await _unitOfWork.OrderMessages.MarkConversationAsReadAsync(orderId, counterpartId, currentUserId);

            return await _unitOfWork.OrderMessages.GetConversationAsync(orderId, currentUserId, counterpartId);
        }

        // ------------------------------------------------------------------ dialog list

        public async Task<IEnumerable<OrderDialog>> GetUserDialogsAsync(string currentUserId, int? orderId = null)
        {
            var summaries = await _unitOfWork.OrderMessages.GetDialogSummariesAsync(currentUserId, orderId);

            var dialogs = summaries.Select(summary =>
            {
                var last = summary.LastMessage;

                // The customer of the conversation is whoever of the two is the customer of the order.
                var customer = last.Sender.Id == last.Order.CustomerId ? last.Sender : last.Receiver;
                var executor = customer.Id == last.Sender.Id ? last.Receiver : last.Sender;

                return new OrderDialog
                {
                    Order = last.Order,
                    Customer = customer,
                    Executor = executor,
                    LastMessage = last,
                    UnreadCount = summary.UnreadCount
                };
            });

            return dialogs
                .OrderByDescending(d => d.LastMessage.SentAt)
                .ToList();
        }

        public async Task<int> GetTotalUnreadCountAsync(string currentUserId)
        {
            return await _unitOfWork.OrderMessages.GetTotalUnreadCountAsync(currentUserId);
        }

        // ------------------------------------------------------------------ rules

        /// <summary>
        /// Decides WHICH conversation the caller means and whether he may use it.
        /// A conversation is always (order, customer of the order, one executor), so it is enough to
        /// find the executor; the customer is fixed by the order.
        /// </summary>
        private async Task<string> ResolveExecutorAsync(
            Order order,
            string currentUserId,
            bool currentUserIsExecutor,
            string? otherPartyId,
            bool forSending)
        {
            string executorId;
            bool customerSide;

            if (currentUserId == order.CustomerId)
            {
                customerSide = true;

                if (!string.IsNullOrWhiteSpace(otherPartyId))
                {
                    executorId = otherPartyId;
                }
                else if (!string.IsNullOrEmpty(order.ExecutorId))
                {
                    // Unambiguous: once an executor is assigned the others are rejected.
                    executorId = order.ExecutorId;
                }
                else
                {
                    throw new ValidationFailedException(
                        "Specify which executor you are talking to (receiverId / participantId): " +
                        "the order has no assigned executor yet.");
                }

                if (executorId == order.CustomerId)
                {
                    throw new ValidationFailedException("You cannot write to yourself.");
                }
            }
            else if (currentUserIsExecutor)
            {
                customerSide = false;

                if (!string.IsNullOrWhiteSpace(otherPartyId) && otherPartyId != order.CustomerId)
                {
                    throw new ForbiddenException("An executor can only talk to the customer of the order.");
                }

                executorId = currentUserId;
            }
            else
            {
                throw new ForbiddenException("Only the customer of the order or an executor can use this chat.");
            }

            var access = await GetAccessAsync(order, executorId, customerSide);

            if (access == ChatAccess.None)
            {
                throw new ForbiddenException(customerSide
                    ? "This executor has neither applied to the order nor written to you."
                    : "You have no access to the chat of this order.");
            }

            if (forSending && access == ChatAccess.ReadOnly)
            {
                throw new ConflictException(
                    "This chat is closed: the order is already taken, finished, or the application was declined. " +
                    "The history stays readable.");
            }

            return executorId;
        }

        /// <summary>
        /// Who may talk to whom on an order:
        ///  - the assigned executor and the customer: always;
        ///  - while the order is OPEN (pending, nobody assigned):
        ///      an executor may write to the customer even before applying (to ask about the job),
        ///      the customer may answer executors who applied or already wrote to him
        ///      (so a customer cannot cold-message arbitrary executors);
        ///    an executor whose application was declined can only read;
        ///  - once the order is taken by someone else, finished or cancelled: the history of
        ///    existing conversations stays readable, nothing new can be sent.
        /// </summary>
        private async Task<ChatAccess> GetAccessAsync(Order order, string executorId, bool customerSide)
        {
            if (order.ExecutorId == executorId)
            {
                return ChatAccess.Active;
            }

            var applications = (await _unitOfWork.Applications.GetByOrderIdAsync(order.Id))
                .Where(a => a.ExecutorId == executorId)
                .ToList();

            var hasApplication = applications.Count > 0;
            var hasLiveApplication = applications.Any(a => a.Status != ApplicationStatus.Rejected);
            var hasHistory = hasApplication ||
                await _unitOfWork.OrderMessages.HasConversationAsync(order.Id, order.CustomerId, executorId);

            var orderIsOpen = order.Status == OrderStatus.Pending && string.IsNullOrEmpty(order.ExecutorId);

            if (orderIsOpen)
            {
                if (hasApplication && !hasLiveApplication)
                {
                    // The customer declined this executor.
                    return ChatAccess.ReadOnly;
                }

                if (customerSide)
                {
                    return hasHistory ? ChatAccess.Active : ChatAccess.None;
                }

                return ChatAccess.Active;
            }

            return hasHistory ? ChatAccess.ReadOnly : ChatAccess.None;
        }

        // ------------------------------------------------------------------ helpers

        private async Task<Order> GetOrderAsync(int orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new NotFoundException("Order not found.");
            }

            return order;
        }

        private static string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ValidationFailedException("Message text cannot be empty.");
            }

            var trimmed = text.Trim();

            if (trimmed.Length > MaxMessageLength)
            {
                throw new ValidationFailedException($"Message text cannot be longer than {MaxMessageLength} characters.");
            }

            return trimmed;
        }
    }
}
