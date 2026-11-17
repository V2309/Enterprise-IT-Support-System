# DeskPilot — Kiến Trúc Hệ Thống

## Tổng quan kiến trúc

DeskPilot sử dụng kiến trúc **3-tier** với giao tiếp hybrid: **SignalR** cho real-time bidirectional và **REST API** cho CRUD operations.

```
┌──────────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                           │
│                                                                      │
│   ┌─────────────────────┐         ┌─────────────────────────┐       │
│   │  DeskPilot.Client   │         │    DeskPilot.Admin       │       │
│   │  (WPF Agent)        │         │    (WPF Dashboard)       │       │
│   │                     │         │                          │       │
│   │  • HardwareMonitor  │         │  • DashboardView         │       │
│   │  • RunbookExecutor  │         │  • TicketsView           │       │
│   │  • SignalRClient    │         │  • ActionLogsView        │       │
│   │  • System Tray      │         │  • DevicesView           │       │
│   └────────┬────────────┘         │  • ToolsView             │       │
│            │                      │  • MaintenanceView       │       │
│            │                      │  • ChatView              │       │
│            │                      └──────────┬───────────────┘       │
└────────────┼─────────────────────────────────┼───────────────────────┘
             │ SignalR                         │ SignalR + REST API
             │ (Agent key)                    │ (Admin key)
             ▼                                 ▼
┌──────────────────────────────────────────────────────────────────────┐
│                        APPLICATION LAYER                             │
│                      DeskPilot.Server                                │
│                                                                      │
│   ┌──────────────────┐  ┌─────────────────────────────────────┐     │
│   │   SupportHub     │  │          REST Controllers            │     │
│   │   (SignalR)      │  │  • MachinesController                │     │
│   │                  │  │  • TicketsController                 │     │
│   │  Agent methods:  │  │  • ActionLogsController              │     │
│   │  • SendHeartbeat │  │  • DevicesController                 │     │
│   │  • CreateTicket  │  │  • ToolsController                   │     │
│   │  • ReportResult  │  │  • MaintenanceController             │     │
│   │  • SendChat      │  │  • ChatController                    │     │
│   │                  │  └─────────────────────────────────────┘     │
│   │  Admin methods:  │                                               │
│   │  • SendCommand   │  ┌─────────────────────────────────────┐     │
│   │  • GetMachines   │  │       Background Services            │     │
│   │  • ResolveTicket │  │  • HeartbeatMonitorService           │     │
│   │  • ReplyChat     │  │    (quét offline + flush DB)         │     │
│   │  • GetLogs/Chat  │  └─────────────────────────────────────┘     │
│   └──────┬───────────┘  └──────────────┬──────────────────────┘     │
│          │                              │                            │
│          ▼                              ▼                            │
│   ┌──────────────────────────────────────────────────────────────┐   │
│   │                    SERVICE LAYER                              │   │
│   │                                                               │   │
│   │  Machines/          Tickets/         ActionLogs/              │   │
│   │  • IMachineService  • ITicketService • IActionLogService     │   │
│   │  • MachineService   • TicketService  • ActionLogService      │   │
│   │  • MachineStateCache                                         │   │
│   │                                                               │   │
│   │  Devices/           Tools/           Maintenance/ Chat/       │   │
│   │  • IDeviceService   • IToolService   • IMaintenance • IChat   │   │
│   │  • DeviceService    • ToolService    • MaintService • ChatServ│   │
│   └──────────────────────────┬───────────────────────────────────┘   │
│                              │                                       │
│   ┌──────────────────────────┴────────────────────────┐             │
│   │  Security: ApiKeyValidator + Middleware (401/403)  │             │
│   └───────────────────────────────────────────────────┘             │
└──────────────────────────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────────────┐
│                          DATA LAYER                                  │
│                                                                      │
│   ┌────────────────────────────────────────────────────────────┐     │
│   │  AppDbContext (EF Core + Npgsql + snake_case convention)   │     │
│   │                                                            │     │
│   │  Entities:           Configurations:                       │     │
│   │  • Machine           • MachineConfiguration                │     │
│   │  • Ticket            • TicketConfiguration                 │     │
│   │  • ActionLog         • ActionLogConfiguration              │     │
│   │  • Device            • DeviceConfiguration                 │     │
│   │  • Tool              • ToolConfiguration                   │     │
│   │  • MaintenanceSchedule                                     │     │
│   │  • ChatMessage       • ChatMessageConfiguration            │     │
│   │                                                            │     │
│   │  Enums:                                                    │     │
│   │  • TicketStatus      • DeviceStatus    • DeviceType        │     │
│   │  • MaintenanceStatus • MaintenanceType • MaintenanceFreq   │     │
│   │  • ToolCategory                                            │     │
│   └────────────────────────────────────────────────────────────┘     │
│                              │                                       │
│                              ▼                                       │
│                    ┌──────────────────┐                              │
│                    │   PostgreSQL     │                              │
│                    │   (Docker)       │                              │
│                    └──────────────────┘                              │
└──────────────────────────────────────────────────────────────────────┘
```

