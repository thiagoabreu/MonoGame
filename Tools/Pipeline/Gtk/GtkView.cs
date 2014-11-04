using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xwt;
using System.Diagnostics;

namespace MonoGame.Tools.Pipeline
{
	partial class GtkView : Gtk.Window, IView
	{
		/// <summary>
		/// The project which will be opened as soon as a controller is attached.
		/// Is used when PipelineTool is launched to open a project, provided by the command line.
		/// </summary>
		public string OpenProjectPath;

		IController _controller;

		// Dialog Filters
		FileDialogFilter MonoGameContentProjectFileFilter;
		FileDialogFilter XnaContentProjectFileFilter;
		FileDialogFilter AllFilesFilter;

		// TreeView Related
		Gtk.TreeStore _treeStore;

		private bool _treeUpdating;
		private bool _treeSort;
		private Gtk.TreeIter _root;

		Gdk.Pixbuf _contentIcon;
		Gdk.Pixbuf _folderClosedIcon;
		Gdk.Pixbuf _projectIcon;

		public GtkView () :
			base (Gtk.WindowType.Toplevel)
		{
			this.Build ();

			mainPaned.Position = (int)(DefaultWidth * 0.33);

			_treeStore = new Gtk.TreeStore (typeof(Gdk.Pixbuf), typeof(string), typeof(object));
			_treeView.AppendColumn ("Img", new Gtk.CellRendererPixbuf(), "pixbuf", 0);
			_treeView.AppendColumn ("Name", new Gtk.CellRendererText(), "text", 1);

			_contentIcon = new Gdk.Pixbuf(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(@"MonoGame.Tools.Pipeline.Icons.blueprint.png"));
			_folderClosedIcon = new Gdk.Pixbuf(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(@"MonoGame.Tools.Pipeline.Icons.folder_closed.png"));
			_projectIcon = new Gdk.Pixbuf(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(@"MonoGame.Tools.Pipeline.Icons.settings.png"));


			_treeStore.AppendValues (_contentIcon, "ra", new Object());
			_treeStore.AppendValues (_contentIcon, "ra2", new Object());
			// DialogFilters
			MonoGameContentProjectFileFilter = new FileDialogFilter ("MonoGame Content Build Projects (*.mgcb)", "*.mgcb");
			XnaContentProjectFileFilter = new FileDialogFilter ("XNA Content Projects (*.contentproj)", "*.contentproj");
			AllFilesFilter = new FileDialogFilter ("All Files (*.*)", "*.*");
		}

		protected override void OnShown ()
		{
			base.OnShown ();

			if (!String.IsNullOrEmpty (OpenProjectPath)) {
				_controller.OpenProject (OpenProjectPath);
				OpenProjectPath = null;
			}
		}

		protected override bool OnDestroyEvent (Gdk.Event evnt)
		{
			if (!_controller.Exit ())
				return false;

			return base.OnDestroyEvent (evnt);
		}

		protected override void OnDestroyed ()
		{
			Gtk.Application.Quit ();
		}

		#region IView implements

		public void Attach (IController controller)
		{
			_controller = controller;

			_controller.OnBuildStarted += () => stopAction.Sensitive = true;
			_controller.OnBuildFinished += () => stopAction.Sensitive = false;
			_controller.OnCanUndoRedoChanged += UpdateUndoRedo;
		}

		public AskResult AskSaveOrCancel ()
		{
			var result = MessageDialog.AskQuestion ("Do you want to save the project first?", Command.Yes, Command.No, Command.Cancel);

			if (result == Command.Yes)
				return AskResult.Yes;

			if (result == Command.No)
				return AskResult.No;

			return AskResult.Cancel;
		}

		public bool AskSaveName (ref string filePath, string title)
		{
			SaveFileDialog save = new SaveFileDialog () {
				Title = title,
				CurrentFolder = System.IO.Path.GetDirectoryName (filePath),
				InitialFileName = System.IO.Path.GetFileName (filePath)
			};
			save.Filters.Add (MonoGameContentProjectFileFilter);
			save.Filters.Add (AllFilesFilter);
			var result = save.Run ();

			filePath = save.FileName;

			return result;
		}

		public bool AskOpenProject (out string projectFilePath)
		{
			OpenFileDialog open = new OpenFileDialog () {
				Title = "Open MGCB Project"
			};
			open.Multiselect = false;
			open.Filters.Add (MonoGameContentProjectFileFilter);
			open.Filters.Add (AllFilesFilter);

			var result = open.Run ();
			projectFilePath = open.FileName;

			return result;
		}

		public bool AskImportProject (out string projectFilePath)
		{
			OpenFileDialog open = new OpenFileDialog () {
				Title = "Import XNA Content Project"
			};
			open.Multiselect = false;
			open.Filters.Add (XnaContentProjectFileFilter);
			open.Filters.Add (AllFilesFilter);

			var result = open.Run ();
			projectFilePath = open.FileName;

			return result;
		}

