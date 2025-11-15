using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmblockupdate
{
    public partial class Form1 : Form
    {
        // Đặt tên tiến trình bạn muốn kiểm tra (KHÔNG bao gồm .exe)
        private const string TargetProcessName = "Zalo"; // Dùng tên này!

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int zaloIsRunning = KillProcessWithWmi(TargetProcessName);
            // 1. Cố gắng kết thúc tiến trình Zalo
            int killedCount = KillProcessWithWmi(TargetProcessName);

            if (killedCount > 0)
            {
                // Đã kết thúc thành công ít nhất một tiến trình
                MessageBox.Show(
                    $"{killedCount} version: '{TargetProcessName}.exe process has been Found and Killed '.",
                    "End Process!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                // ⚠️ Lỗi logic trong code bạn gửi: Thông báo này SAI vị trí nếu killedCount == 0.
                // MessageBox.Show("Zalo has been Killed.", "Infomation", MessageBoxButtons.OK, MessageBoxIcon.Information); 

                // 1. Thiết lập thuộc tính cơ bản cho TextBox
                if (txtcode == null) return;
                txtcode.ReadOnly = true;
                txtcode.Multiline = true;
                txtcode.ScrollBars = ScrollBars.Vertical;

                // 2. Định nghĩa đường dẫn file config.json
                string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string zaloDataPath = Path.Combine(roamingPath, "ZaloData");
                string configFilePath = Path.Combine(zaloDataPath, "config.json");

                // 3. Đọc và định dạng JSON
                try
                {
                    if (File.Exists(configFilePath))
                    {
                        string rawJsonContent = File.ReadAllText(configFilePath);

                        // --- Áp dụng định dạng JSON đẹp ---
                        object jsonObject = JsonConvert.DeserializeObject(rawJsonContent);
                        string formattedJson = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

                        txtcode.Text = $"{formattedJson}";
                    }
                    else
                    {
                        MessageBox.Show($"Can not find config.json file at:\r\n{configFilePath}","Information",MessageBoxButtons.OK,MessageBoxIcon.Information);
                        Application.Exit();
                    }
                }
                catch (JsonReaderException)
                {
                    MessageBox.Show("Error: The content of config.json is not a valid JSON format.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading configuration file: {ex.Message}", "I/O File Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
            }
        }
        /// <summary>
        /// Kiểm tra xem có bất kỳ tiến trình nào với tên đã cho đang chạy hay không.
        /// </summary>
        /// <param name="processName">Tên tiến trình (KHÔNG bao gồm .exe).</param>
        /// <returns>True nếu tiến trình đang chạy, ngược lại là False.</returns>
        private int KillProcessWithWmi(string processName)
        {
            int killCount = 0;
            try
            {
                // ... (Code kết nối WMI) ...
                ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2");
                scope.Connect();

                ObjectQuery query = new ObjectQuery($"SELECT * FROM Win32_Process WHERE Name = '{processName}.exe'");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
                ManagementObjectCollection processList = searcher.Get();

                foreach (ManagementObject process in processList)
                {
                    try
                    {
                        uint result = (uint)process.InvokeMethod("Terminate", null);

                        if (result == 0)
                        {
                            killCount++;
                        }
                        // Bạn có thể bỏ qua lỗi này nếu Zalo đã thoát
                        // else 
                        // { 
                        //     // Bỏ MessageBox lỗi ở đây
                        // } 
                    }
                    catch { /* Bỏ qua các lỗi riêng lẻ của tiến trình nếu tiến trình đã thoát */ }
                }
            }
            catch (Exception ex)
            {
                // Giữ lại MessageBox lỗi tổng quát này để bắt lỗi nghiêm trọng
                // (như lỗi "Not found" do thiếu System.Management Reference)
                MessageBox.Show($"General error when using WMI: {ex.Message}", "WMI Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();

            }
            return killCount;
        }
        private string ConfigFilePath
        {
            get
            {
                string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string zaloDataPath = Path.Combine(roamingPath, "ZaloData");
                return Path.Combine(zaloDataPath, "config.json");
            }
        }
        private void btnedit_Click(object sender, EventArgs e)
        {
            string path = ConfigFilePath;

            try
            {
                if (!File.Exists(path))
                {
                    MessageBox.Show("Config.json file not found", "Errors!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string rawJson = File.ReadAllText(path);
                JObject jsonObject = JObject.Parse(rawJson);

                // --- THÊM/CẬP NHẬT TẤT CẢ CÁC KHÓA BẠN MUỐN ---

                // 1. Khóa số lớn (cần phải dùng JValue hoặc ép kiểu)
                // Khóa này thường có sẵn, ta chỉ cập nhật lại giá trị mong muốn
                jsonObject["zalo_installed"] = new JValue(1691996534469L); // Sử dụng 'L' cho số Long (tùy chọn)

                // 2. Các khóa cấu hình shortcut và kiến trúc hệ điều hành
                jsonObject["shortcut-screenshot"] = "CommandOrControl+Alt+S";
                jsonObject["ver_sc_cap"] = "1.0";
                jsonObject["shortcut-screenshot-withoutZ"] = "CommandOrControl+Alt+A";
                jsonObject["os_architecture"] = "64-bit";

                // 3. Khóa mới cần thêm
                jsonObject["disable_auto_update"] = true;

                // --- KẾT THÚC THÊM/CẬP NHẬT ---

                // Chuyển đổi đối tượng đã chỉnh sửa trở lại thành chuỗi JSON với định dạng đẹp
                string updatedJson = jsonObject.ToString(Formatting.Indented);

                // Ghi đè nội dung mới vào file config.json
                File.WriteAllText(path, updatedJson);

                MessageBox.Show("✅ Successfully updated config.json file!!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();

                // Cập nhật lại txtcode.Text để hiển thị nội dung mới
                if (txtcode != null)
                {
                    txtcode.Text = $"New content of config.json (Formatted):\r\n\r\n{updatedJson}";
                    txtcode.ReadOnly = true;
                    txtcode.Multiline = true;
                    txtcode.ScrollBars = ScrollBars.Vertical;
                }

            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON processing error: The file may be corrupted. {ex.Message}", "JSON syntax error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"File write error: {ex.Message}", "I/O Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();

            }
        }

        private void btnbackup_Click(object sender, EventArgs e)
        {
            string sourceFile = ConfigFilePath;

            // 1. Lấy đường dẫn đến Desktop của người dùng hiện tại
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 2. Định nghĩa tên folder Backup và đảm bảo nó tồn tại
            string backupFolderName = "ZaloConfig_Backup";
            string backupDirectory = Path.Combine(desktopPath, backupFolderName);

            // Tạo thư mục nếu nó chưa tồn tại
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            // 3. Tạo tên file đích (đã thêm timestamp để không ghi đè)
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string destFileName = $"config_backup_{timeStamp}.json";
            string destFile = Path.Combine(backupDirectory, destFileName);

            try
            {
                // 4. Kiểm tra file nguồn có tồn tại không
                if (!File.Exists(sourceFile))
                {
                    MessageBox.Show("❌ Error: Original config.json file not found for backup.", "Backup Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 5. Thực hiện sao chép file
                // Tham số thứ ba (true) là overwrite, nhưng vì ta dùng timestamp, nên đặt là false cũng được
                File.Copy(sourceFile, destFile, true);

                MessageBox.Show(
                    $"✅ Backup successful!!\nFile saved at: {destFile}",
                    "Sao Lưu Hoàn Tất",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                btnedit.Enabled = true;
                btnbackup.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error during backup process: {ex.Message}", "Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
    }
}

