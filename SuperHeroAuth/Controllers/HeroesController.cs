using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperHeroAuth.Data;
using SuperHeroAuth.Models;

namespace SuperHeroAuth.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("/api/[controller]")]
    [ApiController]
    public class HeroesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public HeroesController(AppDbContext context) 
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<SuperHero>>> GetHeroes()
        {
            Console.WriteLine(HttpContext.Request.Headers.Authorization);
            var heroes = await _context.Heroes.ToListAsync();
            if(heroes != null)
                return Ok(heroes);

            return BadRequest("Error Fetching data");
        }


        [HttpGet("{heroId}")]
        public async Task<ActionResult<SuperHero>> GetHeroById(int heroId) 
        {
            var hero = await _context.Heroes.FirstOrDefaultAsync(x => x.Id == heroId);
            if (hero != null)
                return Ok(hero);

            return BadRequest("Hero not found");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<SuperHero[]>> CreateHero([FromBody] SuperHero heroRequest)
        {
            if(ModelState.IsValid)
            {
                await _context.Heroes.AddAsync(heroRequest);
                await _context.SaveChangesAsync();
                var heroes = await _context.Heroes.ToListAsync();
                return Ok(heroes);
            }
            return BadRequest("Invalid fields");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<ActionResult<SuperHero[]>> UpdateHero([FromBody] SuperHero heroRequest)
        {
            if (ModelState.IsValid)
            {
                var hero = await _context.Heroes.FindAsync(heroRequest.Id);
                if (hero != null)
                {
                    hero.Name = heroRequest.Name;
                    hero.FirstName = heroRequest.FirstName;
                    hero.LastName = heroRequest.LastName;
                    hero.Place = heroRequest.Place;

                    await _context.SaveChangesAsync();

                    return Ok(await _context.Heroes.ToListAsync());
                }
            }
            return BadRequest("Invalid fields");
        }
        
        [Authorize(Roles = "Admin")]
        [HttpDelete("{heroId}")]
        public async Task<ActionResult<SuperHero[]>> DeleteHero(int heroId)
        {
            var heroToDelete = await _context.Heroes.FirstOrDefaultAsync(x => x.Id == heroId);
            if(heroToDelete != null)
            {
                _context.Heroes.Remove(heroToDelete);
                await _context.SaveChangesAsync();
                var heroes = await _context.Heroes.ToListAsync();
                return Ok(heroes);
            }
            return BadRequest("Server Error");
        }
    }
}




        //private string createSqlQuery(string? name, string? place, string? firstName, string? lastName)
        //{
        //    var SqlQuery = $"SELECT * FROM HeroesDb WHERE 1 = 1";
        //    if (name != null)
        //    {
        //        SqlQuery = $"{SqlQuery} AND Name = {name}";
        //    }
        //    if (place != null)
        //    {
        //        SqlQuery = $"{SqlQuery} AND Place = {place}";
        //    }
        //    if (firstName != null)
        //    {
        //        SqlQuery = $"{SqlQuery} AND FirstName = {firstName}";
        //    }
        //    if (lastName != null)
        //    {
        //        SqlQuery = $"{SqlQuery} AND LastName = {lastName}";
        //    }

        //    SqlQuery = $"{SqlQuery};";
        //    return SqlQuery;
        //}
