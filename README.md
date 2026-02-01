KNQA Self-Service Portal
A comprehensive employee self-service portal built with Blazor and .NET for the Kenya National Qualifications Authority (KNQA). This portal integrates with Microsoft Dynamics 365 Business Central to provide seamless leave management, employee information access, and HR workflows.

🚀 Features
📋 Leave Management
Create Leave Applications: Submit new leave requests with detailed information

Edit/Delete Applications: Modify or cancel pending leave applications

Leave Balance Tracking: Real-time view of available leave days by type

Leave Type Management: Support for various leave types (Annual, Sick, Maternity, etc.)

Approval Workflow: Track application status (Open, Approved, Rejected)

Duty Handover: Assign duties to colleagues during leave periods

👥 Employee Management
Employee Directory: View active employees

Profile Management: Access personal information

Department Information: View organizational structure

🎨 User Experience
Responsive Design: Works on desktop and mobile devices

Modern UI: Built with MudBlazor components

Real-time Updates: Live status updates and notifications

Search & Filter: Advanced filtering for leave applications

Visual Indicators: Color-coded status and balance indicators

🔒 Security & Integration
Business Central Integration: Real-time sync with Dynamics 365 BC

Role-based Access: Secure employee data access

Authentication: Integrated with existing KNQA systems

Data Validation: Comprehensive form validation

🛠️ Technology Stack
Frontend
Blazor WebAssembly (.NET 8)

MudBlazor - Material Design component library

C# - Full-stack development

CSS - Custom styling with KNQA branding

Backend
.NET 8 Web API

Entity Framework Core (if using local database)

HttpClient - Business Central API integration

JWT Authentication - Secure API access

Integration
Microsoft Dynamics 365 Business Central - HR and leave management

OData API - Real-time data synchronization

RESTful Services - External system integration

DevOps
GitHub - Source control and collaboration

Docker - Containerization support

Azure - Deployment target (recommended)

📁 Project Structure
text
KNQASelfService/
├── Client/                 # Blazor WebAssembly frontend
│   ├── Pages/            # Application pages
│   │   ├── LeaveApplication.razor
│   │   └── EmployeeDirectory.razor
│   ├── Components/       # Reusable components
│   ├── Services/        # Client-side services
│   └── wwwroot/         # Static assets
├── Server/              # .NET Web API backend
│   ├── Controllers/     # API endpoints
│   ├── Services/        # Business logic
│   └── Models/         # Data models
├── Shared/             # Shared code
│   └── Models/        # Shared data models
└── Tests/              # Unit and integration tests
🔧 Setup & Installation
Prerequisites
.NET 8 SDK

Visual Studio 2022+ or VS Code

Business Central Sandbox/Production environment

Git

Installation Steps
Clone the repository

bash
git clone https://github.com/knqa/self-service-portal.git
cd self-service-portal
Configure Business Central Integration

Create appsettings.json or appsettings.Development.json

Add Business Central API credentials:

json
{
  "BusinessCentral": {
    "BaseUrl": "https://api.businesscentral.dynamics.com",
    "CompanyName": "KNQA",
    "Username": "api-user",
    "Password": "api-password"
  }
}
Build and Run

bash
dotnet restore
dotnet build
dotnet run
Access the Portal

Open browser to: https://localhost:5001

Login with employee credentials

🚀 Deployment
Azure App Service
bash
# Publish to Azure
dotnet publish -c Release
az webapp up --name knqa-portal --resource-group knqa-rg
Docker Deployment
bash
# Build Docker image
docker build -t knqa-self-service .

# Run container
docker run -p 8080:80 knqa-self-service
📊 Business Central Integration
The portal integrates with Business Central through:

OData Web Services - Read operations

API Endpoints - Create/Update operations

Custom APIs - Extended business logic

ETag Concurrency - Optimistic locking for data integrity

Key Integration Points
Leave_Applications_List - Leave management

Employees - Employee directory

LeaveTypes - Leave type configurations

EmployeeLeaveBalance - Leave balance tracking

🎯 Key Benefits
For Employees
✅ 24/7 access to leave information

✅ Simplified leave application process

✅ Real-time balance tracking

✅ Mobile-friendly interface

✅ Reduced paperwork

For HR Department
✅ Automated workflows

✅ Reduced administrative workload

✅ Better compliance tracking

✅ Real-time reporting

✅ Integration with existing systems

For Organization
✅ Increased productivity

✅ Improved employee satisfaction

✅ Digital transformation

✅ Cost savings through automation

✅ Scalable solution

🔐 Security Features
Role-based Access Control (RBAC)

JWT Token Authentication

HTTPS Enforcement

Input Validation & Sanitization

API Rate Limiting

Audit Logging

📈 Performance
Blazor WebAssembly - Fast client-side rendering

API Caching - Reduced Business Central calls

Lazy Loading - Optimized resource loading

Minimal Payloads - Efficient data transfer

Compression - GZIP compression enabled

🤝 Contributing
We welcome contributions! Please see our Contributing Guidelines for details.

Fork the repository

Create a feature branch (git checkout -b feature/AmazingFeature)

Commit changes (git commit -m 'Add AmazingFeature')

Push to branch (git push origin feature/AmazingFeature)

Open a Pull Request

📝 License
This project is licensed under the MIT License - see the LICENSE file for details.

👥 Team
Project Lead: [Name]

Development Team: [Names]

HR Department: KNQA HR Team

Business Analysts: [Names]

🙏 Acknowledgments
Kenya National Qualifications Authority

Microsoft Dynamics 365 Business Central Team

Blazor & .NET Community

Open Source Contributors

📞 Support
For support, please:

Check the Wiki

Open an Issue

Contact: support@knqa.go.ke
