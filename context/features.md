# DeskPilot — Danh Sách Chức Năng

> **Quy ước trạng thái chức năng mới:**
>
> | Ký hiệu | Trạng thái | Ý nghĩa |
> |---|---|---|
> | 🔨 | **Đang làm** | Đang code, chưa xong |
> | 🔍 | **Đang kiểm tra** | Code xong, chờ user test & xác nhận |
> | ✅ | **Hoàn thành** | User đã kiểm tra OK |
>
> *Chức năng #1–#43 đã hoàn thành từ trước. Các chức năng mới từ #44 trở đi sẽ ghi kèm trạng thái.*

---

## A. DeskPilot.Client (Agent — Máy trạm nhân viên)

1. **Thu thập thông số phần cứng real-time** — Đọc CPU, RAM (% và GB), Disk (% và GB), trạng thái Print Spooler mỗi 3 giây qua `HardwareMonitorService`.

2. **Gửi heartbeat định kỳ lên Server** — Mỗi 3 giây gửi `MachineStatusDto` qua SignalR để Server theo dõi trạng thái online/offline.

3. **Hiển thị dashboard sức khỏe máy** — Giao diện WPF với các circular gauge (vòng tròn phần trăm) cho CPU, RAM, Disk và chỉ báo tổng quan sức khỏe (Hoạt động tốt / Cần kiểm tra).

4. **Báo lỗi nhanh 1-click** — Ba nút bấm gửi ticket ngay: "Kẹt máy in", "Mất mạng", "Máy chậm" — không cần nhập thêm thông tin.

5. **Báo lỗi tùy chỉnh** — Ô nhập văn bản tự do để mô tả sự cố chi tiết, hỗ trợ gửi bằng Enter.

6. **Tự chẩn đoán & sửa nhanh (Auto-Fix)** — Chạy tự động Runbook: Restart Print Spooler + Flush DNS, hiển thị kết quả từng bước.

7. **Nhận và thực thi lệnh từ xa** — Nhận lệnh từ IT Admin qua SignalR (`ExecuteCommand`), thực thi qua `RunbookExecutor`, báo kết quả ngược lại Server.

8. **Thông báo balloon khi IT gửi lệnh** — Hiển thị Windows notification (balloon tip) khi IT Admin thực thi lệnh từ xa trên máy.

9. **System Tray (khay hệ thống)** — Ẩn xuống System Tray khi đóng cửa sổ, double-click để mở lại; menu chuột phải có "Mở" và "Thoát".

10. **Auto-reconnect SignalR** — Tự động kết nối lại Server khi mất kết nối, với cơ chế retry.

---

## B. DeskPilot.Server (Backend)

11. **SignalR Hub (`SupportHub`)** — Hub trung tâm xử lý toàn bộ giao tiếp real-time giữa Client và Admin, phân luồng nhóm `admins` và `machine:{name}`.

12. **Xác thực API Key** — Hai API key riêng cho Admin và Agent, so sánh bằng `FixedTimeEquals` chống timing attack; middleware chặn sớm 401/403.

13. **Cache trạng thái máy trong RAM (`MachineStateCache`)** — Lưu nhanh trạng thái mới nhất của tất cả máy, phục vụ truy vấn tức thì không cần query DB.

14. **Background Service giám sát heartbeat** — `HeartbeatMonitorService` quét mỗi 5 giây, đánh dấu offline nếu máy không gửi heartbeat trong 15 giây, thông báo Admin.

15. **Ghi batch trạng thái máy xuống PostgreSQL** — Gom trạng thái dirty mỗi 30 giây rồi flush xuống DB, giảm tải write.

16. **Quản lý Ticket (CRUD)** — Tạo, lấy danh sách active, hoàn tất (resolve) ticket; ghi nhật ký khi đóng ticket; cột `ResolvedAt`.

17. **Quản lý Action Log** — Ghi nhật ký mỗi lệnh IT gửi xuống máy trạm; truy vấn 20 bản ghi gần nhất.

