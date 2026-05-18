# ADR-001: Architecture Decisions for CloudNative Inventory API

## Status
Accepted

## Context
The Inventory API needs to be containerized, deployed to Azure, and secured. Several infrastructure and security decisions were made during this process. This document motivates those decisions.

---

## 1. Azure Hosting — Azure Container Apps vs App Service

### Decision
Choosing **Azure Container Apps**

### Reasoning
App Service
Operations: Requires an App Service Plan to manage.
Cost: You pay for the plan even with 0 traffic.
Security: Managed Identity supported.
Simplicity: More config needed.
Container fit: Primarily built for web apps.

Container Apps
Operations: Serverless, nothing to manage.
Cost: Scales to zero, pay per request.
Security: Managed Identity + RBAC built in.
Simplicity: Minimal CLI setup.
Container fit: Built specifically for containers.

Container Apps fits our needs better following these criterias.

### Consequences
- Managed Identity is assigned directly to the Container App, meaning no credentials or secrets are needed to authenticate with Azure services.
- Scaling to zero keeps costs low but introduces cold starts, meaning the first request after a period of inactivity will be slower while the container starts up.
- Images are stored in Azure Container Registry and pulled directly into Container Apps, keeping everything within the Azure ecosystem.

---

## 2. Container Registry — Azure Container Registry

### Decision
Azure Container Registry (ACR) was chosen to store and serve Docker images.

### Reasoning
- Integrates natively with Azure Container Apps using Managed Identity so no extra credentials are needed.
- Images stay within the Azure ecosystem, reducing latency compared to external registries like Docker Hub.
- Access is controlled via RBAC, consistent with the rest of the Azure security model.
- Supports tagging by commit SHA for full traceability.

### Consequences
- Images are private by default, only authorized identities can pull them.
- The Container App uses its Managed Identity with the AcrPull role to pull images without storing credentials.

---

## 3. CI/CD Pipeline Design

### Decision
GitHub Actions with two separate jobs: build-and-test and build-and-deploy.

### Reasoning
- Separating test and deploy into two jobs means a failing test physically blocks deployment, it is not possible to deploy broken code.
- Images are tagged with the commit SHA, making every deployment fully traceable back to the exact code that produced it.
- The entire process from push to running in production is automated with no manual steps.

### How this improves quality, traceability and delivery speed
- Quality: tests must pass before anything is deployed.
- Traceability: every image in ACR maps to an exact commit in Git.
- Delivery speed: push to main and the pipeline handles the rest automatically.

### Consequences
- A broken test on main will block all deployments until fixed.
- Every production deployment can be traced back to a specific commit.

---

## 4. Identity and Permissions — Managed Identity and RBAC

### Decision
System-Assigned Managed Identity on the Container App with the minimum required RBAC roles.

### Roles assigned
| Role | Scope | Reason |
|---|---|---|
| Key Vault Secrets User | kv-cak5u1-dev | Allows reading secrets, nothing else |
| AcrPull | acrcak5u1dev | Allows pulling images, nothing else |

### Reasoning
This follows the principle of least privilege — the Container App has only the permissions it needs and nothing more. Managed Identity means no passwords or credentials are stored anywhere, Azure handles authentication automatically.

### Consequences
- No credentials to rotate or leak.
- Access can be revoked instantly by removing the role assignment.
- The app cannot modify or delete secrets, only read them.

---

## 5. Secret Management Strategy

### Decision
All secrets are stored in Azure Key Vault. No sensitive values appear in code, environment variables passed via pipeline, or version-controlled files.

### How it works
- The secret ExternalServices--VendorApiKey lives only in Key Vault.
- The app reads it at startup via AddAzureKeyVault() using DefaultAzureCredential.
- The only configuration value in Azure is KeyVaultUrl, which is not sensitive since it is just a URL.
- Locally the app runs in Development mode, skipping Key Vault entirely.

### How we verified no secrets leak
- The repo contains no secrets and is verified by reviewing all committed files.
- GitHub Actions secrets are stored in GitHub's encrypted secrets store and never appear in pipeline logs.
- appsettings.json contains only the Key Vault URL, not the secret itself.

### Consequences
- Secrets can be rotated in Key Vault without any code changes or redeployment.
- If the repo is made public, no sensitive data is exposed.

---

## 6. Container Security — Rootless and Multi-Stage Build

### Decision
The Dockerfile uses a multi-stage build and runs the application as a non-root user.

### Multi-stage build
- Build stage: uses the full .NET SDK image to restore, build and publish the app.
- Runtime stage: uses only the slim ASP.NET runtime image, copying only the published output.

The final image contains no SDK, no source code, and no build tools. Only what is needed to run the app.

### Rootless container
The container creates a non-root user appuser and switches to it with USER appuser. If an attacker exploits a vulnerability in the API they only get access as a restricted user, they won't be able to write to system files, install software, or create new users. Without "USER appuser" the same attack would give full root access inside the container.

### Consequences
- Smaller, faster, more secure runtime image.
- Reduced blast radius if the application is compromised.
