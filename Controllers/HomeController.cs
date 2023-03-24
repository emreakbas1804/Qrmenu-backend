using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class HomeController : ControllerBase
    {
        private readonly ApplicationContext context;
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private IProductService productService;
        private ICategoryService categoryService;
        Response errorList = new Response();


        public HomeController(SignInManager<User> SignInManager, UserManager<User> UserManager, IProductService ProductService, ICategoryService CategoryService, ApplicationContext Context)
        {
            signInManager = SignInManager;
            userManager = UserManager;
            productService = ProductService;
            categoryService = CategoryService;
            context = Context;

        }


        [HttpGet]
        [Route("company-all-products")]
        public async Task<IActionResult> CompanyAllProductsAsync(string companyName)
        {

            var user = context.Users.Where(i => i.CompanyName == companyName).FirstOrDefault();
            if (user != null)
            {
                var userId = user.Id;
                var products = await productService.GetCompanyProductsAsync(userId);
                return Ok(products);
            }

            var error = new Response()
            {
                Header = "COMPANY_NOT_FOUND",
                Message = "This company not in our database"
            };

            return BadRequest(error);

        }


        [HttpGet]
        [Route("company-all-categories")]
        public async Task<IActionResult> CompanyAllCategoriesAsync(string companyName)
        {
            var user = context.Users.Where(i => i.CompanyName == companyName).FirstOrDefault();
            if (user != null)
            {
                var userId = user.Id;
                var categories = await categoryService.GetCompanyAllCategoriesAsync(userId);
                
                return Ok(categories);                
            }


            return BadRequest();
        }


        [HttpGet]
        [Route("company-category-products")]
        public async Task<IActionResult> CompanyCategoryProductsAsync(string companyName, string companyCategory)
        {
            var user = context.Users.Where(i => i.CompanyName == companyName).FirstOrDefault();
            if (user != null)
            {
                var userId = user.Id;
                var categories = await categoryService.GetCompanyAllCategoriesAsync(userId);
                foreach (var item in categories)
                {
                    if (item.CategoryName == companyCategory)
                    {
                        var categoryId = item.CategoryId;
                        var products = await productService.GetCompanyCategoryProductsAsync(categoryId);
                        return Ok(products);
                    }
                }


                return BadRequest();
            }


            return BadRequest();
        }

        [HttpGet]
        [Route("produc-detail")]
        public async Task<IActionResult> ProductDetailAsync(string companyName, int productId)
        {
            var user = context.Users.Where(i => i.CompanyName == companyName).FirstOrDefault();
            if (user != null)
            {
                var userId = user.Id;
                var companyProducts = await productService.GetCompanyProductsAsync(userId);
                foreach (var item in companyProducts)
                {
                    if (item.ProductId == productId)
                    {
                        var product = await productService.GetProductDetailAsync(productId);
                        return Ok(product);
                    }
                }


                return BadRequest();
            }


            return BadRequest();
        }

        [Authorize]     
        [HttpPost]
        [Route("add-image")]           
        public async Task<IActionResult> addImageAsync([FromForm] IFormFile image, [FromForm] Category model)
        {
            
            if (image != null)
            {
                var extention = Path.GetExtension(image.FileName);
                var LogoName = string.Format($"{Guid.NewGuid()}{extention}");
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/userCategoryImages", LogoName);
                using (var stream = new FileStream(path, FileMode.CreateNew))
                {
                    await image.CopyToAsync(stream);
                }                
            }
            return Ok();
        }

        
    }
}