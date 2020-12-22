using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TwainLib;
using System.Collections;
using System.Drawing.Imaging;

namespace netmasterWinforums
{

    public partial class Form1 : Form, IMessageFilter
	{
        private bool msgfilter;
        private Twain tw;
        private int picnumber = 0;

        public Form1()
        {
            InitializeComponent();
            tw = new Twain();
            tw.Init(this.Handle);
        }
        

		private void EndingScan()
		{
			if (msgfilter)
			{
				Application.RemoveMessageFilter(this);
				msgfilter = false;
				this.Enabled = true;
				this.Activate();
			}
		}
		IntPtr imgSave;
		bool IMessageFilter.PreFilterMessage(ref Message m)
		{
			TwainCommand cmd = tw.PassMessage(ref m);
			if (cmd == TwainCommand.Not)
				return false;

			switch (cmd)
			{
				case TwainCommand.CloseRequest:
					{
						EndingScan();
						tw.CloseSrc();
						break;
					}
				case TwainCommand.CloseOk:
					{
						EndingScan();
						tw.CloseSrc();
						break;
					}
				case TwainCommand.DeviceEvent:
					{
						break;
					}
				case TwainCommand.TransferReady:
					{
						ArrayList pics = tw.TransferPictures();
						EndingScan();
						tw.CloseSrc();
						picnumber++;
						for (int i = 0; i < pics.Count; i++)
						{
							IntPtr img = (IntPtr)pics[i];
							Bitmap bmp= Twain.BitmapFromDIB(img);
							pictureBox1.Image = bmp;
							pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

							//pictureBox1 newpic = new pictureBox1(img);
							//newpic.MdiParent = this;
							//int picnum = i + 1;
							//newpic.Text = "ScanPass" + picnumber.ToString() + "_Pic" + picnum.ToString();
							//newpic.Show();
							imgSave = img;
						}
						break;
					}
			}

			return true;
		}


		private void button1_Click(object sender, EventArgs e)
        {
			tw.Select();
		}

        private void button2_Click(object sender, EventArgs e)
        {
			if (!msgfilter)
			{
				this.Enabled = false;
				msgfilter = true;
				Application.AddMessageFilter(this);
			}
			tw.Acquire();
		}

        private void button3_Click(object sender, EventArgs e)
        {
			Close();

		}

        private void button4_Click(object sender, EventArgs e)
        {
			//Twain.SavehDibToTiff(imgSave, "bitmaphandleimg", 50, 50);
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Images|*.png;*.bmp;*.jpg";
			ImageFormat format = ImageFormat.Png;
			if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				string ext = System.IO.Path.GetExtension(sfd.FileName);
				switch (ext)
				{
					case ".jpg":
						format = ImageFormat.Jpeg;
						break;
					case ".bmp":
						format = ImageFormat.Bmp;
						break;
				}
				pictureBox1.Image.Save(sfd.FileName, format);
			}

		}
    }
}
