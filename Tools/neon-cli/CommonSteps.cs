﻿//-----------------------------------------------------------------------------
// FILE:	    CommonSteps.cs
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Kube;
using Neon.IO;
using System.Diagnostics.Contracts;

namespace NeonCli
{
    /// <summary>
    /// Implements common configuration steps.
    /// </summary>
    public static class CommonSteps
    {
        /// <summary>
        /// Verifies that the node has the correct operating system installed.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        /// <param name="stepDelay">Ignored.</param>
        public static void VerifyOS(SshProxy<NodeDefinition> node, TimeSpan stepDelay)
        {
            node.Status = "check: OS";

            // $todo(jeff.lill): We're currently hardcoded to Ubuntu 18.04.x

            if (!node.OsName.Equals("Ubuntu", StringComparison.InvariantCultureIgnoreCase) || node.OsVersion < Version.Parse("18.04"))
            {
                node.Fault("Expected: Ubuntu 18.04.x");
            }
        }

        /// <summary>
        /// Customizes the OpenSSH configuration on a node.
        /// </summary>
        /// <param name="node">The target node.</param>
        /// <param name="stepDelayed">Ignored.</param>
        public static void ConfigureOpenSSH(SshProxy<NodeDefinition> node, TimeSpan stepDelayed)
        {
            // Upload the OpenSSH server configuration, restart OpenSSH and
            // then disconnect and wait for the OpenSSH to restart.

            var openSshConfig =
@"# Package generated configuration file
# See the sshd_config(5) manpage for details

# What ports, IPs and protocols we listen for
Port 22
# Use these options to restrict which interfaces/protocols sshd will bind to
#ListenAddress ::
#ListenAddress 0.0.0.0
Protocol 2
# HostKeys for protocol version 2
HostKey /etc/ssh/ssh_host_rsa_key
#HostKey /etc/ssh/ssh_host_dsa_key
#HostKey /etc/ssh/ssh_host_ecdsa_key
#HostKey /etc/ssh/ssh_host_ed25519_key
#Privilege Separation is turned on for security
UsePrivilegeSeparation yes

# Lifetime and size of ephemeral version 1 server key
KeyRegenerationInterval 3600
ServerKeyBits 1024

# Logging
SyslogFacility AUTH
LogLevel INFO

# Authentication:
LoginGraceTime 120
PermitRootLogin prohibit-password
StrictModes yes

RSAAuthentication yes
PubkeyAuthentication yes
#AuthorizedKeysFile	%h/.ssh/authorized_keys

# Don't read the user's ~/.rhosts and ~/.shosts files
IgnoreRhosts yes
# For this to work you will also need host keys in /etc/ssh_known_hosts
RhostsRSAAuthentication no
# similar for protocol version 2
HostbasedAuthentication no
# Uncomment if you don't trust ~/.ssh/known_hosts for RhostsRSAAuthentication
#IgnoreUserKnownHosts yes

# To enable empty passwords, change to yes (NOT RECOMMENDED)
PermitEmptyPasswords no

# Change to yes to enable challenge-response passwords (beware issues with
# some PAM modules and threads)
ChallengeResponseAuthentication no

# Change to no to disable tunnelled clear text passwords
#PasswordAuthentication yes

# Kerberos options
#KerberosAuthentication no
#KerberosGetAFSToken no
#KerberosOrLocalPasswd yes
#KerberosTicketCleanup yes

# GSSAPI options
#GSSAPIAuthentication no
#GSSAPICleanupCredentials yes

AllowTcpForwarding no
X11Forwarding no
X11DisplayOffset 10
PrintMotd no
PrintLastLog yes
TCPKeepAlive yes
#UseLogin no

#MaxStartups 10:30:60
#Banner /etc/issue.net

# Allow client to pass locale environment variables
AcceptEnv LANG LC_*

Subsystem sftp /usr/lib/openssh/sftp-server

# Set this to 'yes' to enable PAM authentication, account processing,
# and session processing. If this is enabled, PAM authentication will
# be allowed through the ChallengeResponseAuthentication and
# PasswordAuthentication.  Depending on your PAM configuration,
# PAM authentication via ChallengeResponseAuthentication may bypass
# the setting of ""PermitRootLogin without-password"".
# If you just want the PAM account and session checks to run without
# PAM authentication, then enable this but set PasswordAuthentication
# and ChallengeResponseAuthentication to 'no'.
UsePAM yes

# Allow connections to be idle for up to an 10 minutes (600 seconds)
# before terminating them.  This configuration pings the client every
# 30 seconds for up to 20 times without a response:
#
#   20*30 = 600 seconds

ClientAliveInterval 30
ClientAliveCountMax 20
TCPKeepAlive yes
";
            node.UploadText("/etc/ssh/sshd_config", openSshConfig);
            node.SudoCommand("systemctl restart sshd");
        }

