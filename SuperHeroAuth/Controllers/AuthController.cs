using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SuperHeroAuth.Data;
using SuperHeroAuth.Models;
using SuperHeroAuth.Models.DTOs;
using SuperHeroAuth.Services;

namespace SuperHeroAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IdentityService _identityService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppDbContext _context;

        public AuthController(
            UserManager<IdentityUser> userManager,
            AppDbContext context,
            IdentityService identityService
            ) 
        {
            _identityService = identityService;
            _userManager = userManager;
            _context = context;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterRequestDTO registerRequest)
        {
            if (ModelState.IsValid)
            {
                var user_exists = await _userManager.FindByEmailAsync(registerRequest.Email);

                if(user_exists != null)
                    return BadRequest(new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Error in user exist request!"
                        }
                    });

                var creating_user = new IdentityUser()
                {
                    Email = registerRequest.Email,
                    UserName = registerRequest.UserName,
                };

                var is_user_created = await _userManager.CreateAsync(creating_user, registerRequest.Password);

                if(is_user_created.Succeeded)
                {
                    var creating_role = new UserRole()
                    {
                        Role = registerRequest.IsAdmin ? "Admin" : "User",
                        UserId = creating_user.Id,
                    };
                    await _context.UserRoles.AddAsync(creating_role);

                    var token = await _identityService.GenerateJwtToken(creating_user);

                    return Ok(token);
                }

                return BadRequest(new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                        {
                            "Error in creating request!"
                        }
                });
            }

            return BadRequest(new AuthResult()
            {
                Result = false,
                Errors = new List<string>()
                {
                    "Error in validating request!"
                }
            });

        }

        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequestDTO loginRequest)
        {
            if(ModelState.IsValid)
            {
                var existing_user = await _userManager.FindByEmailAsync(loginRequest.Email);
                if(existing_user == null)
                    return BadRequest(new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Error in login request!"
                        }
                    });

                var is_password_matching = await _userManager.CheckPasswordAsync(existing_user, loginRequest.Password);
                if (!is_password_matching)
                    return BadRequest(new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Error in login request!"
                        }
                    });

                var token = await _identityService.GenerateJwtToken(existing_user);

                return Ok(token);


            }

            return BadRequest(new AuthResult()
            {
                Result = false,
                Errors = new List<string>()
                {
                    "Error in login request!"
                }
            });

        }

        [HttpPost]
        [Route("RefreshToken")]
        public async Task<ActionResult<AuthResult>> RefreshToken([FromBody] TokenRequestDTO tokenRequest)
        {
            if (ModelState.IsValid)
            {
                var result = await _identityService.VerifyAndGenerateToken(tokenRequest);

                if(result == null)
                {
                    return BadRequest(new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Parameters"
                        }
                     });
                }

                return Ok(result);
            }
            return BadRequest(new AuthResult()
            {
                Result = false,
                Errors = new List<string>()
                {
                    "Invalid Parameters"
                }
            });
        }
    }
}

        //[HttpGet]
        //public async Task<ActionResult<List<User>>> GetUsers()
        //{
        //    var users = await _context.Users.ToListAsync();
        //    return Ok(users);
        //}

        //[HttpGet("{id}")]
        //public async Task<ActionResult<User>> GetUser(int id)
        //{
        //    var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        //    if(user == null)
        //    {
        //        return BadRequest("Error fetching User");
        //    }
        //    return Ok(user);
        //}

        //[HttpPost]
        //public async Task<ActionResult<int>> CreateUser(User user)
        //{
        //    await _context.Users.AddAsync(user);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetUser", new { user.Id }, user.Id);
        //}

        // patch and delete method return NoContent()
        // return a 204

