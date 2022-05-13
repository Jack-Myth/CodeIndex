using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace CodeIndex.VisualStudioExtension
{
    /// <summary>
    /// Interaction logic for CodeIndexSearchControl.xaml.
    /// </summary>
    [ProvideToolboxControl("CodeIndex.VisualStudioExtension.CodeIndexSearchControl", true)]
    public partial class CodeIndexSearchControl : UserControl
    {

        public CodeIndexSearchControl()
        {
            InitializeComponent();
        }

        void ContentTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox_KeyDown(sender, e);
            }
            else
            {
                SearchViewModel?.GetHintWordsAsync();
            }
        }

        void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                {
                    SearchButton.Command?.Execute(null);
                }
            }
            else if (this.ContentComboBox.Items.Count > 0)
            {
                if (e.Key == Key.Down)
                {
                    if (this.ContentComboBox.SelectedIndex < 0)
                    {
                        this.ContentComboBox.SelectedIndex = 0;
                    }
                    this.ContentComboBox.SelectedIndex = (this.ContentComboBox.SelectedIndex + 1) % this.ContentComboBox.Items.Count;
                }
                else if (e.Key == Key.Up)
                {
                    if (this.ContentComboBox.SelectedIndex < 0)
                    {
                        this.ContentComboBox.SelectedIndex = 0;
                    }
                    this.ContentComboBox.SelectedIndex = (this.ContentComboBox.SelectedIndex + this.ContentComboBox.Items.Count - 1) % this.ContentComboBox.Items.Count;
                }
            }
            if (this.ContentComboBox.Items.Count > 0)
            {
                this.ContentComboBox.IsDropDownOpen = true;
            }
        }

        CodeIndexSearchViewModel SearchViewModel => DataContext as CodeIndexSearchViewModel;

        private bool ShouldOpenHintList
        {
            get => ContentComboBox.IsFocused && !ContentComboBox.Items.IsEmpty;
        }

        void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Some operations with this row
            if (sender is DataGridRow row && row.Item is CodeSourceWithMatchedLine codeSourceWithMatchedLine)
            {
                if (File.Exists(codeSourceWithMatchedLine.CodeSource.FilePath))
                {
                    var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                    var window = dte.ItemOperations.OpenFile(codeSourceWithMatchedLine.CodeSource.FilePath);
                    (window.Document.Selection as TextSelection)?.GotoLine(codeSourceWithMatchedLine.MatchedLine, true);
                }
                else
                {
                    // TODO: Download to local to open
                    if (System.Windows.MessageBox.Show("This file is not on your local, do you want to open it in the web portal?", "Info", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start($"{SearchViewModel.ServiceUrl}/Details/{codeSourceWithMatchedLine.CodeSource.CodePK}/{SearchViewModel.IndexPk}/{System.Web.HttpUtility.UrlEncode(SearchViewModel.Content)}/{SearchViewModel.CaseSensitive}/{SearchViewModel.PhaseQuery}");
                    }
                }
            }
        }

        private void ContentTextBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var NewSelectItem = (sender as ComboBox).SelectedItem;
            if (NewSelectItem != null)
            {
                SearchViewModel.Content = (NewSelectItem as Models.HintWord).Word;
            }
            e.Handled = true;
        }
    }
}
