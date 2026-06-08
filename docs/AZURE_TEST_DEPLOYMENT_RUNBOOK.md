# Sisonke Azure Test Deployment Runbook

## Purpose

Prepare Sisonke for secure test hosting on Azure App Service using Azure SQL Database.

This is for testing only, not production launch.

---

## Target Architecture

- Azure Resource Group
- Azure App Service Plan
- Azure App Service
- Azure SQL Database
- Azure Key Vault (later)
- Application Insights
- Managed Identity
- Custom domain (later): `app.sisonkestokvel.co.za` or `test.sisonkestokvel.co.za`

---

## Environment Strategy

**Local development**

- SQLite
- `appsettings.Development.json` supplies `ConnectionStrings:DefaultConnection`
- `ASPNETCORE_ENVIRONMENT = Development`

**Azure test**

- Azure SQL Database
- Connection string stored in Azure App Service Configuration
- `ASPNETCORE_ENVIRONMENT = Staging`

---

## Required Azure App Settings

Set the following in **Azure App Service → Configuration → Application Settings**:

```
ASPNETCORE_ENVIRONMENT = Staging
ConnectionStrings__DefaultConnection = <Azure SQL connection string>
```

> Double underscores (`__`) are the Azure App Service convention for nested config keys.

---

## Security Rules

- Do not put Azure SQL passwords in `appsettings.json`.
- Do not commit secrets or connection strings to source control.
- Use Azure App Service Configuration or Key Vault for all secrets.
- Enable **HTTPS Only** on App Service.
- Set minimum TLS to **1.2 or higher**.
- Enable **Managed Identity** on App Service.
- Restrict Azure SQL firewall to App Service outbound IPs only.
- Do not expose debug exception pages in the hosted test environment (`ASPNETCORE_ENVIRONMENT` must not be `Development` on Azure).
- Do not deploy using production credentials.
- Do not invite real users until the test environment is fully verified.

---

## Azure Resource Naming

Recommended names for the test environment:

| Resource | Name |
|---|---|
| Resource Group | `rg-sisonke-test-za` |
| App Service Plan | `asp-sisonke-test-za` |
| App Service | `app-sisonke-test-za` |
| SQL Server | `sql-sisonke-test-za` |
| SQL Database | `sqldb-sisonke-test` |
| Key Vault | `kv-sisonke-test-za` |
| Application Insights | `ai-sisonke-test-za` |
| Region | South Africa North |

---

## Deployment Sequence

1. Create Azure Resource Group.
2. Create Azure App Service Plan (at least B1 tier).
3. Create Azure App Service (`.NET 10`, Linux or Windows).
4. Enable **Managed Identity** on App Service.
5. Create Azure SQL Server and Database.
6. Configure Azure SQL firewall to allow App Service outbound IPs and your developer IP for migration.
7. Add App Service configuration values (`ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__DefaultConnection`).
8. Apply EF Core migrations to Azure SQL (see Migration Command below).
9. Publish and deploy the application.
10. Test the Azure App Service default URL (`https://app-sisonke-test-za.azurewebsites.net`).
11. Add custom domain only after the default URL works end-to-end.
12. Bind HTTPS certificate to custom domain.
13. Enable **HTTPS Only** and remove HTTP bindings.

---

## Migration Command Example

Run from your local machine against the Azure SQL database.  
**Do not commit the connection string.**

```powershell
cd C:\Users\mpshe\Projects\Sisonke\Sisonke.Web

$env:ASPNETCORE_ENVIRONMENT = "Staging"
$env:ConnectionStrings__DefaultConnection = "<Azure SQL connection string>"

dotnet ef database update
```

After the command completes, clear the environment variables:

```powershell
Remove-Item Env:ConnectionStrings__DefaultConnection
```

---

## Core Hosted Test Cases

After deployment, verify the following flows end-to-end:

- Login and logout
- My Workspace
- Member Home
- Stokvel Dashboard
- Claims — create, submit, secretary review, chairperson approval/rejection, treasurer payout
- Claim eligibility assessment
- Contributions — capture, statement
- Member financial statement
- Meetings — create, agenda, attendance, apologies
- Meeting minutes — generate, edit, submit, approve
- Voting — create poll, cast votes, close
- Operating rules
- Attendance warnings — generate, acknowledge, resolve

---

## Go / No-Go Checklist

### Tenant and Admin

- [ ] MFA or Security Defaults enabled on Azure tenant
- [ ] Admin accounts protected with strong credentials
- [ ] No shared admin accounts
- [ ] Least privilege roles applied

### App Service

- [ ] HTTPS Only enabled
- [ ] Minimum TLS 1.2+
- [ ] Managed Identity enabled
- [ ] `ASPNETCORE_ENVIRONMENT` set to `Staging`
- [ ] No secrets in source code or `appsettings.json`
- [ ] App settings configured in Azure App Service Configuration

### Database

- [ ] Azure SQL created and accessible
- [ ] Firewall restricted to App Service IPs (and developer IP for migration only)
- [ ] Backups enabled
- [ ] EF Core migrations applied successfully
- [ ] Connection string not committed to source control

### Domain

- [ ] Default Azure URL (`*.azurewebsites.net`) works first
- [ ] DNS configured only after default URL verified
- [ ] HTTPS certificate bound to custom domain
- [ ] HTTP redirects to HTTPS

### Monitoring

- [ ] Application Insights connected and receiving telemetry
- [ ] Live log stream accessible in Azure portal
- [ ] Errors reviewed after initial deployment before sharing URL
