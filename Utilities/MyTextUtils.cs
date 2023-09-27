using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatBotZ.Utilities
{
    internal class MyTextUtils
    {
        private string ExtractFileSnippet(string input, string beginDenotation = "```", string endDenotation = "```")
        {
            int startIndex = input.IndexOf(beginDenotation, StringComparison.Ordinal);
            if (startIndex == -1)
            {
                return null;
            }

            int endIndex = input.IndexOf(endDenotation, startIndex + endDenotation.Length, StringComparison.Ordinal);
            return endIndex == -1
                ? null
                : input.Substring(startIndex + beginDenotation.Length, endIndex - startIndex - endDenotation.Length).Trim();
        }

    }
}