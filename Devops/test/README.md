# Hive provisioning and management.

This folder holds the hive definitions, scripts and other files required to provision and manage [TEST] hive.

The `hive` folder holds the possible target hive definitions.  Each of these folders also hold the encrypted `secrets.yaml` and unencrypted `vars.yaml` Ansible files for the cluster.

## Secrets

Hive secrets are saved to `secrets.yaml` as an Ansible compatible variables file.  This file is encrypted at rest and when committed to the source repoisitory via Ansible encryption using the **neon-git** password managed by **neon-cli**.

You can view and edit this file via:

&nbsp;&nbsp;&nbsp;&nbsp;`neon file view secrets.yaml neon-git`
&nbsp;&nbsp;&nbsp;&nbsp;`neon file edit secrets.yaml neon-git`

**WARNING:** You must take care never to commit this file when it is decrypted.

Do not edit this file in Visual Studio or Nodepad.  Use the `neon file edit secrets.yaml tarikino-git` command instead to ensure that it is always encypted at rest.

This file defines the variables described below.  These variables can be referenced as standard global Ansible variables in playbooks or as environment variables in scripts executed via `neon run...`.

## Standard DEVOPS Credentials

`DEVOPS_USERNAME`
`DEVOPS_PASSWORD`

## Default credentials for virtual machine templates. 

`HIVE_NODE_TEMPLATE_USERNAME`
`HIVE_NODE_TEMPLATE_PASSWORD`

## XenServer host credentials.

`HIVE_XENSERVER_USERNAME`
`HIVE_XENSERVER_PASSWORD`

## NuGet credentials

`NUGET_API_KEY`

## Azure credentials

`AZURE_APPLICATIONID`
`AZURE_PASSWORD`
`AZURE_SUBSCRIPTIONID`
`AZURE_TENANTID`

## Neon Docker registry secret (used for anti-tampering)

`REGISTRY_SECRET`

## TLS Certificates

`_neonforge_net_pem` - **neonforge.net** (wildcard)

# Hive setup

The `setup-all.ps1` script provisions and completely configures the hive from a bare Hypervisor, XenServer for production and Hyper-V for development.  You'll need to pass the hive name, like:

&nbsp;&nbsp;&nbsp;&nbsp;`powershell -file setup-all.ps1 home-small`