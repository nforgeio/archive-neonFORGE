﻿//-----------------------------------------------------------------------------
// FILE:	    HostingLoader.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Cryptography;
using Neon.IO;
using Neon.Net;
using Neon.Time;

namespace Neon.Kube
{
    /// <summary>
    /// Cluster hosting utilities.
    /// </summary>
    public class HostingLoader : IHostingLoader
    {
        //---------------------------------------------------------------------
        // Static members

        private static object                                   syncLock = new object();
        private static Dictionary<HostingEnvironments, Type>    environmentToHostingManager;

        /// <summary>
        /// <para>
        /// Loads the known cluster hosting manager assemblies so they'll be available
        /// to <see cref="HostingManagerFactory.GetMaster(ClusterProxy, string)"/>, 
        /// and <see cref="HostingManager.Validate(ClusterDefinition)"/> when
        /// they are called.
        /// </para>
        /// <note>
        /// It is safe to call this multiple times because any calls after the first
        /// will be ignored.
        /// </note>
        /// </summary>
        /// <exception cref="KubeException">Thrown if multiple managers implement support for the same hosting environment.</exception>
        public static void Initialize()
        {
            lock (syncLock)
            {
                if (HostingLoader.environmentToHostingManager != null)
                {
                    return;     // Already initialized
                }

                // $todo(jeff.lill):
                //
                // This is hardcoded to load all of the built-in hosting manager assemblies.  
                // In the future, it would be nice if this could be less hardcoded and also 
                // support loading custom assemblies so that users could author their own 
                // managers.
                //
                // This implemention is also pretty stupid in that it has to load all of the
                // hosting manager assemblies because it doesn't know which manager will be requested.
                // One way to fix this would be to implement some kind of callback that could
                // be registered statically with [HostingManager] before [HostingManager.GetManager()]
                // is called.
                //
                // Another potential problem is that it's possible in the future for hosting
                // manager subassemblies to conflict.  For example, say we have a [XenServer] 
                // hosting manager that uses the latest Azure class libraries but we also have a
                // [XenLegacy] hosting manager that needs to use an older library for some
                // reason.  It could be possible that we can't reference or load both sets
                // of subassemblies at the same time.
                //
                // I'm going to defer this for now though.  I suspect that the ultimate
                // solution will be to handle this as part of a greater extensibility
                // strategy and this is unlikely to become a problem any time soon.

                AwsHostingManager.Load();
                AzureHostingManager.Load();
                GoogleHostingManager.Load();
                HyperVHostingManager.Load();
                HyperVLocalHostingManager.Load();
                MachineHostingManager.Load();
                XenServerHostingManager.Load();

                // We're going to reflect all loaded assemblies for classes that implement
                // [IHostingManager] and are decorated with an [HostingProviderAttribute],
                // end then use the environment specified in the attributes to determine
                // which hosting manager class to instantiate and return.

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                environmentToHostingManager = new Dictionary<HostingEnvironments, Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(HostingManager)))
                        {
                            var providerAttribute = type.GetCustomAttribute<HostingProviderAttribute>();

                            if (providerAttribute != null)
                            {
                                if (environmentToHostingManager.TryGetValue(providerAttribute.Environment, out var existingProviderType))
                                {
                                    throw new KubeException($"Hosting provider types [{existingProviderType.FullName}] and [{type.FullName}] cannot both implement the [{providerAttribute.Environment}] hosting environment.");
                                }
                            }

                            environmentToHostingManager.Add(providerAttribute.Environment, type);
                        }
                    }
                }

                // Configure [HostingManagerFactory.Loader] so it will call an instance of the class to 
                // map hosting a environment type to a concrete hosting manager implementation.

                HostingManagerFactory.Loader = new HostingLoader();
            }
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <inheritdoc/>
        public bool IsCloudEnvironment(HostingEnvironments environment)
        {
            switch (environment)
            {
                case HostingEnvironments.Aws:
                case HostingEnvironments.Azure:
                case HostingEnvironments.Google:

                    return true;

                case HostingEnvironments.HyperV:
                case HostingEnvironments.HyperVLocal:
                case HostingEnvironments.Machine:
                case HostingEnvironments.XenServer:

                    return false;

                default:

                    throw new NotImplementedException($"Hosting manager for [{environment}] is not implemented.");
            }
        }

        /// <summary>
        /// Returns the <see cref="HostingManager"/> for a specific environment.
        /// </summary>
        /// <param name="cluster">The cluster being managed.</param>
        /// <param name="logFolder">
        /// The folder where log files are to be written, otherwise or <c>null</c> or 
        /// empty if logging is disabled.
        /// </param>
        /// <returns>
        /// The <see cref="HostingManager"/> or <c>null</c> if no hosting manager
        /// could be located for the specified cluster environment.
        /// </returns>
        public HostingManager GetManager(ClusterProxy cluster, string logFolder = null)
        {
            Covenant.Requires<ArgumentNullException>(cluster != null);
            Covenant.Assert(environmentToHostingManager != null, $"[{nameof(HostingLoader)}] is not initialized.  You must call [{nameof(HostingLoader)}.{nameof(HostingLoader.Initialize)}()] first.");

            if (!environmentToHostingManager.TryGetValue(cluster.Definition.Hosting.Environment, out var managerType))
            {
                return null;
            }

            return (HostingManager)Activator.CreateInstance(managerType, cluster, logFolder);
        }
    }
}
