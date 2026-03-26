# NetworkDrive

A web-based file browser for Windows network shares (SMB/UNC), built with ASP.NET Core MVC and Clean Architecture.

## Features

- **Browse** folders and files on a remote network share through a modern web UI
- **Upload** files with drag-and-drop support and progress indication
- **Download** files directly from the browser
- **Delete** files and folders with a right-click context menu
- **Preview** images, audio, PDFs, and text files in-browser
- **Authenticate** once with network credentials; sessions persist for 30 days via a secure cookie
- **Impersonate** the authenticated user for every storage operation so file-system permissions are respected

## Architecture

The solution follows **Clean Architecture** with four projects:

```
NetworkDrive.Web            → ASP.NET Core MVC front-end
NetworkDrive.Application    → Use cases (CQRS via MediatR), DTOs, validation
NetworkDrive.Infrastructure → File-system access, Windows auth & impersonation
NetworkDrive.Domain         → Entities, interfaces, domain exceptions
```

Request flow:

```
Browser → Controller → MediatR → Use-Case Handler → IStorageRepository
                                                        ↓
                                              LocalStorageRepository
                                                        ↓
                                              INetworkImpersonator
                                                        ↓
                                              Windows RunImpersonatedAsync
                                                        ↓
                                              UNC file-system operations
```

## Prerequisites

| Requirement | Notes |
|---|---|
| **.NET 10 SDK** | Target framework for all projects |
| **Windows** | Required for the Win32 `LogonUser` API used during authentication and impersonation |
| **Network share** | An accessible SMB/UNC path (e.g. `\\192.168.0.1\share`) |

## Getting Started

1. **Clone the repository**

   ```bash
   git clone https://github.com/Vexelior/NetworkDrive.git
   cd NetworkDrive
   ```

2. **Configure the network share root path** in `NetworkDrive.Web/appsettings.json`:

   ```jsonc
   {
     "Storage": {
       "FileSystem": {
         "RootPath": "\\\\192.168.0.1\\g"   // ← your UNC path
       }
     }
   }
   ```

3. **Run the application**

   ```bash
   dotnet run --project NetworkDrive.Web
   ```

4. Open the URL shown in the console (by default `https://localhost:7043` or `http://localhost:5043`) and sign in with your network credentials.

## Configuration

Configuration lives in `NetworkDrive.Web/appsettings.json`.

| Section | Key | Description |
|---|---|---|
| `Storage:FileSystem` | `RootPath` | UNC path to the network share root |
| `Serilog` | *(various)* | Structured logging to console and rolling file (see [Serilog docs](https://github.com/serilog/serilog-settings-configuration)) |

## Project Structure

```
NetworkDrive.Domain/
├── Entities/          StorageItem (abstract), StorageFile, StorageFolder
├── Exceptions/        DomainException, PathTraversalException, StorageItemNotFoundException
└── Interfaces/        IStorageRepository, INetworkCredentialProvider,
                       INetworkShareAuthService, INetworkImpersonator

NetworkDrive.Application/
├── DTOs/              StorageItemDto
└── UseCases/
    ├── BrowseFolder/  BrowseFolderQuery & Handler
    ├── DownloadFile/  DownloadFileQuery & Handler
    ├── UploadFile/    UploadFileCommand & Handler
    └── DeleteFile/    DeleteFileCommand & Handler

NetworkDrive.Infrastructure/
└── Storage/           LocalStorageRepository, NetworkShareAuthService,
                       NetworkImpersonator, StorageOptions

NetworkDrive.Web/
├── Controllers/       HomeController, AuthController, DriveController
├── Services/          HttpContextNetworkCredentialProvider
├── Views/             Razor views (Login, Drive browser, Error, Layout)
└── wwwroot/           CSS, JS, Bootstrap, jQuery
```

## Key Libraries

| Package | Version | Purpose |
|---|---|---|
| [MediatR](https://github.com/jbogard/MediatR) | 14.1.0 | CQRS / mediator pattern |
| [FluentValidation](https://docs.fluentvalidation.net) | 12.1.1 | Request validation |
| [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore) | 10.0.0 | Structured logging |

## License

This project is licensed under the [MIT License](LICENSE).