        /// <summary>
        /// Configures the global environment variables that describe the configuration 
        /// of the server within the cluster.
        /// </summary>
        /// <param name="node">The server to be updated.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        public static void ConfigureEnvironmentVariables(SshProxy<NodeDefinition> node, ClusterDefinition clusterDefinition)
        {
            node.Status = "environment variables";

            // We're going to append the new variables to the existing Linux [/etc/environment] file.

            var sb = new StringBuilder();

            // Append all of the existing environment variables except for those
            // whose names start with "NEON_" to make the operation idempotent.
            //
            // Note that we're going to special case PATH to add any Neon
            // related directories.

            using (var currentEnvironmentStream = new MemoryStream())
            {
                node.Download("/etc/environment", currentEnvironmentStream);

                currentEnvironmentStream.Position = 0;

                using (var reader = new StreamReader(currentEnvironmentStream))
                {
                    foreach (var line in reader.Lines())
                    {
                        if (line.StartsWith("PATH="))
                        {
                            if (!line.Contains(KubeHostFolders.Bin))
                            {
                                sb.AppendLine(line + $":/snap/bin:{KubeHostFolders.Bin}");
                            }
                            else
                            {
                                sb.AppendLine(line);
                            }
                        }
                        else if (!line.StartsWith("NEON_"))
                        {
                            sb.AppendLine(line);
                        }
                    }
                }
            }

            // Add the global cluster related environment variables. 

            sb.AppendLine($"NEON_CLUSTER_PROVISIONER={clusterDefinition.Provisioner}");
            sb.AppendLine($"NEON_CLUSTER={clusterDefinition.Name}");
            sb.AppendLine($"NEON_DATACENTER={clusterDefinition.Datacenter.ToLowerInvariant()}");
            sb.AppendLine($"NEON_ENVIRONMENT={clusterDefinition.Environment.ToString().ToLowerInvariant()}");

            var sbPackageProxies = new StringBuilder();

            foreach (var proxyEndpoint in clusterDefinition.PackageProxy.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                sbPackageProxies.AppendWithSeparator(proxyEndpoint);
            }
            
            sb.AppendLine($"NEON_PACKAGE_PROXY={sbPackageProxies}");

            if (clusterDefinition.Hosting != null)
            {
                sb.AppendLine($"NEON_HOSTING={clusterDefinition.Hosting.Environment.ToMemberString().ToLowerInvariant()}");
            }

            sb.AppendLine($"NEON_NODE_NAME={node.Name}");

            if (node.Metadata != null)
            {
                sb.AppendLine($"NEON_NODE_ROLE={node.Metadata.Role}");
                sb.AppendLine($"NEON_NODE_IP={node.Metadata.PrivateAddress}");
                sb.AppendLine($"NEON_NODE_HDD={node.Metadata.Labels.StorageHDD.ToString().ToLowerInvariant()}");
            }

            sb.AppendLine($"NEON_ARCHIVE_FOLDER={KubeHostFolders.Archive}");
            sb.AppendLine($"NEON_BIN_FOLDER={KubeHostFolders.Bin}");
            sb.AppendLine($"NEON_CONFIG_FOLDER={KubeHostFolders.Config}");
            sb.AppendLine($"NEON_EXEC_FOLDER={KubeHostFolders.Exec}");
            sb.AppendLine($"NEON_SETUP_FOLDER={KubeHostFolders.Setup}");
            sb.AppendLine($"NEON_STATE_FOLDER={KubeHostFolders.State}");
            sb.AppendLine($"NEON_TMPFS_FOLDER={KubeHostFolders.Tmpfs}");

            // Kubernetes related variables for masters.

            if (node.Metadata.IsMaster)
            {
                sb.AppendLine($"KUBECONFIG=/etc/kubernetes/admin.conf");
            }

            // Upload the new environment to the server.

            node.UploadText("/etc/environment", sb, tabStop: 4);
        }