18. **Điều phối lệnh từ xa** — Nhận lệnh từ Admin → validate → ghi log → chuyển tiếp đúng nhóm máy đích → nhận và phát kết quả.

19. **REST API Controllers** — 6 controller cung cấp endpoint CRUD:
    - `GET/POST/PUT/DELETE api/machines`
    - `GET/POST api/tickets`, `POST api/tickets/{id}/resolve`
    - `GET api/action-logs`
    - `GET/POST/PUT/DELETE api/devices`
    - `GET/POST/PUT/DELETE api/tools`
    - `GET/POST/PUT/DELETE api/maintenance`

20. **Quản lý Thiết bị (Devices)** — CRUD thiết bị IT (máy tính, laptop, máy in, màn hình, thiết bị mạng, ngoại vi, server) với thông tin: mã, serial, model, hãng, IP, MAC, phòng ban, người sử dụng, trạng thái, ngày mua, bảo hành.

21. **Quản lý Công cụ (Tools)** — CRUD công cụ/script IT với phân loại (Hệ thống, Mạng, Máy in, Bảo trì, Bảo mật, Chẩn đoán), version, author, trạng thái active/inactive.

22. **Quản lý Kế hoạch Bảo trì (Maintenance)** — CRUD lịch bảo trì định kỳ: loại bảo trì (vệ sinh, cập nhật, quét virus, sao lưu, kiểm tra mạng, kiểm tra toàn diện), tần suất (1 lần / tuần / tháng / quý / 6 tháng / năm), gán kỹ thuật viên, cảnh báo quá hạn (overdue).

23. **EF Core + PostgreSQL + snake_case** — Database sử dụng Npgsql với convention snake_case, cấu hình entity tách riêng trong `Data/Configuration/`, migration tự động.

24. **Docker hóa** — Dockerfile + docker-compose.yml, cấu hình `.env`, hỗ trợ auto-migrate khi khởi động.

---

## C. DeskPilot.Admin (Dashboard — IT Admin)

25. **Giao diện SPA-like (Single Page Application)** — Điều hướng giữa 7 tab (Chat, Dashboard, Tickets, Logs, Devices, Tools, Maintenance) mà không reload, cache View để giữ trạng thái cuộn.

26. **Dashboard giám sát dàn máy** — Hiển thị toàn bộ máy trạm với trạng thái Online/Offline, thông số CPU/RAM/Disk real-time, đếm số máy online/offline.

27. **Lịch sử metrics theo biểu đồ** — Lưu và hiển thị lịch sử 30 điểm dữ liệu gần nhất cho mỗi máy (RAM, CPU, Disk).

28. **Live Feed (dòng thời gian sự kiện)** — Hiển thị real-time các sự kiện: ticket mới, máy offline, lệnh gửi, kết quả thực thi, ticket hoàn tất — có tag màu phân biệt.

29. **Tìm kiếm & lọc máy trạm** — Tìm kiếm theo tên máy hoặc tên user; lọc theo phòng ban từ sidebar.

30. **Quản lý Ticket** — Xem danh sách ticket đang mở, hoàn tất (resolve) ticket với dialog xác nhận.

31. **Gửi lệnh điều khiển từ xa** — Chọn máy → gửi lệnh (RestartSpooler, FlushDns) → nhận kết quả và hiển thị modal thành công/thất bại.

32. **Xem nhật ký thao tác (Action Logs)** — Danh sách 20 lệnh can thiệp gần nhất, cập nhật real-time khi có lệnh mới.

33. **Quản lý Thiết bị** — Giao diện CRUD thiết bị qua REST API: thêm/sửa/xóa, lọc theo phòng ban/trạng thái/loại, tìm kiếm.

34. **Quản lý Công cụ** — Giao diện CRUD công cụ IT: thêm/sửa/xóa, lọc theo loại/trạng thái, tìm kiếm.

