using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace card_reader
{
    public partial class FormMessage : Form
    {
        public DialogResult result { get; set; }
        public FormMessage()
        {
            InitializeComponent();
        }
        public FormMessage(string message, string  title)
        {
            InitializeComponent();
            this.Font = new System.Drawing.Font("Arial Black", 18F);
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;

            Button buttonYes = new Button()
            {
                Text = "ОК",
                Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))),
                Name = "button1",
                Size = new System.Drawing.Size(350, 50),
                TabIndex = 0,
                Location = new System.Drawing.Point(12, 150),
                Margin = new System.Windows.Forms.Padding(7, 7, 7, 7),
                UseVisualStyleBackColor = true,
                Dock = DockStyle.Bottom
            };
            textBox1.Text = message;
            Controls.Add(buttonYes);
            buttonYes.Click += buttonYes_Click;
            textBox1.Text = message;
            textBox1.Font = this.Font;
            //Size size = TextRenderer.MeasureText(message, this.Font);
            //textBox1.Height = (size.Height+200);
            //textBox1.Width = size.Width;
            this.Text = title;
            //this.Width = textBox1.Width;

            textBox1.Multiline = true;
        }
        public FormMessage(string message, string title, DialogResult dialogResult)
        {
            this.result = dialogResult;
            InitializeComponent();
            this.Font = new System.Drawing.Font("Arial Black", 20F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            button1.Visible = true;
            button1.Enabled = true;
            button2.Visible = true;
            button2.Enabled = true;
  
            textBox1.Text = message;
            textBox1.Font = this.Font;
            this.Width = textBox1.Width;
            //Size size = TextRenderer.MeasureText(message, this.Font);
            //textBox1.Height = (size.Height+200);
            //textBox1.Width = size.Width;

            textBox1.Multiline = true;
            this.Text = title;
            
        }
        private void buttonYes_Click(object sender, EventArgs e)
        {
            this.result = DialogResult.Yes;
            Close();
        }
        private void buttonNo_Click(object sender, EventArgs e)
        {
            this.result = DialogResult.No;
            Close();
        }
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            //this.Width = textBox1.Width;
            //this.Height = textBox1.Height < 1400 ? textBox1.Height : 1400;
            textBox1.Height = textBox1.Height < 1400 ? textBox1.Height : 1200;
        }

        private void FormMessage_Load(object sender, EventArgs e)
        {
            this.Width = textBox1.Width;
            this.Height = textBox1.Height < 1300 ? (textBox1.Height) : 1300;
            //richTextBox1.Height = richTextBox1.Height < 1400 ? richTextBox1.Height : 1200;
            //richTextBox1.Width = richTextBox1.Width < 1200 ? richTextBox1.Width : 1200;
            this.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        }
    }
}
