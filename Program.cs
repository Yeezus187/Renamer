using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace FileRenamer
{
    public class MainForm : Form
    {
        // Steuerelemente
        private Button btnSelectFolder;
        private TextBox txtFolder;
        private Label lblX;
        private ComboBox cmbX;
        private Label lblY;
        private NumericUpDown numY;
        private Label lblZ;
        private NumericUpDown numZ;
        private Label lblZD;
        private ComboBox cmbZD;
        private Button btnRename;
        private Button btnUndo;

        // Hier wird der letzte Umbenennungsvorgang gespeichert (für Undo)
        private List<(string originalPath, string newPath)> lastRenameMapping = new List<(string, string)>();

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Grundeinstellungen des Fensters
            this.Text = "Datei Umbenenner";
            this.Size = new Size(500, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Ordner auswählen
            btnSelectFolder = new Button() { Text = "Ordner auswählen", Location = new Point(10, 10), Size = new Size(120, 25) };
            btnSelectFolder.Click += BtnSelectFolder_Click;
            this.Controls.Add(btnSelectFolder);

            txtFolder = new TextBox() { Location = new Point(140, 10), Size = new Size(330, 25), ReadOnly = true };
            this.Controls.Add(txtFolder);

            // X – Buchstabe auswählen (A bis Z)
            lblX = new Label() { Text = "X (Buchstabe):", Location = new Point(10, 50), Size = new Size(100, 25) };
            this.Controls.Add(lblX);
            cmbX = new ComboBox() { Location = new Point(120, 50), Size = new Size(60, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            for (char c = 'A'; c <= 'Z'; c++)
            {
                cmbX.Items.Add(c.ToString());
            }
            cmbX.SelectedIndex = 0;
            this.Controls.Add(cmbX);

            // Y – 3-stellige Zahl festlegen
            lblY = new Label() { Text = "YYY (3-stellig):", Location = new Point(10, 85), Size = new Size(100, 25) };
            this.Controls.Add(lblY);
            numY = new NumericUpDown()
            {
                Location = new Point(120, 85),
                Size = new Size(60, 25),
                Minimum = 0,
                Maximum = 999,
                Value = 100
            };
            this.Controls.Add(numY);

            // Z – Startwert festlegen
            lblZ = new Label() { Text = "Startwert Z:", Location = new Point(10, 120), Size = new Size(100, 25) };
            this.Controls.Add(lblZ);
            numZ = new NumericUpDown()
            {
                Location = new Point(120, 120),
                Size = new Size(60, 25),
                Minimum = 0,
                Maximum = 999,
                Value = 1
            };
            this.Controls.Add(numZ);

            // Auswahl, ob Z als 2- oder 3-stellige Zahl formatiert werden soll
            lblZD = new Label() { Text = "Z Stellen (2 oder 3):", Location = new Point(200, 120), Size = new Size(130, 25) };
            this.Controls.Add(lblZD);
            cmbZD = new ComboBox() { Location = new Point(340, 120), Size = new Size(60, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbZD.Items.Add("2");
            cmbZD.Items.Add("3");
            cmbZD.SelectedIndex = 0; // Standard: 2-stellig
            this.Controls.Add(cmbZD);

            // Button: Umbenennen
            btnRename = new Button() { Text = "Umbenennen", Location = new Point(10, 170), Size = new Size(120, 30) };
            btnRename.Click += BtnRename_Click;
            this.Controls.Add(btnRename);

            // Button: Rückgängig (Undo)
            btnUndo = new Button() { Text = "Rückgängig", Location = new Point(140, 170), Size = new Size(120, 30) };
            btnUndo.Click += BtnUndo_Click;
            this.Controls.Add(btnUndo);
        }

        // Ordnerauswahl – es wird ein FolderBrowserDialog geöffnet
        private void BtnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Wählen Sie einen Ordner aus:";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolder.Text = fbd.SelectedPath;
                }
            }
        }

        // Umbenennen-Knopf
        private void BtnRename_Click(object sender, EventArgs e)
        {
            // Prüfen, ob ein gültiger Ordner ausgewählt wurde
            if (string.IsNullOrWhiteSpace(txtFolder.Text) || !Directory.Exists(txtFolder.Text))
            {
                MessageBox.Show("Bitte wählen Sie einen gültigen Ordner aus.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string folderPath = txtFolder.Text;
            // Alle Dateien im ausgewählten Ordner (nicht rekursiv)
            string[] files = Directory.GetFiles(folderPath);
            if (files.Length == 0)
            {
                MessageBox.Show("Der ausgewählte Ordner enthält keine Dateien.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Bestätigung vor dem Umbenennen
            DialogResult result = MessageBox.Show($"Es werden {files.Length} Dateien umbenannt. Fortfahren?", "Bestätigung", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            // Vor Beginn des Vorgangs alte Mapping-Liste leeren
            lastRenameMapping.Clear();

            // Aus den Steuerelementen die Werte auslesen:
            string X = cmbX.SelectedItem.ToString();
            string Y = ((int)numY.Value).ToString("D3"); // immer 3-stellig
            int zDigitCount = int.Parse(cmbZD.SelectedItem.ToString());
            int Zstart = (int)numZ.Value;
            int currentZ = Zstart;

            foreach (string filePath in files)
            {
                try
                {
                    string directory = Path.GetDirectoryName(filePath);
                    string originalFileName = Path.GetFileName(filePath);

                    // Neuen Präfix bauen: z. B. "A-100-01"
                    string prefix = $"{X}-{Y}-{currentZ.ToString("D" + zDigitCount)}";
                    // Neuer Dateiname: Präfix + Leerzeichen + Originaldateiname
                    string newFileName = prefix + " " + originalFileName;
                    string newFullPath = Path.Combine(directory, newFileName);

                    // Prüfen, ob bereits eine Datei mit dem neuen Namen existiert
                    if (File.Exists(newFullPath))
                    {
                        MessageBox.Show($"Die Datei {newFileName} existiert bereits. Umbenennung abgebrochen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        // Rollback der bisherigen Umbenennungen dieses Vorgangs
                        UndoRenames();
                        return;
                    }

                    // Datei umbenennen
                    File.Move(filePath, newFullPath);
                    // Mapping für Rückgängig machen speichern
                    lastRenameMapping.Add((filePath, newFullPath));

                    // Z fortlaufend erhöhen
                    currentZ++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Umbenennen: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Bei Fehler: bisherige Umbenennungen zurückrollen
                    UndoRenames();
                    return;
                }
            }

            MessageBox.Show("Umbenennung abgeschlossen!", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Rückgängig-Knopf
        private void BtnUndo_Click(object sender, EventArgs e)
        {
            if (lastRenameMapping.Count == 0)
            {
                MessageBox.Show("Es gibt keine Umbenennung, die rückgängig gemacht werden kann.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show("Möchten Sie die letzte Umbenennung rückgängig machen?", "Rückgängig machen", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            // Rückgängig machen: In umgekehrter Reihenfolge die Dateien zurückbenennen
            for (int i = lastRenameMapping.Count - 1; i >= 0; i--)
            {
                var mapping = lastRenameMapping[i];
                try
                {
                    if (File.Exists(mapping.newPath))
                    {
                        File.Move(mapping.newPath, mapping.originalPath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Rückgängigmachen: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            MessageBox.Show("Rückgängig gemacht.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            lastRenameMapping.Clear();
        }

        // Hilfsmethode zum Rollback, falls während eines Umbenennungsvorgangs ein Fehler auftritt
        private void UndoRenames()
        {
            for (int i = lastRenameMapping.Count - 1; i >= 0; i--)
            {
                var mapping = lastRenameMapping[i];
                try
                {
                    if (File.Exists(mapping.newPath))
                    {
                        File.Move(mapping.newPath, mapping.originalPath);
                    }
                }
                catch { }
            }
            lastRenameMapping.Clear();
        }
    }

    // Programmstart
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