		public void ShowError (string title, string message)
		{
			MessageDialog.ShowError (title, message);
		}

		public void ShowMessage (string message)
		{
			MessageDialog.ShowMessage (message);
		}

		public void BeginTreeUpdate ()
		{
			Debug.Assert(_treeUpdating == false, "Must finish previous tree update!");
			_treeUpdating = true;
			_treeSort = false;
		}

		public void SetTreeRoot (IProjectItem item)
		{
			Debug.Assert(_treeUpdating, "Must call BeginTreeUpdate() first!");

			_treeStore.GetIterFirst (out _root);
			_treeStore.Clear ();

			var project = item as PipelineProject;
			if (project == null)
				return;

			_root = _treeStore.AppendValues (_projectIcon, item.Name, new PipelineProjectProxy (project));
		}

		public void AddTreeItem (IProjectItem item)
		{
			Debug.Assert(_treeUpdating, "Must call BeginTreeUpdate() first!");
			_treeSort = true;

			var path = item.Location;
			var folders = path.Split(new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

			var curPath = string.Empty;
			var parent = _root;
			foreach (var folder in folders)
			{
				curPath = System.IO.Path.Combine(curPath, folder);
				bool valid = _treeStore.GetIterFirst (out parent);
				bool found = false;
				while (valid && (string)_treeStore.GetValue (parent, 1) != folder)
					valid = _treeStore.IterNext (ref parent);
				if (!found)
				{
					parent = _treeStore.AppendValues(parent, _folderClosedIcon,folder,new FolderItem(curPath));
				}
				else
				{
					_treeStore.IterNext (ref parent);
				}
			}
			_root = _treeStore.AppendValues (parent, _contentIcon, item.Name, item);
		}

		public void RemoveTreeItem (ContentItem contentItem)
		{
			throw new NotImplementedException ();
		}

		public void UpdateTreeItem (IProjectItem item)
		{
			throw new NotImplementedException ();
		}

		public void EndTreeUpdate ()
		{
			if (_treeSort) {
				// Sort Tree
				_treeSort = false;
			}
			_treeUpdating = false;
		}

		public void UpdateProperties (IProjectItem item)
		{
			//throw new NotImplementedException ();
		}

		public void OutputAppend (string text)
		{
			if (string.IsNullOrWhiteSpace (text))
				return;

			var buffer = outputView.Buffer;
			var end = buffer.EndIter;
			buffer.Insert (ref end, text);
		}

		public void OutputClear ()
		{
			outputView.Buffer.Clear ();
		}

		public bool ChooseContentFile (string initialDirectory, out List<string> files)
		{
			var dlg = new OpenFileDialog() {
				Title = "Add Content Files",
				Multiselect = true,
				CurrentFolder = initialDirectory
			};
			dlg.Filters.Add(AllFilesFilter);

			bool result = dlg.Run();
			files = new List<string>(dlg.FileNames);
			return result;
		}

		#endregion

		#region Menu Signal Handlers

		protected void OnNewActionActivated (object sender, EventArgs e)
		{
			_controller.NewProject ();
		}

		protected void OnOpenActionActivated (object sender, EventArgs e)
		{
			_controller.OpenProject ();
		}

		protected void OnCloseActionActivated (object sender, EventArgs e)
		{
			_controller.CloseProject ();
		}

		protected void OnImportActionActivated (object sender, EventArgs e)
		{
			_controller.ImportProject ();
		}

		protected void OnSaveActionActivated (object sender, EventArgs e)
		{
			_controller.SaveProject (false);
		}

		protected void OnSaveAsActionActivated (object sender, EventArgs e)
		{
			_controller.SaveProject (true);
		}

		protected void OnQuitActionActivated (object sender, EventArgs e)
		{
			if (_controller.Exit ())
				Gtk.Application.Quit ();
		}

		protected void OnUndoActionActivated (object sender, EventArgs e)
		{
			_controller.Undo ();
		}

		protected void OnRedoActionActivated (object sender, EventArgs e)
		{
			_controller.Redo ();
		}

		void UpdateUndoRedo (bool canUndo, bool canRedo)
		{
			Gtk.Application.Invoke ((s, e) => {
				undoAction.Sensitive = _controller.CanUndo;
				redoAction.Sensitive = _controller.CanRedo;
			});
		}

		protected void OnAddActionActivated (object sender, EventArgs e)
		{
			throw new NotImplementedException ();
		}

		protected void OnAddItemActionActivated (object sender, EventArgs e)
		{
			throw new NotImplementedException ();
		}

		protected void OnRemoveActionActivated (object sender, EventArgs e)
		{
			throw new NotImplementedException ();
		}

		protected void OnExecuteActionActivated (object sender, EventArgs e)
		{
			_controller.LaunchDebugger = DebugModeAction.Active;
			_controller.Build (false);
		}

		protected void OnRefreshActionActivated (object sender, EventArgs e)
		{
			_controller.LaunchDebugger = DebugModeAction.Active;
			_controller.Build (true);
		}

		#endregion

	}
}

