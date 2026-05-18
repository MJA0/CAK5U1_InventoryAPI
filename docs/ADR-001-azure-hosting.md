# ADR-001: Choice of Azure-Service for container-hosting

## Status
Accepted

## Context
An Azure service is required to run the containerized .NET 9 API. The two main options are Azure App Service and Azure Container Apps.

## Decision
Choosing **Azure Container Apps** 

## Reasoning
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

## Consequences
- Managed Identity is assigned directly to the Container App, meaning no credentials or secrets are needed to authenticate with Azure services.
- Scaling to zero keeps costs low but introduces cold starts, meaning the first request after a period of inactivity will be slower while the container starts up.
- Images are stored in Azure Container Registry and pulled directly into Container Apps, keeping everything within the Azure ecosystem.