        /// <summary>
        /// Initializes a near virgin server with the basic capabilities required
        /// for a cluster host node.
        /// </summary>
        /// <param name="node">The target cluster node.</param>
        /// <param name="clusterDefinition">The cluster definition.</param>
        /// <param name="kubeSetupInfo">Kubernetes setup details.</param>
        /// <param name="shutdown">Optionally shuts down the node.</param>
        public static void PrepareNode(SshProxy<NodeDefinition> node, ClusterDefinition clusterDefinition, KubeSetupInfo kubeSetupInfo, bool shutdown = false)
        {
            Covenant.Requires<ArgumentNullException>(node != null);
            Covenant.Requires<ArgumentNullException>(clusterDefinition != null);
            Covenant.Requires<ArgumentNullException>(kubeSetupInfo != null);

            if (node.FileExists($"{KubeHostFolders.State}/setup/prepared"))
            {
                return;     // Already prepared
            }

            //-----------------------------------------------------------------
            // Ensure that the cluster host folders exist.

            node.CreateHostFolders();

            //-----------------------------------------------------------------
            // Package manager configuration.

            if (!clusterDefinition.NodeOptions.AllowPackageManagerIPv6)
            {
                // Restrict the [apt] package manager to using IPv4 to communicate
                // with the package mirrors, since IPv6 often doesn't work.
                
                node.UploadText("/etc/apt/apt.conf.d/99-force-ipv4-transport", "Acquire::ForceIPv4 \"true\";");
                node.SudoCommand("chmod 644 /etc/apt/apt.conf.d/99-force-ipv4-transport");
            }

            // Configure [apt] to retry.

            node.UploadText("/etc/apt/apt.conf.d/99-retries", $"APT::Acquire::Retries \"{clusterDefinition.NodeOptions.PackageManagerRetries}\";");
            node.SudoCommand("chmod 644 /etc/apt/apt.conf.d/99-retries");

            //-----------------------------------------------------------------
            // Other configuration.

            ConfigureOpenSSH(node, TimeSpan.Zero);
            node.UploadConfigFiles(clusterDefinition, kubeSetupInfo);
            node.UploadResources(clusterDefinition, kubeSetupInfo);

            if (clusterDefinition != null)
            {
                ConfigureEnvironmentVariables(node, clusterDefinition);
            }

            node.SudoCommand("safe-apt-get update");

            node.InvokeIdempotentAction("setup/prep-node",
                () =>
                {
                    node.Status = "preparing";
                    node.SudoCommand("setup-prep.sh");
                    node.Reboot(wait: true);
                });

            // We need to upload the cluster configuration and initialize drives attached 
            // to the node.  We're going to assume that these are not already initialized.

            // $todo(jeff.lill): 
            //
            // We may need an option that allows an operator to pre-build a hardware
            // based drive array or something.  I'm going to defer this to later and
            // concentrate on commodity hardware and cloud deployments for now. 

            CommonSteps.ConfigureEnvironmentVariables(node, clusterDefinition);

            node.Status = "setup: disk";
            node.SudoCommand("setup-disk.sh");

            // Clear any DHCP leases to be super sure that cloned node
            // VMs will obtain fresh IP addresses.

            node.Status = "clear: DHCP leases";
            node.SudoCommand("rm -f /var/lib/dhcp/*");

            // Indicate that the node has been fully prepared.

            node.SudoCommand($"touch {KubeHostFolders.State}/setup/prepared");

            // Shutdown the node if requested.

            if (shutdown)
            {
                node.Status = "shutdown";
                node.SudoCommand("shutdown 0", RunOptions.Defaults | RunOptions.Shutdown);
            }
        }
    }
}
