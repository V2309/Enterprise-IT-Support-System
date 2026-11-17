-- =========================================================================
-- DESKPILOT: SCRIPT TẠO BẢNG & DỮ LIỆU MẪU
-- (10 THIẾT BỊ + 10 CÔNG CỤ + 6 KẾ HOẠCH BẢO TRÌ ĐỊNH KỲ)
-- Chạy trực tiếp trên PostgreSQL / Neon Console / DBeaver / pgAdmin
-- =========================================================================

-- 1. BẢNG DEVICES (Nếu chưa có)
CREATE TABLE IF NOT EXISTS devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(150) NOT NULL,
    device_type VARCHAR(50) NOT NULL DEFAULT 'Computer',
    serial_number VARCHAR(100),
    model VARCHAR(100),
    manufacturer VARCHAR(100),
    ip_address VARCHAR(45),
    mac_address VARCHAR(30),
    department VARCHAR(100),
    assigned_to VARCHAR(100),
    status VARCHAR(30) NOT NULL DEFAULT 'InUse',
    specifications VARCHAR(1000),
    notes VARCHAR(1000),
    purchase_date TIMESTAMP WITH TIME ZONE,
    warranty_expiry TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_devices_department ON devices(department);
CREATE INDEX IF NOT EXISTS ix_devices_status ON devices(status);
CREATE INDEX IF NOT EXISTS ix_devices_device_type ON devices(device_type);

