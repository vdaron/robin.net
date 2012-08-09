using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using robin.core;

namespace Robin.Inspector
{
	public partial class Form1 : Form
	{
		private RrdDb theRRdDb;
		public Form1()
		{
			InitializeComponent();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(theOpenFileDialog.ShowDialog() == DialogResult.OK)
			{
				if(theRRdDb != null)
				{
					theRrdtreeView.Nodes.Clear();
					theRRdDb.Close();
				}
				theRRdDb = RrdDb.Open(theOpenFileDialog.FileName,true);
				TreeNode dataSourceNode = theRrdtreeView.Nodes.Add("DataSources", "Data Sources");
				TreeNode archivesNode = theRrdtreeView.Nodes.Add("Archives", "Archives");
				foreach (var dataSource in theRRdDb.DataSources)
				{
					TreeNode node = new TreeNode(dataSource.Name);
					node.Tag = dataSource;
					dataSourceNode.Nodes.Add(node);
				}
				foreach (var archive in theRRdDb.Archives)
				{
					TreeNode node = new TreeNode();
					node.Tag = archive;
					node.Text = String.Format("{0}-{1}-{2}-{3}", archive.ConsolidationFunction, archive.Xff, archive.Steps,archive.Rows);
					archivesNode.Nodes.Add(node);
				}

			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if(theRRdDb != null)
				theRRdDb.Close();
		}

		private void theRrdtreeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if(e.Node.Tag != null)
			{
				if(e.Node.Tag is DataSource)
				{
					SetUserControl(new DataSourceUserControl {DataSource = e.Node.Tag as DataSource});
				}
				else if(e.Node.Tag is Archive)
				{
					SetUserControl(new ArchiveUserControl() { Archive = e.Node.Tag as Archive });
				}
			}
			else
			{
				SetUserControl(null);
			}
		}

		private void SetUserControl(UserControl aControl)
		{
			rightPanel.Controls.Clear();
			if(aControl != null)
			{
				rightPanel.Controls.Add(aControl);
				aControl.Dock = DockStyle.Fill;
			}
		}
	}
}

