using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

using AccountService.Data;
using AccountService.Models;

namespace AccountService.Utility
{
    public class UserService
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly MessageBroker _messagebroker;


        public UserService(AppDbContext dbContext, IConfiguration configuration, MessageBroker messageBroker)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _messagebroker = messageBroker;
        }

        public async Task<bool> Register(User user){

                user.createdat = DateTime.UtcNow;
                user.passwordb = Hash(user.passwordb);
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();

                await _messagebroker.PublishEvent(
                "user.events",
                "user.created",
                user.userid.ToString()
                );

                return true;
        }

        public string? Login(LoginModel logindata)
        {
            try
            {

                var hashedPassword = Hash(logindata.password);

                var user = _dbContext.Users.FirstOrDefault(u => u.username ==logindata.username && u.passwordb == hashedPassword);

                if (user != null){
                    Console.WriteLine(user.ToString());
                    return GenerateJwtToken(user);

                }else{
                    return null;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public User GetUser(int uid)
        {
            return _dbContext.Users.FirstOrDefault( u=> u.userid == uid)!;
        }

        public async Task<bool> UpdateUser(int uid, User newData)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.userid == uid);

            if(user == null)
            {
                return false;
            }

            user.fullname = newData.fullname;
            user.passwordb = Hash(newData.passwordb);
            user.username = newData.username;

            _dbContext.SaveChanges();

            await _messagebroker.PublishEvent(
            "user.events",
            "user.updated",
             $"User {newData.userid} Updated.");

            return true;
        }



        public string Hash(String plaintext){
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(plaintext);

                var hashBytes = sha256.ComputeHash(bytes);
   
                return Convert.ToBase64String(hashBytes);
            }
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
            );

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.username),
                new Claim(ClaimTypes.NameIdentifier, user.userid.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(jwtSettings["DurationInMinutes"])
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



    }
}