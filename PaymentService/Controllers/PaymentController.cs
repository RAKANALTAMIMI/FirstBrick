using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using PaymentService.Models;
using PaymentService.Utility;

namespace PaymentService.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(DbExceptionFilter))]
    [ApiController]
    [Route("v1")]

    public class PaymentController : ControllerBase
    {
        private readonly PaymentServicex _PaymentService;

        public PaymentController(PaymentServicex PaymentService)
        {
            _PaymentService = PaymentService;
        }

        [HttpPost("ApplepayTopup/{amount}")]
        public async Task<IActionResult> TopupWallet(decimal amount){
            
            if (amount <= 0)
                return BadRequest("Amount must be greater than zero");

            var userid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? throw new UnauthorizedAccessException("User ID not found in token"));


            await _PaymentService.AddFundsToWallet(userid, amount);
            await _PaymentService.CreateTSX(userid, TransactionType.Topup.ToString(), amount);

            return Ok(new { message = "Topup successful", userid, amount });
        }

        [HttpGet("balance/{userId}")]
        public IActionResult GetBalance(int userId)
        {
            var balance = _PaymentService.GetWalletBalance(userId);
            return Ok(new { userId, balance });
        }

        [HttpGet("transactions/{page}")]
        public IActionResult GetTransactions(int page = 1, int pageSize = 10)
        {
            var userid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token"));

            var transactions = _PaymentService.GetUserTransactions(userid, page, pageSize);
            return Ok(transactions);
        }

    }
}