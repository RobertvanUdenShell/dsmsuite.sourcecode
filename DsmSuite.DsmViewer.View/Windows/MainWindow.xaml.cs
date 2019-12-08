﻿using System.Reflection;
using System.Windows;
using DsmSuite.DsmViewer.ViewModel.Main;
using DsmSuite.DsmViewer.Application.Core;
using DsmSuite.DsmViewer.Model.Core;

namespace DsmSuite.DsmViewer.View.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private MainViewModel _mainViewModel;
        private ProgressWindow _progressWindow;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void OpenModel(string filename)
        {
            _mainViewModel.OpenFileCommand.Execute(filename);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            DsmModel model = new DsmModel("Viewer", Assembly.GetExecutingAssembly());
            DsmApplication application = new DsmApplication(model);
            _mainViewModel = new MainViewModel(application);
            _mainViewModel.ReportCreated += OnReportCreated;
            _mainViewModel.ElementsReportReady += OnElementsReportReady;
            _mainViewModel.RelationsReportReady += OnRelationsReportReady;
            _mainViewModel.ProgressViewModel.BusyChanged += OnProgressViewModelBusyChanged;

            _mainViewModel.ElementCreateStarted += OnElementElementCreateStarted;
            _mainViewModel.ElementEditStarted += OnElementElementEditStarted;
            DataContext = _mainViewModel;

            OpenModelFile();
        }

        private void OnElementElementCreateStarted(object sender, ViewModel.Editing.ElementCreateViewModel viewModel)
        {
            ElementView elementCreateView = new ElementView();
            elementCreateView.DataContext = viewModel;
            elementCreateView.ShowDialog();
        }

        private void OnElementElementEditStarted(object sender, ViewModel.Editing.ElementEditViewModel viewModel)
        {
            ElementView elementEditView = new ElementView();
            elementEditView.DataContext = viewModel;
            elementEditView.ShowDialog();
        }

        private void OpenModelFile()
        {
            App app = System.Windows.Application.Current as App;
            if ((app != null) && (app.CommandLineArguments.Length == 1))
            {
                string filename = app.CommandLineArguments[0];
                if (filename.EndsWith(".dsm"))
                {
                    _mainViewModel.OpenFileCommand.Execute(filename);
                }
            }
        }

        private void OnElementsReportReady(object sender, ViewModel.Lists.ElementListViewModel e)
        {
            ElementListView view = new ElementListView
            {
                DataContext = e,
                Owner = this
            };
            view.Show();
        }

        private void OnRelationsReportReady(object sender, ViewModel.Lists.RelationListViewModel e)
        {
            RelationListView view = new RelationListView
            {
                DataContext = e,
                Owner = this
            };
            view.Show();
        }

        private void OnProgressViewModelBusyChanged(object sender, bool visible)
        {
            if (visible)
            {
                if (_progressWindow == null)
                {
                    _progressWindow = new ProgressWindow
                    {
                        DataContext = _mainViewModel.ProgressViewModel,
                        Owner = this
                    };
                    _progressWindow.ShowDialog();
                }
            }
            else
            {
                _progressWindow.Close();
                _progressWindow = null;
            }
        }

        private void OnReportCreated(object sender, ReportViewModel e)
        {
            ReportView reportView = new ReportView { DataContext = e };
            reportView.Show();
        }
    }
}
