﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ZiggyZiggyWallet.Commons;
using ZiggyZiggyWallet.DTOs;
using ZiggyZiggyWallet.DTOs.Systems;
using ZiggyZiggyWallet.DTOs.Transactions;
using ZiggyZiggyWallet.Models;
using ZiggyZiggyWallet.Services.Interfaces;

namespace ZiggyZiggyWallet.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ITransactionServices _transServe;
        private readonly IWalletServices _wallServe;
        private readonly ICurrencyServices _curServe;

        public TransactionController(ITransactionServices transServe, ICurrencyServices curServe, IWalletServices wallServe, IMapper mapper)
        {
            _mapper = mapper;
            _transServe = transServe;
            _wallServe = wallServe;
            _curServe = curServe;
        }

        [Authorize(Roles = "Noob, Elite")]
        [HttpPost("send-Money")]
        public async Task<IActionResult> TransferMoney(TransactionToAdd model, string sWalletAddress, string rWalletAddress, string sCurrency, float amount, string description, string userId)
        {
            //check if user logged is the one making the changes - only works for system using Auth tokens
            ClaimsPrincipal currentUser = this.User;
            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (!userId.Equals(currentUserId))
            {
                ModelState.AddModelError("Denied", $"You are not allowed to Send Money with Someone's Address");
                var result2 = Util.BuildResponse<string>(false, "Access denied!", ModelState, "");
                return BadRequest(result2);
            }

            if (sWalletAddress == null && rWalletAddress == null)
            {
                ModelState.AddModelError("Not found", "Wallet Address is Invalid");
                return NotFound(Util.BuildResponse<object>(false, "Result is empty", ModelState, null));
            }

            Wallet sWalletDetail = new Wallet();
            Wallet rWalletDetail = new Wallet();
            sWalletDetail = await _wallServe.GetWalletByAddress(sWalletAddress);
            rWalletDetail = await _wallServe.GetWalletByAddress(rWalletAddress);

            var sWalletId = sWalletDetail.Id;
            var rWalletId = rWalletDetail.Id;
            var sCurrDetail = await _curServe.GetCurrencyById(sCurrency);

            var sendMoney = await _transServe.SendMoney(model, sWalletId, sCurrency, rWalletId, amount, description);
            if (sendMoney != null)
            {
                return Ok(Util.BuildResponse<string>(true, $"{sWalletDetail.Address} Sends {sCurrDetail.ShortCode} {amount} ({sCurrDetail.Name}) or it's Equivalent to {rWalletDetail.Address} Wallet Balance", null, "Money Sent Successfully"));
            }
            ModelState.AddModelError("Not found", $"{ sWalletDetail.Name} Balance is Low, Please check with smaller amount");
            return NotFound(Util.BuildResponse<object>(false, "Low Balance", ModelState, null));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin-topup")]
        public async Task<IActionResult> AdminTopUpWallet(TransactionToAdd model, float amount, string currencyId, string wallAddr)
        {
            //check if user logged is the one making the changes - only works for system using Auth tokens
            ClaimsPrincipal currentUser = this.User;
            var currentUserRole = currentUser.FindFirst(ClaimTypes.Role).Value;
            if (!currentUserRole.Equals("Admin"))
            {
                ModelState.AddModelError("Denied", $"You are not allowed to TopUp Users Wallet use the TopUp by Card Feature");
                var result2 = Util.BuildResponse<string>(false, "Access denied!", ModelState, "");
                return BadRequest(result2);
            }
            string toppedBy = "Admin";

            var wallDet = await _wallServe.GetWalletByAddress(wallAddr);

            var wallId = wallDet.Id;

            var sCurrDetail = await _curServe.GetCurrencyById(currencyId);

            var topUp = await _transServe.TopUp(model, amount, currencyId, wallId, toppedBy);
            if (topUp != null)
            {
                return Ok(Util.BuildResponse<string>(true, $"{wallDet.Address}  {sCurrDetail.ShortCode} has been topped up with {amount} in {sCurrDetail.Name}by {toppedBy}", null, "Wallet Topped Successfully"));
            }
            ModelState.AddModelError("Not found", $"{ wallDet.Name} Wallet TopUp failed.");
            return NotFound(Util.BuildResponse<object>(false, "failed", ModelState, null));
        }


        [Authorize(Roles = "Noob,Elite")]
        [HttpPost("card-topup")]
        public async Task<IActionResult> CardTopUpWallet(TransactionToAdd model, float amount, string currencyId, string wallAddr, string cardNo, int ccv, int yearOfExp, string pin, string userId)
        {
            //check if user logged is the one making the changes - only works for system using Auth tokens
            ClaimsPrincipal currentUser = this.User;
            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (!currentUserId.Equals(userId))
            {
                ModelState.AddModelError("Denied", $"You are not allowed to TopUp Users Wallet use the TopUp by Card Feature");
                var result2 = Util.BuildResponse<string>(false, "Access denied!", ModelState, "");
                return BadRequest(result2);
            }


            var wallDet = await _wallServe.GetWalletByAddress(wallAddr);
            var wallId = wallDet.Id;
            var CurrDet = await _curServe.GetCurrencyById(currencyId);
            var CurrAbb = CurrDet.Abbrevation;

            string toppedBy = "Card";
            //Verify The cardDetails
            //DummyCard
            var dummyCardNo = "12345678901234";
            List<string> dummyCardType = new List<string> { "usd", "aud", "jpy", "mxn" };
            var dummyCcv = 123;
            var dummyYearOfExp = 2024;
            var dummyCardPin = "1234";
            var dummyBalance = 4500;
            if (dummyCardNo == cardNo)
            {
                if (dummyCardType.Contains(CurrAbb))
                {

                    if (dummyCcv == ccv)
                    {
                        if (dummyYearOfExp == yearOfExp)
                        {
                            if (dummyCardPin == pin)
                            {
                                if (dummyBalance < amount)
                                {

                                    var topUp = await _transServe.TopUp(model, amount, currencyId, wallId, toppedBy);
                                    if (topUp != null)
                                    {
                                        return Ok(Util.BuildResponse<string>(true, $"{wallDet.Address}  {CurrDet.ShortCode} has been topped up with {amount} in {CurrDet.Name} by {toppedBy}", null, "Wallet Topped Successfully"));
                                    }
                                }
                                ModelState.AddModelError("Low Card Balance", $"{ wallDet.Name} Wallet TopUp failed.");
                            }
                        }

                    }
                }
            }
            ModelState.AddModelError("Card detail Error", $"{ wallDet.Name} Wallet TopUp failed.");
            return NotFound(Util.BuildResponse<object>(false, "failed", ModelState, null));
        }



        
        [HttpPost("transactions-list/{wallAddr}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserWalletList(string wallAddr)
        {
            var wallDet = await _wallServe.GetWalletByAddress(wallAddr);
            var wallId = wallDet.Id;
            var wallName= wallDet.Name;
            var tranz = await _transServe.WalletTransactionHistory(wallId);
            if (tranz == null)
            {
                ModelState.AddModelError("Not found", $"No result found for {wallName}'s wallet transactions");
                return NotFound(Util.BuildResponse<object>(false, "Result is empty", ModelState, null));
            }

            // map result
            var listOfTransToReturn = new List<TransactionToReturn>();
            foreach (var trans in tranz)
            {
                var transToReturn = _mapper.Map<TransactionToReturn>(trans);
                listOfTransToReturn.Add(transToReturn);

            }

            return Ok(Util.BuildResponse<List<TransactionToReturn>>(true, $"List of {wallName}'s wallet transactions", null, listOfTransToReturn));
        }

    }
}
