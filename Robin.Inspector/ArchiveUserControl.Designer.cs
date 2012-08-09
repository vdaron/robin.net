namespace Robin.Inspector
{
	partial class ArchiveUserControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.archiveTextBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// archiveTextBox
			// 
			this.archiveTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.archiveTextBox.Location = new System.Drawing.Point(0, 0);
			this.archiveTextBox.Multiline = true;
			this.archiveTextBox.Name = "archiveTextBox";
			this.archiveTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.archiveTextBox.Size = new System.Drawing.Size(562, 281);
			this.archiveTextBox.TabIndex = 0;
			// 
			// ArchiveUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.archiveTextBox);
			this.Name = "ArchiveUserControl";
			this.Size = new System.Drawing.Size(562, 281);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox archiveTextBox;
	}
}
