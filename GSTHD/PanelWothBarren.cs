using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GSTHD
{
    class PanelWothBarren : Panel, UpdatableFromSettings
    {
        Settings Settings;

        public List<WotH> ListWotH = new List<WotH>();
        public List<Barren> ListBarren = new List<Barren>();

        public TextBoxCustom textBoxCustom;
        private int GossipStoneCount;
        private string[] ListImage_WothItemsOption;
        private int PathGoalCount;
        private string[] ListImage_GoalsOption;
        Size GossipStoneSize;
        int GossipStoneSpacing;
        int PathGoalSpacing;
        int NbMaxRows;
        Label LabelSettings = new Label();

        public PanelWothBarren(ObjectPanelWotH data, Settings settings)
        {
            Settings = settings;

            GossipStoneSize = data.GossipStoneSize;
            this.BackColor = data.BackColor;
            this.Location = new Point(data.X, data.Y);
            this.Name = data.Name;
            this.Size = new Size(data.Width, data.Height);
            this.GossipStoneCount = data.GossipStoneCount.GetValueOrDefault(settings.DefaultWothGossipStoneCount);
            this.PathGoalCount = data.PathGoalCount.GetValueOrDefault(settings.DefaultPathGoalCount);
            this.GossipStoneSpacing = data.GossipStoneSpacing;
            this.PathGoalSpacing = data.PathGoalSpacing;
            this.TabStop = false;
            if(data.IsScrollable)
                this.MouseWheel += Panel_MouseWheel;
        }

        public PanelWothBarren(ObjectPanelBarren data, Settings settings)
        {
            Settings = settings;

            this.BackColor = data.BackColor;
            this.Location = new Point(data.X, data.Y);
            this.Name = data.Name;
            this.Size = new Size(data.Width, data.Height);
            this.TabStop = false;
            if (data.IsScrollable)
                this.MouseWheel += Panel_MouseWheel;
        }

        public void UpdateFromSettings()
        {
            foreach (var woth in ListWotH)
            {
                woth.UpdateFromSettings();
            }
            foreach (var barren in ListBarren)
            {
                barren.UpdateFromSettings();
            }
        }

        private void Panel_MouseWheel(object sender, MouseEventArgs e)
        {
            var panel = (Panel)sender;
            if (e.Delta < 0)
            {
                foreach (var element in panel.Controls)
                {
                    if (element is Label)
                        ((Label)element).Location = new Point(((Label)element).Location.X, ((Label)element).Location.Y - 15);
                    if (element is GossipStone)
                        ((GossipStone)element).Location = new Point(((GossipStone)element).Location.X, ((GossipStone)element).Location.Y - 15);
                    if (element is TextBox)
                        ((TextBox)element).Location = new Point(((TextBox)element).Location.X, ((TextBox)element).Location.Y - 15);
                }
            }
            if (e.Delta > 0)
            {
                foreach (var element in panel.Controls)
                {
                    if (element is Label)
                        ((Label)element).Location = new Point(((Label)element).Location.X, ((Label)element).Location.Y + 15);
                    if (element is GossipStone)
                        ((GossipStone)element).Location = new Point(((GossipStone)element).Location.X, ((GossipStone)element).Location.Y + 15);
                    if (element is TextBox)
                        ((TextBox)element).Location = new Point(((TextBox)element).Location.X, ((TextBox)element).Location.Y + 15);
                }
            }
            ((PanelWothBarren)panel).SetSuggestionContainer();
        }

        public void PanelWoth(Dictionary<string, string> PlacesWithTag, ObjectPanelWotH data)
        {
            ListImage_WothItemsOption = data.GossipStoneImageCollection ?? Settings.DefaultGossipStoneImages;
            ListImage_GoalsOption = data.PathGoalImageCollection ?? Settings.DefaultPathGoalImages;
            NbMaxRows = data.NbMaxRows;

            LabelSettings = new Label
            {
                ForeColor = data.LabelColor,
                BackColor = data.LabelBackColor,
                Font = new Font(data.LabelFontName, data.LabelFontSize, data.LabelFontStyle),
                Width = data.Width,
                Height = data.LabelHeight,
            };

            textBoxCustom = new TextBoxCustom
            (
                PlacesWithTag,
                new Point(0, 0),
                data.TextBoxBackColor,
                new Font(data.TextBoxFontName, data.TextBoxFontSize, data.TextBoxFontStyle),
                data.TextBoxName,
                new Size(data.Width, data.TextBoxHeight),
                data.TextBoxText
            );
            textBoxCustom.TextBoxField.KeyDown += textBoxCustom_KeyDown_WotH;
            textBoxCustom.TextBoxField.MouseClick += textBoxCustom_MouseClick;
            this.Controls.Add(textBoxCustom.TextBoxField);
        }

        public void PanelBarren(Dictionary<string, string> PlacesWithTag, ObjectPanelBarren data)
        {
            NbMaxRows = data.NbMaxRows;

            LabelSettings = new Label
            {
                ForeColor = data.LabelColor,
                BackColor = data.LabelBackColor,
                Font = new Font(data.LabelFontName, data.LabelFontSize, data.LabelFontStyle),
                Width = data.LabelWidth,
                Height = data.LabelHeight
            };

            textBoxCustom = new TextBoxCustom
                (
                    PlacesWithTag,
                    new Point(0, 0),
                    data.TextBoxBackColor,
                    new Font(data.TextBoxFontName, data.TextBoxFontSize, data.TextBoxFontStyle),
                    data.TextBoxName,
                    new Size(data.TextBoxWidth, data.TextBoxHeight),
                    data.TextBoxText
                );
            textBoxCustom.TextBoxField.KeyDown += textBoxCustom_KeyDown_Barren;
            textBoxCustom.TextBoxField.MouseClick += textBoxCustom_MouseClick;
            this.Controls.Add(textBoxCustom.TextBoxField);
        }

        public void SetSuggestionContainer()
        {
            textBoxCustom.SetSuggestionsContainerLocation(this.Location);
            textBoxCustom.SuggestionContainer.BringToFront();
        }

        public PanelWothBarrenState GetTrackerState()
        {
            var state = new PanelWothBarrenState();

            foreach (var woth in ListWotH)
            {
                var entry = new WothEntryState()
                {
                    Name = woth.Name,
                    ColorIndex = woth.GetColorIndex(),
                };

                foreach (var gossipStone in woth.listGossipStone)
                {
                    entry.GossipStoneStates.Add(gossipStone.GetState());
                }

                state.WothEntries.Add(entry);
            }

            foreach (var barren in ListBarren)
            {
                state.BarrenEntries.Add(new BarrenEntryState()
                {
                    Name = barren.Name,
                    ColorIndex = barren.GetColorIndex(),
                });
            }

            return state;
        }

        public void ApplyTrackerState(PanelWothBarrenState state)
        {
            ResetTrackerState();

            if (state == null)
                return;

            foreach (var entry in state.WothEntries)
            {
                var woth = AddWotHEntry(entry.Name, false);
                if (woth == null)
                    continue;

                woth.SetColorIndex(entry.ColorIndex);

                var count = System.Math.Min(woth.listGossipStone.Count, entry.GossipStoneStates.Count);
                for (int i = 0; i < count; i++)
                {
                    woth.listGossipStone[i].SetState(entry.GossipStoneStates[i]);
                }
            }

            foreach (var entry in state.BarrenEntries)
            {
                var barren = AddBarrenEntry(entry.Name);
                if (barren == null)
                    continue;

                barren.SetColorIndex(entry.ColorIndex);
            }
        }

        public void ResetTrackerState()
        {
            foreach (var woth in ListWotH.ToList())
            {
                RemoveWotH(woth);
            }

            foreach (var barren in ListBarren.ToList())
            {
                RemoveBarren(barren);
            }
        }

        private Barren AddBarrenEntry(string selectedPlace)
        {
            var place = (selectedPlace ?? string.Empty).ToUpper().Trim();
            if (string.IsNullOrEmpty(place))
                return null;

            if (ListBarren.Count >= NbMaxRows)
                return null;

            if (ListBarren.Any(x => x.Name == place))
                return null;

            Barren newBarren;
            if (ListBarren.Count <= 0)
                newBarren = new Barren(Settings, place, new Point(0, -LabelSettings.Height), LabelSettings);
            else
                newBarren = new Barren(Settings, place, ListBarren.Last().LabelPlace.Location, LabelSettings);

            ListBarren.Add(newBarren);
            Controls.Add(newBarren.LabelPlace);
            newBarren.LabelPlace.MouseClick += LabelPlace_MouseClick_Barren;
            textBoxCustom.newLocation(new Point(0, newBarren.LabelPlace.Location.Y + newBarren.LabelPlace.Height), Location);
            return newBarren;
        }

        private WotH AddWotHEntry(string selectedPlace, bool enforceDuplicateRule)
        {
            var place = (selectedPlace ?? string.Empty).ToUpper().Trim();
            if (string.IsNullOrEmpty(place))
                return null;

            if (ListWotH.Count >= NbMaxRows)
                return null;

            if (enforceDuplicateRule && !Settings.EnableDuplicateWoth && ListWotH.Any(x => x.Name == place))
                return null;

            WotH newWotH;
            if (ListWotH.Count <= 0)
            {
                newWotH = new WotH(Settings, place,
                    GossipStoneCount, ListImage_WothItemsOption, GossipStoneSpacing,
                    PathGoalCount, ListImage_GoalsOption, PathGoalSpacing,
                    new Point(2, -LabelSettings.Height), LabelSettings, GossipStoneSize);
            }
            else
            {
                newWotH = new WotH(Settings, place,
                    GossipStoneCount, ListImage_WothItemsOption, GossipStoneSpacing,
                    PathGoalCount, ListImage_GoalsOption, PathGoalSpacing,
                    ListWotH.Last().LabelPlace.Location, LabelSettings, GossipStoneSize);
            }

            ListWotH.Add(newWotH);
            Controls.Add(newWotH.LabelPlace);
            newWotH.LabelPlace.MouseClick += LabelPlace_MouseClick_WotH;

            foreach (var gossipStone in newWotH.listGossipStone)
            {
                Controls.Add(gossipStone);
            }

            textBoxCustom.newLocation(new Point(0, newWotH.LabelPlace.Location.Y + newWotH.LabelPlace.Height), Location);
            return newWotH;
        }

        private void textBoxCustom_MouseClick(object sender, MouseEventArgs e)
        {
            ((TextBox)sender).Text = string.Empty;
        }

        private void textBoxCustom_KeyDown_Barren(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                var textbox = (TextBox)sender;
                AddBarrenEntry(textbox.Text);
                textbox.Text = string.Empty;
            }
        }

        private void textBoxCustom_KeyDown_WotH(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                var textbox = (TextBox)sender;
                if (ListWotH.Count < NbMaxRows)
                {
                    AddWotHEntry(textbox.Text, true);
                }
                textbox.Text = string.Empty;
            }
        }

        private void LabelPlace_MouseClick_Barren(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                var label = (Label)sender;
                var barren = this.ListBarren.Where(x => x.LabelPlace.Name == label.Name).ToList()[0];
                this.RemoveBarren(barren);
            }
        }

        private void LabelPlace_MouseClick_WotH(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                var label = (Label)sender;
                var woth = this.ListWotH.Where(x => x.LabelPlace.Name == label.Name).ToList()[0];
                this.RemoveWotH(woth);
            }
        }

        public void RemoveWotH(WotH woth)
        {
            ListWotH.Remove(woth);

            this.Controls.Remove(woth.LabelPlace);
            foreach (var gossipStone in woth.listGossipStone)
            {
                this.Controls.Remove(gossipStone);
            }

            for (int i = 0; i < ListWotH.Count; i++)
            {
                var wothLabel = ListWotH[i].LabelPlace;
                var newY = i * wothLabel.Height;
                wothLabel.Location = new Point(wothLabel.Left, newY);

                for (int j = 0; j < ListWotH[i].listGossipStone.Count; j++)
                {
                    var newX = ListWotH[i].listGossipStone[j].Location.X;
                    ListWotH[i].listGossipStone[j].Location = new Point(newX, newY);
                }
            }
            textBoxCustom.newLocation(new Point(2, ListWotH.Count * woth.LabelPlace.Height), this.Location);
        }

        public void RemoveBarren(Barren barren)
        {
            ListBarren.Remove(barren);

            this.Controls.Remove(barren.LabelPlace);

            for (int i = 0; i < ListBarren.Count; i++)
            {
                var wothLabel = ListBarren[i].LabelPlace;
                wothLabel.Location = new Point(2, (i * wothLabel.Height));
            }
            textBoxCustom.newLocation(new Point(2, ListBarren.Count * barren.LabelPlace.Height), this.Location);
        }
    }
}
