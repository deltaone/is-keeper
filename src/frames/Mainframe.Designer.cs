namespace Core
{
    partial class Mainframe
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
			this.cntPause = new System.Windows.Forms.Button();
			this.cntExit = new System.Windows.Forms.Button();
			this.cntStatusLine = new System.Windows.Forms.ToolStripStatusLabel();
			this.cntStatusStrip = new System.Windows.Forms.StatusStrip();
			this.cntTaskGrid = new System.Windows.Forms.DataGridView();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewProgressColumn1 = new Core.DataGridViewProgressColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.cntMessageLog = new ControlsEx.RichTextLog();
			this.TaskID = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Note = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.TaskProgress = new Core.DataGridViewProgressColumn();
			this.TaskStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.RootID = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.cntStatusStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.cntTaskGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// cntPause
			// 
			this.cntPause.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.cntPause.Location = new System.Drawing.Point(797, 113);
			this.cntPause.Name = "cntPause";
			this.cntPause.Size = new System.Drawing.Size(109, 23);
			this.cntPause.TabIndex = 2;
			this.cntPause.Text = "Пауза";
			this.cntPause.UseVisualStyleBackColor = true;
			this.cntPause.Click += new System.EventHandler(this.OnPauseCommand);
			// 
			// cntExit
			// 
			this.cntExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.cntExit.Location = new System.Drawing.Point(797, 142);
			this.cntExit.Name = "cntExit";
			this.cntExit.Size = new System.Drawing.Size(109, 23);
			this.cntExit.TabIndex = 3;
			this.cntExit.Text = "Выход";
			this.cntExit.UseVisualStyleBackColor = true;
			this.cntExit.Click += new System.EventHandler(this.OnExitCommand);
			// 
			// cntStatusLine
			// 
			this.cntStatusLine.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.cntStatusLine.Name = "cntStatusLine";
			this.cntStatusLine.Size = new System.Drawing.Size(51, 17);
			this.cntStatusLine.Text = "[Статус]";
			// 
			// cntStatusStrip
			// 
			this.cntStatusStrip.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.cntStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cntStatusLine});
			this.cntStatusStrip.Location = new System.Drawing.Point(0, 362);
			this.cntStatusStrip.Name = "cntStatusStrip";
			this.cntStatusStrip.Size = new System.Drawing.Size(918, 22);
			this.cntStatusStrip.SizingGrip = false;
			this.cntStatusStrip.TabIndex = 1;
			this.cntStatusStrip.Text = "statusStrip";
			// 
			// cntTaskGrid
			// 
			this.cntTaskGrid.AllowUserToAddRows = false;
			this.cntTaskGrid.AllowUserToDeleteRows = false;
			this.cntTaskGrid.AllowUserToResizeRows = false;
			this.cntTaskGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.cntTaskGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.TaskID,
            this.Note,
            this.TaskProgress,
            this.TaskStatus,
            this.RootID});
			this.cntTaskGrid.Location = new System.Drawing.Point(0, 0);
			this.cntTaskGrid.Name = "cntTaskGrid";
			this.cntTaskGrid.ReadOnly = true;
			this.cntTaskGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.cntTaskGrid.Size = new System.Drawing.Size(776, 177);
			this.cntTaskGrid.TabIndex = 4;
			// 
			// dataGridViewTextBoxColumn1
			// 
			this.dataGridViewTextBoxColumn1.DataPropertyName = "TaskID";
			this.dataGridViewTextBoxColumn1.HeaderText = "ID";
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			// 
			// dataGridViewProgressColumn1
			// 
			this.dataGridViewProgressColumn1.DataPropertyName = "TaskProgress";
			this.dataGridViewProgressColumn1.HeaderText = "Progress";
			this.dataGridViewProgressColumn1.Name = "dataGridViewProgressColumn1";
			this.dataGridViewProgressColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.dataGridViewProgressColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			// 
			// dataGridViewTextBoxColumn2
			// 
			this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTextBoxColumn2.DataPropertyName = "TaskStatus";
			this.dataGridViewTextBoxColumn2.HeaderText = "Status";
			this.dataGridViewTextBoxColumn2.MinimumWidth = 200;
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			// 
			// cntMessageLog
			// 
			this.cntMessageLog.BackColor = System.Drawing.SystemColors.ControlLight;
			this.cntMessageLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.cntMessageLog.Font = new System.Drawing.Font("Consolas", 9F);
			this.cntMessageLog.ForeColor = System.Drawing.Color.Blue;
			this.cntMessageLog.Location = new System.Drawing.Point(0, 183);
			this.cntMessageLog.LogMaxLines = 150;
			this.cntMessageLog.Name = "cntMessageLog";
			this.cntMessageLog.ReadOnly = true;
			this.cntMessageLog.Size = new System.Drawing.Size(918, 174);
			this.cntMessageLog.TabIndex = 0;
			this.cntMessageLog.TabStop = false;
			this.cntMessageLog.Text = "";
			// 
			// TaskID
			// 
			this.TaskID.DataPropertyName = "TaskID";
			this.TaskID.HeaderText = "ID";
			this.TaskID.Name = "TaskID";
			this.TaskID.ReadOnly = true;
			this.TaskID.Width = 50;
			// 
			// Note
			// 
			this.Note.DataPropertyName = "TaskNote";
			this.Note.HeaderText = "Note";
			this.Note.Name = "Note";
			this.Note.ReadOnly = true;
			// 
			// TaskProgress
			// 
			this.TaskProgress.DataPropertyName = "TaskProgress";
			this.TaskProgress.HeaderText = "Progress";
			this.TaskProgress.Name = "TaskProgress";
			this.TaskProgress.ReadOnly = true;
			this.TaskProgress.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.TaskProgress.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			// 
			// TaskStatus
			// 
			this.TaskStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.TaskStatus.DataPropertyName = "TaskStatus";
			this.TaskStatus.HeaderText = "Status";
			this.TaskStatus.MinimumWidth = 200;
			this.TaskStatus.Name = "TaskStatus";
			this.TaskStatus.ReadOnly = true;
			// 
			// RootID
			// 
			this.RootID.DataPropertyName = "RootTaskID";
			this.RootID.HeaderText = "RootID";
			this.RootID.Name = "RootID";
			this.RootID.ReadOnly = true;
			// 
			// Mainframe
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(918, 384);
			this.Controls.Add(this.cntTaskGrid);
			this.Controls.Add(this.cntExit);
			this.Controls.Add(this.cntPause);
			this.Controls.Add(this.cntStatusStrip);
			this.Controls.Add(this.cntMessageLog);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MaximizeBox = false;
			this.Name = "Mainframe";
			this.Text = "Mainframe";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Mainframe_FormClosing);
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Mainframe_FormClosed);
			this.cntStatusStrip.ResumeLayout(false);
			this.cntStatusStrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.cntTaskGrid)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private ControlsEx.RichTextLog cntMessageLog;
        private System.Windows.Forms.Button cntPause;
        private System.Windows.Forms.Button cntExit;
        public System.Windows.Forms.ToolStripStatusLabel cntStatusLine;
        protected System.Windows.Forms.StatusStrip cntStatusStrip;
		private System.Windows.Forms.DataGridView cntTaskGrid;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private DataGridViewProgressColumn dataGridViewProgressColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn TaskID;
		private System.Windows.Forms.DataGridViewTextBoxColumn Note;
		private DataGridViewProgressColumn TaskProgress;
		private System.Windows.Forms.DataGridViewTextBoxColumn TaskStatus;
		private System.Windows.Forms.DataGridViewTextBoxColumn RootID;
	}
}