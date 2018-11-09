using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;using ExchangeSharp;

namespace ExchangeSharpConsole
{
    public static partial class ExchangeSharpConsoleMain
    {
        private static void Assert(bool expression)
        {
            if (!expression)
            {
                throw new ApplicationException("Test failure, unexpected result");
            }
        }

   
       
    }
}

