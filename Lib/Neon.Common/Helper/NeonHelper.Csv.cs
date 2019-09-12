﻿//-----------------------------------------------------------------------------
// FILE:	    NeonHelper.Csv.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neon.Common
{
    public static partial class NeonHelper
    {
        private static char[] csvEscapes = new char[] { '\r', '\n', ',' };

        /// <summary>
        /// Escapes a string passed so that is suitable for writing to
        /// a CSV file as a field.
        /// </summary>
        /// <param name="value">The field value.</param>
        /// <returns>The escaped string.</returns>
        /// <remarks>
        /// The method surrounds the value with double quotes if it contains
        /// a comma or CRLF as well as escaping any double quotes in the
        /// string with second double quote.
        /// </remarks>
        public static string EscapeCsv(string value)
        {
            Covenant.Requires<ArgumentNullException>(value != null);

            bool needsQuotes = value.IndexOfAny(csvEscapes) != -1;

            if (value.IndexOf('"') != -1)
            {
                value = value.Replace("\"", "\"\"");
            }

            if (needsQuotes)
            {
                return "\"" + value + "\"";
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Parses a CSV encoded string into its component fields.
        /// </summary>
        /// <param name="value">The encoded CSV string.</param>
        /// <returns>The decoded fields.</returns>
        /// <exception cref="FormatException">Thrown if the CSV file format is not valid.</exception>
        public static string[] ParseCsv(string value)
        {
            Covenant.Requires<ArgumentNullException>(value != null);

            List <string>    fields = new List<string>(20);
            int             pStart, pos;
            string          field;

            pStart = 0;
            while (true)
            {

                if (pStart < value.Length && value[pStart] == '"')
                {
                    var sb = new StringBuilder(100);

                    // We have a quoted CSV field

                    pStart++;
                    while (true)
                    {
                        pos = value.IndexOf('"', pStart);
                        if (pos == -1)
                        {
                            throw new FormatException("Missing terminating quote in quoted CSV field.  The row may be span multiple lines.  Consider using the CsvReader class.");
                        }
                        else if (pos < value.Length - 1 && value[pos + 1] == '"')
                        {
                            // We found an escaped quote ("")

                            sb.Append(value, pStart, pos - pStart + 1);
                            pStart = pos + 2;
                        }
                        else
                        {
                            sb.Append(value, pStart, pos - pStart);
                            field = sb.ToString();
                            fields.Add(field);

                            pStart = pos + 1;
                            break;
                        }
                    }

                    if (pStart >= value.Length || value[pStart] != ',')
                    {
                        break;
                    }

                    pStart++;
                }
                else
                {
                    // We have an unquoted CSV field.

                    pos = value.IndexOf(',', pStart);
                    if (pos == -1)
                    {
                        field = value.Substring(pStart);
                        fields.Add(field);
                        break;
                    }
                    else
                    {
                        field = value.Substring(pStart, pos - pStart);
                        fields.Add(field);

                        pStart = pos + 1;
                    }
                }
            }

            return fields.ToArray();
        }
    }
}
