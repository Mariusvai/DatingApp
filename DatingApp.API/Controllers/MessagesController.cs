using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/users/{userId}/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public MessagesController(IDatingRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;

        }

        [HttpGet("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if (userId !=int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();

            var mesasgeFromRepo = await _repo.GetMessage(id);

            if (mesasgeFromRepo == null)
                return NotFound();

            return Ok(mesasgeFromRepo);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, 
            [FromQuery]MessageParams messageParams)
        {
            if (userId !=int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();

            messageParams.UserId = userId;

            var messagesFromRepo = await _repo.GetMessagesForUser(messageParams);

            var mesasges = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            Response.AddPagination(messagesFromRepo.CurrentPage, 
                messagesFromRepo.PageSize, messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

            return Ok(mesasges);
        }

        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            if (userId !=int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();

            var messasgeFromRepo = await _repo.GetMessageThread(userId, recipientId);

            var mesasgeThread = _mapper.Map<IEnumerable<MessageToReturnDto>>(messasgeFromRepo);

            return Ok(mesasgeThread);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto)
        {
            var sender = await _repo.GetUser(userId);

            if (sender.Id !=int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();

            messageForCreationDto.SenderId = userId;

            var recipent = await _repo.GetUser(messageForCreationDto.RecipientId);

            if (recipent == null)
                return BadRequest("Could not find user");

            var mesasge = _mapper.Map<Message>(messageForCreationDto);

            _repo.Add(mesasge);

            

            if (await _repo.SaveAll()) {
                var mesasgeToReturn = _mapper.Map<MessageToReturnDto>(mesasge);
                return CreatedAtRoute("GetMessage", new {id = mesasge.Id}, mesasgeToReturn);
            }
                

            throw new Exception("Creating the message failed on save");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId)
        {
            if (userId !=int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();

            var messagesFromRepo = await _repo.GetMessage(id);

            if (messagesFromRepo.SenderId == userId)
                messagesFromRepo.SenderDeleted = true;

            if (messagesFromRepo.RecipientId == userId)
                messagesFromRepo.RecipientDeleted = true;

            if (messagesFromRepo.SenderDeleted && messagesFromRepo.RecipientDeleted)
                _repo.Delete(messagesFromRepo);

            if (await _repo.SaveAll())
                return NoContent();

            throw new Exception("Error deleting the message");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarMessageAsRead(int userId, int id)
        {
            if (userId !=int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();

            var mesasge = await _repo.GetMessage(id);

            if (mesasge.RecipientId != userId)
                return Unauthorized();

            mesasge.IsRead = true;
            mesasge.DateRead = DateTime.Now;

            await _repo.SaveAll();

            return NoContent();
        }
    }
}