using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GSTHD
{
    class DoubleItem : PictureBox
    {
        List<string> ListImageName;
        bool isMouseDown = false;
        bool isColoredLeft = false;
        bool isColoredRight = false;
        Size DoubleItemSize;

        public DoubleItem(ObjectPoint data)
        {
            if(data.ImageCollection != null)
                ListImageName = data.ImageCollection.ToList();

            DoubleItemSize = data.Size;

            if (ListImageName.Count > 0)
            {
                this.Name = ListImageName[0];
                this.Image = Image.FromFile(@"Resources/" + ListImageName[0]);
                this.SizeMode = PictureBoxSizeMode.StretchImage;
                this.Size = DoubleItemSize;
            }

            this.BackColor = Color.Transparent;
            this.Location = new Point(data.X, data.Y);
            this.TabStop = false;
            this.AllowDrop = false;
            this.MouseUp += this.Click_MouseUp;
            this.MouseDown += this.Click_MouseDown;
            this.MouseMove += this.Click_MouseMove;
        }

        private void Click_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!isColoredLeft && !isColoredRight) isColoredLeft = true;
                else if (isColoredLeft && !isColoredRight) isColoredLeft = false;
                else if (!isColoredLeft && isColoredRight) isColoredLeft = true;
                else if (isColoredLeft && isColoredRight) isColoredLeft = false;

                UpdateImageFromState();

            }
            if (e.Button == MouseButtons.Right)
            {
                if (!isColoredLeft && !isColoredRight) isColoredRight = true;
                else if (isColoredLeft && !isColoredRight) isColoredRight = true;
                else if (!isColoredLeft && isColoredRight) isColoredRight = false;
                else if (isColoredLeft && isColoredRight) isColoredRight = false;

                UpdateImageFromState();
            }
        }

        private void UpdateImageFromState()
        {
            if (ListImageName == null || ListImageName.Count < 4)
                return;

            var index = GetState();
            this.Image = Image.FromFile(@"Resources/" + ListImageName[index]);
        }

        public int GetState()
        {
            if (!isColoredLeft && !isColoredRight) return 0;
            if (isColoredLeft && !isColoredRight) return 1;
            if (!isColoredLeft && isColoredRight) return 2;
            return 3;
        }

        public void SetState(int state)
        {
            var normalized = System.Math.Max(0, System.Math.Min(3, state));
            isColoredLeft = normalized == 1 || normalized == 3;
            isColoredRight = normalized == 2 || normalized == 3;
            UpdateImageFromState();
        }

        public void ResetState()
        {
            isColoredLeft = false;
            isColoredRight = false;
            UpdateImageFromState();
        }

        private void Click_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Clicks != 1)
                isMouseDown = false;
            else isMouseDown = true;
        }


        private void Click_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isMouseDown)
            {
                // TODO change that bool to DragBehaviour.AutocheckDragDrop
                var dropContent = new DragDropContent(false, ListImageName[4]);
                this.DoDragDrop(dropContent, DragDropEffects.Copy);
                isMouseDown = false;
            }
            if (e.Button == MouseButtons.Right && isMouseDown)
            {
                var dropContent = new DragDropContent(false, ListImageName[5]);
                this.DoDragDrop(dropContent, DragDropEffects.Copy);
                isMouseDown = false;
            }
        }
    }
}
