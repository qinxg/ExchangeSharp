using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;using Centipede;

namespace CentipedeConsole
{
    public static partial class CentipedeConsoleMain
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

