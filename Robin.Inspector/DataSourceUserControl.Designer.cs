namespace Robin.Inspector
{
	partial class DataSourceUserControl
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
			this.sourceTextBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// sourceTextBox
			// 
			this.sourceTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.sourceTextBox.Location = new System.Drawing.Point(0, 0);
			this.sourceTextBox.Multiline = true;
			this.sourceTextBox.Name = "sourceTextBox";
			this.sourceTextBox.Size = new System.Drawing.Size(495, 291);
			this.sourceTextBox.TabIndex = 0;
			// 
			// DataSourceUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.sourceTextBox);
			this.Name = "DataSourceUserControl";
			this.Size = new System.Drawing.Size(495, 291);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox sourceTextBox;
	}
}
