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
	public partial class DataSourceUserControl : UserControl
	{
		private DataSource dataSource;
		public DataSourceUserControl()
		{
			InitializeComponent();
		}


		public DataSource DataSource
		{
			get { return dataSource; }
			set
			{
				dataSource = value;
				ReloadInfos();
			}
		}

		private void ReloadInfos()
		{
			sourceTextBox.Text = dataSource.ToString();
		}
	}
}
