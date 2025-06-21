using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Hubs;
using olx_be_api.Models;
using System;

namespace olx_be_api.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController(AppDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<MessageResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessages()
        {
            var messages = await _context.Messages.ToListAsync();

            var response = messages.Select(m => new MessageResponseDto
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                ChatRoomId = m.ChatRoomId,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            }).ToList();

            return Ok(new ApiResponse<List<MessageResponseDto>>
            {
                success = true,
                message = "Messages retrieved successfully",
                data = response
            });
        }

        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<MessageResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessage(Guid id)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    success = false,
                    message = "Authentication required"
                });
            }

            var message = await _context.Messages
                .Include(m => m.ChatRoom)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Message not found"
                });
            }

            if (message.ChatRoom.BuyerId != userId && message.ChatRoom.SellerId != userId)
            {
                return Forbid("Akses ditolak");
            }

            return Ok(new ApiResponse<MessageResponseDto>
            {
                success = true,
                message = "Message retrieved successfully",
                data = new MessageResponseDto
                {
                    Id = message.Id,
                    Content = message.Content,
                    SenderId = message.SenderId,
                    ChatRoomId = message.ChatRoomId,
                    IsRead = message.IsRead,
                    CreatedAt = message.CreatedAt
                }
            });
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<MessageResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageDto createMessageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Invalid message data",
                    errors = ModelState
                });
            }

            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    success = false,
                    message = "Authentication required"
                });
            }

            var chatRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(c => c.Id == createMessageDto.ChatRoomId &&
                                         (c.BuyerId == userId || c.SellerId == userId));

            if (chatRoom == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Chat room not found or access denied"
                });
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChatRoomId = createMessageDto.ChatRoomId,
                SenderId = userId,
                Content = createMessageDto.Content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Messages.AddAsync(message);

            var recipientId = chatRoom.BuyerId == userId ? chatRoom.SellerId : chatRoom.BuyerId;
            var sender = await _context.Users.FindAsync(userId);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = recipientId,
                Title = $"New message from {sender?.Name ?? "Someone"}",
                Message = message.Content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Notifications.AddAsync(notification);

            await _context.SaveChangesAsync();

            var messageResponse = new MessageResponseDto
            {
                Id = message.Id,
                Content = message.Content,
                SenderId = message.SenderId,
                ChatRoomId = message.ChatRoomId,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt
            };

            await _hubContext.Clients
                .Group(chatRoom.Id.ToString())
                .SendAsync("ReceiveMessage", messageResponse);

            return CreatedAtAction(nameof(GetMessage), new { id = message.Id },
                new ApiResponse<MessageResponseDto>
                {
                    success = true,
                    message = "Message created successfully",
                    data = messageResponse
                });
        }

        [HttpPatch("{id}/read")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<MessageResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    success = false,
                    message = "Authentication required"
                });
            }

            var message = await _context.Messages
                .Include(m => m.ChatRoom)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Message not found"
                });
            }

            if (message.ChatRoom.BuyerId != userId && message.ChatRoom.SellerId != userId)
            {
                return Forbid("Akses ditolak");
            }

            if (message.SenderId != userId)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new ApiResponse<MessageResponseDto>
            {
                success = true,
                message = "Message marked as read",
                data = new MessageResponseDto
                {
                    Id = message.Id,
                    Content = message.Content,
                    SenderId = message.SenderId,
                    ChatRoomId = message.ChatRoomId,
                    IsRead = message.IsRead,
                    CreatedAt = message.CreatedAt
                }
            });
        }
    }
}