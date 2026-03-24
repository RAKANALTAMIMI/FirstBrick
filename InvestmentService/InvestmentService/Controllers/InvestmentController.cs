using Microsoft.AspNetCore.Mvc;

using InvestmentService.Models;
using InvestmentService.Utility;
using System.ComponentModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace InvestmentService.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(DbExceptionFilter))]
    [ApiController]
    [Route("v1")]

    public class InvestmentController : ControllerBase
    {
        private readonly InvestmentServicex _investmentService;

        public InvestmentController(InvestmentServicex investmentService)
        {
            _investmentService = investmentService;
        }

        [HttpPost("project")]
        public async Task<IActionResult> RegistreProject([FromBody] Project newProject){

            if (!ModelState.IsValid){
            return BadRequest(ModelState);
            }

            var response = await _investmentService.RegistreProject(newProject);

            if (response.Success){
                return Ok(response);
            }

            else{

                return BadRequest(response);
            }
        }

        [HttpGet("projects")]
        public async Task<IActionResult> GetAll(){

            var response = await _investmentService.GetAll();

            if (response.Success){
                return Ok(response);
            }
            else{

                return NotFound(response);
            }
        }

        [HttpPost("invest")]
        public async Task<IActionResult> Invest([FromBody] Investment newInvestment){

            if (!ModelState.IsValid){
            return BadRequest(ModelState);
            }

            var response = await _investmentService.Invest(newInvestment);

            if (response.Success)
            {
                return Ok(response);
            }
            else{
                return BadRequest(response);
            }
        }
        
        
        [HttpGet("portfolio")]
        public async Task<IActionResult> GetPortfolio()
        {
            var userid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? throw new UnauthorizedAccessException("User ID not found in token"));

            var response = await _investmentService.GetPortfolio(userid);

            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return NotFound(response);
            }
        }

        [HttpGet("portfolio/project/{projectid}")]
        public async Task<IActionResult> GetPortfolioProject(int projectid){
            
            var userid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? throw new UnauthorizedAccessException("User ID not found in token"));

            var response = await _investmentService.GetPortfolioProject(userid, projectid);

            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return NotFound(response);
            }
        }



    }
}