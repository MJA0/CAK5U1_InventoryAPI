# CAK5U1_InventoryAPI

## CloudNative Inventory API
A containerized .NET 9 Web API deployed to Azure Container Apps with secure configuration management via Azure Key Vault and automated CI/CD via GitHub Actions. The API manages a product inventory and demonstrates cloud-native DevOps principles including containerization, secret management, and automated deployment.

## Azure Services Used
- **Azure Container Registry (ACR)** - stores and serves Docker images used in deployment
- **Azure Container Apps** - hosts the containerized API in a serverless, scalable environment that scales to zero when idle
- **Azure Key Vault** - stores secrets securely so no sensitive values ever appear in code or version-controlled files
- **Managed Identity** - allows the Container App to authenticate with Key Vault automatically without any stored credentials

---

## 1. Running the API Locally

Clone the repo:

    git clone https://github.com/YOUR_USERNAME/CAK5U1_InventoryAPI.git
    cd CAK5U1_InventoryAPI

### **Via Dotnet**
Run with dotnet:

    cd CloudNativeInventory.Api
    dotnet run

The terminal will print the port the API is listening on, for example:

    Now listening on: http://localhost:5000
    
Test the endpoint in a browser or PowerShell with correct port:

    Invoke-RestMethod http://localhost:5000/api/inventory

Expected response:

    id  name    stockQuantity  price
    --  ----    -------------  -----
    1   Laptop  10             9999

### **Via Docker**
To run via Docker, make sure Docker Desktop is running first, then from the solution root:

    docker build -t cloudnativeinventory .
    docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development cloudnativeinventory

Test:

    Invoke-RestMethod http://localhost:8080/api/inventory

Expected response:

    id  name    stockQuantity  price
    --  ----    -------------  -----
    1   Laptop  10             9999
    
*Note: ASPNETCORE_ENVIRONMENT=Development is required locally to skip Key Vault initialization. Without it the app will crash on startup because KeyVaultUrl is not configured locally.^*
    
---

## 2. CI/CD Pipeline

The pipeline is defined in .github/workflows/main.yml and triggers automatically on every push to main or on pull requests.

### Job 1 - build-and-test:
*(runs on every push and PR)*
1. dotnet restore - restores all NuGet packages
2. dotnet build - compiles the solution
3. dotnet test - runs all unit tests, pipeline stops here if any test fails

### Job 2 - build-and-deploy:
*(only runs on main branch if Job 1 passes)*
1. Logs in to Azure Container Registry using stored credentials
2. Builds the Docker image using the multi-stage Dockerfile
3. Tags the image with the commit SHA for full traceability and also as latest
4. Pushes both tags to ACR
5. Logs in to Azure using the service principal credentials
6. Deploys the new image to Azure Container Apps creating a new revision

This design ensures that broken code or failing tests can never reach production.

Required GitHub Secrets:

| Secret | Description |
|---|---|
| ACR_USERNAME | Azure Container Registry username |
| ACR_PASSWORD | Azure Container Registry password |
| AZURE_CREDENTIALS | Service principal credentials as JSON containing clientId, clientSecret, subscriptionId and tenantId |

Non-sensitive variables and values hardcoded in workflow env block:

| Variable | Value |
|---|---|
| AZURE_CONTAINER_REGISTRY | acrcak5u1dev |
| CONTAINER_APP_NAME | ca-cak5u1-api |
| RESOURCE_GROUP | rg-adcha-dev |

---

## 3. Deploy and Verification

Deployment is fully automatic: push to main, tests pass, image builds and deploys. No manual steps required.

The Container App runs with these environment variables set in Azure:

| Variable | Value |
|---|---|
| KeyVaultUrl | https://kv-cak5u1-dev.vault.azure.net/ |
| ASPNETCORE_ENVIRONMENT | Production |

After deploy, verify that the API is running:

    Invoke-RestMethod https://ca-cak5u1-api.jollyground-ef37c881.polandcentral.azurecontainerapps.io/api/inventory

Expected response if the APIT is working correctly:

    id  name    stockQuantity  price
    --  ----    -------------  -----
    1   Laptop  10             9999

Verify that the secret is loaded securely from Key Vault and not from local config:

    Invoke-RestMethod https://ca-cak5u1-api.jollyground-ef37c881.polandcentral.azurecontainerapps.io/api/inventory/system/verify-integration

Expected response when Key Vault is working correctly:

    status  message
    ------  -------
    Secured Hemlighet laddades framgångsrikt via söker konfiguration.

*If it returns HTTP 500 with status Unsecured, the secret is not being loaded from Key Vault. Check that the Managed Identity has the Key Vault Secrets User role and that KeyVaultUrl is set correctly on the Container App.*

---

## 4. Architecture Decision Record

See docs/ADR-001-azure-hosting.md for full documentation of decisions made, including:
- Why Azure Container Apps was chosen over App Service
- Why Azure Container Registry was used for image storage
- How the CI/CD pipeline is designed and why tests act as a deployment gate
- How Managed Identity and RBAC replace hardcoded credentials
- How secrets are managed so no sensitive values appear in code or Git history
- Why the container runs as a non-root user and what the multi-stage build achieves
