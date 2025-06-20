using EonaCat.NightReign.Helpers;

namespace EonaCat.NightReign
{
    namespace EonaCat.NightReign
    {
        public class SteamIdSelectionForm : Form
        {
            private List<bool> isMatchingIdList = new();

            private ListBox listBox;
            private Button okButton;
            private Button cancelButton;
            private readonly Dictionary<string, string> steamAccounts;
            private readonly byte[] oldSteamId;

            public string SelectedSteamId { get; private set; }

            public SteamIdSelectionForm(Dictionary<string, string> steamAccounts, byte[] oldSteamId)
            {
                this.steamAccounts = steamAccounts ?? new();
                this.oldSteamId = oldSteamId;

                InitializeComponents();
            }

            private void InitializeComponents()
            {
                Text = "Select Your Steam Account";
                Size = new Size(400, 300);
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

                listBox = new ListBox
                {
                    Dock = DockStyle.Top,
                    Height = 200,
                    DrawMode = DrawMode.OwnerDrawFixed
                };
                listBox.DrawItem += ListBox_DrawItem;

                foreach (var kvp in steamAccounts)
                {
                    bool isMatch = oldSteamId != null && oldSteamId.SequenceEqual(SteamHelper.ConvertToSteamIdBytes(kvp.Key));
                    listBox.Items.Add($"{kvp.Value} ({kvp.Key})");
                    isMatchingIdList.Add(isMatch);
                }

                okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                    Width = 80,
                    Left = 200,
                    Top = 220
                };
                okButton.Click += OkButton_Click;

                cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                    Width = 80,
                    Left = 290,
                    Top = 220
                };

                Controls.Add(listBox);
                Controls.Add(okButton);
                Controls.Add(cancelButton);

                AcceptButton = okButton;
                CancelButton = cancelButton;
            }

            private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
            {
                if (e.Index < 0 || e.Index >= listBox.Items.Count) return;

                e.DrawBackground();

                bool isMatch = isMatchingIdList[e.Index];
                string text = listBox.Items[e.Index].ToString();

                using (Brush brush = new SolidBrush(isMatch ? Color.Red : e.ForeColor))
                {
                    e.Graphics.DrawString(text, e.Font, brush, e.Bounds);
                }

                e.DrawFocusRectangle();
            }


            private void OkButton_Click(object sender, EventArgs e)
            {
                if (listBox.SelectedItem != null)
                {
                    string selected = listBox.SelectedItem.ToString();
                    var match = steamAccounts.FirstOrDefault(kvp =>
                        selected.Contains(kvp.Key) && selected.Contains(kvp.Value));
                    SelectedSteamId = match.Key;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Please select a Steam account.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
