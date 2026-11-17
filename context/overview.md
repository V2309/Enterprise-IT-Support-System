# DeskPilot — Tổng Quan Hệ Thống

## Giới thiệu

**DeskPilot** là hệ thống hỗ trợ kỹ thuật IT doanh nghiệp (Enterprise IT Support System), cho phép đội ngũ IT giám sát, chẩn đoán và điều khiển từ xa toàn bộ dàn máy trạm trong tổ chức theo **thời gian thực**. Hệ thống hoạt động theo mô hình **Client – Server – Admin**, giao tiếp qua **SignalR** (WebSocket) và **REST API**, lưu trữ dữ liệu trên **PostgreSQL**.

## Mục tiêu

- Giám sát sức khỏe phần cứng (CPU, RAM, Disk) toàn bộ máy trạm trong doanh nghiệp theo thời gian thực.
- Cho phép nhân viên tự báo lỗi nhanh (1-click) hoặc mô tả sự cố tùy chỉnh mà không cần gọi điện hay gửi email.
- IT Admin có thể gửi lệnh khắc phục từ xa (Restart Print Spooler, Flush DNS, …) xuống đúng máy chỉ định.
- Quản lý tài sản IT: thiết bị, công cụ, lịch bảo trì định kỳ.
- Ghi nhật ký toàn bộ thao tác can thiệp (Audit Log) để truy vết và báo cáo.

## Thành phần chính

| Thành phần | Công nghệ | Mô tả |
|---|---|---|
| **DeskPilot.Server** | ASP.NET Core, SignalR, EF Core, PostgreSQL | Backend trung tâm: xử lý nghiệp vụ, lưu trữ dữ liệu, điều phối giao tiếp giữa Client và Admin |
| **DeskPilot.Client** | WPF (.NET 10, Windows) | Agent cài trên mỗi máy trạm: thu thập phần cứng, gửi heartbeat, nhận lệnh từ xa, báo lỗi |
| **DeskPilot.Admin** | WPF (.NET 10, Windows) | Ứng dụng quản trị cho IT: dashboard giám sát, quản lý ticket, gửi lệnh, quản lý thiết bị/công cụ/bảo trì |
| **DeskPilot.Shared** | .NET Class Library | Thư viện dùng chung: DTO, HubProtocol, RemoteCommands — chia sẻ giữa cả 3 project |

## Cơ chế hoạt động

```
┌───────────────┐       SignalR (WebSocket)        ┌──────────────────┐
│  DeskPilot    │  ──── Heartbeat (3s/lần) ──────▶ │   DeskPilot      │
│  Client       │  ──── Ticket báo sự cố ────────▶ │   Server         │
│  (Máy trạm)   │  ◀─── Lệnh khắc phục ────────── │   (ASP.NET Core) │
└───────────────┘       Báo kết quả thực thi ─────▶└──────────────────┘
                                                          │  ▲
                                                          │  │
                                                   SignalR │  │ REST API
                                                          │  │
                                                          ▼  │
                                                   ┌──────────────────┐
                                                   │  DeskPilot       │
                                                   │  Admin           │
                                                   │  (IT Dashboard)  │
                                                   └──────────────────┘
```

1. **Client** thu thập CPU/RAM/Disk mỗi 3 giây → gửi heartbeat lên Server qua SignalR.
2. **Server** cache trạng thái trong RAM (`MachineStateCache`) → đẩy real-time cho tất cả Admin đang kết nối → ghi batch xuống PostgreSQL mỗi 30 giây.
3. **Background Service** (`HeartbeatMonitorService`) quét mỗi 5 giây, nếu máy nào không gửi heartbeat trong 15 giây → đánh dấu **Offline** → thông báo Admin.
4. **Admin** gửi lệnh sửa lỗi (RestartSpooler, FlushDns) → Server chuyển tiếp đúng máy đích → Client thực thi và báo kết quả ngược lại.

## Bảo mật

- Xác thực bằng **API Key** (header `X-Api-Key`) với hai vai trò: `Admin` và `Agent`.
- So sánh API key bằng `CryptographicOperations.FixedTimeEquals` để chống timing attack.
- REST API chỉ cho phép role `Admin`; Agent chỉ được truy cập SignalR Hub.
- Middleware chặn sớm (401/403) mọi request không hợp lệ trước khi vào controller hoặc hub.

## Triển khai

- Server đóng gói bằng **Docker** (Dockerfile + docker-compose.yml).
- Cấu hình bí mật (connection string, API key) qua file `.env` (không commit lên git).
- Hỗ trợ auto-migration khi khởi động (`Database:MigrateOnStartup`).
- Client và Admin publish thành bộ cài đặt chạy trên Windows.
