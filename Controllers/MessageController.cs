﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;
using System;

namespace olx_be_api.Controllers
{
    [Route("api/message")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;
        public MessageController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<MessageResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult GetAllMessages()
        {
            var messages = _context.Messages.ToList();
            if (messages.Count == 0)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "No messages found."
                });
            }
            return Ok(new ApiResponse<List<MessageResponseDto>>
            {
                success = true,
                message = "Successfully retrieved messages.",
                data = messages.Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    ChatRoomId = m.ChatRoomId,
                    IsRead = m.IsRead,
                    CreatedAt = m.CreatedAt
                }).ToList()
            });
        }

        [HttpGet("chatroom/{chatRoomId}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<MessageResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMessagesByChatRoom(Guid chatRoomId)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    success = false,
                    message = "User authentication required"
                });
            }

            var chatRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(c => c.Id == chatRoomId && (c.BuyerId == userId || c.SellerId == userId));

            if (chatRoom == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    success = false,
                    message = "Chat room not found or you don't have access to it"
                });
            }

            var messages = await _context.Messages
                .Where(m => m.ChatRoomId == chatRoomId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            if (messages.Count == 0)
            {
                return Ok(new ApiResponse<List<MessageResponseDto>>
                {
                    success = true,
                    message = "No messages in this chat room yet.",
                    data = new List<MessageResponseDto>()
                });
            }

            var unreadMessages = messages.Where(m => !m.IsRead && m.SenderId != userId).ToList();
            if (unreadMessages.Any())
            {
                unreadMessages.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return Ok(new ApiResponse<List<MessageResponseDto>>
            {
                success = true,
                message = "Successfully retrieved chat room messages.",
                data = messages.Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    ChatRoomId = m.ChatRoomId,
                    IsRead = m.IsRead,
                    CreatedAt = m.CreatedAt
                }).ToList()
            });
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<MessageResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto createMessageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    success = false,
                    message = "Invalid message data.",
                    errors = ModelState
                });
            }

            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    success = false,
                    message = "User authentication required"
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
                    message = "Chat room not found or you don't have access to it"
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
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMessagesByChatRoom), new { chatRoomId = message.ChatRoomId },
                new ApiResponse<MessageResponseDto>
                {
                    success = true,
                    message = "Message sent successfully.",
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

        [HttpGet("user/chats")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<ChatRoomResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserChatRooms()
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    success = false,
                    message = "User authentication required"
                });
            }

            var chatRooms = await _context.ChatRooms
                .Include(c => c.Product)
                .Include(c => c.Buyer)
                .Include(c => c.Seller)
                .Where(c => c.BuyerId == userId || c.SellerId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            if (chatRooms.Count == 0)
            {
                return Ok(new ApiResponse<List<ChatRoomResponseDto>>
                {
                    success = true,
                    message = "You don't have any active chats.",
                    data = new List<ChatRoomResponseDto>()
                });
            }

            var chatRoomIds = chatRooms.Select(c => c.Id).ToList();
            var messagesQuery = _context.Messages
                .Where(m => chatRoomIds.Contains(m.ChatRoomId))
                .GroupBy(m => m.ChatRoomId);

            var lastMessages = await messagesQuery
                .Select(g => g.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
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
                message = "Successfully retrieved user chat rooms.",
                data = response
            });
        }

        [HttpPost("chatroom")]
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
                    message = "Invalid chat room data.",
                    errors = ModelState
                });
            }

            var buyerId = User.GetUserId();
            if (buyerId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    success = false,
                    message = "User authentication required"
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
                    message = "You cannot start a chat for your own product"
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
                    message = "Chat room already exists.",
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

            return CreatedAtAction(nameof(GetMessagesByChatRoom), new { chatRoomId = chatRoom.Id },
                new ApiResponse<ChatRoomResponseDto>
                {
                    success = true,
                    message = "Chat room created successfully.",
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

        [HttpPatch("read/{messageId}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<MessageResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkMessageAsRead(Guid messageId)
        {
            var userId = User.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiErrorResponse
                {
                    success = false,
                    message = "User authentication required"
                });
            }

            var message = await _context.Messages
                .Include(m => m.ChatRoom)
                .FirstOrDefaultAsync(m => m.Id == messageId);

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
                return Forbid();
            }

            if (message.SenderId != userId)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new ApiResponse<MessageResponseDto>
            {
                success = true,
                message = "Message marked as read.",
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