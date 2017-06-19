/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IBSampleApp.messages;
using System.Windows.Forms;
using IBSampleApp.util;
using IBApi;

namespace IBSampleApp.ui
{
    public class AccountManager
    {
        private const int ACCOUNT_ID_BASE = 50000000;

        private const int ACCOUNT_SUMMARY_ID = ACCOUNT_ID_BASE + 1;

        private const string ACCOUNT_SUMMARY_TAGS = "AccountType,NetLiquidation,TotalCashValue,SettledCash,AccruedCash,BuyingPower,EquityWithLoanValue,PreviousEquityWithLoanValue,"
             +"GrossPositionValue,ReqTEquity,ReqTMargin,SMA,InitMarginReq,MaintMarginReq,AvailableFunds,ExcessLiquidity,Cushion,FullInitMarginReq,FullMaintMarginReq,FullAvailableFunds,"
             +"FullExcessLiquidity,LookAheadNextChange,LookAheadInitMarginReq ,LookAheadMaintMarginReq,LookAheadAvailableFunds,LookAheadExcessLiquidity,HighestSeverity,DayTradesRemaining,Leverage";

        private IBClient ibClient;
        private List<string> managedAccounts;
        private ComboBox accountSelector;
        private DataGridView accountSummaryGrid;
        private DataGridView accountValueGrid;
        private DataGridView accountPortfolioGrid;
        private DataGridView positionsGrid;
        private Dictionary<string, PositionMessage> positions;
        private Dictionary<string, Contract> contracts;
        private OrderManager orderManager;

        private bool accountSummaryRequestActive = false;
        private bool accountUpdateRequestActive = false;
        private string currentAccountSubscribedToTupdate;

        public AccountManager(IBClient ibClient, ComboBox accountSelector, DataGridView accountSummaryGrid, DataGridView accountValueGrid,
            DataGridView accountPortfolioGrid, DataGridView positionsGrid, OrderManager ordermanager)
        {
            IbClient = ibClient;
            AccountSelector = accountSelector;
            AccountSummaryGrid = accountSummaryGrid;
            AccountValueGrid = accountValueGrid;
            AccountPortfolioGrid = accountPortfolioGrid;
            PositionsGrid = positionsGrid;
            positions = new Dictionary<string, PositionMessage>();
            contracts = new Dictionary<string, Contract>();
            orderManager = ordermanager;
        }

        public void UpdateUI(IBMessage message)
        {
            switch (message.Type)
            {
                case MessageType.AccountSummary:
                    HandleAccountSummary((AccountSummaryMessage)message);
                    break;
                case MessageType.AccountSummaryEnd:
                    HandleAccountSummaryEnd();
                    break;
                case MessageType.AccountValue:
                    HandleAccountValue((AccountValueMessage)message);
                    break;
                case MessageType.PortfolioValue:
                    HandlePortfolioValue((UpdatePortfolioMessage)message);
                    break;
                case MessageType.AccountDownloadEnd:
                    break;
                case MessageType.Position:
                    HandlePosition((PositionMessage)message);
                    break;
                case MessageType.PositionEnd:
                    break;
            }
        }

        private void HandleAccountSummaryEnd()
        {
            accountSummaryRequestActive = false;
        }

        private void  HandleAccountSummary(AccountSummaryMessage summaryMessage)
        {
            for (int i = 0; i < accountSummaryGrid.Rows.Count; i++)
            {
                if (accountSummaryGrid[0, i].Value.Equals(summaryMessage.Tag) && accountSummaryGrid[3, i].Value.Equals(summaryMessage.Account))
                {
                    accountSummaryGrid[1, i].Value = summaryMessage.Value;
                    accountSummaryGrid[2, i].Value = summaryMessage.Currency;
                    return;
                }
            }
            accountSummaryGrid.Rows.Add(1);
            accountSummaryGrid[0, accountSummaryGrid.Rows.Count-1].Value = summaryMessage.Tag;
            accountSummaryGrid[1, accountSummaryGrid.Rows.Count - 1].Value = summaryMessage.Value;
            accountSummaryGrid[2, accountSummaryGrid.Rows.Count - 1].Value = summaryMessage.Currency;
            accountSummaryGrid[3, accountSummaryGrid.Rows.Count - 1].Value = summaryMessage.Account;
        }

