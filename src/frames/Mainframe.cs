﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Timers;
using System.ComponentModel;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;

namespace Core
{
	public partial class Mainframe : Form
	{
		private bool _stateClose = false;
		private bool _statePause = false;

		private readonly NotifyIcon _notifyIcon;
		private System.Timers.Timer _mainframeTimer = new System.Timers.Timer();

		struct BalloonMessage
		{
			public int timeout;
			public string title;
			public string message;
			public ToolTipIcon icon;
		}
		private Queue<BalloonMessage> _balloonQueue = new Queue<BalloonMessage>();

		struct TaskState
		{   // [System.ComponentModel.DisplayName("ID")] - for autogenerated columns
			public int TaskID { get; set; }
			public int RootTaskID { get; set; }
			public string TaskNote { get; set; }
			public int TaskProgress { get; set; }
			public string TaskStatus { get; set; }
			public DateTime TimeStamp { get; set; }
		}
		BindingList<TaskState> _taskStates = new BindingList<TaskState>();

		private TaskContext _taskContext;
		//-----------------------------------------------------------------------------

		public Mainframe()
		{
			InitializeComponent();

			GM.PrintEventHandler += cntMessageLog.AppendTextLog;
			this.Icon = new Icon(Tools.ResourceGetStream("res.icons.main.ico"));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Text = GM.assemblyCombinedTitle;

			// Notify icon
			_notifyIcon = new NotifyIcon();
			_notifyIcon.Text = GM.assemblyCombinedTitle;
			_notifyIcon.Icon = new Icon(Tools.ResourceGetStream("res.icons.main.ico")); //Icon.ExtractAssociatedIcon(Application.ExecutablePath);            
			_notifyIcon.Visible = true;
			_notifyIcon.DoubleClick += new EventHandler(NotifyIcon_DoubleClick);
			_notifyIcon.BalloonTipClicked += new EventHandler(NotifyIcon_DoubleClick);
			// Context menu
			_notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
			_notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Пауза",
				new Bitmap(Tools.ResourceGetStream("res.icons.pause.ico")), new EventHandler(OnPauseCommand)));
			_notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
			_notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Выход",
				new Bitmap(Tools.ResourceGetStream("res.icons.exit.ico")), new EventHandler(OnExitCommand)));

			SetStatusLine("Started!");

			// Ensure each column's DataPropertyName property is set to the corresponding name of the DataColumn's ColumnName.
			cntTaskGrid.AutoGenerateColumns = false;
			cntTaskGrid.RowHeadersVisible = false;
			cntTaskGrid.DataSource = _taskStates;

			_mainframeTimer.AutoReset = true;
			_mainframeTimer.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
			_mainframeTimer.Elapsed += Mainframe_TimerTick;
			_mainframeTimer.Enabled = true;

			CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
			_taskContext = new TaskContext()
			{
				tokenSource = _cancellationTokenSource,
				token = _cancellationTokenSource.Token,
				mainframe = this,
				pause = false
			};
			_taskContext.tasks = new Task[] { // Task[] tasks = { };
				Task.Factory.StartNew(KeeperCore.TaskMain, _taskContext, _taskContext.token)
			};
		}

		private void Mainframe_TimerTick(object sender, ElapsedEventArgs e)
		{
			cntTaskGrid.ThreadUI(() =>
			{
				//cleanup task list
				for(int i = _taskStates.Count - 1; i >= 0; i--)
				{
					if(DateTime.Now - _taskStates[i].TimeStamp > TimeSpan.FromSeconds(5) && _taskStates[i].TaskProgress >= 100)
						_taskStates.RemoveAt(i);
				}

				//for(int i = 20; i >= 0; i--)
				//	SetTaskProgress(GM.Random.Next(1, 45 + 1), GM.Random.Next(90, 100 + 1), DateTime.Now.ToString(), null, 0);

				// process balloons
				if(_balloonQueue.Count > 0)
				{
					BalloonMessage _ = _balloonQueue.Dequeue();
					_notifyIcon.ShowBalloonTip(_.timeout, _.title, _.message, _.icon);
				}
			});
		}

		private void Mainframe_FormClosing(object sender, FormClosingEventArgs e)
        {
			if (e.CloseReason != CloseReason.UserClosing || _stateClose) return;
			e.Cancel = true;
			this.Hide();

			//This will allow ALT+F4 or anything in the Application calling Application.Exit(); to act as normal while clicking the (X) will minimize the Application.
			//if(e.CloseReason == CloseReason.ApplicationExitCall)
			//	return;
			//else
			//{
			//	e.Cancel = true;
			//	WindowState = FormWindowState.Minimized;
			//}

		}

		private void Mainframe_FormClosed(object sender, FormClosedEventArgs e)
        {
            GM.PrintEventHandler -= cntMessageLog.AppendTextLog;
			_notifyIcon.Visible = false;
			_notifyIcon.Dispose();
		}

		private void NotifyIcon_DoubleClick(object sender, EventArgs e)
		{
			if(this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;
			if(!this.Visible) this.Show();
			this.Activate();
		}

		private void OnPauseCommand(object sender, EventArgs e) // OnPauseCommand
		{
			if(_statePause)
			{
				cntPause.Text = _notifyIcon.ContextMenuStrip.Items[0].Text = "Пауза";
				GM.Print("Продолжаем работу ...");
			}
			else
			{
				cntPause.Text = _notifyIcon.ContextMenuStrip.Items[0].Text = "Продолжить";
				GM.Print("Пауза ...");
			}
			_statePause = !_statePause;
			_taskContext.pause = _statePause;			
		}

		private void OnExitCommand(object sender, EventArgs e) // OnExitCommand
		{
			SetStatusLine("Завершаем работу!");

			_taskContext.tokenSource.Cancel();
			// stackoverflow.com/questions/40912357/c-sharp-task-waitall-cancelation-with-error-handling#
			Task.WaitAll(_taskContext.tasks); //Task.WhenAll(_taskContext.tasks);  Task.WaitAll(_taskContext.tasks, _taskContext.token);
			KeeperCore.Shutdown();

			_stateClose = true;
			this.Close();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys dataKey)
		{
			if(dataKey == Keys.Escape)
				this.Close();
			return base.ProcessCmdKey(ref msg, dataKey);
		}

		// ----------------------------------------------------------------------------
		public void ShowBalloonTip(int timeout, string title, string message, ToolTipIcon icon)
		{
			cntTaskGrid.ThreadUI(() => {
				_balloonQueue.Enqueue(new BalloonMessage() { timeout = timeout, title = title, message = message, icon = icon });
			});
		}

		public void SetTaskProgress(int taskID, int progress = -1, string status = null, string note = null, int rootTaskID = -1)
		{
			cntTaskGrid.ThreadUI(() =>
			{
				int index = -1;
				int rootIndex = -1;
				// TODO: int index = myList.FindIndex(a => a.Prop == oProp); -1 if not found
				TaskState state = _taskStates.FirstOrDefault(item => item.TaskID == taskID);
				if(state.Equals(default(TaskState)))
				{
					state.TaskID = taskID;
					state.RootTaskID = -1;
				}					
				else
					index = _taskStates.IndexOf(state);

				if(progress >= 0)
					state.TaskProgress = progress;
				if(status != null)
					state.TaskStatus = status;
				if(note != null)
					state.TaskNote = note;
				if(rootTaskID >= 0 && rootTaskID != state.RootTaskID)
				{
					state.RootTaskID = rootTaskID;
					TaskState root = _taskStates.FirstOrDefault(item => item.TaskID == rootTaskID);
					if(!root.Equals(default(TaskState)))
						rootIndex = _taskStates.IndexOf(root) + 1;
				}

				if(index != -1)
				{
					if(rootIndex != -1)
					{
						_taskStates.RemoveAt(index);
						_taskStates.Insert(rootIndex, state);
					}
					else
						_taskStates[index] = state;
				}
				else
				{
					index = rootIndex != -1 ? rootIndex : _taskStates.Count;
					_taskStates.Insert(index, state);
				}

			});
		}

		public void SetStatusLine(string status = "")
		{
			cntStatusStrip.ThreadUI(() => { cntStatusLine.Text = status; });
		}
		
		// ----------------------------------------------------------------------------
	}
	
	public class TaskContext
	{
		public CancellationTokenSource tokenSource;
		public CancellationToken token;
		public Mainframe mainframe;		
		public Task[] tasks;
		public bool pause;
	}
}
