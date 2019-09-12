﻿//-----------------------------------------------------------------------------
// FILE:	    NeonHelper.Platform.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neon.Common
{
    public static partial class NeonHelper
    {
        private static bool     osChecked;
        private static string   osDescription;
        private static string   frameworkDescription;
        private static bool     isWindows;
        private static bool     isLinux;
        private static bool     isOSX;

        /// <summary>
        /// Detects the current operating system.
        /// </summary>
        private static void DetectOS()
        {
            if (osChecked)
            {
                return;     // Already did a detect
            }

            try
            {
                osDescription        = RuntimeInformation.OSDescription;
                frameworkDescription = RuntimeInformation.FrameworkDescription;
                isWindows            = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                isLinux              = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                isOSX                = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            }
            finally
            {
                // Set the global to true so we won't test again.

                osChecked = true;
            }
        }

        /// <summary>
        /// Returns the operation system description.
        /// </summary>
        public static string OsDescription
        {
            get
            {
                if (osChecked)
                {
                    return osDescription;
                }

                DetectOS();
                return osDescription;
            }
        }

        /// <summary>
        /// Returns the .NET runtime description.
        /// </summary>
        public static string FrameworkDescription
        {
            get
            {
                if (osChecked)
                {
                    return frameworkDescription;
                }

                DetectOS();
                return frameworkDescription;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the application is running on a Windows variant
        /// operating system.
        /// </summary>
        public static bool IsWindows
        {
            get
            {
                if (osChecked)
                {
                    return isWindows;
                }

                DetectOS();
                return isWindows;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the application is running on a Linux variant
        /// operating system.
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                if (osChecked)
                {
                    return isLinux;
                }

                DetectOS();
                return isLinux;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the application is running on Max OSX.
        /// </summary>
        public static bool IsOSX
        {
            get
            {
                if (osChecked)
                {
                    return isOSX;
                }

                DetectOS();
                return isOSX;
            }
        }
    }
}