        private void HandleAccountValue(AccountValueMessage accountValueMessage)
        {
            for (int i = 0; i < accountValueGrid.Rows.Count; i++)
            {
                if (accountValueGrid[0, i].Value.Equals(accountValueMessage.Key))
                {
                    accountValueGrid[1, i].Value = accountValueMessage.Value;
                    accountValueGrid[2, i].Value = accountValueMessage.Currency;
                    return;
                }
            }
            accountValueGrid.Rows.Add(1);
            accountValueGrid[0, accountValueGrid.Rows.Count - 1].Value = accountValueMessage.Key;
            accountValueGrid[1, accountValueGrid.Rows.Count - 1].Value = accountValueMessage.Value;
            accountValueGrid[2, accountValueGrid.Rows.Count - 1].Value = accountValueMessage.Currency;
        }

        private void HandlePortfolioValue(UpdatePortfolioMessage updatePortfolioMessage)
        {
            
            for (int i = 0; i < accountPortfolioGrid.Rows.Count; i++)
            {
                if (accountPortfolioGrid[0, i].Value.Equals(Utils.ContractToString(updatePortfolioMessage.Contract)))
                {
                    accountPortfolioGrid[1, i].Value = updatePortfolioMessage.Position;
                    accountPortfolioGrid[2, i].Value = updatePortfolioMessage.MarketPrice;
                    accountPortfolioGrid[3, i].Value = updatePortfolioMessage.MarketValue;
                    accountPortfolioGrid[4, i].Value = updatePortfolioMessage.AverageCost;
                    accountPortfolioGrid[5, i].Value = updatePortfolioMessage.UnrealisedPNL;
                    accountPortfolioGrid[6, i].Value = updatePortfolioMessage.RealisedPNL;
                    return;
                }
            }
            
            accountPortfolioGrid.Rows.Add(1);
            accountPortfolioGrid[0, accountPortfolioGrid.Rows.Count - 1].Value = Utils.ContractToString(updatePortfolioMessage.Contract); ;
            accountPortfolioGrid[1, accountPortfolioGrid.Rows.Count - 1].Value = updatePortfolioMessage.Position;
            accountPortfolioGrid[2, accountPortfolioGrid.Rows.Count - 1].Value = updatePortfolioMessage.MarketPrice;
            accountPortfolioGrid[3, accountPortfolioGrid.Rows.Count - 1].Value = updatePortfolioMessage.MarketValue;
            accountPortfolioGrid[4, accountPortfolioGrid.Rows.Count - 1].Value = updatePortfolioMessage.AverageCost;
            accountPortfolioGrid[5, accountPortfolioGrid.Rows.Count - 1].Value = updatePortfolioMessage.UnrealisedPNL;
            accountPortfolioGrid[6, accountPortfolioGrid.Rows.Count - 1].Value = updatePortfolioMessage.RealisedPNL;
        }

        private List<OrderItem> RetrieveOrderItems()
        {
            var orders = new List<OrderItem>();

            var file = new System.IO.StreamReader("stops.txt");
            string line;
            char[] delimiterChars = { ' ', ',','\t' };
            while ((line = file.ReadLine()) != null)
            {
                var words = line.Trim().Replace(" ", string.Empty).Split(delimiterChars);
                orders.Add(new OrderItem(words[0], words[1], int.Parse(words[2]), double.Parse(words[3])));
            } 
            //orders.Add(new OrderItem("ABMD", "HReg", 30, 130.0));

            return orders;
        }

        private Boolean ValidateOrder(OrderItem item)
        {
            var res = false;

            var key = item.Symbol + "_" + item.Account;
            if(positions.ContainsKey(key))
            {
                var pm = positions[key];
                if(pm.Position >= item.Quantity && contracts.ContainsKey(item.Symbol))
                {
                    res = true;
                }
            }            

            return res;
        }

        public async void ContractLookup()
        {
            foreach (var position in positions.Values)
            {
                if (!contracts.ContainsKey(position.Contract.Symbol))
                {
                    var contractList = await ibClient.ResolveContract("STK", position.Contract.Symbol, "USD", "SMART");
                    contracts.Add(position.Contract.Symbol, contractList.First());
                }
            }
            
            positionsGrid.Invoke(new MethodInvoker(() => MessageBox.Show("Contract lookup complete!"))); 
        }

        private List<OrderCondition> OrderConditions(OrderItem item)
        {
            var conditions = new List<OrderCondition>();

            TimeCondition c1 = (TimeCondition)OrderCondition.Create(OrderConditionType.Time);
            c1.IsMore = true;
            var now = DateTime.Now;
            var marketClose = new DateTime(now.Year, now.Month, now.Day, 13, 0, 1);

            string dateStr = "";
            if(now < now.AddDays(1).Date && now > marketClose)
            {
                dateStr = now.AddDays(1).ToString("yyyyMMdd");
            } else
            {
                dateStr = now.ToString("yyyyMMdd");
            }

            c1.Time = dateStr + " 12:58:17";
            c1.IsConjunctionConnection = true;
            conditions.Add(c1);

            PriceCondition c2 = (PriceCondition)OrderCondition.Create(OrderConditionType.Price);
            c2.ConId = contracts[item.Symbol].ConId;
            c2.Exchange = "SMART";
            c2.IsMore = false;
            c2.Price = item.StopPrice;
            c2.IsConjunctionConnection = true;
            conditions.Add(c2);

            return conditions;
        }

