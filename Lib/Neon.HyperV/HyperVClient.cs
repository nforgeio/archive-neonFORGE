﻿//-----------------------------------------------------------------------------
// FILE:	    HyperVClient.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Windows;

using Newtonsoft.Json.Linq;

namespace Neon.HyperV
{
    /// <summary>
    /// <para>
    /// Abstracts management of local Hyper-V virtual machines and components
    /// on Windows via PowerShell.
    /// </para>
    /// <note>
    /// This class requires elevated administrative rights.
    /// </note>
    /// </summary>
    /// <threadsafety instance="false"/>
    public class HyperVClient : IDisposable
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Returns the path to the default Hyper-V virtual drive folder.
        /// </summary>
        public static string DefaultDriveFolder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Hyper-V", "Virtual hard disks"); }
        }

        //---------------------------------------------------------------------
        // Instance members

        private PowerShell      powershell;

        /// <summary>
        /// Default constructor to be used to manage Hyper-V objects
        /// on the local Windows machine.
        /// </summary>
        public HyperVClient()
        {
            if (!NeonHelper.IsWindows)
            {
                throw new NotSupportedException($"{nameof(HyperVClient)} is only supported on Windows.");
            }

            powershell = new PowerShell();
        }

        /// <summary>
        /// Releases all resources associated with the instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases all associated resources.
        /// </summary>
        /// <param name="disposing">Pass <c>true</c> if we're disposing, <c>false</c> if we're finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (powershell != null)
                {
                    powershell.Dispose();
                    powershell = null;
                }

                GC.SuppressFinalize(this);
            }

            powershell = null;
        }

        /// <summary>
        /// Ensures that the instance has not been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
        private void CheckDisposed()
        {
            if (powershell == null)
            {
                throw new ObjectDisposedException(nameof(HyperVClient));
            }
        }

        /// <summary>
        /// Extracts virtual machine properties from a dynamic PowerShell result.
        /// </summary>
        /// <param name="rawMachine">The dynamic machine properties.</param>
        /// <returns>The parsed <see cref="VirtualMachine"/>.</returns>
        private VirtualMachine ExtractVM(dynamic rawMachine)
        {
            var vm = new VirtualMachine();

            vm.Name = rawMachine.Name;

            switch ((string)rawMachine.State)
            {
                case "Off":

                    vm.State = VirtualMachineState.Off;
                    break;

                case "Starting":

                    vm.State = VirtualMachineState.Starting;
                    break;

                case "Running":

                    vm.State = VirtualMachineState.Running;
                    break;

                case "Paused":

                    vm.State = VirtualMachineState.Paused;
                    break;

                case "Saved":

                    vm.State = VirtualMachineState.Saved;
                    break;

                default:

                    vm.State = VirtualMachineState.Unknown;
                    break;
            }

            return vm;
        }

        /// <summary>
        /// Creates a virtual machine. 
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        /// <param name="memorySize">
        /// A string specifying the memory size.  This can be a long byte count or a
        /// byte count or a number with units like <b>512MB</b>, <b>0.5GB</b>, <b>2GB</b>, 
        /// or <b>1TB</b>.  This defaults to <b>2GB</b>.
        /// </param>
        /// <param name="minimumMemorySize">
        /// Optionally specifies the minimum memory size.  This defaults to <c>null</c> which will
        /// set this to <paramref name="memorySize"/>.
        /// </param>
        /// <param name="processorCount">
        /// The number of virutal processors to assign to the machine.  This defaults to <b>4</b>.
        /// </param>
        /// <param name="diskSize">
        /// A string specifying the primary disk size.  This can be a long byte count or a
        /// byte count or a number with units like <b>512MB</b>, <b>0.5GB</b>, <b>2GB</b>, 
        /// or <b>1TB</b>.  This defaults to <b>64GB</b>.
        /// </param>
        /// <param name="drivePath">
        /// Optionally specifies the path where the virtual hard drive will be located.  Pass 
        /// <c>null</c> or empty to default to <b>MACHINE-NAME.vhdx</b> located in the default
        /// Hyper-V virtual machine drive folder.
        /// </param>
        /// <param name="checkpointDrives">Optionally enables drive checkpoints.  This defaults to <c>false</c>.</param>
        /// <param name="templateDrivePath">
        /// If this is specified and <paramref name="drivePath"/> is not <c>null</c> then
        /// the hard drive template at <paramref name="templateDrivePath"/> will be copied
        /// to <paramref name="drivePath"/> before creating the machine.
        /// </param>
        /// <param name="switchName">Optional name of the virtual switch.</param>
        /// <param name="extraDrives">
        /// Optionally specifies any additional virtual drives to be created and 
        /// then attached to the new virtual machine (e.g. for Ceph OSD).
        /// </param>
        /// <remarks>
        /// <note>
        /// The <see cref="VirtualDrive.Path"/> property of <paramref name="extraDrives"/> may be
        /// passed as <c>null</c> or empty.  In this case, the drive name will default to
        /// being located in the standard Hyper-V virtual drivers folder and will be named
        /// <b>MACHINE-NAME-#.vhdx</b>, where <b>#</b> is the one-based index of the drive
        /// in the enumeration.
        /// </note>
        /// </remarks>
        public void AddVM(
            string                      machineName, 
            string                      memorySize        = "2GB", 
            string                      minimumMemorySize = null, 
            int                         processorCount    = 4,
            string                      diskSize          = "64GB",
            string                      drivePath         = null,
            bool                        checkpointDrives  = false,
            string                      templateDrivePath = null, 
            string                      switchName        = null,
            IEnumerable<VirtualDrive>   extraDrives       = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(machineName));
            CheckDisposed();

            if (string.IsNullOrEmpty(minimumMemorySize))
            {
                minimumMemorySize = memorySize;
            }

            var driveFolder = DefaultDriveFolder;

            if (string.IsNullOrEmpty(drivePath))
            {
                drivePath = Path.Combine(driveFolder, $"{machineName}-[0].vhdx");
            }
            else
            {
                driveFolder = Path.GetDirectoryName(Path.GetFullPath(drivePath));
            }

            if (VMExists(machineName))
            {
                throw new HyperVException($"Virtual machine [{machineName}] already exists.");
            }

            // Copy the template VHDX file.

            if (templateDrivePath != null)
            {
                File.Copy(templateDrivePath, drivePath);
            }

            // Resize the VHDX.

            // $hack(jeff.lill):
            //
            // For some reason, the PowerShell [Resize-VHD] command does not like 
            // hard disk file names formatted as we're doing (e.g. with embedded
            // square brackets).  We're going to work around this by temporarily
            // renaming the disk file while we're resizing it.

            var tempDrivePath = Path.Combine(driveFolder, Guid.NewGuid().ToString("D") + ".vhdx");

            File.Move(drivePath, tempDrivePath);

            try
            {
                powershell.Execute($"Resize-VHD -Path \"{tempDrivePath}\" -SizeBytes {diskSize}");
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }
            finally
            {
                // Restore the drive to its original file name.

                File.Move(tempDrivePath, drivePath);
            }

            // Create the virtual machine.

            var command = $"New-VM -Name \"{machineName}\" -MemoryStartupBytes {minimumMemorySize} -Generation 1";

            if (!string.IsNullOrEmpty(drivePath))
            {
                command += $" -VHDPath \"{drivePath}\"";
            }

            if (!string.IsNullOrEmpty(switchName))
            {
                command += $" -SwitchName \"{switchName}\"";
            }

            try
            {
                powershell.Execute(command);
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }

            // We need to configure the VM's processor count and min/max memory settings.

            try
            {
                powershell.Execute($"Set-VM -Name \"{machineName}\" -ProcessorCount {processorCount} -MemoryMinimumBytes {minimumMemorySize} -MemoryMaximumBytes {memorySize}");
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }

            // Create and attach any additional drives as required.

            if (extraDrives != null)
            {
                var diskNumber = 1;

                foreach (var drive in extraDrives)
                {
                    if (string.IsNullOrEmpty(drive.Path))
                    {
                        drive.Path = Path.Combine(driveFolder, $"{machineName}-[{diskNumber}].vhdx");
                    }

                    if (drive.Size <= 0)
                    {
                        throw new ArgumentException("Virtual drive size must be greater than 0.");
                    }

                    if (File.Exists(drive.Path))
                    {
                        File.Delete(drive.Path);
                    }

                    var fixedOrDynamic = drive.IsDynamic ? "-Dynamic" : "-Fixed";

                    try
                    {
                        powershell.Execute($"New-VHD -Path \"{drive.Path}\" {fixedOrDynamic} -SizeBytes {drive.Size} -BlockSizeBytes 1MB");
                        powershell.Execute($"Add-VMHardDiskDrive -VMName \"{machineName}\" -Path \"{drive.Path}\"");
                    }
                    catch (Exception e)
                    {
                        throw new HyperVException(e.Message, e);
                    }

                    diskNumber++;
                }
            }

            // Windows 10 releases since the August 2017 Creators Update enable automatic
            // virtual drive checkpointing (which is annoying).

            if (!checkpointDrives)
            {
                try
                {
                    powershell.Execute($"Set-VM -CheckpointType Disabled -Name \"{machineName}\"");
                }
                catch (Exception e)
                {
                    throw new HyperVException(e.Message, e);
                }
            }
        }

        /// <summary>
        /// Removes a named virtual machine.
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        public void RemoveVM(string machineName)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(machineName));
            CheckDisposed();

            var machine = GetVM(machineName);
            var drives  = GetVMDrives(machineName);

            // Remove the machine along with any of of its virtual hard drive files.

            try
            {
                powershell.Execute($"Remove-VM -Name \"{machineName}\" -Force");
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }

            foreach (var drivePath in drives)
            {
                File.Delete(drivePath);
            }
        }

        /// <summary>
        /// Lists the virtual machines.
        /// </summary>
        /// <returns><see cref="IEnumerable{VirtualMachine}"/>.</returns>
        public IEnumerable<VirtualMachine> ListVMs()
        {
            CheckDisposed();

            try
            {
                var machines = new List<VirtualMachine>();
                var table    = powershell.ExecuteJson("Get-VM");

                foreach (dynamic rawMachine in table)
                {
                    machines.Add(ExtractVM(rawMachine));
                }

                return machines;
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }

        }

        /// <summary>
        /// Gets the current status for a named virtual machine.
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        /// <returns>The <see cref="VirtualMachine"/>.</returns>
        public VirtualMachine GetVM(string machineName)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(machineName));
            CheckDisposed();

            try
            {
                var machines = new List<VirtualMachine>();
                var table    = powershell.ExecuteJson($"Get-VM -Name \"{machineName}\"");

                Covenant.Assert(table.Count == 1);

                return ExtractVM(table.First());
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }
        }

        /// <summary>
        /// Determines whether a named virtual machine exists.
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        /// <returns><c>true</c> if the machine exists.</returns>
        public bool VMExists(string machineName)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(machineName));
            CheckDisposed();

            return ListVMs().Count(vm => vm.Name.Equals(machineName, StringComparison.InvariantCultureIgnoreCase)) > 0;
        }

        /// <summary>
        /// Starts the named virtual machine.
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        public void StartVM(string machineName)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(machineName));
            CheckDisposed();

            try
            {
                powershell.Execute($"Start-VM -Name \"{machineName}\"");
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }
        }

        /// <summary>
        /// Stops the named virtual machine.
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        public void StopVM(string machineName)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(machineName));
            CheckDisposed();

            try
            {
                powershell.Execute($"Stop-VM -Name \"{machineName}\"");
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }
        }

        /// <summary>
        /// Returns host file system paths to any virtual drives attached to
        /// the named virtual machine.
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        /// <returns>The list of fully qualified virtual drive file paths.</returns>
        public List<string> GetVMDrives(string machineName)
        {
            try
            {
                var drives    = new List<string>();
                var rawDrives = powershell.ExecuteJson($"Get-VMHardDiskDrive -VMName \"{machineName}\"");

                foreach (dynamic rawDrive in rawDrives)
                {
                    drives.Add(rawDrive.Path);
                }

                return drives;
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }

        }

        /// <summary>
        /// Returns the virtual network switches.
        /// </summary>
        /// <returns>The list of switches.</returns>
        public List<VirtualSwitch> ListVMSwitches()
        {
            try
            {
                var switches    = new List<VirtualSwitch>();
                var rawSwitches = powershell.ExecuteJson($"Get-VMSwitch");

                foreach (dynamic rawSwitch in rawSwitches)
                {
                    var virtualSwitch
                        = new VirtualSwitch()
                        {
                            Name = rawSwitch.Name
                        };

                    switch (rawSwitch.SwitchType)
                    {
                        case "Internal":

                            virtualSwitch.Type = VirtualSwitchType.Internal;
                            break;

                        case "External":

                            virtualSwitch.Type = VirtualSwitchType.External;
                            break;

                        case "Private":

                            virtualSwitch.Type = VirtualSwitchType.Private;
                            break;

                        default:

                            virtualSwitch.Type = VirtualSwitchType.Unknown;
                            break;
                    }

                    switches.Add(virtualSwitch);
                }

                return switches;
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }

        }

        /// <summary>
        /// Adds a virtual ethernet switch to Hyper-V with external connectivity
        /// to the ethernet adapter named <b>Ethernet</b>.
        /// </summary>
        /// <param name="switchName">The new switch name.</param>
        /// <param name="gateway">Address of the hive network gateway, used to identify a connected network interface.</param>
        public void NewVMExternalSwitch(string switchName, IPAddress gateway)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(switchName));
            Covenant.Requires<ArgumentNullException>(gateway != null);

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                throw new HyperVException($"No network connection detected.  Hyper-V provisioning requires a connected network.");
            }

            // We're going to look for an active (non-loopback) interface that is configured
            // to use the correct upstream gateway and also has at least one nameserver.

            // $todo(jeff.lill):
            //
            // This may be a problem for machines with multiple active network interfaces
            // because I may choose the wrong one (e.g. the slower card).  It might be
            // useful to have an optional hive node definition property the explicitly
            // specifies the adapter to use for a given node.
            //
            // Another problem we'll see is for laptops with wi-fi adapters.  Lets say we
            // setup a hive when wi-fi is connected and then the user docks the laptop,
            // connecting to a new wired adapter.  The hive's virtual switch will still
            // be configured to use the wi-fi adapter.  The only workaround for this is
            // probably for the user to modify the virtual switch.
            //
            // This last issue is really just another indication that neonHIVEs aren't 
            // really portable in the sense that you can't expect to relocate a hive 
            // from one network environment to another (that's why we bought the portable 
            // routers for motel use). So we'll consider this as by design.

            var connectedAdapter = (NetworkInterface)null;

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                var nicProperties = nic.GetIPProperties();

                if (nicProperties.DnsAddresses.Count > 0 &&
                    nicProperties.GatewayAddresses.Count(nicGateway => nicGateway.Address.Equals(gateway)) > 0)
                {
                    connectedAdapter = nic;
                    break;
                }
            }

            if (connectedAdapter == null)
            {
                throw new HyperVException($"Cannot identify a connected network adapter.");
            }

            try
            {
                var adapters      = powershell.ExecuteJson($"Get-NetAdapter");
                var targetAdapter = (string)null;

                foreach (dynamic adapter in adapters)
                {
                    if (((string)adapter.Name).Equals(connectedAdapter.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        targetAdapter = adapter.Name;
                        break;
                    }
                }

                if (targetAdapter == null)
                {
                    throw new HyperVException($"Internal Error: Cannot identify a connected network adapter.");
                }

                powershell.Execute($"New-VMSwitch -Name \"{switchName}\" -NetAdapterName \"{targetAdapter}\"");
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }
        }

        /// <summary>
        /// Returns the virtual network adapters attached to the named virtual machine.
        /// </summary>
        /// <param name="machineName">The machine name.</param>
        /// <param name="waitForAddresses">Optionally waits until at least one adapter has been able to acquire at least one IPv4 address.</param>
        /// <returns>The list of network adapters.</returns>
        public List<VirtualNetworkAdapter> ListVMNetworkAdapters(string machineName, bool waitForAddresses = false)
        {
            try
            {
                var stopwatch = new Stopwatch();

                while (true)
                {
                    var adapters    = new List<VirtualNetworkAdapter>();
                    var rawAdapters = powershell.ExecuteJson($"Get-VMNetworkAdapter -VMName \"{machineName}\"");

                    adapters.Clear();

                    foreach (dynamic rawAdapter in rawAdapters)
                    {
                        var adapter
                            = new VirtualNetworkAdapter()
                            {
                                Name           = rawAdapter.Name,
                                VMName         = rawAdapter.VMName,
                                IsManagementOs = ((string)rawAdapter.IsManagementOs).Equals("True", StringComparison.InvariantCultureIgnoreCase),
                                SwitchName     = rawAdapter.SwitchName,
                                MacAddress     = rawAdapter.MacAddress,
                                Status         = (string)((JArray)rawAdapter.Status).FirstOrDefault()
                            };

                        // Parse the IP addresses.

                        var addresses = (JArray)rawAdapter.IPAddresses;

                        if (addresses.Count > 0)
                        {
                            foreach (string address in addresses)
                            {
                                if (!string.IsNullOrEmpty(address))
                                {
                                    var ipAddress = IPAddress.Parse(address.Trim());

                                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                                    {
                                        adapter.Addresses.Add(IPAddress.Parse(address.Trim()));
                                    }
                                }
                            }
                        }

                        adapters.Add(adapter);
                    }

                    var retry = false;

                    foreach (var adapter in adapters)
                    {
                        if (adapter.Addresses.Count == 0 && waitForAddresses)
                        {
                            if (stopwatch.Elapsed >= TimeSpan.FromSeconds(30))
                            {
                                throw new TimeoutException($"Network adapter [{adapter.Name}] for virtual machine [{machineName}] was not able to acquire an IP address.");
                            }

                            retry = true;
                            break;
                        }
                    }

                    if (retry)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        continue;
                    }

                    return adapters;
                }
            }
            catch (Exception e)
            {
                throw new HyperVException(e.Message, e);
            }
        }
    }
}
