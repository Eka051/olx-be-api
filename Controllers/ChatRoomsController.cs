using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;
using Microsoft.EntityFrameworkCore;

namespace olx_be_api.Controllers
{
    [Route("api/chatRooms")]
    [ApiController]
    public class ChatRoomsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChatRoomsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}/messages")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<MessageResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessages(Guid id)
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

            var chatRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(c => c.Id == id && (c.BuyerId == userId || c.SellerId == userId));

            if (chatRoom == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Chat room not found or access denied"
                });
            }

            var messages = await _context.Messages
                .Where(m => m.ChatRoomId == id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var unreadMessages = messages.Where(m => !m.IsRead && m.SenderId != userId).ToList();
            if (unreadMessages.Any())
            {
                unreadMessages.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

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

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<ChatRoomResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetChatRooms()
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

            var chatRooms = await _context.ChatRooms
                .Include(c => c.Product)
                .Include(c => c.Buyer)
                .Include(c => c.Seller)
                .Where(c => c.BuyerId == userId || c.SellerId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var chatRoomIds = chatRooms.Select(c => c.Id).ToList();
            var messagesQuery = _context.Messages
                .Where(m => chatRoomIds.Contains(m.ChatRoomId))
                .GroupBy(m => m.ChatRoomId);

            var lastMessages = await messagesQuery
                .Select(g => g.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
                .Where(m => m != null)
                .ToDictionaryAsync(m => m!.ChatRoomId, m => m);

            var unreadCounts = await messagesQuery
                .Select(g => new
                {
                    ChatRoomId = g.Key,
                    UnreadCount = g.Count(m => !m.IsRead && m.SenderId != userId)
                })
                .ToDictionaryAsync(x => x.ChatRoomId, x => x.UnreadCount);

            var response = chatRooms.Select(c => new ChatRoomResponseDto
            {
                Id = c.Id,
                ProductId = c.ProductId,
                ProductTitle = c.Product.Title,
                BuyerId = c.BuyerId,
                BuyerName = c.Buyer.Name,
                SellerId = c.SellerId,
                SellerName = c.Seller.Name,
                CreatedAt = c.CreatedAt,
                LastMessage = lastMessages.ContainsKey(c.Id) ? lastMessages[c.Id]!.Content : null,
                LastMessageAt = lastMessages.ContainsKey(c.Id) ? lastMessages[c.Id]!.CreatedAt : c.CreatedAt,
                UnreadCount = unreadCounts.ContainsKey(c.Id) ? unreadCounts[c.Id] : 0
            }).ToList();

            return Ok(new ApiResponse<List<ChatRoomResponseDto>>
            {
                success = true,
                message = "Chat rooms retrieved successfully",
                data = response
            });
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ChatRoomResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateChatRoom([FromBody] CreateChatRoomDto createChatRoomDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Invalid chat room data",
                    errors = ModelState
                });
            }

            var buyerId = User.GetUserId();
            if (buyerId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    success = false,
                    message = "Authentication required"
                });
            }

            var product = await _context.Products.FindAsync(createChatRoomDto.ProductId);
            if (product == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Product not found"
                });
            }

            var sellerId = product.UserId;

            if (sellerId == buyerId)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Cannot create chat room with your own product"
                });
            }

            var existingChat = await _context.ChatRooms
                .FirstOrDefaultAsync(c => c.ProductId == createChatRoomDto.ProductId &&
                                         c.BuyerId == buyerId &&
                                         c.SellerId == sellerId);

            if (existingChat != null)
            {
                return Ok(new ApiResponse<ChatRoomResponseDto>
                {
                    success = true,
                    message = "Chat room already exists",
                    data = new ChatRoomResponseDto
                    {
                        Id = existingChat.Id,
                        ProductId = existingChat.ProductId,
                        ProductTitle = product.Title,
                        BuyerId = existingChat.BuyerId,
                        SellerId = existingChat.SellerId,
                        CreatedAt = existingChat.CreatedAt
                    }
                });
            }

            var chatRoom = new ChatRoom
            {
                Id = Guid.NewGuid(),
                ProductId = createChatRoomDto.ProductId,
                BuyerId = buyerId,
                SellerId = sellerId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ChatRooms.AddAsync(chatRoom);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(createChatRoomDto.InitialMessage))
            {
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    ChatRoomId = chatRoom.Id,
                    SenderId = buyerId,
                    Content = createChatRoomDto.InitialMessage,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetMessages), new { id = chatRoom.Id },
                new ApiResponse<ChatRoomResponseDto>
                {
                    success = true,
                    message = "Chat room created successfully",
                    data = new ChatRoomResponseDto
                    {
                        Id = chatRoom.Id,
                        ProductId = chatRoom.ProductId,
                        ProductTitle = product.Title,
                        BuyerId = chatRoom.BuyerId,
                        SellerId = chatRoom.SellerId,
                        CreatedAt = chatRoom.CreatedAt
                    }
                });
        }

        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteChatRoom(Guid id)
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

            var chatRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(c => c.Id == id && (c.BuyerId == userId || c.SellerId == userId));

            if (chatRoom == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Chat room not found or access denied"
                });
            }

            try
            {
                _context.ChatRooms.Remove(chatRoom);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
                {
                    success = false,
                    message = "An error occurred while deleting the chat room",
                    errors = new { exception = ex.Message }
                });
            }

            return NoContent();
        }
    }
}
