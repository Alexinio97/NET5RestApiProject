using Catalog.Dtos;
using Catalog.Entities;
using Catalog.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catalog.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : Controller
    {
        private readonly IItemsRepository _itemsRepo;

        public ItemsController(IItemsRepository itemsRepo)
        {
            _itemsRepo = itemsRepo;
        }

        // GET: /items
        [HttpGet]
        public IEnumerable<ItemDto> GetItems()
        {
            return _itemsRepo.GetItems().Select(item => item.AsDto());
        }

        // GET /items/{id}
        [HttpGet("{id}")]
        public ActionResult<ItemDto> GetItem(Guid id)
        {
            var item = _itemsRepo.GetItem(id).AsDto();

            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        // POST /items
        [HttpPost]
        public ActionResult<ItemDto> CreateItem(CreateItemDto itemDto)
        {
            Item item = new()
            {
                Id = Guid.NewGuid(),
                Name = itemDto.Name,
                Price = itemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };

            _itemsRepo.CreateItem(item);

            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item.AsDto());
        }

        // PUT /items/
        [HttpPut("{id}")]
        public ActionResult UpdateItem(Guid id, UpdateItemDto itemDto)
        {
            var existingItem = _itemsRepo.GetItem(id);

            if (existingItem is null)
            {
                return NotFound();
            }

            Item updatedItem = existingItem with
            {
                Name = itemDto.Name,
                Price = itemDto.Price
            };
            _itemsRepo.UpdateItem(updatedItem);

            return NoContent();
        }

        // DELETE /items
        [HttpDelete("{id}")]
        public ActionResult DeleteItem(Guid id)
        {
            var existingItem = _itemsRepo.GetItem(id);

            if (existingItem is null)
            {
                return NotFound();
            }

            _itemsRepo.DeleteItem(id);

            return NoContent();
        }
    }
}
