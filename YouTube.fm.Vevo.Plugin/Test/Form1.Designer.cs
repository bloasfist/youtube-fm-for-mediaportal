namespace Test
{
  partial class Form1
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.textBox1 = new System.Windows.Forms.TextBox();
        this.button1 = new System.Windows.Forms.Button();
        this.button2 = new System.Windows.Forms.Button();
        this.textBox2 = new System.Windows.Forms.TextBox();
        this.tabControl1 = new System.Windows.Forms.TabControl();
        this.tabPage1 = new System.Windows.Forms.TabPage();
        this.tabPage2 = new System.Windows.Forms.TabPage();
        this.textBox3 = new System.Windows.Forms.TextBox();
        this.button3 = new System.Windows.Forms.Button();
        this.pictureBox1 = new System.Windows.Forms.PictureBox();
        this.listBox1 = new System.Windows.Forms.ListBox();
        this.tabControl1.SuspendLayout();
        this.tabPage1.SuspendLayout();
        this.tabPage2.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
        this.SuspendLayout();
        // 
        // textBox1
        // 
        this.textBox1.Location = new System.Drawing.Point(20, 26);
        this.textBox1.Name = "textBox1";
        this.textBox1.Size = new System.Drawing.Size(232, 20);
        this.textBox1.TabIndex = 0;
        // 
        // button1
        // 
        this.button1.Location = new System.Drawing.Point(286, 24);
        this.button1.Name = "button1";
        this.button1.Size = new System.Drawing.Size(75, 23);
        this.button1.TabIndex = 1;
        this.button1.Text = "Search";
        this.button1.UseVisualStyleBackColor = true;
        this.button1.Click += new System.EventHandler(this.button1_Click);
        // 
        // button2
        // 
        this.button2.Location = new System.Drawing.Point(458, 111);
        this.button2.Name = "button2";
        this.button2.Size = new System.Drawing.Size(114, 27);
        this.button2.TabIndex = 2;
        this.button2.Text = "Last fm test";
        this.button2.UseVisualStyleBackColor = true;
        this.button2.Click += new System.EventHandler(this.button2_Click);
        // 
        // textBox2
        // 
        this.textBox2.Location = new System.Drawing.Point(20, 76);
        this.textBox2.Multiline = true;
        this.textBox2.Name = "textBox2";
        this.textBox2.Size = new System.Drawing.Size(310, 192);
        this.textBox2.TabIndex = 3;
        // 
        // tabControl1
        // 
        this.tabControl1.Controls.Add(this.tabPage1);
        this.tabControl1.Controls.Add(this.tabPage2);
        this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tabControl1.Location = new System.Drawing.Point(0, 0);
        this.tabControl1.Name = "tabControl1";
        this.tabControl1.SelectedIndex = 0;
        this.tabControl1.Size = new System.Drawing.Size(660, 324);
        this.tabControl1.TabIndex = 4;
        // 
        // tabPage1
        // 
        this.tabPage1.Controls.Add(this.textBox2);
        this.tabPage1.Controls.Add(this.textBox1);
        this.tabPage1.Controls.Add(this.button2);
        this.tabPage1.Controls.Add(this.button1);
        this.tabPage1.Location = new System.Drawing.Point(4, 22);
        this.tabPage1.Name = "tabPage1";
        this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
        this.tabPage1.Size = new System.Drawing.Size(652, 298);
        this.tabPage1.TabIndex = 0;
        this.tabPage1.Text = "Youtube";
        this.tabPage1.UseVisualStyleBackColor = true;
        // 
        // tabPage2
        // 
        this.tabPage2.Controls.Add(this.listBox1);
        this.tabPage2.Controls.Add(this.pictureBox1);
        this.tabPage2.Controls.Add(this.button3);
        this.tabPage2.Controls.Add(this.textBox3);
        this.tabPage2.Location = new System.Drawing.Point(4, 22);
        this.tabPage2.Name = "tabPage2";
        this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
        this.tabPage2.Size = new System.Drawing.Size(652, 298);
        this.tabPage2.TabIndex = 1;
        this.tabPage2.Text = "FanArt";
        this.tabPage2.UseVisualStyleBackColor = true;
        // 
        // textBox3
        // 
        this.textBox3.Location = new System.Drawing.Point(17, 21);
        this.textBox3.Name = "textBox3";
        this.textBox3.Size = new System.Drawing.Size(206, 20);
        this.textBox3.TabIndex = 0;
        // 
        // button3
        // 
        this.button3.Location = new System.Drawing.Point(249, 19);
        this.button3.Name = "button3";
        this.button3.Size = new System.Drawing.Size(75, 23);
        this.button3.TabIndex = 1;
        this.button3.Text = "Search";
        this.button3.UseVisualStyleBackColor = true;
        this.button3.Click += new System.EventHandler(this.button3_Click);
        // 
        // pictureBox1
        // 
        this.pictureBox1.Location = new System.Drawing.Point(17, 65);
        this.pictureBox1.Name = "pictureBox1";
        this.pictureBox1.Size = new System.Drawing.Size(307, 188);
        this.pictureBox1.TabIndex = 2;
        this.pictureBox1.TabStop = false;
        // 
        // listBox1
        // 
        this.listBox1.FormattingEnabled = true;
        this.listBox1.Location = new System.Drawing.Point(369, 22);
        this.listBox1.Name = "listBox1";
        this.listBox1.Size = new System.Drawing.Size(263, 238);
        this.listBox1.TabIndex = 3;
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(660, 324);
        this.Controls.Add(this.tabControl1);
        this.Name = "Form1";
        this.Text = "Form1";
        this.tabControl1.ResumeLayout(false);
        this.tabPage1.ResumeLayout(false);
        this.tabPage1.PerformLayout();
        this.tabPage2.ResumeLayout(false);
        this.tabPage2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TextBox textBox1;
    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.Button button2;
    private System.Windows.Forms.TextBox textBox2;
    private System.Windows.Forms.TabControl tabControl1;
    private System.Windows.Forms.TabPage tabPage1;
    private System.Windows.Forms.TabPage tabPage2;
    private System.Windows.Forms.PictureBox pictureBox1;
    private System.Windows.Forms.Button button3;
    private System.Windows.Forms.TextBox textBox3;
    private System.Windows.Forms.ListBox listBox1;
  }
}

