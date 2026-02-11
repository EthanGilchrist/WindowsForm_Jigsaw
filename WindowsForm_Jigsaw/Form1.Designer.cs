namespace WindowsForm_Jigsaw
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
            this.cowBorder = new System.Windows.Forms.PictureBox();
            this.puzzleBox = new System.Windows.Forms.PictureBox();
            this.statusMessage = new System.Windows.Forms.TextBox();
            this.imageLoadButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.cowBorder)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.puzzleBox)).BeginInit();
            this.SuspendLayout();
            // 
            // cowBorder
            // 
            this.cowBorder.Image = global::WindowsForm_Jigsaw.Properties.Resources.black_600_1200;
            this.cowBorder.Location = new System.Drawing.Point(606, 427);
            this.cowBorder.Name = "cowBorder";
            this.cowBorder.Size = new System.Drawing.Size(103, 91);
            this.cowBorder.TabIndex = 2;
            this.cowBorder.TabStop = false;
            this.cowBorder.Visible = false;
            // 
            // puzzleBox
            // 
            this.puzzleBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.puzzleBox.Image = global::WindowsForm_Jigsaw.Properties.Resources.blank_600_1200;
            this.puzzleBox.Location = new System.Drawing.Point(12, 15);
            this.puzzleBox.Name = "puzzleBox";
            this.puzzleBox.Size = new System.Drawing.Size(1200, 900);
            this.puzzleBox.TabIndex = 0;
            this.puzzleBox.TabStop = false;
            this.puzzleBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.puzzleBox_MouseDown);
            this.puzzleBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.puzzleBox_MouseMove);
            this.puzzleBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.puzzleBox_MouseUp);
            // 
            // statusMessage
            // 
            this.statusMessage.Location = new System.Drawing.Point(287, 624);
            this.statusMessage.Name = "statusMessage";
            this.statusMessage.Size = new System.Drawing.Size(643, 26);
            this.statusMessage.TabIndex = 3;
            this.statusMessage.Text = "lorem ipsum";
            this.statusMessage.Visible = false;
            // 
            // imageLoadButton
            // 
            this.imageLoadButton.Location = new System.Drawing.Point(1226, 25);
            this.imageLoadButton.Name = "imageLoadButton";
            this.imageLoadButton.Size = new System.Drawing.Size(77, 55);
            this.imageLoadButton.TabIndex = 4;
            this.imageLoadButton.Text = "Load Image";
            this.imageLoadButton.UseVisualStyleBackColor = true;
            this.imageLoadButton.Click += new System.EventHandler(this.imageLoadButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1315, 944);
            this.Controls.Add(this.imageLoadButton);
            this.Controls.Add(this.statusMessage);
            this.Controls.Add(this.cowBorder);
            this.Controls.Add(this.puzzleBox);
            this.MinimumSize = new System.Drawing.Size(750, 750);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.cowBorder)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.puzzleBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox puzzleBox;
        private System.Windows.Forms.PictureBox cowBorder;
        private System.Windows.Forms.TextBox statusMessage;
        private System.Windows.Forms.Button imageLoadButton;
    }
}

