﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StockOrderMVC.Models;

namespace StockOrderMVC.Controllers
{
    public class StockOrderController : Controller
    {
        // GET: StockOrder
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CreateOrders()
        {
            List<StockOrder> orderlist = new List<StockOrder>();
            Random rn = new Random();

            for (int i = 0; i < 20; i++)
            {
                StockOrder order = new StockOrder();
                order.UserID = "buyer" + i;
                order.OrderType = "buying";
                order.TargetPrice = rn.Next(95, 105);
                order.Amount = rn.Next(10, 20);
                order.OrderTime = DateTime.Now;

                orderlist.Add(order);
            }

            for (int i = 0; i < 20; i++)
            {
                StockOrder order = new StockOrder();
                order.UserID = "seller" + i;
                order.OrderType = "selling";
                order.TargetPrice = rn.Next(95, 105);
                order.Amount = rn.Next(10, 20);
                order.OrderTime = DateTime.Now;

                orderlist.Add(order);
            }

            orderlist.Sort();

            var q = orderlist.GroupBy(o => o.TargetPrice).Select(o => new StockOrderVM
            {
                TargetPrice = o.Key,
                BuyAmount = o.Where(od => od.OrderType == "buying").Sum(od => od.Amount),
                SellAmount = o.Where(od => od.OrderType == "selling").Sum(od => od.Amount)
            });

            int AccBuying = 0;
            int AccSelling = q.Sum(o => o.SellAmount);

            var GroupedOrderList = q.ToList();

            foreach (var i in GroupedOrderList)
            {
                AccBuying += i.BuyAmount;

                i.AccBuyAmount = AccBuying;
                i.AccSellAmount = AccSelling;

                AccSelling -= i.SellAmount;
            }

            Session["OrderList"] = GroupedOrderList;
            return PartialView(GroupedOrderList);
        }

        public ActionResult CallAuction()
        {
            List<StockOrderVM> orderlist = Session["OrderList"] as List<StockOrderVM>;
            List<decimal> checkpoints = new List<decimal>();
            List<decimal> prices = new List<decimal>();

            decimal dealAmount = 0;
            decimal resultPrice = 0;
            int resultOrders = 0;

            foreach (var i in orderlist)
            {
                if (i.AccBuyAmount >= i.AccSellAmount)
                {
                    dealAmount = i.AccSellAmount * i.TargetPrice;
                }
                else
                {
                    dealAmount = i.AccBuyAmount * i.TargetPrice;
                }

                checkpoints.Add(dealAmount);
                prices.Add(i.TargetPrice);
            }

            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] == checkpoints.Max())
                {
                    resultPrice = prices[i];
                }
            }

            foreach (var i in orderlist)
            {
                if (i.TargetPrice > resultPrice)
                {
                    i.BuyAmount = 0;
                }
                else if (i.TargetPrice < resultPrice)
                {
                    i.SellAmount = 0;
                }
                else
                {
                    if (i.AccBuyAmount > i.AccSellAmount)
                    {
                        i.BuyAmount = i.AccBuyAmount - i.AccSellAmount;
                        i.SellAmount = 0;
                        resultOrders = i.AccSellAmount;
                    }
                    else
                    {
                        i.BuyAmount = 0;
                        i.SellAmount = i.AccSellAmount - i.AccBuyAmount;
                        resultOrders = i.AccBuyAmount;
                    }
                }

            }

            ViewBag.resultPrice = resultPrice;
            ViewBag.resultOrders = resultOrders;
            Session["OrderList_CA"] = orderlist;

            return PartialView(orderlist);
        }

        public ActionResult CallByTrade(StockOrder order)
        {
            List<StockOrderVM> orderlist = Session["OrderList_CA"] as List<StockOrderVM>;
            List<string> tradeResults = new List<string>();

            if (order.OrderType == "buying")
            {
                var selling = orderlist.Where(o => o.TargetPrice <= order.TargetPrice && o.SellAmount != 0).ToList();

                if(selling.Count == 0)
                {
                    var target = orderlist.Where(o => o.TargetPrice == order.TargetPrice).Single();
                    target.BuyAmount += order.Amount;
                    ViewBag.message = "沒有可搓合的交易，加入至目前買單數量";
                }
                else
                {
                    for (int i = selling.Count - 1; i >= 0; i--)
                    {
                        if (order.Amount > selling[i].SellAmount)
                        {
                            order.Amount -= selling[i].SellAmount;
                            string result = "成功買入" + selling[i].SellAmount + "張，價格為" + selling[i].TargetPrice + "元";
                            selling[i].SellAmount = 0;
                            tradeResults.Add(result);
                        }
                        else
                        {
                            selling[i].SellAmount -= order.Amount;
                            string result = "成功買入" + order.Amount + "張，價格為" + selling[i].TargetPrice + "元";
                            order.Amount = 0;
                            tradeResults.Add(result);
                            break;
                        }
                    }
                }
            }
            else
            {
                var buying = orderlist.Where(o => o.TargetPrice >= order.TargetPrice && o.BuyAmount != 0).ToList();

                if(buying.Count == 0)
                {
                    var target = orderlist.Where(o => o.TargetPrice == order.TargetPrice).Single();
                    target.SellAmount += order.Amount;
                    ViewBag.message = "沒有可搓合的交易，加入至目前賣單數量";
                }
                else
                {
                    foreach (var i in buying)
                    {
                        if (order.Amount > i.BuyAmount)
                        {
                            order.Amount -= i.BuyAmount;
                            string result = "成功賣出" + i.BuyAmount + "張，價格為" + i.TargetPrice + "元";
                            i.BuyAmount = 0;
                            tradeResults.Add(result);
                        }
                        else
                        {
                            i.BuyAmount -= order.Amount;
                            string result = "成功賣出" + order.Amount + "張，價格為" + i.TargetPrice + "元";
                            order.Amount = 0;
                            tradeResults.Add(result);
                            break;
                        }
                    }
                }
            }

            ViewBag.tradeResults = tradeResults;
            return PartialView(orderlist);
        }

        public ActionResult Auction()
        {
            return View();
        }

        public ActionResult Trade()
        {
            return View();
        }

        public ActionResult Example()
        {
            return View();
        }
    }
}