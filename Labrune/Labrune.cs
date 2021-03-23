using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Labrune
{
    public partial class Labrune : Form
    {
        public Labrune()
        {
            InitializeComponent();
        }

        List<LanguageChunk> LangChunks = new List<LanguageChunk>();
        bool IsFileModified = false;
        bool HasLabels = false;

        List<int> FoundValuesIndexes = new List<int>();
        List<int> ModifiedValuesIndexes = new List<int>();
        int FindIndex;
        int ModifyIndex;
        int CurrentChunk = 0;

        string ReadNullTerminated(BinaryReader rdr)
        {
            var bldr = new List<Byte>();
            byte nc;
            while ((nc = rdr.ReadByte()) > 0)
                bldr.Add(nc);

       

            return Encoding.GetEncoding("ISO-8859-1").GetString(bldr.ToArray());
        }

        public MemoryStream DecryptWorldLanguageFile(String LangFilePath)
        {
            byte[] LangFileArray = File.ReadAllBytes(LangFilePath); // Read

            if (LangFileArray[0] == 0x6B) // If encrypted
            {
                for (int i = LangFileArray.Length - 1; i >= 1; --i) // Decrypt
                    LangFileArray[i] ^= LangFileArray[i - 1];

                LangFileArray[0] ^= 0x6B;

             
            }

            return new MemoryStream(LangFileArray);

        }
        
        public void MarkFileAsModified()
        {
            if (!IsFileModified) Text = "*" + Text;
            IsFileModified = true;
            ModifiedValuesIndexes.Clear();
            PrevModifiedToolStripMenuItem.Enabled = true;
            NextModifiedToolStripMenuItem.Enabled = true;
            textFileModifiedEntriesOnlyToolStripMenuItem.Enabled = true;
        }

        public void MarkFileAsUnModified()
        {
            if (IsFileModified) Text = Text.TrimStart('*');
            IsFileModified = false;
            ModifiedValuesIndexes.Clear();
            PrevModifiedToolStripMenuItem.Enabled = false;
            NextModifiedToolStripMenuItem.Enabled = false;
            textFileModifiedEntriesOnlyToolStripMenuItem.Enabled = false;
        }

        public void SortStringRecords()
        {
            LangChunks[CurrentChunk].Strings.Sort((x, y) => x.Hash.CompareTo(y.Hash));
        }

        public void RefreshStringView()
        {
            String SelectedItem = String.Empty;
            if (LangStringView.SelectedItems.Count != 0) SelectedItem = LangStringView.SelectedItems[0].SubItems[1].Text; // Get the hash as the text

            LangStringView.Items.Clear();
            int StrID = 0;

            LangStringView.BeginUpdate();

            CurrentChunk = LangChunkSelector.SelectedIndex;

            foreach (LanguageStringRecord StR in LangChunks[CurrentChunk].Strings)
            {
                var StrItm = new ListViewItem();

                StrItm.Text = StrID++.ToString();
                StrItm.SubItems.Add(StR.Hash.ToString("X8"));
                StrItm.SubItems.Add(StR.Label);
                StrItm.SubItems.Add(StR.Text);
                if (StR.IsModified == true) StrItm.BackColor = Color.LightYellow;
                LangStringView.Items.Add(StrItm);
            }

            LangStringView.EndUpdate();

            if (SelectedItem != String.Empty)
            {
                var ItemWithTheHash = LangStringView.FindItemWithText(SelectedItem, true, 0);
                if (ItemWithTheHash != null)
                {
                    LangStringView.Items[ItemWithTheHash.Index].Selected = true;
                    LangStringView.Items[ItemWithTheHash.Index].EnsureVisible();
                }
            }
        }

        public void EditStringRecord(uint _Hash, uint _NewHash, string _NewLabel, string _NewText)
        {
            var StrRec = LangChunks[CurrentChunk].Strings.Find(x => x.Hash == _Hash);
            if (StrRec != null)
            {
                StrRec.Hash = _NewHash;
                StrRec.Label = _NewLabel;
                StrRec.Text = _NewText;
                StrRec.IsModified = true;
            }

            MarkFileAsModified();

            if (_Hash != _NewHash)
            {
                SortStringRecords();
            }
            RefreshStringView();
        }

        public void EditStringRecord_NoUpdate(uint _Hash, uint _NewHash, string _NewLabel, string _NewText)
        {
            var StrRec = LangChunks[CurrentChunk].Strings.Find(x => x.Hash == _Hash);
            if (StrRec != null)
            {
                StrRec.Hash = _NewHash;
                StrRec.Label = _NewLabel;
                StrRec.Text = _NewText;
                StrRec.IsModified = true;
            }
        }

        public void AddNewStringRecord(uint _Hash, string _Label, string _Text)
        {
            var NewStrRec = new LanguageStringRecord(_Hash, _Label, _Text);
            var StrRec = LangChunks[CurrentChunk].Strings.Find(x => x.Hash == _Hash);

            if (StrRec != null) // Add if there isn't another string with the same hash
            {
                EditStringRecord(_Hash, _Hash, _Label, _Text);
            }
            else // Edit if there is already one
            {
                LangChunks[CurrentChunk].Strings.Add(NewStrRec);
            }

            MarkFileAsModified();
            SortStringRecords();
            RefreshStringView();
        }

        public void AddNewStringRecord_NoUpdate(uint _Hash, string _Label, string _Text)
        {
            var NewStrRec = new LanguageStringRecord(_Hash, _Label, _Text);
            var StrRec = LangChunks[CurrentChunk].Strings.Find(x => x.Hash == _Hash);

            if (StrRec != null)  // Add if there isn't another string with the same hash
            {
                EditStringRecord_NoUpdate(_Hash, _Hash, _Label, _Text);
            }
            else
            {
                LangChunks[CurrentChunk].Strings.Add(NewStrRec);
            }
            
        }

        public void UpdateAfterImport()
        {
            MarkFileAsModified();

            foreach (LanguageChunk i in LangChunks)
            {
                CurrentChunk = LangChunks.IndexOf(i);
                SortStringRecords();
            }

            LangChunkSelector.SelectedIndex = 0;
            RefreshStringView();
        }

        public void RemoveStringRecord(uint _Hash)
        {
            var StrRec = LangChunks[CurrentChunk].Strings.Find(x => x.Hash == _Hash);
            if (StrRec != null)
            {
                LangChunks[CurrentChunk].Strings.Remove(StrRec);
            }

            MarkFileAsModified();
            SortStringRecords();
            RefreshStringView();
        }

        public void EnableMenuOptions()
        {
            ImportToolStripMenuItem.Enabled = true;
            exportToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            AddToolStripMenuItem.Enabled = true;
            EditStrToolStripMenuItem.Enabled = true;
            RemoveToolStripMenuItem.Enabled = true;
            SearchToolStripMenuItem.Enabled = true;
        }

        public void DisableMenuOptions()
        {
            ImportToolStripMenuItem.Enabled = false;
            exportToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Enabled = false;
            AddToolStripMenuItem.Enabled = false;
            EditStrToolStripMenuItem.Enabled = false;
            RemoveToolStripMenuItem.Enabled = false;
            SearchToolStripMenuItem.Enabled = false;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OpenLanguageFileDlg.ShowDialog() == DialogResult.OK)
            {
                // Initialize
                foreach(LanguageChunk c in LangChunks) c.Strings.Clear();
                LangChunks.Clear();
                LangChunkSelector.Items.Clear();
                int NrLangChk = 0;

                MemoryStream LangFileStream = DecryptWorldLanguageFile(OpenLanguageFileDlg.FileName); // Decrypt the file if it's from NFSW.

                using (BinaryReader StringFileReader = new BinaryReader(LangFileStream))
                {
                    while (StringFileReader.BaseStream.Position < StringFileReader.BaseStream.Length)
                    {
                        uint ChkID = StringFileReader.ReadUInt32();
                        int ChkSz = StringFileReader.ReadInt32();

                        if (ChkID == 0x00039000) // 00 90 03 00 - BCHUNK_LANGUAGE
                        {
                            var LngChk = new LanguageChunk();
                            LngChk.Offset = (int)StringFileReader.BaseStream.Position - 8;
                            LngChk.Size = ChkSz;
                            LngChk.NumberOfStringRecords = StringFileReader.ReadInt32();

                            // Check if it's old (MW, U, U2) style or new (C+) style.
                            if (LngChk.NumberOfStringRecords == 0x10) // There is a 0x10 instead of string record count in old files. (Quick and dirty approach)
                            {
                                LngChk.Version = LanguageFileVersion.Old;
                                LngChk.NumberOfStringRecords = StringFileReader.ReadInt32();
                                LngChk.StringRecordsOffset = StringFileReader.ReadInt32();
                                LngChk.TextOffset = StringFileReader.ReadInt32();
                                LngChk.UnkData = StringFileReader.ReadBytes(LngChk.StringRecordsOffset - 0x10);
                            }
                            else
                            {
                                LngChk.Version = LanguageFileVersion.New;
                                LngChk.StringRecordsOffset = StringFileReader.ReadInt32();
                                LngChk.TextOffset = StringFileReader.ReadInt32();
                                LngChk.Category = ReadNullTerminated(StringFileReader);
                            }

                            LngChk.Strings = new List<LanguageStringRecord>();

                            StringFileReader.BaseStream.Position = LngChk.Offset + 8 + LngChk.StringRecordsOffset;

                            for (int i=0; i<LngChk.NumberOfStringRecords; i++) // Traverse all the string records and read them
                            {
                                StringFileReader.BaseStream.Position = LngChk.Offset + 8 + LngChk.StringRecordsOffset + i * 8;

                                var StrRec = new LanguageStringRecord();

                                StrRec.Hash = StringFileReader.ReadUInt32();

                                StringFileReader.BaseStream.Position = LngChk.Offset + 8 + LngChk.TextOffset + StringFileReader.ReadInt32();
                                StrRec.Text = ReadNullTerminated(StringFileReader);
                                StrRec.IsModified = false;
                                
                                LngChk.Strings.Add(StrRec);
                            }

                            LangChunks.Add(LngChk);
                            LangChunkSelector.Items.Add("#" + NrLangChk++ + (LngChk.Version == LanguageFileVersion.New ? " - " + LngChk.Category : ""));

                            StringFileReader.BaseStream.Position = LngChk.Offset + 8 + LngChk.Size;

                        }
                        else
                        {
                            StringFileReader.BaseStream.Position += ChkSz;
                        }
                    }
                }

                LangFileStream.Dispose();
                LangFileStream.Close();

                // Check for labels

                String LabelFileType = OpenLanguageFileDlg.FileName.LastIndexOf('_') == -1 ? "" : OpenLanguageFileDlg.FileName.Substring(OpenLanguageFileDlg.FileName.LastIndexOf('_'));
                String LabelFileName = Path.Combine(Path.GetDirectoryName(OpenLanguageFileDlg.FileName), "Labels" + (String.IsNullOrEmpty(LabelFileType) ? ".bin" : LabelFileType));
                if (File.Exists(LabelFileName))
                {
                    NrLangChk = 0;

                    MemoryStream LabelFileStream = DecryptWorldLanguageFile(LabelFileName);

                    using (BinaryReader LabelFileReader = new BinaryReader(LabelFileStream))
                    {
                        while (LabelFileReader.BaseStream.Position < LabelFileReader.BaseStream.Length)
                        {
                            uint ChkID = LabelFileReader.ReadUInt32();
                            int ChkSz = LabelFileReader.ReadInt32();

                            if (ChkID == 0x00039000) // 00 90 03 00 - BCHUNK_LANGUAGE
                            {
                                // TODO: Check if it's old (MW, U, U2) style or new (C+) style.
                                // TODO: Decrypt the file if it's from NFSW.

                                var LngChk = new LanguageChunk();
                                LngChk.Offset = (int)LabelFileReader.BaseStream.Position - 8;
                                LngChk.Size = ChkSz;
                                LngChk.NumberOfStringRecords = LabelFileReader.ReadInt32();


                                if (LngChk.NumberOfStringRecords == 0x10) // There is a 0x10 instead of string record count in old files. (Quick and dirty approach)
                                {
                                    LngChk.Version = LanguageFileVersion.Old;
                                    LngChk.NumberOfStringRecords = LabelFileReader.ReadInt32();
                                    LngChk.StringRecordsOffset = LabelFileReader.ReadInt32();
                                    LngChk.TextOffset = LabelFileReader.ReadInt32();
                                    LngChk.UnkData = LabelFileReader.ReadBytes(LngChk.StringRecordsOffset - 0x10);
                                }
                                else
                                {
                                    LngChk.Version = LanguageFileVersion.New;
                                    LngChk.StringRecordsOffset = LabelFileReader.ReadInt32();
                                    LngChk.TextOffset = LabelFileReader.ReadInt32();
                                    LngChk.Category = ReadNullTerminated(LabelFileReader);
                                }

                                LngChk.Strings = new List<LanguageStringRecord>();

                                LabelFileReader.BaseStream.Position = LngChk.Offset + 8 + LngChk.StringRecordsOffset;

                                for (int i = 0; i < LngChk.NumberOfStringRecords; i++) // Traverse all the string records and read them
                                {
                                    LabelFileReader.BaseStream.Position = LngChk.Offset + 8 + LngChk.StringRecordsOffset + i * 8;

                                    var StrRec = new LanguageStringRecord();
                                    
                                    StrRec.Hash = LabelFileReader.ReadUInt32();

                                    LabelFileReader.BaseStream.Position = LngChk.Offset + 8 + LngChk.TextOffset + LabelFileReader.ReadInt32();
                                    StrRec.Text = ReadNullTerminated(LabelFileReader);

                                    /*LngChk.Strings.Add(StrRec);*/

                                    foreach (LanguageStringRecord StR in LangChunks[NrLangChk].Strings)
                                    {
                                        if (StrRec.Hash == StR.Hash)
                                        {
                                            StR.Label = StrRec.Text;
                                            break;
                                        }
                                    }

                                }

                                NrLangChk++;
                                LabelFileReader.BaseStream.Position = LngChk.Offset + 8 + LngChk.Size;

                            }
                            else
                            {
                                LabelFileReader.BaseStream.Position += ChkSz;
                            }
                        }
                    }

                    HasLabels = true;

                }
                

            }

            if (LangChunkSelector.Items.Count > 0)
            {
                LangChunkSelector.SelectedItem = LangChunkSelector.Items[0];
                StatusBarText.Text = "Hazır.";
                Text = OpenLanguageFileDlg.FileName;
                EnableMenuOptions();

                if (LangChunkSelector.Items.Count == 1) LangChunkSelector.Enabled = false;
                else LangChunkSelector.Enabled = true;
            }
            else
            {
                StatusBarText.Text = "Seçili dosyada hiçbir dil parçası algılanmadı.";
                DisableMenuOptions();
                LangChunkSelector.Enabled = false;
            }
        }

        private void LangChunkSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            ModifiedValuesIndexes.Clear();
            FoundValuesIndexes.Clear();

            FindNextToolStripMenuItem.Enabled = false;
            FindPreviousToolStripMenuItem.Enabled = false;

            RefreshStringView();
        }

        private void fontSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(FontSettingsDlg.ShowDialog() == DialogResult.OK)
            {
                LangStringView.Font = FontSettingsDlg.Font;
            }
        }

        private void LangStringView_DoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = LangStringView.HitTest(e.X, e.Y);
            ListViewItem item = info.Item;

            if (item != null)
            {
                var StringEditor = new LabruneEdit();
                if (BinHash.Hash(item.SubItems[2].Text).ToString("X8") != item.SubItems[1].Text) StringEditor.CheckUseCustomHash.Checked = true;
                StringEditor.ID = item.Text;
                StringEditor.Hash = item.SubItems[1].Text;
                StringEditor.Label = item.SubItems[2].Text;
                StringEditor.Value = item.SubItems[3].Text;

                var Result = StringEditor.ShowDialog();

                if (Result == DialogResult.OK)
                {
                    EditStringRecord(uint.Parse(StringEditor.Hash, System.Globalization.NumberStyles.HexNumber), uint.Parse(StringEditor.NewHash, System.Globalization.NumberStyles.HexNumber), StringEditor.NewLabel, StringEditor.NewValue);
                }
                
            }

            else
            {
                LangStringView.SelectedItems.Clear();
            }
        }

        private void Labrune_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsFileModified == true)
            {

                TaskDialog ExitDialog = new TaskDialog();
                ExitDialog.StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No | TaskDialogStandardButtons.Cancel;

                if (HasLabels && NFS_Dil_Duzenleyici.Properties.Settings.Default.AlsoSaveLabels)
                {
                    ExitDialog.FooterText = "Bu işlem aynı zamanda Etiketler dosyasını da kaydedecektir.";

                    ExitDialog.FooterIcon = TaskDialogStandardIcon.Information;
                }

                ExitDialog.Icon = TaskDialogStandardIcon.Warning;
                ExitDialog.InstructionText = "Kaydet?";
                ExitDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                ExitDialog.Text = "Çıkmadan önce değişikliklerinizi kaydetmek ister misiniz?";

                var result = ExitDialog.Show();

                if (result == TaskDialogResult.Yes)
                {
                    SaveToolStripMenuItem_Click(this, new EventArgs());
                }
                else if (result == TaskDialogResult.Cancel)
                {
                    // Return to the form
                    e.Cancel = true;
                }

            }
        }

        private void LabruneTextFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String TXTLineBuffer;
            int ImportedEntries = 0;
            int IgnoredEntries = 0;

            if (OpenLabruneDumpDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FileStream TXTFileStream = File.Open(OpenLabruneDumpDialog.FileName, FileMode.Open);

                    using (StreamReader TXTFile = new StreamReader(TXTFileStream))
                    {
                        while ((TXTLineBuffer = TXTFile.ReadLine()) != null)
                        {
                            if (!TXTLineBuffer.StartsWith("#")) // If it's not a comment
                            {
                                char[] charsToTrim = {'\t'};
                                String[] Parts = TXTLineBuffer.Split(charsToTrim, StringSplitOptions.None);

                                if (Parts.Length >= 4)
                                {
                                    try
                                    {
                                        // Merge string if it contains tabs
                                        String LabruneString = "";
                                        for (int i = 3; i < Parts.Length; i++) LabruneString += Parts[i] + '\t';

                                        CurrentChunk = Int32.Parse(Parts[0]); // Switch to the specified chunk
                                        if (CurrentChunk <= LangChunks.Count)
                                        {
                                            AddNewStringRecord_NoUpdate(UInt32.Parse(Parts[1], System.Globalization.NumberStyles.HexNumber), Parts[2], LabruneString);
                                            ImportedEntries++;
                                        }
                                        else IgnoredEntries++;

                                    }
                                    catch (Exception)
                                    {
                                        IgnoredEntries++;
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    UpdateAfterImport();

                    String Status = "Imported" + " " + ImportedEntries + " " + "entries for" + " " + LangChunks.Count + " " + "language chunk(s).";
                    if (IgnoredEntries > 0) Status += "\n" + "Ignored" + " " + IgnoredEntries + " " + "entries due to some formatting errors in selected text file.";


                    TaskDialog MsgDialog = new TaskDialog();
                    MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    MsgDialog.Icon = TaskDialogStandardIcon.Information;
                    MsgDialog.InstructionText = "Bitti!";
                    MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs"; 
                    MsgDialog.Text = Status;
                    MsgDialog.Show();

                }
                catch (Exception ex)
                {
                    TaskDialog ErrDialog = new TaskDialog();
                    ErrDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    ErrDialog.Icon = TaskDialogStandardIcon.Error;
                    ErrDialog.InstructionText = "Hata!";
                    ErrDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    ErrDialog.Text = "There is something wrong with the selected text file." + Environment.NewLine + "It may be currently used, corrupted or Labrune doesn't have enough permissions to process it.";
                    ErrDialog.DetailsExpanded = false;
                    ErrDialog.DetailsExpandedText = ex.ToString();
                    ErrDialog.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter;
                    ErrDialog.Show();
                }
            }
        }

        private void AddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var StringEditor = new LabruneEdit();
            StringEditor.IsNewString = true;

            var Result = StringEditor.ShowDialog();

            if (Result == DialogResult.OK)
            {
                AddNewStringRecord(uint.Parse(StringEditor.NewHash, System.Globalization.NumberStyles.HexNumber), StringEditor.NewLabel, StringEditor.NewValue);
            }
        }

        private void RemoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem item = null;
            if (LangStringView.SelectedItems.Count != 0) item = LangStringView.SelectedItems[0];

            if (item != null)
            {
                RemoveStringRecord(uint.Parse(item.SubItems[1].Text,System.Globalization.NumberStyles.HexNumber));
            }

            else
            {
                LangStringView.SelectedItems.Clear();
            }
        }

        private void EditStrToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewItem item = null;
            if (LangStringView.SelectedItems.Count != 0) item = LangStringView.SelectedItems[0];

            if (item != null)
            {
                var StringEditor = new LabruneEdit();
                if (BinHash.Hash(item.SubItems[2].Text).ToString("X8") != item.SubItems[1].Text) StringEditor.CheckUseCustomHash.Checked = true;
                StringEditor.ID = item.Text;
                StringEditor.Hash = item.SubItems[1].Text;
                StringEditor.Label = item.SubItems[2].Text;
                StringEditor.Value = item.SubItems[3].Text;

                var Result = StringEditor.ShowDialog();

                if (Result == DialogResult.OK)
                {
                    EditStringRecord(uint.Parse(StringEditor.Hash, System.Globalization.NumberStyles.HexNumber), uint.Parse(StringEditor.NewHash, System.Globalization.NumberStyles.HexNumber), StringEditor.NewLabel, StringEditor.NewValue);
                }

            }

            else
            {
                LangStringView.SelectedItems.Clear();
            }
        }

        private void SearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var Finder = new LabruneFind();
            Finder.ValueToFind = "";
            FindNextToolStripMenuItem.Enabled = false;
            FindPreviousToolStripMenuItem.Enabled = false;

            foreach (ListViewItem item in LangStringView.Items) // Clear previous results
            {
                if (item.BackColor == Color.LightGreen)
                {
                    item.BackColor = Color.FromKnownColor(KnownColor.Window);
                }
            }

            var Result = Finder.ShowDialog();

            if ((Result == DialogResult.OK) && (Finder.ValueToFind != String.Empty))
            {
                FoundValuesIndexes.Clear();

                if (Finder.AlsoSearchInHashesAndLabels)
                {
                    if (Finder.IsCaseSensitive)
                    {
                        foreach (ListViewItem item in LangStringView.Items)
                        {
                            if (item.SubItems[1].Text.Contains(Finder.ValueToFind) || item.SubItems[2].Text.Contains(Finder.ValueToFind) || item.SubItems[3].Text.Contains(Finder.ValueToFind))
                            {
                                FoundValuesIndexes.Add(item.Index);
                                item.BackColor = Color.LightGreen; // Mark results
                            }
                        }
                    }

                    else
                    {
                        foreach (ListViewItem item in LangStringView.Items)
                        {
                            var enUS = new System.Globalization.CultureInfo("en-US");

                            if (item.SubItems[1].Text.ToUpper(enUS).Contains(Finder.ValueToFind.ToUpper(enUS))|| item.SubItems[2].Text.ToUpper(enUS).Contains(Finder.ValueToFind.ToUpper(enUS)) || item.SubItems[3].Text.ToUpper(enUS).Contains(Finder.ValueToFind.ToUpper(enUS)))
                            {
                                FoundValuesIndexes.Add(item.Index);
                                item.BackColor = Color.LightGreen; // Mark results
                            }
                        }
                    }
                }
                else
                {
                    if (Finder.IsCaseSensitive)
                    {
                        foreach (ListViewItem item in LangStringView.Items)
                        {
                            if (item.SubItems[3].Text.Contains(Finder.ValueToFind))
                            {
                                FoundValuesIndexes.Add(item.Index);
                                item.BackColor = Color.LightGreen; // Mark results
                            }
                        }
                    }

                    else
                    {
                        foreach (ListViewItem item in LangStringView.Items)
                        {
                            if (item.SubItems[3].Text.ToUpper().Contains(Finder.ValueToFind.ToUpper()))
                            {
                                FoundValuesIndexes.Add(item.Index);
                                item.BackColor = Color.LightGreen; // Mark results
                            }
                        }
                    }
                }
                
                if (FoundValuesIndexes.Count != 0)
                {
                    FindIndex = 0;
                    LangStringView.SelectedIndices.Clear();
                    LangStringView.Items[FoundValuesIndexes[FindIndex]].Selected = true;
                    LangStringView.Items[FoundValuesIndexes[FindIndex]].EnsureVisible();

                    FindNextToolStripMenuItem.Enabled = true;
                    FindPreviousToolStripMenuItem.Enabled = true;
                }

                else
                {
                    TaskDialog MsgDialog = new TaskDialog();
                    MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    MsgDialog.Icon = TaskDialogStandardIcon.Warning;
                    MsgDialog.InstructionText = "Bulunamadı.";
                    MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    MsgDialog.Text = "Arama, metin içeren hiçbir şey bulamadı \"" + Finder.ValueToFind + "\".";
                    MsgDialog.Show();

                    FindNextToolStripMenuItem.Enabled = false;
                    FindPreviousToolStripMenuItem.Enabled = false;
                }
            }
        }

        private void FindPreviousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FoundValuesIndexes.Count != 0)
            {
                FindIndex = (FindIndex - 1 + FoundValuesIndexes.Count) % FoundValuesIndexes.Count;
                LangStringView.SelectedIndices.Clear();
                LangStringView.Items[FoundValuesIndexes[FindIndex]].Selected = true;
                LangStringView.Items[FoundValuesIndexes[FindIndex]].EnsureVisible();
            }
        }

        private void FindNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FoundValuesIndexes.Count != 0)
            {
                FindIndex = (FindIndex + 1) % FoundValuesIndexes.Count;
                LangStringView.SelectedIndices.Clear();
                LangStringView.Items[FoundValuesIndexes[FindIndex]].Selected = true;
                LangStringView.Items[FoundValuesIndexes[FindIndex]].EnsureVisible();
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ReCompilerConfigurationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "Yeniden Derleme için Dil Yapılandırma dosyalarını içeren klasörü seçin";

            int ImportedEntries = 0;
            int IgnoredEntries = 0;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DirectoryInfo di = new DirectoryInfo(dialog.FileName);
                FileInfo[] INIFiles = di.GetFiles("*.ini");

                if (INIFiles.Length == 0)
                {
                    TaskDialog MsgNDialog = new TaskDialog();
                    MsgNDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    MsgNDialog.Icon = TaskDialogStandardIcon.Warning;
                    MsgNDialog.InstructionText = "Bulunamadı.";
                    MsgNDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    MsgNDialog.Text = "Seçili klasörde Yeniden Derleme Dil Ayar dosyası yok.";
                    MsgNDialog.Show();
                }
                else
                {
                    foreach (FileInfo i in INIFiles)
                    {
                        try
                        {
                            var IniReader = new IniFile(i.FullName);
                            String RCLabel = Path.GetFileNameWithoutExtension(i.FullName);
                            String RCText = IniReader.GetValue("INFO", "Name", ""); // Get text

                            if (!(String.IsNullOrEmpty(RCLabel) || String.IsNullOrEmpty(RCText)))
                            {
                                uint RCHash = (uint)BinHash.Hash(RCLabel); // Hash the label
                                AddNewStringRecord_NoUpdate(RCHash, RCLabel, RCText); // Add
                                ImportedEntries++;
                            }
                            else IgnoredEntries++;
                        }
                        
                        catch (Exception)
                        {
                            IgnoredEntries++;
                        }
                    }
                    UpdateAfterImport();

                    String Status = "İçe Aktarılan Toplam" + " " + ImportedEntries + " " + " girdi.";
                    if (IgnoredEntries > 0) Status += "\n" + " Hatalı " + " " + IgnoredEntries + " " + "Kayıt Yok sayıldı";

                    TaskDialog MsgDialog = new TaskDialog();
                    MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    MsgDialog.Icon = TaskDialogStandardIcon.Information;
                    MsgDialog.InstructionText = "Bitti!";
                    MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    MsgDialog.Text = Status;
                    MsgDialog.Show();
                }
            }
        }

        private void EdConfigurationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "Yapılandırma dosyalarını içeren klasörü seçin.";

            int ImportedEntries = 0;
            int IgnoredEntries = 0;
            int NoEntries = 0;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DirectoryInfo di = new DirectoryInfo(dialog.FileName);
                FileInfo[] INIFiles = di.GetFiles("*.ini");

                if (INIFiles.Length == 0)
                {
                   
                    TaskDialog MsgNDialog = new TaskDialog();
                    MsgNDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    MsgNDialog.Icon = TaskDialogStandardIcon.Warning;
                    MsgNDialog.InstructionText = "Bulunamadı.";
                    MsgNDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    MsgNDialog.Text = "Seçili klasörde hiç Config dosyası yok.";
                    MsgNDialog.Show();
                }
                else
                {
                    foreach (FileInfo i in INIFiles)
                    {
                        try
                        {
                            var IniReader = new IniFile(i.FullName);

                            int NumberOfResources = IniReader.GetInteger("RESOURCES", "NumberOfResources", 0, 0);

                            if (NumberOfResources == 0)
                            {
                                String EdLabel = IniReader.GetValue("RESOURCES", "Label", "");
                                String EdText = IniReader.GetValue("RESOURCES", "Name", "");

                                if (!(String.IsNullOrEmpty(EdLabel) || String.IsNullOrEmpty(EdText)))
                                {
                                    uint EdHash = (uint)BinHash.Hash(EdLabel); // Hash the label
                                    AddNewStringRecord_NoUpdate(EdHash, EdLabel, EdText); // Add
                                    ImportedEntries++;
                                }
                                else NoEntries++;
                            }
                            else 
                            {
                                for (int r = 1; r <= NumberOfResources; r++)
                                {
                                    String EdLabel = IniReader.GetValue(String.Format("RESOURCE{0}", r), "Label", "");
                                    String EdText = IniReader.GetValue(String.Format("RESOURCE{0}", r), "Name", "");

                                    if (!(String.IsNullOrEmpty(EdLabel) || String.IsNullOrEmpty(EdText)))
                                    {
                                        uint EdHash = (uint)BinHash.Hash(EdLabel);
                                        AddNewStringRecord_NoUpdate(EdHash, EdLabel, EdText); 
                                        ImportedEntries++;
                                    }
                                    else NoEntries++;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            IgnoredEntries++;
                        }
                    }
                }
                UpdateAfterImport();

                String Status = "Toplam" + " " + ImportedEntries + " " + "girdi İçe Aktarıldı.";
                if (NoEntries > 0) Status += "\n" + NoEntries + " " + "yapılandırma dosyalarının% 'si herhangi bir dizi kaynağı içermiyor.";
                if (IgnoredEntries > 0) Status += "\n" + "bazı hatalardan kaynaklanan girişler" + " " + IgnoredEntries + " " + "Yok sayıldı";

                

                TaskDialog MsgDialog = new TaskDialog();
                MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                MsgDialog.Icon = TaskDialogStandardIcon.Information;
                MsgDialog.InstructionText = "Bitti!";
                MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                MsgDialog.Text = Status;
                MsgDialog.Show();

            }
        }

        private void LangEdDumpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String TXTLineBuffer;
            int ImportedEntries = 0;
            int IgnoredEntries = 0;

            if (OpenLangEdDumpDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FileStream TXTFileStream = File.Open(OpenLangEdDumpDialog.FileName, FileMode.Open);

                    using (StreamReader TXTFile = new StreamReader(TXTFileStream))
                    {
                        while ((TXTLineBuffer = TXTFile.ReadLine()) != null)
                        {
                            char[] charsToTrim = {'\t'};
                            String[] Parts = TXTLineBuffer.Split(charsToTrim, StringSplitOptions.None);

                            if (Parts.Length >= 3)
                            {
                                try
                                {
                                    // Merge string if it contains tabs
                                    String LangEdString = "";
                                    for (int i = 2; i < Parts.Length; i++) LangEdString += Parts[i] + '\t';

                                    if (!(String.IsNullOrEmpty(Parts[1])))
                                    {
                                        uint LangEdHash = (uint)BinHash.Hash(Parts[1]); // Hash the label
                                        AddNewStringRecord_NoUpdate(LangEdHash, Parts[1], LangEdString); // Add
                                        ImportedEntries++;
                                    }
                                        
                                }
                                catch (Exception)
                                {
                                    IgnoredEntries++;
                                    continue;
                                }
                            }
                        }
                    }
                    UpdateAfterImport();

                    String Status = "Toplam" + " " + ImportedEntries + " " + "Girdi içeri Aktarıldı.";
                    if (IgnoredEntries > 0) Status += "\n" + "seçilen metin dosyasındaki bazı biçimlendirme hatalarından kaynaklanan girişler." + " " + IgnoredEntries + " " + "Yok sayıldı.";


                    TaskDialog MsgDialog = new TaskDialog();
                    MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    MsgDialog.Icon = TaskDialogStandardIcon.Information;
                    MsgDialog.InstructionText = "Bitti!";
                    MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    MsgDialog.Text = Status;
                    MsgDialog.Show();
                }
                catch (Exception ex)
                {
                    TaskDialog ErrDialog = new TaskDialog();
                    ErrDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    ErrDialog.Icon = TaskDialogStandardIcon.Error;
                    ErrDialog.InstructionText = "Hata!";
                    ErrDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    ErrDialog.Text = "Seçili metin dosyasında bir sorun var." + Environment.NewLine + " Şu anda kullanılmış, bozuk veya bunu işlemek için yeterli izne sahip değilsiniz.";
                    ErrDialog.DetailsExpanded = false;
                    ErrDialog.DetailsExpandedText = ex.ToString();
                    ErrDialog.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter;
                    ErrDialog.Show();
                }
            }
        }

        private void TextFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ExportFileDialog.ShowDialog() == DialogResult.OK)
            {
                String TXTFileName = ExportFileDialog.FileName.ToString();
                if (TXTFileName != "")
                {
                    try
                    {
                        using (StreamWriter TXTFile = new StreamWriter(TXTFileName))
                        {
                            TXTFile.WriteLine("#\t" + Text);
                            TXTFile.WriteLine("#\t" + "Dosya Oluşturuldu: " + DateTime.Now.ToString());
                            TXTFile.WriteLine("#");
                            TXTFile.WriteLine("#" + "Chunk" + "\t" + "Hash (HEX)" + "\t" + "Etiket" + "\t" + "Metin");
                            TXTFile.WriteLine("#" + " " + "---------------------------------------------------------------------------------------------------------");

                            foreach (LanguageChunk i in LangChunks)
                            {
                                TXTFile.WriteLine("# Chunk " + LangChunks.IndexOf(i) + (String.IsNullOrEmpty(i.Category) ? "" : " - " + i.Category));
                                foreach (LanguageStringRecord sR in i.Strings)
                                {
                                    TXTFile.WriteLine("{0}\t{1}\t{2}\t{3}", LangChunks.IndexOf(i), sR.Hash.ToString("X8"), sR.Label, sR.Text);
                                }
                            }

                            TaskDialog MsgDialog = new TaskDialog();
                            MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                            MsgDialog.Icon = TaskDialogStandardIcon.Information;
                            MsgDialog.InstructionText = "Bitti!";
                            MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                            MsgDialog.Text = "Tüm değerler dışa aktarıldı" + " " + TXTFileName;
                            MsgDialog.Show();
                        }

                    }
                    catch (Exception ex)
                    {
                       
                        TaskDialog ErrDialog = new TaskDialog();
                        ErrDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                        ErrDialog.Icon = TaskDialogStandardIcon.Error;
                        ErrDialog.InstructionText = "Hata!";
                        ErrDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                        ErrDialog.Text = "Değerler dışa aktarılamadı.";
                        ErrDialog.DetailsExpanded = false;
                        ErrDialog.DetailsExpandedText = ex.ToString();
                        ErrDialog.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter;
                        ErrDialog.Show();
                    }
                }
            }
        }

        private void ReCompilerOldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String TXTLineBuffer;
            int ImportedEntries = 0;
            int IgnoredEntries = 0;

            if (OpenReCompilerIniDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FileStream TXTFileStream = File.Open(OpenReCompilerIniDialog.FileName, FileMode.Open);

                    using (StreamReader TXTFile = new StreamReader(TXTFileStream))
                    {
                        while ((TXTLineBuffer = TXTFile.ReadLine()) != null)
                        {
                            if (!(TXTLineBuffer.StartsWith("#") || TXTLineBuffer.StartsWith("[")))
                            {
                                char[] charsToTrim = { '=' };
                                String[] Parts = TXTLineBuffer.Split(charsToTrim, StringSplitOptions.RemoveEmptyEntries);

                                if (Parts.Length == 2)
                                {
                                    try
                                    {
                                        // Remove leading and trailing spaces or tabs.
                                        Parts[0] = Parts[0].TrimStart(' ', '\t').TrimEnd(' ', '\t');
                                        Parts[1] = Parts[1].TrimStart(' ', '\t').TrimEnd(' ', '\t');
                                        AddNewStringRecord_NoUpdate((UInt32)BinHash.Hash(Parts[0]), Parts[0], Parts[1]);
                                        ImportedEntries++;

                                    }
                                    catch (Exception)
                                    {
                                        IgnoredEntries++;
                                        continue;
                                    }
                                }
                                else IgnoredEntries++;
                            }
                        }
                    }
                    UpdateAfterImport();

                    String Status = "Toplam" + " " + ImportedEntries + " " + "Girdi içe Aktarıldı.";
                    if (IgnoredEntries > 0) Status += "\n" + "seçili metin dosyasındaki bazı biçimlendirme hataları nedeniyle toplam."+ " " + IgnoredEntries + " " + " girişler Yok sayıldı";

                    TaskDialog MsgDialog = new TaskDialog();
                    MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    MsgDialog.Icon = TaskDialogStandardIcon.Information;
                    MsgDialog.InstructionText = "Bitti!";
                    MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    MsgDialog.Text = Status;
                    MsgDialog.Show();

                }
                catch (Exception ex)
                {
                    TaskDialog ErrDialog = new TaskDialog();
                    ErrDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    ErrDialog.Icon = TaskDialogStandardIcon.Error;
                    ErrDialog.InstructionText = "Hata!";
                    ErrDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    ErrDialog.Text = "Seçilen Yeniden Derleme yapılandırma dosyasında bir sorun var." + Environment.NewLine + "Şu anda kullanılmış, bozuk veya bunu işlemek için yeterli izne sahip değilsiniz.";
                    ErrDialog.DetailsExpanded = false;
                    ErrDialog.DetailsExpandedText = ex.ToString();
                    ErrDialog.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter;
                    ErrDialog.Show();
                }
            }
        }

        private void NextModifiedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ModifiedValuesIndexes.Count == 0)
            {
                foreach (ListViewItem item in LangStringView.Items)
                {
                    if (item.BackColor == Color.LightYellow)
                    {
                        ModifiedValuesIndexes.Add(item.Index);
                    }
                }
                ModifyIndex = 0;
            }

            if (ModifiedValuesIndexes.Count != 0)
            {
                ModifyIndex = (ModifyIndex + 1) % ModifiedValuesIndexes.Count;
                LangStringView.SelectedIndices.Clear();
                LangStringView.Items[ModifiedValuesIndexes[ModifyIndex]].Selected = true;
                LangStringView.Items[ModifiedValuesIndexes[ModifyIndex]].EnsureVisible();
            }

        }

        private void PrevModifiedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ModifiedValuesIndexes.Count == 0)
            {
                foreach (ListViewItem item in LangStringView.Items)
                {
                    if (item.BackColor == Color.LightYellow)
                    {
                        ModifiedValuesIndexes.Add(item.Index);
                    }
                }
                ModifyIndex = 0;
            }

            if (ModifiedValuesIndexes.Count != 0)
            {
                ModifyIndex = (ModifyIndex - 1 + ModifiedValuesIndexes.Count) % ModifiedValuesIndexes.Count;
                LangStringView.SelectedIndices.Clear();
                LangStringView.Items[ModifiedValuesIndexes[ModifyIndex]].Selected = true;
                LangStringView.Items[ModifiedValuesIndexes[ModifyIndex]].EnsureVisible();
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Start dialog from the location of opened file
            SaveLanguageFileDlg.InitialDirectory = Path.GetDirectoryName(OpenLanguageFileDlg.FileName);

            if (SaveLanguageFileDlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    String LanguageFileName = SaveLanguageFileDlg.FileName;
                    String LabelFileType = SaveLanguageFileDlg.FileName.LastIndexOf('_') == -1 ? "" : SaveLanguageFileDlg.FileName.Substring(SaveLanguageFileDlg.FileName.LastIndexOf('_'));
                    String LabelFileName = Path.Combine(Path.GetDirectoryName(SaveLanguageFileDlg.FileName), "Etiketler" + (String.IsNullOrEmpty(LabelFileType) ? ".bin" : LabelFileType));
                    int NrChkToWrite = 0;

                    // Re-read the opened file to take other chunks from it
                    MemoryStream LangFileStream = DecryptWorldLanguageFile(OpenLanguageFileDlg.FileName);
                    var LangFileWriter = new BinaryWriter(File.Create(LanguageFileName + ".tmp"));

                    using (BinaryReader StringFileReader = new BinaryReader(LangFileStream))
                    {
                        while (StringFileReader.BaseStream.Position < StringFileReader.BaseStream.Length)
                        {
                            int ChkOffset = (int)LangFileWriter.BaseStream.Position;
                            uint ChkID = StringFileReader.ReadUInt32();
                            int ChkSz = StringFileReader.ReadInt32();

                            if (ChkID == 0x00039000) // 00 90 03 00 - BCHUNK_LANGUAGE
                            {
                                if (NrChkToWrite <= LangChunks.Count)
                                {
                                    // Create hash and string tables
                                    var LanguageHashTable = new MemoryStream();
                                    var LanguageStringTable = new MemoryStream();
                                    var LanguageHashTableWriter = new BinaryWriter(LanguageHashTable);
                                    var LanguageStringTableWriter = new BinaryWriter(LanguageStringTable);

                                    foreach(LanguageStringRecord StrRec in LangChunks[NrChkToWrite].Strings)
                                    {
                                        // Write hash and offset for the hashes table
                                        LanguageHashTableWriter.Write(StrRec.Hash);
                                        LanguageHashTableWriter.Write((int)LanguageStringTableWriter.BaseStream.Position);

                                        // Write string for the strings table
                                        LanguageStringTableWriter.Write(Encoding.GetEncoding("ISO-8859-1").GetBytes(StrRec.Text + "\0"));
                                    }

                                    // Fix strings table size to %4
                                    int PaddingDifference = ((int)LanguageStringTableWriter.BaseStream.Length % 4);
                                    while (PaddingDifference != 0)
                                    {
                                        LanguageStringTableWriter.Write((byte)0);
                                        PaddingDifference = (PaddingDifference + 1) % 4;
                                    }

                                    if (LangChunks[NrChkToWrite].Version == LanguageFileVersion.Old) // U, U2, MW
                                    {
                                        LangFileWriter.Write(ChkID);
                                        LangFileWriter.Write((int)-1); // will be fixed after writing everything
                                        LangFileWriter.Write((int)0x10);
                                        LangFileWriter.Write(LangChunks[NrChkToWrite].Strings.Count);
                                        LangFileWriter.Write((int)(0x10 + LangChunks[NrChkToWrite].UnkData.Length)); // Hash table offset
                                        LangFileWriter.Write((int)(0x10 + LangChunks[NrChkToWrite].UnkData.Length + LanguageHashTableWriter.BaseStream.Length)); // String table offset
                                        LangFileWriter.Write(LangChunks[NrChkToWrite].UnkData);
                                        LangFileWriter.Write(LanguageHashTable.ToArray());
                                        LangFileWriter.Write(LanguageStringTable.ToArray());

                                    }
                                    else // C, PS, UC, W
                                    {
                                        LangFileWriter.Write(ChkID);
                                        LangFileWriter.Write((int)-1); // will be fixed after writing everything
                                        LangFileWriter.Write(LangChunks[NrChkToWrite].Strings.Count);
                                        LangFileWriter.Write((int)(0x1C)); // Hash table offset
                                        LangFileWriter.Write((int)(0x1C + LanguageHashTableWriter.BaseStream.Length)); // String table offset
                                        byte[] LangFileCategory = Encoding.GetEncoding("ISO-8859-1").GetBytes(LangChunks[NrChkToWrite].Category);
                                        Array.Resize(ref LangFileCategory, 16);
                                        LangFileWriter.Write(LangFileCategory);
                                        LangFileWriter.Write(LanguageHashTable.ToArray());
                                        LangFileWriter.Write(LanguageStringTable.ToArray());
                                    }

                                    LanguageStringTableWriter.Dispose();
                                    LanguageStringTableWriter.Close();
                                    LanguageStringTable.Dispose();
                                    LanguageStringTable.Close();
                                    LanguageHashTableWriter.Dispose();
                                    LanguageHashTableWriter.Close();
                                    LanguageHashTable.Dispose();
                                    LanguageHashTable.Close();
                                }

                                NrChkToWrite++;

                                // Write Chunk Size
                                int ChkEndOffset = (int)LangFileWriter.BaseStream.Position; // Get where we are
                                LangFileWriter.BaseStream.Position = ChkOffset + 4; // Go back to chunk size
                                LangFileWriter.Write(ChkEndOffset - ChkOffset - 8); // Chunk Size = End - Start - 8
                                LangFileWriter.BaseStream.Position = ChkEndOffset; // Go back where we are

                                // Process the other file
                                StringFileReader.BaseStream.Position += ChkSz; // for other chunks
                            }

                            else // Copy existing data from the file
                            {
                                LangFileWriter.Write(ChkID);
                                LangFileWriter.Write(ChkSz);
                                LangFileWriter.Write(StringFileReader.ReadBytes((ChkSz)));
                            }
                        }
                    }

                    LangFileWriter.Dispose();
                    LangFileWriter.Close();

                    // Take a backup
                    if (NFS_Dil_Duzenleyici.Properties.Settings.Default.CreateBackups) File.Copy(LanguageFileName, LanguageFileName + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".dildosyasiyedek", false);

                    // Copy tmp over the language file
                    if (File.Exists(LanguageFileName + ".tmp"))
                    {
                        File.Copy(LanguageFileName + ".tmp", LanguageFileName, true);
                        File.Delete(LanguageFileName + ".tmp");
                    }
                        
                    
                    // Label time!
                    if (File.Exists(LabelFileName) && HasLabels && NFS_Dil_Duzenleyici.Properties.Settings.Default.AlsoSaveLabels)
                    {
                        NrChkToWrite = 0;

                        // Re-read the opened label file to take other chunks from it
                        MemoryStream LabelFileStream = DecryptWorldLanguageFile(LabelFileName);
                        var LabelFileWriter = new BinaryWriter(File.Create(LabelFileName + ".tmp"));

                        using (BinaryReader LabelFileReader = new BinaryReader(LabelFileStream))
                        {
                            while (LabelFileReader.BaseStream.Position < LabelFileReader.BaseStream.Length)
                            {
                                int ChkOffset = (int)LabelFileWriter.BaseStream.Position;
                                uint ChkID = LabelFileReader.ReadUInt32();
                                int ChkSz = LabelFileReader.ReadInt32();

                                if (ChkID == 0x00039000) // 00 90 03 00 - BCHUNK_LANGUAGE
                                {
                                    if (NrChkToWrite <= LangChunks.Count)
                                    {
                                        // Create hash and string tables
                                        var LabelHashTable = new MemoryStream();
                                        var LabelStringTable = new MemoryStream();
                                        var LabelHashTableWriter = new BinaryWriter(LabelHashTable);
                                        var LabelStringTableWriter = new BinaryWriter(LabelStringTable);

                                        foreach (LanguageStringRecord StrRec in LangChunks[NrChkToWrite].Strings)
                                        {
                                            // Write hash and offset for the hashes table
                                            LabelHashTableWriter.Write(StrRec.Hash);
                                            LabelHashTableWriter.Write((int)LabelStringTableWriter.BaseStream.Position);

                                            // Write labels for the strings table
                                            LabelStringTableWriter.Write(Encoding.GetEncoding("ISO-8859-1").GetBytes(StrRec.Label + "\0"));
                                        }

                                        // Fix strings table size to %4
                                        int PaddingDifference = ((int)LabelStringTableWriter.BaseStream.Length % 4);
                                        while (PaddingDifference != 0)
                                        {
                                            LabelStringTableWriter.Write((byte)0);
                                            PaddingDifference = (PaddingDifference + 1) % 4;
                                        }

                                        if (LangChunks[NrChkToWrite].Version == LanguageFileVersion.Old) // U, U2, MW
                                        {
                                            LabelFileWriter.Write(ChkID);
                                            LabelFileWriter.Write((int)-1); // will be fixed after writing everything
                                            LabelFileWriter.Write((int)0x10);
                                            LabelFileWriter.Write(LangChunks[NrChkToWrite].Strings.Count);
                                            LabelFileWriter.Write((int)(0x10 + LangChunks[NrChkToWrite].UnkData.Length)); // Hash table offset
                                            LabelFileWriter.Write((int)(0x10 + LangChunks[NrChkToWrite].UnkData.Length + LabelHashTableWriter.BaseStream.Length)); // String table offset
                                            LabelFileWriter.Write(LangChunks[NrChkToWrite].UnkData);
                                            LabelFileWriter.Write(LabelHashTable.ToArray());
                                            LabelFileWriter.Write(LabelStringTable.ToArray());

                                        }
                                        else // C, PS, UC, W
                                        {
                                            LabelFileWriter.Write(ChkID);
                                            LabelFileWriter.Write((int)-1); // will be fixed after writing everything
                                            LabelFileWriter.Write(LangChunks[NrChkToWrite].Strings.Count);
                                            LabelFileWriter.Write((int)(0x1C)); // Hash table offset
                                            LabelFileWriter.Write((int)(0x1C + LabelHashTableWriter.BaseStream.Length)); // String table offset
                                            byte[] LangFileCategory = Encoding.GetEncoding("ISO-8859-1").GetBytes(LangChunks[NrChkToWrite].Category);
                                            Array.Resize(ref LangFileCategory, 16);
                                            LabelFileWriter.Write(LangFileCategory);
                                            LabelFileWriter.Write(LabelHashTable.ToArray());
                                            LabelFileWriter.Write(LabelStringTable.ToArray());
                                        }

                                        LabelStringTableWriter.Dispose();
                                        LabelStringTableWriter.Close();
                                        LabelStringTable.Dispose();
                                        LabelStringTable.Close();
                                        LabelHashTableWriter.Dispose();
                                        LabelHashTableWriter.Close();
                                        LabelHashTable.Dispose();
                                        LabelHashTable.Close();
                                    }

                                    NrChkToWrite++;

                                    // Write Chunk Size
                                    int ChkEndOffset = (int)LabelFileWriter.BaseStream.Position; // Get where we are
                                    LabelFileWriter.BaseStream.Position = ChkOffset + 4; // Go back to chunk size
                                    LabelFileWriter.Write(ChkEndOffset - ChkOffset - 8); // Chunk Size = End - Start - 8
                                    LabelFileWriter.BaseStream.Position = ChkEndOffset; // Go back where we are

                                    // Process the other file
                                    LabelFileReader.BaseStream.Position += ChkSz; // for other chunks
                                }

                                else // Copy existing data from the file
                                {
                                    LabelFileWriter.Write(ChkID);
                                    LabelFileWriter.Write(ChkSz);
                                    LabelFileWriter.Write(LabelFileReader.ReadBytes((ChkSz)));
                                }
                            }
                        }

                        LabelFileWriter.Dispose();
                        LabelFileWriter.Close();

                        // Take a backup
                        if (NFS_Dil_Duzenleyici.Properties.Settings.Default.CreateBackups) File.Copy(LabelFileName, LabelFileName + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".dildosyasiyedek", false);

                        // Copy tmp over the language file
                        if (File.Exists(LabelFileName + ".tmp"))
                        {
                            File.Copy(LabelFileName + ".tmp", LabelFileName, true);
                            File.Delete(LabelFileName + ".tmp");
                        }
                    }

                    TaskDialog MsgDialog = new TaskDialog();
                    MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    MsgDialog.Icon = TaskDialogStandardIcon.Information;
                    MsgDialog.InstructionText = "Bitti!";
                    MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    MsgDialog.Text = "Dosya Baaşrıyla Kaydedildi.";
                    MsgDialog.Show();
                    MarkFileAsUnModified();

                }
                catch (Exception ex)
                {
                    TaskDialog ErrDialog = new TaskDialog();
                    ErrDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                    ErrDialog.Icon = TaskDialogStandardIcon.Error;
                    ErrDialog.InstructionText = "Hata!";
                    ErrDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                    ErrDialog.Text = "Dil dosyasını kaydedemedik." + Environment.NewLine + "Şu anda kullanılmış, bozuk veya  bunu işlemek için yeterli izne sahip değilsiniz.";
                    ErrDialog.DetailsExpanded = false;
                    ErrDialog.DetailsExpandedText = ex.ToString();
                    ErrDialog.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter;
                    ErrDialog.Show();
                }
            }
        }

        private void AboutLabruneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var AboutWindow = new LabruneAbout();
            AboutWindow.ShowDialog();
        }

        private void OptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var OptionsWindow = new LabruneOptions();
            OptionsWindow.ShowDialog();
        }

        private void TextFileModifiedEntriesOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsFileModified)
            {
                if (ExportFileDialog.ShowDialog() == DialogResult.OK)
                {
                    String TXTFileName = ExportFileDialog.FileName.ToString();
                    if (TXTFileName != "")
                    {
                        try
                        {
                            using (StreamWriter TXTFile = new StreamWriter(TXTFileName))
                            {
                                TXTFile.WriteLine("#\t" + Text);
                                TXTFile.WriteLine("#\t" + "Dosya oluşturuldu: " + DateTime.Now.ToString());
                                TXTFile.WriteLine("#");
                                TXTFile.WriteLine("#" + "Chunk" + "\t" + "Hash (HEX)" + "\t" + "Etiket" + "\t" + "Değer");
                                TXTFile.WriteLine("#" + " " + "---------------------------------------------------------------------------------------------------------");

                                foreach (LanguageChunk i in LangChunks)
                                {
                                    TXTFile.WriteLine("# Chunk " + LangChunks.IndexOf(i) + (String.IsNullOrEmpty(i.Category) ? "" : " - " + i.Category));
                                    foreach (LanguageStringRecord sR in i.Strings)
                                    {
                                        if (sR.IsModified == true) TXTFile.WriteLine("{0}\t{1}\t{2}\t{3}", LangChunks.IndexOf(i), sR.Hash.ToString("X8"), sR.Label, sR.Text);
                                    }
                                }

                                TaskDialog MsgDialog = new TaskDialog();
                                MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                                MsgDialog.Icon = TaskDialogStandardIcon.Information;
                                MsgDialog.InstructionText = "Bitti!";
                                MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                                MsgDialog.Text = "Değiştirilen tüm değerler şuraya aktarılır:" + " " + TXTFileName;
                                MsgDialog.Show();
                            }

                        }
                        catch (Exception ex)
                        {
                            TaskDialog ErrDialog = new TaskDialog();
                            ErrDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                            ErrDialog.Icon = TaskDialogStandardIcon.Error;
                            ErrDialog.InstructionText = "Hata!";
                            ErrDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                            ErrDialog.Text = "Değerler dışa aktarılamadı.";
                            ErrDialog.DetailsExpanded = false;
                            ErrDialog.DetailsExpandedText = ex.ToString();
                            ErrDialog.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter;
                            ErrDialog.Show();
                        }
                    }
                }
            }
            else
            {
                TaskDialog MsgDialog = new TaskDialog();
                MsgDialog.StandardButtons = TaskDialogStandardButtons.Ok;
                MsgDialog.Icon = TaskDialogStandardIcon.Warning;
                MsgDialog.InstructionText = "Uyarı!";
                MsgDialog.Caption = "SinnerClownCeviri.Com-LaZEnEs";
                MsgDialog.Text = "Değiştirilmiş değer yok.";
                MsgDialog.Show();
            }
            
        }

        private void LangStatusBar_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
