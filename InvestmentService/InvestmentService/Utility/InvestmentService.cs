using Microsoft.EntityFrameworkCore;
using System.Text.Json;


using InvestmentService.Models;
using InvestmentService.Data;

namespace InvestmentService.Utility{

    public class InvestmentServicex{

        private readonly AppDbContext _dbcontext;
        private readonly MessageBroker _messageBroker;

        public InvestmentServicex(AppDbContext dbcontext, MessageBroker msgbroker){
            _dbcontext = dbcontext;
            _messageBroker = msgbroker;
        }

        public async Task<ResponseDto<Project>> RegistreProject(Project newProject){


            await _dbcontext.Projects.AddAsync(newProject);
            _dbcontext.SaveChanges();

            return ResponseDto<Project>.SuccessResponse(newProject, $"Project {newProject.title} registered successfully");
        

        }





        public async Task<ResponseDto<List<Project>>> GetAll()
        {
            var projects = await _dbcontext.Projects.ToListAsync();

            if (projects == null || projects.Count == 0){
                return ResponseDto<List<Project>>.FailureResponse("No projects found");

            }else{

                return ResponseDto<List<Project>>.SuccessResponse(projects, "Projects found");
            }
        }




        public async Task<ResponseDto<Investment>> Invest(Investment newInvestment){

            if (newInvestment.amount <= 0){
                return ResponseDto<Investment>.FailureResponse("Amount Can't be less that 1");
            }

            var project = await _dbcontext.Projects.FirstOrDefaultAsync(p => p.projectid == newInvestment.projectid);

            if (project == null)
                return ResponseDto<Investment>.FailureResponse($"Project {newInvestment.projectid} for this investment couldn't be found");


            await _dbcontext.Investments.AddAsync(newInvestment);
            await _dbcontext.SaveChangesAsync();

            var investmentEvent = new InvestmentCreatedEvent{
                investmentid = newInvestment.investmentid,
                userid = newInvestment.userid,
                amount = newInvestment.amount
            };

            var eventJson = JsonSerializer.Serialize(investmentEvent);

            await _messageBroker.PublishEvent(
                "investment.events",
                "investment.created",
                eventJson
            );

            Console.WriteLine("Published" + investmentEvent);
            

            return ResponseDto<Investment>.SuccessResponse(newInvestment,$"{newInvestment.amount} was invested in { project.title }");;
        }



        public async Task<ResponseDto<List<Investment>>> GetPortfolio(int userid){

            var userInvestments = await _dbcontext.Investments.Where(i => i.userid == userid).ToListAsync();

            if (userInvestments == null || userInvestments.Count == 0){

                return ResponseDto<List<Investment>>.FailureResponse($"Couldn't find any investment for user {userid}");
            }
            else{

                return ResponseDto<List<Investment>>.SuccessResponse(userInvestments, "User Investments");
            }
        }



        public async Task<ResponseDto<Investment>> GetPortfolioProject(int userid, int projectid){

            var investment = await _dbcontext.Investments.FirstOrDefaultAsync(i => i.userid == userid && i.projectid == projectid);

            if (investment == null){

                return ResponseDto<Investment>.FailureResponse($"No investment found for user {userid} in project {projectid}");
            }
            else{

                return ResponseDto<Investment>.SuccessResponse(investment, "Investment found");
            }
        }

        public async Task HandlePaymentServiceResponse(InvestmentRespondEvent _event){
            
            var investment = await _dbcontext.Investments.FirstOrDefaultAsync(Investment => Investment.investmentid == _event.investmentid);

            if (_event.status){
                investment.status = "Paid";
            }
            else{
                investment.status = "Payment Failed";
            }

            Console.WriteLine(investment);

            await _dbcontext.SaveChangesAsync();
        }

    }
}