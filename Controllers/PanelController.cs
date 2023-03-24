using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Business.Abstract;
using Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using webApi.Context;
using webApi.Identity;

namespace webApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PanelController : ControllerBase
    {
        private readonly ApplicationContext context;
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private IProductService productService;
        private ICategoryService categoryService;
        Response response = new Response();

        public PanelController(SignInManager<User> SignInManager, UserManager<User> UserManager, IProductService ProductService, ICategoryService CategoryService, ApplicationContext Context)
        {
            signInManager = SignInManager;
            userManager = UserManager;
            productService = ProductService;
            categoryService = CategoryService;
            context = Context;
        }


        [HttpPost]
        [Route("add-category")]
        public async Task<IActionResult> AddCategoryAsync([FromForm] IFormFile categoryImage, [FromForm] Category model)
        {
            var categoryImageNameDatabase = "";
            if (categoryImage != null)
            {
                categoryImageNameDatabase = await AddImageAsync(categoryImage, "userCategoryImages");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = new Category
            {
                UserId = userId,
                CategoryName = model.CategoryName,
                CategoryImageUrl = categoryImageNameDatabase

            };
            await categoryService.TAddAsync(category);
            return Ok();
        }

        [HttpPost]
        [Route("add-product")]
        public async Task<IActionResult> AddProductAsync([FromForm] IFormFile productImage, [FromForm] Product model)
        {
            var productImageNameDatabase = "";
            if (productImage != null)
            {
                productImageNameDatabase = await AddImageAsync(productImage, "userProductImages");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await categoryService.TGetByIdAsync(model.CategoryId);
            if (category != null)
            {
                model.UserId = userId;
                model.ProductImageUrl = productImageNameDatabase;
                await productService.TAddAsync(model);
                return Ok();
            }
            response.Header = "CATEGORY_NOT_FOUND";
            return BadRequest(response);
        }


        [HttpPut]
        [Route("update-category")]

        public async Task<IActionResult> UpdateCategoryAsync([FromForm] Category model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await categoryService.TGetByIdAsync(model.CategoryId);
            if (category != null && category.UserId == userId)
            {
                category.CategoryName = model.CategoryName;
                await categoryService.TUpdateAsync(category);
                return Ok();
            }
            response.Header = "Error";                        
            return BadRequest(response);
        }

        [HttpPut]
        [Route("update-product")]

        public async Task<IActionResult> UpdateProductAsync([FromBody] Product model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = await productService.TGetByIdAsync(model.ProductId);
            if (product != null && product.UserId == userId)
            {
                product.ProductName = model.ProductName;
                await productService.TUpdateAsync(product);
                return Ok();
            }
            return BadRequest();
        }

        [HttpDelete]
        [Route("delete-category")]
        public async Task<IActionResult> DeleteCategoryAsync([FromBody] int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await categoryService.TGetByIdAsync(id);
            if (category != null && category.UserId == userId)
            {
                await categoryService.TDeleteAsync(category);
                return Ok();
            }
            return BadRequest();
        }

        [HttpDelete]
        [Route("delete-product")]
        public async Task<IActionResult> DeleteProductAsync([FromBody] int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = await productService.TGetByIdAsync(id);
            if (product != null && product.UserId == userId)
            {
                await productService.TDeleteAsync(product);
                return Ok();
            }
            return BadRequest();
        }

        [HttpGet]
        [Route("get-categories-count")]
        public async Task<IActionResult> GetTotalCategory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                response.Header = "USER_NOT_FOUND";
                return BadRequest(response);
            }
            var count = await categoryService.GetCompanyCategoryCountAsync(userId);

            return Ok(count);
        }

        [HttpGet]
        [Route("get-products-count")]
        public async Task<IActionResult> GetTotalProduct()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                response.Header = "USER_NOT_FOUND";
                return BadRequest(response);
            }
            var count = await productService.GetCompanyProductCountAsync(userId);

            return Ok(count);
        }



        [HttpGet]
        [Route("get-categories")]
        public async Task<IActionResult> GetCategories()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                response.Header = "USER_NOT_FOUND";
                return BadRequest(response);
            }
            var categories = await categoryService.GetCompanyAllCategoriesAsync(userId);
            return Ok(categories);

        }

        [HttpGet]
        [Route("get-products")]
        public async Task<IActionResult> GetProducts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                response.Header = "USER_NOT_FOUND";
                return BadRequest(response);
            }
            var categories = await productService.GetCompanyProductsAsync(userId);
            return Ok(categories);

        }

        [HttpGet]
        [Route("get-category")]
        public async Task<IActionResult> GetCategory(int categoryId)
        {            
            var category = await categoryService.TGetByIdAsync(categoryId);
            return Ok(category);
        }

        [HttpGet]
        [Route("get-product")]
        public async Task<IActionResult> GetProduct(int productId)
        {            
            var product = await productService.TGetByIdAsync(productId);
            return Ok(product);
        }




        public async Task<string> AddImageAsync(IFormFile Image, string Adress)
        {
            var extention = Path.GetExtension(Image.FileName);
            var imageName = string.Format($"{Guid.NewGuid()}{extention}");
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/" + Adress, imageName);
            using (var stream = new FileStream(path, FileMode.CreateNew))
            {
                await Image.CopyToAsync(stream);
            }
            return imageName;
        }


    }
}