using hospital.models;
using hospital.packages;
using Hospital.Services;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Ocsp;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Web;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace hospital.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("MyPolicy")]
    public class UserController : ControllerBase
    {
        private readonly userpackage _userpack;
        private readonly IConfiguration _configuration;
        private readonly EmailService _mailjet;


        public UserController(userpackage userpackage,IConfiguration configuration, EmailService mailjet)
        {
            _userpack = userpackage;
            _configuration = configuration;
            _mailjet = mailjet;

        }


    

   
    


    [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateEmployee([FromBody] registermodel model)
        {
            try
            {
                string code = GenerateRandomCode();
                await _userpack.CreateUser(model,code);
                var email = model.email;
                var confirmationLink = $"http://localhost:4200/api/VerifyEmail/{HttpUtility.UrlEncode(model.email)}";
                var emailSendDto = new EmailConfiguration
                {

                    Subject = "Confirm your email",
                    Body = $"<p>Please confirm your email by clicking the following link: <a href=\"{confirmationLink}\">{confirmationLink}</a></p>",
                    TextPart = "Please confirm your email by clicking the link below.",
                    To = email,
                    Name = "Recipient Name"
                };

                bool emailSent = await _mailjet.SendEmailAsync(emailSendDto);

                if (emailSent)
                {
                    return Ok(new { message = "user created successfully" });
                }
                else
                {
                    return StatusCode(500, "Failed to send email.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating user: {ex.Message}");
            }
        }

        private string GenerateNewJsonWebToken(List<Claim> claims)
        {
            var authSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var tokenObject = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.MaxValue,
                claims: claims,
                signingCredentials: new SigningCredentials(authSecret, SecurityAlgorithms.HmacSha256)
            );
            string token = new JwtSecurityTokenHandler().WriteToken(tokenObject);
            return token;
        }

        private string GenerateRandomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            var code = new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return code;
        }

        [HttpPost("LoginUser")]
        public async Task<IActionResult> LoginUser([FromBody] loginmodel model)
        {
            try
            {
                registermodel user = await _userpack.LoginUser(model);
                if (user== null)
                {
                    return BadRequest(new { meesage = "user does not exist" });

                }
               else if (user.emailconfirmed == 0)
                {
                    return BadRequest(new { message = "please verify email" });
                }
                else if (user.twofactorenabled == 1)
                {
                    string randomCode = GenerateRandomCode();
                    DateTime expirationTime = DateTime.Now.AddMinutes(5);
                    var request = new EmailConfiguration()
                    {
                        Subject = "2-step verification code",
                        Body = randomCode,
                        TextPart = "",
                        To = model.email,
                        Name = "Recipient Name"
                    };
                    bool res= await _mailjet.SendEmailAsync(request);
                    bool resp = await _userpack.SaveCode(user.id, randomCode);
                   
                    if (resp)
                    {
                        return Ok(new { Email = model.email, Time = expirationTime, Code = randomCode });
                    }
                    else
                    {
                        return BadRequest("error");
                    }
               
                }
                else
                {
                    var authClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.email),
                        new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                        new Claim("JWTID", Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "user")
                    };
                    var token = GenerateNewJsonWebToken(authClaims);
                    return Ok(new { message = user, token });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating user: {ex.Message}");
            }
        }

        [HttpPut("UpdateUser/{id}")]
        public IActionResult UpdateEmployee([FromBody] registermodel model, int id)
        {
            _userpack.UpdateUser(model,id);
            return Ok(new { message = "created successfully" });
        }

        [HttpDelete("DeleteUser/{id}")]
        public IActionResult DeleteEmployee(int id)
        {
            try
            {
                _userpack.DeleteUser(id);
                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating user: {ex.Message}");
            }
        }


        [HttpGet("GetAllUser")]
        public IActionResult GetUsers()
        {
            try
            {
                var users = _userpack.GetUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving users: {ex.Message}");
            }
        }

        [HttpGet("GetUser/{id}")]
        public IActionResult GetUser(int id)
        {
            try
            {
                var users = _userpack.GetUser(id);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving users: {ex.Message}");
            }
        }


        [HttpGet("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail(string email, string randomcode)
        {
            registermodel res=await _userpack.VerifyEmail(email,randomcode);

            if (res != null)
            {
                return Ok(res);

            }
            else
            {
                return BadRequest(new { message = "wrong code" });
            }
        }

        [HttpGet("Two-factor-auth/{id}")]
        public async Task<IActionResult> Twofactor(int id)
        {
            authmodel res = await _userpack.TwoFactorEnabled(id);

            if (res.twofactorenabled==1)
            {
                //return Ok(new {Message="two step auth is on"});
                return Ok(res);

            }
            else if (res.twofactorenabled == 0)
            {
                //return Ok(new { Message = "two step auth is on" });
                return Ok(res);
            }
            else
            {
                return BadRequest(new { message = "two fac not enabled" });
            }
        }

        [HttpPost("sendcodetoemail/{id}")]
        public async Task<IActionResult> SaveCode(int id, [FromBody] emailmodel model)
        {
            string randomCode = GenerateRandomCode();
            var request = new EmailConfiguration()
            {
                Subject ="Email change verification code",
                Body = randomCode,
                TextPart = "",
                To = model.email,
                Name = "Recipient Name"
            };
            await _mailjet.SendEmailAsync(request);
           bool res= await _userpack.SendEmailCode(id, randomCode);

            if (res)
            {
                //return Ok(new {Message="two step auth is on"});
                return Ok(new { randomeCode = randomCode });

            }

            else
            {
                return BadRequest(new { message = "two fac not enabled" });
            }
        }


        [HttpPost("confirmemailcode/{id}")]
        public async Task<IActionResult> ConfirmEmailCode( int id, string code)
        {
            codemodel res = await _userpack.ConfirmEmailCode(id, code);

            if (res.randomcode==null)
            {
                return Ok(new { message = "enter new email",res });
            }

            else
            {
                return BadRequest(new { message = "incorrect code",res });
            }
        }
    }
}
