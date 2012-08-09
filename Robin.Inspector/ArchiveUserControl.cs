using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using robin.core;

namespace Robin.Inspector
{
	public partial class ArchiveUserControl : UserControl
	{
		private Archive archive;
		public ArchiveUserControl()
		{
			InitializeComponent();
		}

		public Archive Archive
		{
			get { return archive; }
			set { archive = value;
				ReloadInfos();
			}
		}

		private void ReloadInfos()
		{
			archiveTextBox.Text = archive.Dump();
		}
	}
}