-- 2. BẢNG TOOLS (Nếu chưa có)
CREATE TABLE IF NOT EXISTS tools (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(150) NOT NULL,
    category VARCHAR(50) NOT NULL DEFAULT 'System',
    description VARCHAR(500),
    command_or_script VARCHAR(4000) NOT NULL,
    default_parameters VARCHAR(500),
    version VARCHAR(30) NOT NULL DEFAULT '1.0.0',
    author VARCHAR(100) NOT NULL DEFAULT 'IT Admin',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_tools_category ON tools(category);
CREATE INDEX IF NOT EXISTS ix_tools_is_active ON tools(is_active);

-- 3. BẢNG MAINTENANCE_SCHEDULES (Nếu chưa có)
CREATE TABLE IF NOT EXISTS maintenance_schedules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(200) NOT NULL,
    device_id UUID REFERENCES devices(id) ON DELETE SET NULL,
    department VARCHAR(100),
    maintenance_type VARCHAR(50) NOT NULL DEFAULT 'FullInspection',
    frequency VARCHAR(30) NOT NULL DEFAULT 'Quarterly',
    scheduled_date TIMESTAMP WITH TIME ZONE NOT NULL,
    last_performed_date TIMESTAMP WITH TIME ZONE,
    next_due_date TIMESTAMP WITH TIME ZONE,
    assigned_technician VARCHAR(100) NOT NULL DEFAULT 'IT Support',
    status VARCHAR(30) NOT NULL DEFAULT 'Planned',
    checklist_notes VARCHAR(2000),
    result_summary VARCHAR(2000),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_maintenance_status ON maintenance_schedules(status);
CREATE INDEX IF NOT EXISTS ix_maintenance_next_due ON maintenance_schedules(next_due_date);
CREATE INDEX IF NOT EXISTS ix_maintenance_device ON maintenance_schedules(device_id);
CREATE INDEX IF NOT EXISTS ix_maintenance_dept ON maintenance_schedules(department);

-- =========================================================================
-- 4. CHÈN 10 DỮ LIỆU MẪU CHO BẢNG DEVICES
-- =========================================================================
INSERT INTO devices (device_code, name, device_type, serial_number, model, manufacturer, ip_address, mac_address, department, assigned_to, status, specifications, notes)
VALUES
('TB-PC-01', 'PC Dell OptiPlex 7090', 'Computer', 'SN-DELL-7090-01', 'OptiPlex 7090 MT', 'Dell', '192.168.1.101', '00:1A:2B:3C:4D:5E', 'Kế toán', 'Nguyễn Thị Mai', 'InUse', 'Intel Core i5-11500, 16GB RAM DDR4, SSD 512GB NVMe', 'Máy phục vụ phần mềm kế toán MISA'),
('TB-LT-02', 'Laptop Lenovo ThinkPad T14 Gen 3', 'Laptop', 'SN-THINK-T14-02', 'ThinkPad T14', 'Lenovo', '192.168.1.102', '00:1A:2B:3C:4D:5F', 'Kinh doanh', 'Trần Văn Hùng', 'InUse', 'AMD Ryzen 5 PRO 6650U, 16GB RAM, SSD 512GB', 'Cấp phát cho sales thị trường'),
('TB-LT-03', 'Laptop Dell XPS 15 9520', 'Laptop', 'SN-XPS-9520-03', 'XPS 15 9520', 'Dell', '192.168.1.103', '00:1A:2B:3C:4D:60', 'Kỹ thuật', 'Lê Hoàng Nam', 'InUse', 'Intel Core i7-12700H, 32GB RAM, RTX 3050 Ti, 1TB SSD', 'Máy dựng môi trường test Dev'),
('TB-PC-04', 'PC HP ProDesk 400 G7', 'Computer', 'SN-HP-400-04', 'ProDesk 400 G7 SFF', 'HP', '192.168.1.104', '00:1A:2B:3C:4D:61', 'Nhân sự', 'Phạm Thu Hà', 'InUse', 'Intel Core i5-10500, 8GB RAM, SSD 256GB', 'Máy văn phòng hành chính'),
('TB-PR-05', 'Máy in HP LaserJet Pro M404dn', 'Printer', 'SN-HPPR-404-05', 'LaserJet M404dn', 'HP', '192.168.1.200', '00:1A:2B:3C:4D:62', 'Kế toán', 'Dùng chung', 'InUse', 'In laser trắng đen 2 mặt tự động, kết nối LAN Gigabit', 'Máy in hóa đơn và chứng từ phòng kế toán'),
('TB-MN-06', 'Màn hình Dell UltraSharp U2422H', 'Monitor', 'SN-DELL-MN-06', 'UltraSharp U2422H', 'Dell', NULL, NULL, 'Marketing', 'Vũ Minh Tuấn', 'InUse', '24 inch IPS FHD, 100% sRGB, viền siêu mỏng', 'Màn hình phụ làm đồ họa'),
('TB-NW-07', 'Switch Cisco Catalyst 2960X 24P', 'NetworkDevice', 'SN-CISCO-2960-07', 'Catalyst 2960X-24TS-L', 'Cisco', '192.168.1.2', '00:1A:2B:3C:4D:63', 'Kỹ thuật', 'Phòng Server IT', 'InUse', '24 cổng Gigabit Ethernet + 4 SFP uplink', 'Switch core phân phối mạng tầng 2'),
('TB-LT-08', 'Laptop MacBook Pro 14 M2 Pro', 'Laptop', 'SN-APPLE-MBP-08', 'MacBook Pro 14 2023', 'Apple', '192.168.1.108', '00:1A:2B:3C:4D:64', 'Ban giám đốc', 'Đỗ Quốc Anh', 'InUse', 'Apple M2 Pro 10-core, 16GB RAM, SSD 512GB', 'Cấp phát cho Giám đốc điều hành'),
('TB-PC-09', 'PC Dell Vostro 3888 (Dự phòng)', 'Computer', 'SN-DELL-V3888-09', 'Vostro 3888', 'Dell', NULL, NULL, 'Kỹ thuật', 'Kho IT', 'InStock', 'Intel Core i3-10100, 8GB RAM, SSD 256GB', 'Máy dự phòng thay thế khẩn cấp'),
('TB-PR-10', 'Máy in Canon LBP2900 (Đang sửa)', 'Printer', 'SN-CANON-2900-10', 'LBP 2900', 'Canon', NULL, NULL, 'Kỹ thuật', 'Kho IT Bảo trì', 'Maintenance', 'In laser A4 đơn năng 12 trang/phút', 'Đang chờ thay cụm sấy và bao lụa')
ON CONFLICT (device_code) DO NOTHING;

-- =========================================================================
-- 5. CHÈN 10 DỮ LIỆU MẪU CHO BẢNG TOOLS
-- =========================================================================
INSERT INTO tools (code, name, category, description, command_or_script, default_parameters, version, author, is_active)
VALUES
('FLUSH-DNS', 'Xoá bộ nhớ đệm DNS', 'Network', 'Xoá cache DNS cục bộ khi máy tính không thể truy cập website hoặc server nội bộ', 'ipconfig /flushdns', NULL, '1.0.0', 'IT Admin', TRUE),
('RESTART-SPOOLER', 'Khởi động lại dịch vụ Máy in', 'Printer', 'Khắc phục lỗi in ấn bị treo, xóa hàng đợi in kẹt trong Print Spooler', 'Stop-Service -Name Spooler -Force; Remove-Item -Path "$env:SystemRoot\System32\spool\PRINTERS\*" -Force -Recurse -ErrorAction SilentlyContinue; Start-Service -Name Spooler', NULL, '1.1.0', 'IT Admin', TRUE),
('CLEAR-TEMP', 'Dọn dẹp file rác hệ thống (Temp)', 'Maintenance', 'Giải phóng dung lượng ổ C bằng cách xóa các file tạm trong %TEMP% và Windows Temp', 'Remove-Item -Path "$env:TEMP\*" -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item -Path "C:\Windows\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue', NULL, '1.0.0', 'IT Admin', TRUE),
('RENEW-IP', 'Làm mới địa chỉ IP (DHCP Release/Renew)', 'Network', 'Nhả và xin cấp lại địa chỉ IP từ DHCP Server khi máy bị trùng IP hoặc limited access', 'ipconfig /release; Start-Sleep -Seconds 1; ipconfig /renew', NULL, '1.0.0', 'IT Admin', TRUE),
('RESTART-AUDIO', 'Khởi động lại dịch vụ Âm thanh (Audio)', 'System', 'Khắc phục sự cố tai nghe/loa bị mất âm thanh đột ngột', 'Restart-Service -Name AudioSrv -Force', NULL, '1.0.0', 'IT Admin', TRUE),
('RESET-WINSOCK', 'Đặt lại cấu hình mạng Winsock', 'Network', 'Khôi phục lại toàn bộ stack TCP/IP và Winsock khi mất kết nối mạng nghiêm trọng', 'netsh winsock reset; netsh int ip reset', NULL, '1.0.0', 'IT Admin', TRUE),
('SCAN-SFC', 'Kiểm tra và sửa lỗi file hệ thống (SFC)', 'System', 'Chạy trình System File Checker để tự động phục hồi các file DLL/Windows bị hỏng', 'sfc /scannow', NULL, '1.0.0', 'IT Admin', TRUE),
('UPDATE-DEFENDER', 'Cập nhật chữ ký Windows Defender', 'Security', 'Buộc cập nhật mẫu nhận diện mã độc mới nhất từ Microsoft', 'Update-MpSignature', NULL, '1.0.0', 'IT Admin', TRUE),
('CHECK-BATTERY', 'Xuất báo cáo tình trạng Pin laptop', 'Diagnostic', 'Tạo file báo cáo chi tiết độ chai pin và chu kỳ sạc ra ổ C', 'powercfg /batteryreport /output "C:\battery_report.html"', NULL, '1.0.0', 'IT Admin', TRUE),
('KILL-NOT-RESPONDING', 'Tắt ứng dụng đang bị treo (Not Responding)', 'System', 'Tìm và đóng cưỡng bức các tiến trình phần mềm đang bị đơ không phản hồi', 'Get-Process | Where-Object { $_.Responding -eq $false } | Stop-Process -Force', NULL, '1.0.0', 'IT Admin', TRUE)
ON CONFLICT (code) DO NOTHING;

-- =========================================================================
-- 6. CHÈN DỮ LIỆU MẪU CHO KẾ HOẠCH BẢO TRÌ (MAINTENANCE_SCHEDULES)
-- =========================================================================
INSERT INTO maintenance_schedules (title, department, maintenance_type, frequency, scheduled_date, next_due_date, assigned_technician, status, checklist_notes)
VALUES
('Vệ sinh tra keo tản nhiệt định kỳ dàn máy Kế toán', 'Kế toán', 'HardwareCleaning', 'Quarterly', NOW() + INTERVAL '5 days', NOW() + INTERVAL '5 days', 'Nguyễn Kỹ Thuật', 'Planned', '1. Thổi bụi case\n2. Tra keo MX-4\n3. Kiểm tra quạt CPU\n4. Dọn dẹp dây nguồn gọn gàng'),
('Bảo dưỡng & nạp mực máy in HP phòng Kế toán', 'Kế toán', 'PrinterService', 'Monthly', NOW() + INTERVAL '2 days', NOW() + INTERVAL '2 days', 'Trần Hỗ Trợ', 'Planned', '1. Vệ sinh gương quét và cụm sấy\n2. Thay hộp mực 58A\n3. In test bản in demo 2 mặt'),
('Cập nhật bản vá bảo mật Windows & quét virus toàn công ty', 'Kỹ thuật', 'SecurityCheck', 'Monthly', NOW() - INTERVAL '1 days', NOW() - INTERVAL '1 days', 'Lê Bảo Mật', 'Overdue', '1. Cập nhật Windows Patch mới nhất\n2. Quét full scan Defender\n3. Kiểm tra chứng chỉ SSL'),
('Kiểm tra tải nhiệt & nguồn điện Switch Core tầng 2', 'Kỹ thuật', 'NetworkInspection', 'Quarterly', NOW() + INTERVAL '12 days', NOW() + INTERVAL '12 days', 'Đỗ Network', 'Planned', '1. Đo điện áp UPS cấp cho Switch\n2. Kiểm tra nhiệt độ quạt tản nhiệt\n3. Rà soát port lỗi CRC'),
('Sao lưu dữ liệu ổ D máy kế toán trưởng ra NAS nội bộ', 'Kế toán', 'BackupData', 'Weekly', NOW() + INTERVAL '3 days', NOW() + INTERVAL '3 days', 'Nguyễn Kỹ Thuật', 'Planned', '1. Đồng bộ thư mục MISA Backup\n2. Kiểm tra dung lượng ổ đĩa\n3. Test khôi phục mẫu 1 file'),
('Kiểm tra tổng thể dàn máy phòng Kinh doanh (Đã xong Q1)', 'Kinh doanh', 'FullInspection', 'Quarterly', NOW() - INTERVAL '30 days', NOW() + INTERVAL '60 days', 'Trần Hỗ Trợ', 'Completed', 'Kiểm tra tốc độ đọc ghi SSD, dọn dẹp file tạm, kiểm tra kết nối VPN');
