using Microsoft.EntityFrameworkCore;
using System.Text.Json;


using PaymentService.Models;
using PaymentService.Data;

namespace PaymentService.Utility{

    public class PaymentServicex{
        private readonly AppDbContext _dbcontext;
        private readonly MessageBroker _messageBroker;

        public PaymentServicex(AppDbContext dbcontext, MessageBroker msgbroker){
            _dbcontext = dbcontext;
            _messageBroker = msgbroker;
        }

        public async Task<ResponseDto<object>> AddFundsToWallet(int userId, decimal amount){
            try
            {
                var wallet = await  _dbcontext.Wallets.FirstOrDefaultAsync(w => w.user_id == userId);

                wallet!.balance += amount;
                await _dbcontext.SaveChangesAsync();

                return ResponseDto<object>.SuccessResponse(new { userId, wallet.balance }, "Funds added successfully");
            }
            catch (Exception ex)
            {
                return ResponseDto<object>.FailureResponse("Failed to add funds", ex.Message);
            }
        }

        public ResponseDto<decimal> GetWalletBalance(int userId){
            try
            {
                var wallet = _dbcontext.Wallets.FirstOrDefault(w => w.user_id == userId);
                var balance = wallet?.balance ?? 0;

                return ResponseDto<decimal>.SuccessResponse(balance, "Balance retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponseDto<decimal>.FailureResponse("Failed to retrieve balance", ex.Message);
            }
        }

        public ResponseDto<object> GetUserTransactions(int userId, int page, int pageSize){
            try
            {
                var transactions = _dbcontext.Transactions
                    .Where(t => t.user_id == userId)
                    .OrderByDescending(t => t.created_at)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return ResponseDto<object>.SuccessResponse(new { page, pageSize, transactions }, "Transactions retrieved successfully");
            }
            catch (Exception ex)
            {
                return ResponseDto<object>.FailureResponse("Failed to retrieve transactions", ex.Message);
            }
        }

        public async Task<Wallets> CreateWallet(int userid){

            Wallets wallet = new()
            {
                balance = 0,
                user_id = userid
            };

            await _dbcontext.Wallets.AddAsync(wallet);
            await _dbcontext.SaveChangesAsync();

            return wallet;

        }

        public async Task CreateTSX(int userId, string type, decimal amount){

            Transactions TRX = new()
            {
                user_id = userId,
                transaction_type = type,
                amount = amount,
                created_at = DateTime.UtcNow
            };

            await _dbcontext.Transactions.AddAsync(TRX);
            await _dbcontext.SaveChangesAsync();

        }

        public async Task<bool> HandleInvestments(int userId, decimal amount, int investmentID){

            // step 1: check balance
            // step 2: create a transaction
            // step 3: publish an event about the given investment weather it got enough balance or not
            
            var UserWallet = await _dbcontext.Wallets.FirstOrDefaultAsync(w => w.user_id == userId);

            if(UserWallet!.balance < amount){


                await CreateTSX(userId, TransactionType.Investment.ToString(), amount);

                var InvestmentRespond = new InvestmentRespondEvent{
                    investmentid = investmentID,
                    userid = userId,
                    status = false
                };

                var eventJson = JsonSerializer.Serialize(InvestmentRespond);

                await _messageBroker.PublishEvent(
                    "payment.events",
                    "payment.respond",
                    eventJson
                );

                Console.Write("failed payment");
                return false;

            }else{

                UserWallet.balance = UserWallet.balance - amount;

                await _dbcontext.SaveChangesAsync();


                await CreateTSX(userId, TransactionType.Investment.ToString(), amount);

                var InvestmentRespond = new InvestmentRespondEvent{
                    investmentid = investmentID,
                    userid = userId,
                    status = true
                };

                var eventJson = JsonSerializer.Serialize(InvestmentRespond);

                await _messageBroker.PublishEvent(
                    "payment.events",
                    "payment.respond",
                    eventJson
                );
                return true;

            }

        }

    }

}