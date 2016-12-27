namespace ThreeBody
{
    partial class ThreeBodyForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ThreeBodyForm));
            this.SpacePbx = new System.Windows.Forms.PictureBox();
            this.MainContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.editSystemXMLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gotoTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.oSDToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.demoThreeBodyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.demoSolarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showOrbitsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.SpacePbx)).BeginInit();
            this.MainContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // SpacePbx
            // 
            this.SpacePbx.BackColor = System.Drawing.Color.Black;
            this.SpacePbx.ContextMenuStrip = this.MainContextMenu;
            this.SpacePbx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SpacePbx.Location = new System.Drawing.Point(0, 0);
            this.SpacePbx.Name = "SpacePbx";
            this.SpacePbx.Size = new System.Drawing.Size(784, 556);
            this.SpacePbx.TabIndex = 0;
            this.SpacePbx.TabStop = false;
            this.SpacePbx.Click += new System.EventHandler(this.SpacePbxClick);
            this.SpacePbx.DoubleClick += new System.EventHandler(this.SpacePbxDoubleClick);
            this.SpacePbx.MouseDown += new System.Windows.Forms.MouseEventHandler(this.SpacePbxMouseDown);
            this.SpacePbx.MouseMove += new System.Windows.Forms.MouseEventHandler(this.SpacePbxMouseMove);
            this.SpacePbx.MouseUp += new System.Windows.Forms.MouseEventHandler(this.SpacePbxMouseUp);
            // 
            // MainContextMenu
            // 
            this.MainContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.editSystemXMLToolStripMenuItem,
            this.reloadToolStripMenuItem,
            this.gotoTimeToolStripMenuItem,
            this.oSDToolStripMenuItem,
            this.demoThreeBodyToolStripMenuItem,
            this.demoSolarToolStripMenuItem,
            this.showOrbitsToolStripMenuItem});
            this.MainContextMenu.Name = "MainContextMenu";
            this.MainContextMenu.Size = new System.Drawing.Size(170, 180);
            // 
            // editSystemXMLToolStripMenuItem
            // 
            this.editSystemXMLToolStripMenuItem.Name = "editSystemXMLToolStripMenuItem";
            this.editSystemXMLToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.editSystemXMLToolStripMenuItem.Text = "&Edit System XML";
            this.editSystemXMLToolStripMenuItem.Click += new System.EventHandler(this.EditSystemXmlToolStripMenuItemClick);
            // 
            // reloadToolStripMenuItem
            // 
            this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            this.reloadToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.reloadToolStripMenuItem.Text = "&Reload";
            this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
            // 
            // gotoTimeToolStripMenuItem
            // 
            this.gotoTimeToolStripMenuItem.Name = "gotoTimeToolStripMenuItem";
            this.gotoTimeToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.gotoTimeToolStripMenuItem.Text = "&Goto Time";
            this.gotoTimeToolStripMenuItem.Click += new System.EventHandler(this.GotoTimeToolStripMenuItemClick);
            // 
            // oSDToolStripMenuItem
            // 
            this.oSDToolStripMenuItem.CheckOnClick = true;
            this.oSDToolStripMenuItem.Name = "oSDToolStripMenuItem";
            this.oSDToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.oSDToolStripMenuItem.Text = "&OSD";
            this.oSDToolStripMenuItem.Click += new System.EventHandler(this.OsdToolStripMenuItemClick);
            // 
            // demoThreeBodyToolStripMenuItem
            // 
            this.demoThreeBodyToolStripMenuItem.Name = "demoThreeBodyToolStripMenuItem";
            this.demoThreeBodyToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.demoThreeBodyToolStripMenuItem.Text = "Demo ThreeBody!";
            this.demoThreeBodyToolStripMenuItem.Click += new System.EventHandler(this.demoThreeBodyToolStripMenuItem_Click);
            // 
            // demoSolarToolStripMenuItem
            // 
            this.demoSolarToolStripMenuItem.Name = "demoSolarToolStripMenuItem";
            this.demoSolarToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.demoSolarToolStripMenuItem.Text = "Demo &Solar";
            this.demoSolarToolStripMenuItem.Click += new System.EventHandler(this.DemoSolarToolStripMenuItemClick);
            // 
            // showOrbitsToolStripMenuItem
            // 
            this.showOrbitsToolStripMenuItem.CheckOnClick = true;
            this.showOrbitsToolStripMenuItem.Name = "showOrbitsToolStripMenuItem";
            this.showOrbitsToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.showOrbitsToolStripMenuItem.Text = "Show Orbits";
            this.showOrbitsToolStripMenuItem.Click += new System.EventHandler(this.showTrajetoryToolStripMenuItem_Click);
            // 
            // ThreeBodyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 556);
            this.Controls.Add(this.SpacePbx);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "ThreeBodyForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Three Body";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ThreeBodyForm_FormClosing);
            this.Load += new System.EventHandler(this.ThreeBodyFormLoad);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ThreeBodyFormKeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ThreeBodyFormKeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.SpacePbx)).EndInit();
            this.MainContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox SpacePbx;
        private System.Windows.Forms.ContextMenuStrip MainContextMenu;
        private System.Windows.Forms.ToolStripMenuItem editSystemXMLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem oSDToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gotoTimeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem demoSolarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem demoThreeBodyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showOrbitsToolStripMenuItem;
    }
}

