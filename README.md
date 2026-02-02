KNQA Self-Service Portal

A comprehensive Employee Self-Service Portal built with Blazor and .NET 8 for the Kenya National Qualifications Authority (KNQA).
The system integrates with Microsoft Dynamics 365 Business Central to streamline leave management, employee information access, and HR workflows.

🚀 Features
📋 Leave Management

Create Leave Applications – Submit new leave requests with full details

Edit / Delete Applications – Modify or cancel pending leave requests

Leave Balance Tracking – Real-time leave balance by leave type

Multiple Leave Types – Annual, Sick, Maternity, Study, etc.

Approval Workflow – Track status (Open, Approved, Rejected)

Duty Handover – Assign responsibilities during leave periods

👥 Employee Management

Employee Directory – View active employees

Profile Management – Access personal and employment details

Department Information – View organizational structure

🎨 User Experience

Responsive Design – Optimized for desktop and mobile

Modern UI – Built using MudBlazor Material components

Real-time Updates – Live notifications and status updates

Search & Filters – Advanced filtering for leave applications

Visual Indicators – Color-coded leave status and balances

🔒 Security & Integration

Business Central Integration – Real-time sync with Dynamics 365 BC

Role-Based Access Control (RBAC)

Authentication – Integrated with KNQA identity systems

Data Validation – Client-side and server-side validation

🛠️ Technology Stack
Frontend

Blazor WebAssembly (.NET 8)

MudBlazor – UI component library

C# – Full-stack development

CSS – Custom KNQA branding

Backend

.NET 8 Web API

Entity Framework Core (optional local persistence)

HttpClient – Business Central integration

JWT Authentication

Integration

Microsoft Dynamics 365 Business Central

OData APIs – Read operations

REST APIs – Create / update operations

DevOps
🔧 Setup & Installation
Prerequisites

.NET 8 SDK

Visual Studio 2022+ or VS Code

Business Central Sandbox or Production environment

Git

Installation Steps
1️⃣ Clone the Repository
git clone https://github.com/knqa/self-service-portal.git
cd self-service-portal

2️⃣ Configure Business Central Integration

Create appsettings.json or appsettings.Development.json:

{
  "BusinessCentral": {
    "BaseUrl": "https://api.businesscentral.dynamics.com",
    "CompanyName": "KNQA",
    "Username": "api-user",
    "Password": "api-password"
  }
}


⚠️ Use environment variables or Azure Key Vault in production.

3️⃣ Build and Run
dotnet restore
dotnet build
dotnet run

4️⃣ Access the Portal

Open your browser at:

https://localhost:5001


Login using employee credentials.

🚀 Deployment
Azure App Service
dotnet publish -c Release
az webapp up --name knqa-portal --resource-group knqa-rg

Docker Deployment
# Build image
docker build -t knqa-self-service .

# Run container
docker run -p 8080:80 knqa-self-service

📊 Business Central Integration

The portal integrates with Business Central using:

OData Web Services – Read operations

REST APIs – Create and update operations

Custom APIs – Extended business logic

ETag Concurrency – Optimistic locking

Key Endpoints

Leave_Applications_List – Leave management

Employees – Employee directory

LeaveTypes – Leave configuration

EmployeeLeaveBalance – Leave balances

🎯 Key Benefits
For Employees

✅ 24/7 access to leave information
✅ Simplified leave application process
✅ Real-time leave balance tracking
✅ Mobile-friendly experience
✅ Reduced paperwork

For HR

✅ Automated workflows
✅ Reduced administrative workload
✅ Improved compliance
✅ Real-time reporting

For KNQA

✅ Increased productivity
✅ Improved employee satisfaction
✅ Digital transformation
✅ Scalable and secure solution

🔐 Security Features

Role-Based Access Control (RBAC)

JWT Authentication

HTTPS Enforcement

Input Validation & Sanitization

API Rate Limiting

Audit Logging

📈 Performance

Blazor WebAssembly for fast client rendering

API caching to reduce BC calls

Lazy loading for optimized resources

GZIP compression enabled

GitHub – Source control

Docker – Containerization