35. **Quản lý Kế hoạch Bảo trì** — Giao diện CRUD lịch bảo trì: thêm/sửa/xóa/hoàn tất, lọc theo trạng thái/phòng ban/thiết bị/quá hạn.

36. **Windows Toast Notification** — Bắn thông báo Windows khi có ticket mới kèm âm thanh cảnh báo.

37. **Cảnh báo ngưỡng tài nguyên** — Tự động cảnh báo khi máy trạm vượt ngưỡng CPU/RAM/Disk.

38. **In-app Modal Dialog** — Hệ thống modal overlay đa năng (Info, Success, Warning, Error, Confirm) thay thế MessageBox cũ.

39. **Auto-reconnect SignalR** — Kết nối lại tự động với retry progressive (0s, 2s, 5s, 10s), fallback sang vòng lặp retry vô hạn khi auto-reconnect bỏ cuộc.

---

## D. DeskPilot.Shared (Thư viện dùng chung)

40. **DTO với INotifyPropertyChanged** — `MachineStatusDto` implement INPC để WPF tự cập nhật UI khi dữ liệu thay đổi.

41. **HubProtocol (RemoteCommands + HubHeaders)** — Định nghĩa tập lệnh hợp lệ (RestartSpooler, FlushDns) với normalize alias; header xác thực dùng chung.

42. **Timezone conversion** — DTO tự động chuyển đổi UTC → Local time khi hiển thị trên giao diện.

43. **DeviceDto / ToolDto / MaintenanceScheduleDto** — DTO đầy đủ với computed properties hiển thị tiếng Việt, màu sắc trạng thái, cờ cảnh báo quá hạn.

---

## E. Chức Năng Mới

44. ✅ **Chat IT Admin ↔ Nhân viên** — Nhân viên nhắn tin trực tiếp cho IT từ Client, IT Admin trả lời từ ChatView. Tin nhắn gửi qua SignalR, lưu lịch sử trong PostgreSQL, hiển thị real-time hai chiều. Không cần đăng nhập — dùng machine name + Windows username làm định danh. Giao diện tin nhắn bo góc, tự động cuộn, nút gửi icon tròn xanh dương mũi tên trắng, cách lề chống thanh cuộn đè nội dung.
45. ✅ **Gửi ảnh trong Chat qua ImageKit** — Hỗ trợ tải ảnh lên trong cuộc hội thoại từ cả Client và Agent máy trạm, tự động upload lên ImageKit CDN lưu trữ tại thư mục `/chat`, bảo mật private key ở Server. Hiển thị ảnh trực tiếp trong bong bóng chat, bấm vào ảnh sẽ mở Lightbox modal phóng to ảnh giữa màn hình với nền mờ (nhấn ESC hoặc bấm ra ngoài để đóng), không mở tab trình duyệt.
46. ✅ **Xem trước ảnh đính kèm (Preview Thumbnail) & Dán ảnh (Ctrl + V) trước khi gửi Chat** — Cho phép nhân viên và IT Admin chọn ảnh từ file hoặc dán ảnh chụp màn hình bằng `Ctrl + V`. Ảnh sẽ được hiển thị dạng thẻ thumbnail xem trước (preview) ngay phía trên ô nhập kèm nút X để hủy/đổi ảnh khác nếu chọn nhầm, người dùng có thể nhập thêm tin nhắn văn bản và chỉ khi bấm Gửi (hoặc Enter) thì ảnh mới được upload lên ImageKit và gửi đi cùng lúc.
47. 🔍 **Số tin nhắn chưa đọc (Unread Badge) & Âm thanh thông báo (Sound Notification)** — Hiển thị số lượng tin nhắn chưa đọc (badge đỏ) trên danh sách máy trạm tại Dashboard Admin và nút Chat trên Client, phát âm thanh cảnh báo nhẹ khi có tin nhắn mới gửi đến.
