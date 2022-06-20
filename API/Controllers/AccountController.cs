using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOS;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext Context;
        private readonly ITokenService Tokenservice;
        public IMapper Mapper { get; }
        public AccountController(DataContext context, ITokenService tokenservice, IMapper mapper)
        {
            this.Mapper = mapper;
            Tokenservice = tokenservice;
            Context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto){
           
           if(await UsernameTaken(registerDto.Username)) return BadRequest("Username is taken");

           var user = Mapper.Map<AppUser>(registerDto);
           
            using var hmac = new HMACSHA512();


                user.UserName = registerDto.Username.ToLower();
                user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
                user.PasswordSalt = hmac.Key;


            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            return new UserDto{
                Username = user.UserName,
                Token = Tokenservice.CreateToken(user),
                KnownAs = user.KnownAs
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto){
            var user = await Context.Users
                .Include(p=> p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == loginDto.Username);

            if (user == null) return Unauthorized("Invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }
            
            return new UserDto
            {
                Username = user.UserName,
                Token = Tokenservice.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs 
            };
        }

        private async Task<bool> UsernameTaken(string username){
            return await Context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}