/*

*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ExchangeSharp
{
    /// <summary>
    /// List of exchange names
    /// Note: When making a new exchange, add a partial class underneath the exchange class with the name, decouples
    /// the names from a global list here and keeps them with each exchange class.
    /// </summary>
    public static partial class ExchangeName
    {
        private static readonly HashSet<string> exchangeNames = new HashSet<string>();

        static ExchangeName()
        {
            foreach (FieldInfo field in typeof(ExchangeName).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                exchangeNames.Add(field.GetValue(null).ToString());
            }
        }

        /// <summary>
        /// Check if an exchange name exists
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>True if name exists, false otherwise</returns>
        public static bool HasName(string name)
        {
            return exchangeNames.Contains(name);
        }

        /// <summary>
        /// Get a list of all exchange names
        /// </summary>
        public static IReadOnlyCollection<string> ExchangeNames { get { return exchangeNames; } }
    }
}
