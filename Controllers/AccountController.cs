using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using System;
using webApi.Context;
using webApi.Identity;
using Entity;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using webApi.EmailServices;
using System.Net.Mail;
using Business.Abstract;
using Microsoft.AspNetCore.Authorization;

namespace webApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationContext context;
        private readonly IConfiguration configuration;
        private readonly SignInManager<User> signInManager;
        private readonly IUserDetailService userdetailService;
        private readonly UserManager<User> userManager;
        Response response = new Response();

        public AccountController(ApplicationContext Context, SignInManager<User> SignInManager, UserManager<User> UserManager, IConfiguration Configuration, IUserDetailService UserDetailService)
        {
            context = Context;
            signInManager = SignInManager;
            userManager = UserManager;
            configuration = Configuration;
            userdetailService = UserDetailService;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            var user = new User()
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Kvkk = model.Kvkk,
                CompanyName = TurkishToEnglish(model.CompanyName),
                CompanyAddress = model.CompanyAddress,
                LicanceKey = model.LicanceKey
            };

            if (userdetailService.CheckLicanceKey(model.LicanceKey))
            {
                var result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var tokenCode = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var url = Url.Action("ConfirmEmail", "Account", new
                    {
                        userId = user.Id,
                        token = tokenCode
                    });
                    MailMessage MyMail = new MailMessage()
                    {
                        Subject = "Hesap onayı",
                        Body = $"Lütfen email hesabınızı onaylamak için linke <a href='http://localhost:4000{url}'>tıklayınız.</a>"
                    };
                    EmailService.SendEmail(user.Email, MyMail);
                    return Ok();
                }

                if (result.Errors.First().Code == "DuplicateUserName")
                {
                    response.Header = "EMAIL_USING";
                }
                return BadRequest(response);
            }
            response.Header = "INVALID_LICANCE";
            return BadRequest(response);

        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                response.Header = "USER_NOT_FOUND";
                return BadRequest(response);
            }
            var result = await signInManager.PasswordSignInAsync(user, model.Password, false, false);
            if (result.Succeeded)
            {
                if (!await userManager.IsEmailConfirmedAsync(user))
                {
                    var tokenCode = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var url = Url.Action("ConfirmEmail", "Account", new
                    {
                        userId = user.Id,
                        token = tokenCode
                    });
                    MailMessage MyMail = new MailMessage()
                    {
                        Subject = "Hesap onayı",
                        Body = $"Lütfen email hesabınızı onaylamak için linke <a href='http://localhost:4000{url}'>tıklayınız.</a>"
                    };
                    EmailService.SendEmail(user.Email, MyMail);
                    response.Header = "EMAIL_NOT_CONFIRM";
                    return BadRequest(response);
                }
                // jwt
                var userRole = await userManager.GetRolesAsync(user);
                var jwtCode = await GenerateJWT(user.Id, user.Email, userRole[0]);
                var responseUser = new JwtUser()
                {
                    JwtCode = jwtCode,
                    UserRole = userRole[0]

                };
                return Ok(responseUser);
            }

            response.Header = "WRONG_PASSWORD";
            response.Message = "This password invalid.";
            return BadRequest(response);
        }


        public async Task<string> GenerateJWT(string userId, string email, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["Secret"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Role, role),

                }),
                Expires = DateTime.UtcNow.AddYears(3),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            if (context.UserTokens.Where(i => i.UserId == userId).FirstOrDefault() == null)
            {
                var userToken = new IdentityUserToken<String>()
                {
                    UserId = userId,
                    LoginProvider = "system api",
                    Name = email,
                    Value = tokenHandler.WriteToken(token)
                };
                await context.UserTokens.AddAsync(userToken);
                await context.SaveChangesAsync();
            }
            else
            {
                context.UserTokens.Where(i => i.UserId == userId).First().Value = tokenHandler.WriteToken(token);
                context.Update(context.UserTokens.Where(i => i.UserId == userId).First());
                context.SaveChanges();
            }
            return tokenHandler.WriteToken(token);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return BadRequest("user not found");
            }
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("user not found");
            }
            var result = await userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                var userRole = await userManager.GetRolesAsync(user);
                if (userRole.FirstOrDefault() == null)
                {
                    var userNewRole = new IdentityUserRole<string>()
                    {
                        UserId = user.Id,
                        RoleId = "1"
                    };
                    await context.UserRoles.AddAsync(userNewRole);
                    await context.SaveChangesAsync();
                }
                return Redirect("http://localhost:4200/");
            }
            return BadRequest();
        }


        [HttpPost]
        [Route("forgot-password")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                response.Header = "USER_NOT_FOUND";
                return BadRequest(response);
            }
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var url = $"/#/sifremi-sifirla?token={token}&userId={user.Id}";

            MailMessage MyMail = new MailMessage()
            {
                Subject = "Hesap parola sıfırlaması",
                Body = $"Parolanızı sıfırlamak için linke <a href='http://localhost:4200{url}'>tıklayınız.</a>"
            };
            EmailService.SendEmail(email, MyMail);
            return Ok();
        }


        [HttpPost]
        [Route("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            // buraya dönüş yap
            var user = await userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                response.Header = "USER_NOT_FOUND";
                return BadRequest(response);
            }


            var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                return Ok();
            }
            if (result.Errors.First().Code == "InvalidToken")
            {
                response.Header = "INVALID_TOKEN";
            }
            return BadRequest(response);

        }

        public string TurkishToEnglish(string companyName)
        {
            char[] turkishChars = { 'ı', 'ğ', 'İ', 'Ğ', 'ç', 'Ç', 'ş', 'Ş', 'ö', 'Ö', 'ü', 'Ü', ' ', 'â', 'û', 'î', 'ô' };
            char[] englishChars = { 'i', 'g', 'I', 'G', 'c', 'C', 's', 'S', 'o', 'O', 'u', 'U', '-', 'a', 'u', 'i', 'o' };

            // Match chars
            for (int i = 0; i < turkishChars.Length; i++)
                companyName = companyName.Replace(turkishChars[i], englishChars[i]);

            return companyName.ToLower();
        }

        


    }
}