        public void PrepareStopOrders()
        {
            var items = RetrieveOrderItems();
            foreach(var item in items)
            {
                if (ValidateOrder(item))
                {
                    var order = new Order() { Account = item.Account, Action = "SELL", OrderType = "MKT", TotalQuantity = item.Quantity };
                    order.Conditions.AddRange(OrderConditions(item));
                    orderManager.PlaceOrder(contracts[item.Symbol], order);
                }
                
            }
        }

        public void HandlePosition(PositionMessage positionMessage)
        {
            positions.Add(positionMessage.Contract.Symbol + "_" + positionMessage.Account, positionMessage);
            //for (int i = 0; i < positionsGrid.Rows.Count; i++)
            //{
            //    if (positionsGrid[0, i].Value.Equals(Utils.ContractToString(positionMessage.Contract)))
            //    {
            //        positionsGrid[1, i].Value = positionMessage.Account;
            //        positionsGrid[2, i].Value = positionMessage.Position;
            //        positionsGrid[3, i].Value = positionMessage.AverageCost;
            //        return;
            //    }
            //}

            positionsGrid.Rows.Add(1);
            positionsGrid[0, positionsGrid.Rows.Count - 1].Value = Utils.ContractToString(positionMessage.Contract);
            positionsGrid[1, positionsGrid.Rows.Count - 1].Value = positionMessage.Account;
            positionsGrid[2, positionsGrid.Rows.Count - 1].Value = positionMessage.Position;
            positionsGrid[3, positionsGrid.Rows.Count - 1].Value = positionMessage.AverageCost;
        }

        public void RequestAccountSummary()
        {
            if (!accountSummaryRequestActive)
            {
                accountSummaryRequestActive = true;
                accountSummaryGrid.Rows.Clear();
                ibClient.ClientSocket.reqAccountSummary(ACCOUNT_SUMMARY_ID, "All", ACCOUNT_SUMMARY_TAGS);
            }
            else
            {
                ibClient.ClientSocket.cancelAccountSummary(ACCOUNT_SUMMARY_ID);
            }
        }

        public void SubscribeAccountUpdates()
        {
            if (!accountUpdateRequestActive)
            {
                currentAccountSubscribedToTupdate = accountSelector.SelectedItem.ToString();
                accountUpdateRequestActive = true;
                accountValueGrid.Rows.Clear();
                accountPortfolioGrid.Rows.Clear();
                ibClient.ClientSocket.reqAccountUpdates(true, currentAccountSubscribedToTupdate);
            }
            else
            {
                ibClient.ClientSocket.reqAccountUpdates(false, currentAccountSubscribedToTupdate);
                currentAccountSubscribedToTupdate = null;
                accountUpdateRequestActive = false;
            }
        }

        public void RequestPositions()
        {
            positions.Clear();
            positionsGrid.DataSource = null;
            positionsGrid.Rows.Clear();
            positionsGrid.Refresh();
            ibClient.ClientSocket.reqPositions();
        }
        
        public List<string> ManagedAccounts
        {
            get { return managedAccounts; }
            set 
            { 
                managedAccounts = value; 
                SetManagedAccounts(value);
            }
        }

        public void SetManagedAccounts(List<string> managedAccounts)
        {
            AccountSelector.Items.AddRange(managedAccounts.ToArray());
            AccountSelector.SelectedIndex = 0;
        }

        public ComboBox AccountSelector
        {
            get { return accountSelector; }
            set { accountSelector = value; }
        }

        public DataGridView AccountSummaryGrid
        {
            get { return accountSummaryGrid; }
            set { accountSummaryGrid = value; }
        }

        public DataGridView AccountValueGrid
        {
            get { return accountValueGrid; }
            set { accountValueGrid = value; }
        }

        public DataGridView AccountPortfolioGrid
        {
            get { return accountPortfolioGrid; }
            set { accountPortfolioGrid = value; }
        }

        public DataGridView PositionsGrid
        {
            get { return positionsGrid; }
            set { positionsGrid = value; }
        }

        public IBClient IbClient
        {
            get { return ibClient; }
            set { ibClient = value; }
        }
    }
}
