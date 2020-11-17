using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BlazorApp3.Server.Data;
using BlazorApp3.Server.Helpers;
using BlazorApp3.Server.Models;
using BlazorApp3.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wallet = BlazorApp3.Server.Models.Wallet;

namespace BlazorApp3.Server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public WalletController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        [HttpGet]
        public List<Wallet> GetWallets()
        {
            var userId = userManager.GetUserId(User);
            var wallets = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId).Wallets;
            return wallets;
        }

        [HttpGet]
        [Route("{id}")]
        public Wallet GetWallet(Guid id)
        {
            var userId = userManager.GetUserId(User);
            var wallet = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId).Wallets.FirstOrDefault(x => x.Id == id);
            return wallet;
        }

        [HttpPost]
        public IActionResult CreateWallet([FromQuery] string currency)
        {
            if (!CurrencyManager.Currencies.Contains(currency))
            {
                return BadRequest();
            }

            var userId = userManager.GetUserId(User);

            var user = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId);

            if (user.Wallets.Any(x => x.Currency == currency))
            {
                return BadRequest();
            }

            var wallet = new Wallet
            {
                Amount = 0,
                Currency = currency
            };

            if (user.Wallets == null)
            {
                user.Wallets = new List<Wallet>();
            }

            user.Wallets.Add(wallet);

            context.SaveChanges();

            return Ok();
        }

        [HttpDelete]
        [Route("{id}")]
        public IActionResult DeleteWallet([FromRoute] Guid id)
        {
            var userId = userManager.GetUserId(User);
            var user = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId);

            if (!user.Wallets.Any(x => x.Id == id))
            {
                return BadRequest();
            }

            var wallet = context.Wallets.Find(id);
            context.Wallets.Remove(wallet);
            context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [Route("transfer")]
        public ActionResult MakeTransfer([FromBody] TransferDto data)
        {
            var userId = userManager.GetUserId(User);
            var user = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.Id == userId);

            if (!user.Wallets.Any(x => x.Currency == data.Currency))
            {
                return BadRequest();
            }

            var source = user.Wallets.FirstOrDefault(x => x.Currency == data.Currency);

            if (source.Amount < data.Amount)
            {
                return BadRequest();
            }

            var destinationUser = context.Users.Include(x => x.Wallets).FirstOrDefault(x => x.UserName == data.Username);

            var destination = destinationUser.Wallets.FirstOrDefault(x => x.Currency == data.Currency);

            if (destination == null)
            {
                destination = new Wallet
                {
                    Amount = 0,
                    Currency = data.Currency
                };

                destinationUser.Wallets.Add(destination);
            }

            source.Amount -= data.Amount;
            destination.Amount += data.Amount;

            var transaction = new Transaction
            {
                Amount = data.Amount,
                Date = DateTime.Now,
                DestinationWalletId = destination.Id,
                SourceWalletId = source.Id
            };
            context.Add(transaction);

            context.SaveChanges();

            return Ok();
        }
        [HttpGet]
        [Route("transferHistory/{limit}/{page}/{direction}")]
        public async Task<TransactionsHistoryData> GetTransferHistory(int limit, int page, Direction direction = Direction.None)
        {
            var userId = userManager.GetUserId(User);
            
            var user1 = await userManager.GetUserAsync(User);
            context.Entry(user1).Collection(u => u.Wallets).Load();
            var wallets = user1.Wallets;
            //var user = context.Users.Include(u => u.Wallets).Where(x => x.Id == userId).FirstOrDefault();
            //var walletIds = user.Wallets.Select(w => w.Id);
            var walletIds = context.Wallets.Where(w => w.ApplicationUserId == userId).Select(w => w.Id).ToList();

            List<Transaction> transactions;
            switch (direction)
            {
                case Direction.Inbound:
                    transactions = context.Transactions.Where(t => walletIds.Contains(t.DestinationWalletId))
                        .Skip((page - 1) * limit).Take(limit).ToList();
                    break;
                case Direction.Outbound:
                    transactions = context.Transactions.Where(t => walletIds.Contains(t.SourceWalletId))
                        .Skip((page - 1) * limit).Take(limit).ToList();
                    break;
                case Direction.None:
                default:
                    transactions = context.Transactions.Where(t =>
                        walletIds.Contains(t.SourceWalletId) || walletIds.Contains(t.DestinationWalletId))
                        .Skip((page - 1) * limit).Take(limit).ToList();
                    break;
            }

            var transactionsData = new TransactionsHistoryData
            {
                Transactions = transactions.Select(DomainMapper.ToDto).ToArray(),
                Count = context.Transactions.Where(t =>
                walletIds.Contains(t.SourceWalletId) || walletIds.Contains(t.DestinationWalletId)).Count()
            };

            return transactionsData;
        }
    }
}
