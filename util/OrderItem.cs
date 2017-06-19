using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBSampleApp.util
{
    public class OrderItem
    {
        public OrderItem() { }

        public OrderItem(string symbol, string accountKey, int quantity, double price)
        {
            Symbol = symbol;
            Account = accountKeys[accountKey];
            Quantity = quantity;
            StopPrice = price;
        }

        public string Symbol { get; set; }

        public int Quantity { get; set; }

        public double StopPrice { get; set; }

        public string Account { get; set; }

        private Dictionary<string, string> accountKeys = new Dictionary<string, string>
        {
            {"HReg", "U417010" },
            {"HIra", "U1050100" },
            {"HRoth", "U872127" },
            {"ZReg", "U1382327" },
            {"ZIra", "U1372425" },
            {"ZRoth", "U1386005" }
        };
    }
}
