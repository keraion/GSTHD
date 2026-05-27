using GSTHD.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GSTHD
{
    public partial class MainForm : Form
    {
        Dictionary<string, string> ListPlacesWithTag = new Dictionary<string, string>();
        SortedSet<string> ListPlaces = new SortedSet<string>();
        SortedSet<string> ListSometimesHintsSuggestions = new SortedSet<string>();

        MainForm_MenuBar MenuBar;
        LocalSettings LocalSettings;
        Layout ActiveLayout;
        Settings ActiveSettings;

        private const int TrackerAutosaveDebounceMs = 1000;
        private readonly Timer TrackerAutosaveTimer;
        private readonly OpenFileDialog OpenTrackerStateDialog;
        private bool TrackerStateDirty;
        private bool IsApplyingTrackerState;

        private string TrackerStateFilePath => Path.Combine(AppContext.BaseDirectory, TrackerState.DefaultFileName);

        //PictureBox pbox_collectedSkulls;

        public MainForm()
        {
            InitializeComponent();

            TrackerAutosaveTimer = new Timer()
            {
                Interval = TrackerAutosaveDebounceMs,
            };
            TrackerAutosaveTimer.Tick += TrackerAutosaveTimer_Tick;

            OpenTrackerStateDialog = new OpenFileDialog()
            {
                Filter = "Tracker state files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Load Tracker State",
            };

            FormClosing += MainForm_FormClosing;
        }


        //private void MainForm_KeyDown(object sender, KeyEventArgs e)
        //{
        //    //if (e.Control && e.KeyCode == Keys.R)
        //    //{
        //    //    this.Controls.Clear();
        //    //    e.Handled = true;
        //    //    e.SuppressKeyPress = true;
        //    //    this.Form1_Load(sender, new EventArgs());
        //    //}

        //    /*
        //    if(e.KeyCode == Keys.F2)
        //    {
        //        var window = new Editor(CurrentLayout);
        //        window.Show();
        //    }
        //    */
        //}

        private void Initialize(object sender, EventArgs e)
        {
            //this.AcceptButton = null;
            //this.MaximizeBox = false;
            //this.KeyPreview = true;
            //this.KeyDown += changeCollectedSkulls;

            LoadSettings();
            LoadMenuBar();
            LoadActiveLayout();
        }

        private void LoadSettings()
        {
            LocalSettings = LoadLocalSettings();
            ActiveSettings = new Settings(LocalSettings);
        }

        private void LoadMenuBar()
        {
            MenuBar = new MainForm_MenuBar(this, LocalSettings);
            MenuBar.Dock = DockStyle.Top;
            MenuBar.SetRenderer();
        }

        private void LoadActiveLayout()
        {
            Layout layout;
            try
            {
                layout = PreloadLayout(LocalSettings.ActiveLayout);
            }
            catch (GSTHDException ex) when (
                ex is FilesNotFoundException ||
                ex is InvalidLayoutFileException)
            {
                // do nothing, application will open in an empty no-layout state
                ShowErrorMessage(ex);
                return;
            }

            ActiveLayout = layout;
            PostloadLayout();
        }

        public void LoadLayout(string layoutPath)
        {
            Layout layout;
            try
            {
                layout = PreloadLayout(layoutPath);
            }
            catch (GSTHDException ex) when (
                ex is FilesNotFoundException ||
                ex is InvalidLayoutFileException)
            {
                // do nothing, stay on previous layout state
                ShowErrorMessage(ex);
                return;
            }

            FlushPendingTrackerSave();
            Controls.Clear();
            ActiveLayout = layout;
            LocalSettings.ActiveLayout = layoutPath;
            JsonIO.Write(LocalSettings, LocalSettings.LocalSettingsFileName);
            PostloadLayout();
        }

        public void ReloadActiveLayout()
        {
            Layout layout;
            try
            {
                layout = PreloadLayout(LocalSettings.ActiveLayout);
            }
            catch (GSTHDException ex)  when (
                ex is FilesNotFoundException ||
                ex is InvalidLayoutFileException)
            {
                // do nothing, stay on previous layout state
                ShowErrorMessage(ex);
                return;
            }

            FlushPendingTrackerSave();
            Controls.Clear();
            ActiveLayout = layout;
            PostloadLayout();
        }

        //public SaveState()
        //{
        //    // TODO
        //}

        //public void LoadSavedState()
        //{
        //    // TODO
        //}

        private LocalSettings LoadLocalSettings()
        {
            ListPlaces.Clear();
            ListPlaces.Add("");
            ListPlacesWithTag.Clear();
            var oot_places_path = Path.Combine(AppContext.BaseDirectory, "Resources", "oot_places.json");
            JObject json_places = JObject.Parse(File.ReadAllText(oot_places_path));
            foreach (var property in json_places)
            {
                ListPlaces.Add(property.Key.ToString());
                ListPlacesWithTag.Add(property.Key, property.Value.ToString());
            }

            ListSometimesHintsSuggestions.Clear();
            var hints_path = Path.Combine(AppContext.BaseDirectory, "Resources", "sometimes_hints.json");
            JObject json_hints = JObject.Parse(File.ReadAllText(hints_path));
            foreach (var categorie in json_hints)
            {
                foreach (var hint in categorie.Value)
                {
                    ListSometimesHintsSuggestions.Add(hint.ToString());
                }
            }

            return JsonIO.Read<LocalSettings>(LocalSettings.LocalSettingsFileName);
        }

        private string FixLayoutPath(string layoutPath)
        {
            if (File.Exists(layoutPath))
            {
                return layoutPath;
            }

            var fixedPath = $@"Layouts\{layoutPath}.json";
            if (File.Exists(fixedPath))
            {
                return fixedPath;
            }
            else throw new FilesNotFoundException(layoutPath, fixedPath);
        }

        private Layout PreloadLayout(string layoutPath)
        {
            var path = FixLayoutPath(layoutPath);

            var layout = new Layout(path);
            layout.LoadContents(ActiveSettings, ListSometimesHintsSuggestions, ListPlacesWithTag, this);
            return layout;
        }

        private void SetSize()
        {
            Size = new Size(ActiveLayout.Size.Width, ActiveLayout.Size.Height + MenuBar.Size.Height);
        }

        private void SetBackColor()
        {
            if (ActiveLayout.Settings.BackgroundColor.HasValue)
                BackColor = ActiveLayout.Settings.BackgroundColor.Value;
            ActiveLayout.BackColor = BackColor;
        }

        private void PostloadLayout()
        {
            ActiveLayout.Dock = DockStyle.Top;
            SetSize();
            SetBackColor();

            MenuBar.SetActiveLayout(ActiveLayout);
            ActiveSettings.SetLayoutSettings(ActiveLayout.Settings);

            Controls.Add(ActiveLayout);
            Controls.Add(MenuBar);

            HookTrackerStateObservers();
            TryLoadTrackerState(TrackerStateFilePath, showErrors: false, warnOnLayoutMismatch: false);
        }

        private void HookTrackerStateObservers()
        {
            if (ActiveLayout == null)
                return;

            HookControlTree(ActiveLayout);
        }

        private void HookControlTree(Control control)
        {
            if (control == null)
                return;

            control.MouseUp -= TrackerStateInteraction;
            control.MouseUp += TrackerStateInteraction;
            control.MouseWheel -= TrackerStateInteraction;
            control.MouseWheel += TrackerStateInteraction;
            control.DragDrop -= TrackerStateInteraction;
            control.DragDrop += TrackerStateInteraction;
            control.TextChanged -= TrackerStateInteraction;
            control.TextChanged += TrackerStateInteraction;

            control.ControlAdded -= TrackerControlAdded;
            control.ControlAdded += TrackerControlAdded;
            control.ControlRemoved -= TrackerControlRemoved;
            control.ControlRemoved += TrackerControlRemoved;

            foreach (Control child in control.Controls)
            {
                HookControlTree(child);
            }
        }

        private void TrackerControlAdded(object sender, ControlEventArgs e)
        {
            HookControlTree(e.Control);
            MarkTrackerStateDirty();
        }

        private void TrackerControlRemoved(object sender, ControlEventArgs e)
        {
            MarkTrackerStateDirty();
        }

        private void TrackerStateInteraction(object sender, EventArgs e)
        {
            MarkTrackerStateDirty();
        }

        private void MarkTrackerStateDirty()
        {
            if (IsApplyingTrackerState || ActiveLayout == null)
                return;

            TrackerStateDirty = true;
            TrackerAutosaveTimer.Stop();
            TrackerAutosaveTimer.Start();
        }

        private void TrackerAutosaveTimer_Tick(object sender, EventArgs e)
        {
            TrackerAutosaveTimer.Stop();
            SaveTrackerState(TrackerStateFilePath, showErrors: false);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            TrackerAutosaveTimer.Stop();
            SaveTrackerState(TrackerStateFilePath, showErrors: false);
        }

        private void FlushPendingTrackerSave()
        {
            if (!TrackerStateDirty)
                return;

            TrackerAutosaveTimer.Stop();
            SaveTrackerState(TrackerStateFilePath, showErrors: false);
        }

        private TrackerState BuildTrackerState()
        {
            var state = new TrackerState()
            {
                Version = TrackerState.CurrentVersion,
                LayoutPath = ActiveLayout?.FilePath ?? string.Empty,
                SavedAtUtc = DateTime.UtcNow,
            };

            if (ActiveLayout == null)
                return state;

            var siblingCounters = new Dictionary<string, int>();
            CaptureTrackerState(ActiveLayout, "Layout", siblingCounters, state);

            return state;
        }

        private void CaptureTrackerState(Control container, string containerKey, Dictionary<string, int> siblingCounters, TrackerState state)
        {
            siblingCounters.Clear();

            foreach (Control child in container.Controls)
            {
                var controlKey = BuildControlKey(containerKey, child, siblingCounters);

                if (child is Item item)
                {
                    state.Items[controlKey] = item.GetState();
                }
                else if (child is DoubleItem doubleItem)
                {
                    state.DoubleItems[controlKey] = doubleItem.GetState();
                }
                else if (child is CollectedItem collectedItem)
                {
                    state.CollectedItems[controlKey] = collectedItem.GetState();
                }
                else if (child is Medallion medallion)
                {
                    state.Medallions[controlKey] = medallion.GetState();
                    state.MedallionDungeons[controlKey] = medallion.GetDungeonState();
                }
                else if (child is Song song)
                {
                    state.Songs[controlKey] = song.GetState();
                }
                else if (child is SongMarker songMarker)
                {
                    state.SongMarkers[controlKey] = songMarker.GetState();
                }
                else if (child is GossipStone gossipStone)
                {
                    state.GossipStones[controlKey] = gossipStone.GetState();
                }
                else if (child is PanelWothBarren panel)
                {
                    state.PanelStates[panel.Name] = panel.GetTrackerState();
                }

                if (child.HasChildren)
                {
                    var nestedSiblingCounters = new Dictionary<string, int>();
                    CaptureTrackerState(child, controlKey, nestedSiblingCounters, state);
                }
            }
        }

        private void ApplyTrackerState(TrackerState state)
        {
            if (ActiveLayout == null)
                return;

            IsApplyingTrackerState = true;
            try
            {
                var controls = BuildControlMap();
                foreach (var pair in controls)
                {
                    var key = pair.Key;
                    var control = pair.Value;

                    if (control is Item item)
                    {
                        if (state.Items.TryGetValue(key, out var saved)) item.SetState(saved);
                        else item.ResetState();
                    }
                    else if (control is DoubleItem doubleItem)
                    {
                        if (state.DoubleItems.TryGetValue(key, out var saved)) doubleItem.SetState(saved);
                        else doubleItem.ResetState();
                    }
                    else if (control is CollectedItem collectedItem)
                    {
                        if (state.CollectedItems.TryGetValue(key, out var saved)) collectedItem.SetState(saved);
                        else collectedItem.ResetState();
                    }
                    else if (control is Medallion medallion)
                    {
                        if (state.Medallions.TryGetValue(key, out var saved)) medallion.SetState(saved);
                        else medallion.ResetState();

                        if (state.MedallionDungeons.TryGetValue(key, out var dungeonSaved)) medallion.SetDungeonState(dungeonSaved);
                    }
                    else if (control is Song song)
                    {
                        if (state.Songs.TryGetValue(key, out var saved)) song.SetState(saved);
                        else song.ResetState();
                    }
                    else if (control is SongMarker songMarker)
                    {
                        if (state.SongMarkers.TryGetValue(key, out var saved)) songMarker.SetState(saved);
                        else songMarker.ResetState();
                    }
                    else if (control is GossipStone gossipStone)
                    {
                        if (state.GossipStones.TryGetValue(key, out var saved)) gossipStone.SetState(saved);
                        else gossipStone.ResetState();
                    }
                    else if (control is PanelWothBarren panel)
                    {
                        if (state.PanelStates.TryGetValue(panel.Name, out var panelState)) panel.ApplyTrackerState(panelState);
                        else panel.ResetTrackerState();
                    }
                }

                TrackerStateDirty = false;
                TrackerAutosaveTimer.Stop();
            }
            finally
            {
                IsApplyingTrackerState = false;
            }
        }

        private Dictionary<string, Control> BuildControlMap()
        {
            var result = new Dictionary<string, Control>();
            if (ActiveLayout == null)
                return result;

            var siblingCounters = new Dictionary<string, int>();
            FillControlMap(ActiveLayout, "Layout", siblingCounters, result);
            return result;
        }

        private void FillControlMap(Control container, string containerKey, Dictionary<string, int> siblingCounters, Dictionary<string, Control> result)
        {
            siblingCounters.Clear();

            foreach (Control child in container.Controls)
            {
                var controlKey = BuildControlKey(containerKey, child, siblingCounters);
                result[controlKey] = child;

                if (child.HasChildren)
                {
                    var nestedSiblingCounters = new Dictionary<string, int>();
                    FillControlMap(child, controlKey, nestedSiblingCounters, result);
                }
            }
        }

        private string BuildControlKey(string containerKey, Control control, Dictionary<string, int> siblingCounters)
        {
            var identity = $"{control.GetType().Name}@{control.Left},{control.Top},{control.Width},{control.Height}";
            siblingCounters.TryGetValue(identity, out var count);
            var nextCount = count + 1;
            siblingCounters[identity] = nextCount;
            return $"{containerKey}/{identity}[{nextCount}]";
        }

        private bool SaveTrackerState(string filePath, bool showErrors)
        {
            if (ActiveLayout == null)
                return false;

            try
            {
                var state = BuildTrackerState();
                JsonIO.Write(state, filePath);
                TrackerStateDirty = false;
                return true;
            }
            catch (Exception ex)
            {
                if (showErrors)
                {
                    MessageBox.Show($"Could not save tracker state. {ex.Message}", $"{Config.ErrorMessageTitlePrefix}: Save Tracker State");
                }
                return false;
            }
        }

        public bool TryLoadTrackerState(string filePath, bool showErrors = true, bool warnOnLayoutMismatch = true)
        {
            if (ActiveLayout == null || !File.Exists(filePath))
                return false;

            try
            {
                var state = JsonIO.Read<TrackerState>(filePath);
                var mismatch = !string.IsNullOrEmpty(state.LayoutPath)
                    && !string.Equals(state.LayoutPath, ActiveLayout.FilePath, StringComparison.OrdinalIgnoreCase);

                if (mismatch && warnOnLayoutMismatch)
                {
                    var dialogResult = MessageBox.Show(
                        "This state file was saved from a different layout. Continue and map matching entries only?",
                        "Layout Mismatch",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (dialogResult != DialogResult.Yes)
                        return false;
                }

                ApplyTrackerState(state);
                return true;
            }
            catch (Exception ex)
            {
                if (showErrors)
                {
                    MessageBox.Show($"Could not load tracker state. {ex.Message}", $"{Config.ErrorMessageTitlePrefix}: Load Tracker State");
                }
                return false;
            }
        }

        public void LoadTrackerStateFromDialog()
        {
            if (OpenTrackerStateDialog.ShowDialog() != DialogResult.OK)
                return;

            if (TryLoadTrackerState(OpenTrackerStateDialog.FileName, showErrors: true, warnOnLayoutMismatch: true))
            {
                SaveTrackerState(TrackerStateFilePath, showErrors: false);
            }
        }

        public void UpdateSettings()
        {
            ActiveSettings.Update();
            ActiveLayout.UpdateFromSettings();
        }

        //private void changeCollectedSkulls(object sender, KeyEventArgs k)
        //{
        //    if (k.KeyCode == Keys.F9) { }
        //    //button_chrono_Click(sender, new EventArgs());
        //    if (k.KeyCode == Keys.F11) { }
        //    //label_collectedSkulls_MouseDown(pbox_collectedSkulls.Controls[0], new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
        //    if (k.KeyCode == Keys.F12) { }
        //    //label_collectedSkulls_MouseDown(pbox_collectedSkulls.Controls[0], new MouseEventArgs(MouseButtons.Right, 1, 0, 0, 0));
        //}

        public void Reset(object sender)
        {
            FlushPendingTrackerSave();
            ControlExtensions.ClearAndDispose(ActiveLayout);
            ReloadActiveLayout();
            Process.GetCurrentProcess().Refresh();
        }

        public void ShowErrorMessage(GSTHDException ex)
        {
            MessageBox.Show(ex.Message, $"{Config.ErrorMessageTitlePrefix}: {ex.Title}");
        }
    }

    public class FilesNotFoundException : GSTHDException
    {
        private static string GetMessage(string[] filePaths)
        {
            if (filePaths.Length == 0)
                return GenericMessage;

            var sb = new StringBuilder();
            foreach (var filePath in filePaths)
            {
                sb.Append($"File \"{filePath}\" not found. ");
            }

            return sb.ToString();
        }

        private static string GenericMessage = $"File(s) not found.";

        public FilesNotFoundException(params string[] fileNames)
            : base(Config.LayoutFileExceptionTitle, GetMessage(fileNames)) { }
        public FilesNotFoundException(string[] fileNames, Exception inner)
            : base(Config.LayoutFileExceptionTitle, GetMessage(fileNames), inner) { }
    }
}