## Cấu trúc Solution

```
DeskPilot/
├── DeskPilot.Server/           # Backend API + SignalR Hub
│   ├── Controllers/            # REST API endpoints (Machines, Tickets, Devices, Tools, Maintenance, Chat)
│   ├── Hubs/                   # SignalR Hub (SupportHub)
│   ├── Services/               # Business logic (tổ chức theo domain)
│   │   ├── Machines/           # Quản lý máy trạm + cache RAM
│   │   ├── Tickets/            # Quản lý phiếu sự cố
│   │   ├── ActionLogs/         # Nhật ký thao tác
│   │   ├── Devices/            # Quản lý tài sản thiết bị
│   │   ├── Tools/              # Quản lý công cụ IT
│   │   ├── Maintenance/        # Lịch bảo trì
│   │   ├── Chat/               # Dịch vụ trò chuyện IT Support
│   │   ├── Storage/            # Dịch vụ lưu trữ ImageKit (Cloud Image CDN)
│   │   └── Background/         # Hosted services (HeartbeatMonitor)
│   ├── Models/                 # Entity classes + Enums
│   ├── Data/                   # AppDbContext + EF Configurations
│   ├── Security/               # API Key authentication
│   ├── Migrations/             # EF Core migrations
│   └── Program.cs              # DI + Middleware pipeline
│
├── DeskPilot.Client/           # WPF Agent (máy trạm)
│   ├── Services/
│   │   ├── HardwareMonitorService.cs   # Thu thập CPU/RAM/Disk
│   │   ├── SignalRClientService.cs     # Kết nối SignalR tới Server + Chat
│   │   └── RunbookExecutor.cs          # Thực thi lệnh sửa lỗi
│   ├── MainWindow.xaml(.cs)    # Giao diện chính, Khung Chat trực tiếp + System Tray
│   └── Assets/                 # Icon, hình ảnh
│
├── DeskPilot.Admin/            # WPF Dashboard (IT Admin)
│   ├── Services/
│   │   ├── AdminApiClient.cs           # HTTP client gọi REST API
│   │   └── ModalDialogService.cs       # In-app modal dialogs
│   ├── ViewModels/             # Quản lý trạng thái theo module (Partial Classes)
│   │   ├── DashboardViewModel.cs              # Core: Khởi tạo, SPA Tabs, SignalR
│   │   ├── DashboardViewModel.Machines.cs     # Module Giám sát máy & Metrics
│   │   ├── DashboardViewModel.Tickets.cs      # Module Phiếu sự cố & Live feed
│   │   ├── DashboardViewModel.Actions.cs      # Module Lệnh từ xa & Action logs
│   │   ├── DashboardViewModel.Chat.cs         # Module Trò chuyện thời gian thực
│   │   ├── DashboardViewModel.Devices.cs      # Module Quản lý thiết bị
│   │   ├── DashboardViewModel.Tools.cs        # Module Quản lý công cụ kỹ thuật
│   │   └── DashboardViewModel.Maintenance.cs  # Module Lịch bảo trì định kỳ
│   ├── Views/                  # Các tab giao diện theo từng module riêng biệt
│   │   ├── Dashboard/          # DashboardView.xaml(.cs) (Tổng quan dàn máy)
│   │   ├── Tickets/            # TicketsView.xaml(.cs) (Quản lý phiếu sự cố)
│   │   ├── ActionLogs/         # ActionLogsView.xaml(.cs) (Nhật ký lệnh)
│   │   ├── Devices/            # DevicesView.xaml(.cs) (Quản lý thiết bị)
│   │   ├── Tools/              # ToolsView.xaml(.cs) (Quản lý công cụ)
│   │   ├── Maintenance/        # MaintenanceView.xaml(.cs) (Kế hoạch bảo trì)
│   │   └── Chat/               # ChatView.xaml(.cs) (Trò chuyện trực tiếp IT Support)
│   └── MainWindow.xaml(.cs)    # Shell điều hướng SPA
│
├── DeskPilot.Shared/           # Thư viện dùng chung
│   ├── DTO/                    # Data Transfer Objects
│   │   ├── MachineStatusDto.cs
│   │   ├── TicketDto.cs / ActionLogDto.cs
│   │   ├── DeviceDto.cs
│   │   ├── ToolDto.cs
│   │   ├── MaintenanceScheduleDto.cs
│   │   └── ChatMessageDto.cs
│   └── HubProtocol.cs          # RemoteCommands + HubHeaders
│
├── docker-compose.yml          # Triển khai Server bằng Docker
├── .env / .env.example         # Biến môi trường bí mật
└── seed_data.sql               # Dữ liệu mẫu
```

## Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Runtime | .NET 10 |
| Backend Framework | ASP.NET Core Minimal API |
| Real-time | SignalR (WebSocket) |
| ORM | Entity Framework Core + Npgsql |
| Database | PostgreSQL |
| Naming Convention | snake_case (`EFCore.NamingConventions`) |
| Desktop UI | WPF (Windows Presentation Foundation) |
| UI Pattern | MVVM (Model-View-ViewModel) |
| Containerization | Docker + docker-compose |
| Notifications | Windows Toast (Microsoft.Toolkit.Uwp.Notifications) |
| System Tray | Hardcodet.Wpf.TaskbarNotification |

## Luồng dữ liệu chính

### 1. Heartbeat (Client → Server → Admin)
```
Client                    Server                         Admin
  │                         │                              │
  │── SendHeartbeat(dto) ──▶│                              │
  │                         │── cache.Upsert(dto) ────┐    │
  │                         │                          │    │
  │                         │── ReceiveMachineStatus ──┼──▶ │
  │                         │                          │    │
  │                         │ (mỗi 30s) FlushAsync() ──┘    │
  │                         │── SaveSnapshots(DB) ─▶ PostgreSQL
```

### 2. Ticket (Client → Server → Admin → Resolve)
```
Client                    Server                         Admin
  │                         │                              │
  │── CreateTicket() ──────▶│                              │
  │                         │── ticketService.Create() ─▶ DB
  │                         │── ReceiveNewTicket ────────▶ │
  │                         │                              │
  │                         │◀────── ResolveTicket(id) ────│
  │                         │── ticketService.Resolve() ─▶ DB
  │                         │── TicketResolved ──────────▶ │
```

### 3. Remote Command (Admin → Server → Client → Feedback)
```
Admin                     Server                         Client
  │                         │                              │
  │── SendCommandToClient ─▶│                              │
  │                         │── actionLog.Create() ──────▶ DB
  │                         │── ExecuteCommand ──────────▶ │
  │◀── ReceiveNewActionLog ─│                              │
  │                         │                              │
  │                         │◀── ReportCommandResult ──────│
  │◀── ReceiveCommandFeedback│                              │
```

### 4. Real-time Chat & ImageKit Upload (Client ↔ Server ↔ Admin)
```
Client / Admin                Server                       ImageKit CDN / DB
  │                             │                                  │
  │── POST /api/chat/upload ───▶│                                  │
  │   (file binary)             │── UploadImageAsync() ───────────▶│ (/chat folder)
  │                             │◀── CDN Image URL ────────────────│
  │◀── { url } ─────────────────│                                  │
  │                             │                                  │
  │── SendChatMessage(msg, url)▶│                                  │
  │                             │── chatService.Save(msg, url) ───▶ DB (image_url)
  │                             │── ReceiveChatMessage ───────────▶ Admin (Render Image)
  │◀── ReceiveChatMessage ──────│ (Render Image)                   │
```

## Database Schema (PostgreSQL, snake_case)

| Bảng | Mô tả | Khóa chính |
|---|---|---|
| `machines` | Trạng thái máy trạm (CPU, RAM, Disk, Online/Offline) | `id` (Guid) |
| `tickets` | Phiếu yêu cầu hỗ trợ kỹ thuật | `id` (int, auto-increment) |
| `action_logs` | Nhật ký lệnh IT đã thực thi | `id` (int, auto-increment) |
| `devices` | Tài sản thiết bị IT (máy tính, máy in, mạng…) | `id` (Guid) |
| `tools` | Công cụ / script IT | `id` (Guid) |
| `maintenance_schedules` | Kế hoạch bảo trì định kỳ | `id` (Guid) |
| `chat_messages` | Tin nhắn trao đổi hỗ trợ kỹ thuật thời gian thực (`message`, `image_url`) | `id` (int, auto-increment) |
