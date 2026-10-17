using API.DTO.Category;
using AutoMapper;
using BLL.Services.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;

        public CategoriesController(ICategoryService categoryService, IMapper mapper)
        {
            _categoryService = categoryService;
            _mapper = mapper;
        }

        [HttpGet("main")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMainCategories()
        {
            var categories = await _categoryService.GetMainCategoriesAsync();
            var dtos = _mapper.Map<IEnumerable<CategoryResponseDto>>(categories);
            return Ok(dtos);
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveCategories()
        {
            var categories = await _categoryService.GetActiveCategoriesAsync();
            var dtos = _mapper.Map<IEnumerable<CategoryResponseDto>>(categories);
            return Ok(dtos);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound(new { message = $"Category with id {id} not found." });
            }

            var dto = _mapper.Map<CategoryResponseDto>(category);
            return Ok(dto);
        }

        [HttpGet("{id:int}/subcategories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWithSubcategories(int id)
        {
            var category = await _categoryService.GetWithSubcategoriesAsync(id);
            if (category == null)
            {
                return NotFound(new { message = $"Category with id {id} not found." });
            }

            var dto = _mapper.Map<CategoryResponseDto>(category);
            return Ok(dto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            try
            {
                var category = _mapper.Map<Category>(dto);
                var createdCategory = await _categoryService.CreateCategoryAsync(category);
                var responseDto = _mapper.Map<CategoryResponseDto>(createdCategory);

                return CreatedAtAction(nameof(GetById), new { id = createdCategory.Id }, responseDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            try
            {
                var categoryToUpdate = _mapper.Map<Category>(dto);
                categoryToUpdate.Id = id;

                await _categoryService.UpdateCategoryAsync(categoryToUpdate);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                await _categoryService.ToggleActiveStatusAsync(id);
                return Ok(new { message = "Category status toggled successfully." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}