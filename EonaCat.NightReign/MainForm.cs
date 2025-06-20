using EonaCat.NightReign.EonaCat.NightReign;
using EonaCat.NightReign.Helpers;

namespace EonaCat.NightReign
{
    public partial class MainForm : Form
    {
        private const int STEAM_ID_BYTE_LENGTH = 8;

        private System.ComponentModel.IContainer components = null;
        private string _steamId;

        public object NeightReignFileName => "NR0000";

        public MainForm()
        {
            InitializeComponent();
            SetupFormUI();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void SetupFormUI()
        {
            Text = "EonaCat ER NightReign Save Transfer";
            Size = new Size(400, 150);
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;

            var label = new Label
            {
                Text = "This tool allows you to change the Steam ID in your Elden Ring NightReign save file.",
                Width = 360,
                Height = 40,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Top = 10,
                Left = 10
            };

            var decryptButton = new Button
            {
                Text = "Select save file for steam ID change",
                Width = 220,
                Height = 30,
                Top = 50,
                Left = 35
            };
            decryptButton.Click += DecryptButton_Click;

            Controls.Add(label);
            Controls.Add(decryptButton);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            BackgroundImage = Properties.Resources._1;
            ClientSize = new Size(800, 450);
            Name = "MainForm";
            ResumeLayout(false);
        }

        private void DecryptButton_Click(object sender, EventArgs e)
        {
            var inputFile = GetInputFile();
            if (string.IsNullOrWhiteSpace(inputFile))
            {
                return;
            }

            string folderPath;
            try
            {
                folderPath = FileEngine.Decrypt(inputFile, Console.WriteLine);
            }
            catch (Exception ex)
            {
                ShowError("Failed to decrypt SL2 file", ex.Message);
                return;
            }

            var ERDataFiles = Directory.GetFiles(folderPath, "ELDENRING_DATA*").OrderBy(f => f).ToArray();
            string ERData10Path = Path.Combine(folderPath, "ELDENRING_DATA_10");

            if (!File.Exists(ERData10Path))
            {
                ShowError("Missing File", $"ELDENRING_DATA_10 not found in {folderPath}");
                return;
            }

            byte[] oldSteamId;
            try
            {
                using var fs = new FileStream(ERData10Path, FileMode.Open, FileAccess.Read);
                fs.Seek(0x8, SeekOrigin.Begin);
                oldSteamId = new byte[STEAM_ID_BYTE_LENGTH];
                fs.Read(oldSteamId, 0, STEAM_ID_BYTE_LENGTH);
            }
            catch (Exception ex)
            {
                ShowError("Failed to read ELDENRING_DATA_10", ex.Message);
                return;
            }

            Console.WriteLine("Old Steam ID (bytes): " + BitConverter.ToString(oldSteamId));

            var steamIds = SteamHelper.GetAllSteamAccounts();
            var newSteamId = string.Empty;
            byte[] newSteamIdBytes = null;

            // If there are steamIds, show them in a dialog to select
            if (steamIds != null && steamIds.Count > 0)
            {
                var steamIdForm = new SteamIdSelectionForm(steamIds, oldSteamId);
                if (steamIdForm.ShowDialog() == DialogResult.OK)
                {
                    newSteamId = steamIdForm.SelectedSteamId;
                    newSteamIdBytes = SteamHelper.ConvertToSteamIdBytes(newSteamId);
                }
            }

            if (steamIds == null || steamIds.Count() == 0 || string.IsNullOrEmpty(newSteamId) || newSteamId.Length != 17 || !newSteamId.All(char.IsDigit) || newSteamIdBytes  == null)
            {
                AskSteamIdWindow(x =>
                {
                    newSteamIdBytes = x;
                });
            }

            if (newSteamIdBytes == null)
            {
                MessageBox.Show("Invalid Steam ID format. Please enter a valid 17-digit Steam ID.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (oldSteamId.SequenceEqual(newSteamIdBytes))
            {
                MessageBox.Show("The new Steam ID is the same as the old one.", "No Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Console.WriteLine("New Steam ID (bytes): " + BitConverter.ToString(newSteamIdBytes));
            int filesModified = 0;

            foreach (var file in ERDataFiles)
            {
                byte[] data = File.ReadAllBytes(file);
                if (!data.ContainsSubsequence(oldSteamId))
                {
                    continue;
                }

                var newData = BytesHelper.ReplaceBytes(data, oldSteamId, newSteamIdBytes);
                if (!data.SequenceEqual(newData))
                {
                    File.WriteAllBytes(file, newData);
                    filesModified++;
                }
            }

            if (filesModified == 0)
            {
                MessageBox.Show("No files were modified. The old Steam ID might not be present in any slots.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _steamId = newSteamId;
            Console.WriteLine($"Steam ID replaced in {filesModified} file(s)");

            var outputFile = GetOutputFile();
            if (string.IsNullOrEmpty(outputFile))
            {
                return;
            }

            try
            {
                FileEngine.Encrypt(outputFile);
                MessageBox.Show($"Save file saved as {outputFile}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FileEngine.RemoveEncryptedFolder();
            }
            catch (Exception ex)
            {
                ShowError("Failed to re-encrypt and save", ex.Message);
            }
        }

        private string GetInputFile()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select SL2 File",
                Filter = "SL2 Files (*.sl2)|*.sl2|All Files (*.*)|*.*"
            };
            return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : null;
        }

        private string GetOutputFile()
        {
            string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nightreign");
            string path = !string.IsNullOrEmpty(_steamId) && _steamId.Length == 17 && Directory.Exists(Path.Combine(basePath, _steamId))
                ? Path.Combine(basePath, _steamId)
                : basePath;

            using var sfd = new SaveFileDialog
            {
                Title = "Save New Encrypted SL2 File As",
                Filter = "SL2 Files (*.sl2)|*.sl2|All Files (*.*)|*.*",
                FileName = $"{NeightReignFileName}.sl2",
                DefaultExt = "sl2",
                InitialDirectory = path
            };
            return sfd.ShowDialog() == DialogResult.OK ? sfd.FileName : null;
        }

        private void AskSteamIdWindow(Action<byte[]> callback)
        {
            var inputForm = new Form
            {
                Text = "Enter your 17 digits Steam ID (steamID64 (Dec))",
                Size = new Size(450, 150),
                MaximizeBox = false,
                MinimizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                BackgroundImage = Properties.Resources._1,
                ForeColor = Color.White,
            };

            var label = new Label
            {
                Text = "Enter your 17-digit Steam ID:",
                Top = 20,
                Left = 20,
                Width = 300,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var inputBox = new TextBox
            {
                Top = 50,
                Left = 20,
                Width = 200
            };

            var submitBtn = new Button
            {
                Text = "Submit",
                Top = 80,
                Left = 20,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
            };

            submitBtn.Click += (s, e) =>
            {
                string input = inputBox.Text.Trim();
                if (!IsValidSteamId(input))
                {
                    MessageBox.Show("Steam ID must be exactly 17 digits!", "Invalid Steam ID", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _steamId = input;
                byte[] steamIdBytes = SteamHelper.ConvertToSteamIdBytes(_steamId);
                inputForm.Close();
                callback(steamIdBytes);
            };

            inputForm.Controls.Add(label);
            inputForm.Controls.Add(inputBox);
            inputForm.Controls.Add(submitBtn);
            inputForm.AcceptButton = submitBtn;
            inputForm.ShowDialog();
        }

        private bool IsValidSteamId(string input) =>
            input.Length == 17 && input.All(char.IsDigit);

        private void ShowError(string title, string message) =>
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
