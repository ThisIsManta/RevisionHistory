Imports System
Imports System.IO
Imports System.Net
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Navigation

Partial Public Class RevisionWindow

  Private _Saved As Boolean = False

  Public Shared SelectedGroupName As String = Nothing

  Public ReadOnly Property Saved As Boolean
    Get
      Return _Saved
    End Get
  End Property

  Public Sub New()
    MyBase.New()

    Me.InitializeComponent()
  End Sub

  Private Sub RevisionWindow_Activated(sender As Object, e As System.EventArgs) Handles Me.Activated
    If Clipboard.GetText.StartsWith("Revision") Then
      RevisionBox.Text = Clipboard.GetText
      RevisionBox.SelectionStart = RevisionBox.Text.Length
    End If
  End Sub

  Private Sub RevisionWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
    If GroupBox.SelectedIndex = -1 Then
      SelectedGroupName = Nothing
    Else
      SelectedGroupName = DirectCast(GroupBox.SelectedItem, ComboBoxItem).Content.ToString
    End If
  End Sub

  Private Sub RevisionWindow_Initialized(sender As Object, e As System.EventArgs) Handles Me.Initialized
    Dim Groups = XDocument.Load("Log.xml").Element("Revisions").Elements("Revision").Where(Function(Current As XElement) Current.Attribute("xGroup") IsNot Nothing).Select(Function(Current As XElement) Current.Attribute("xGroup").Value).Distinct.ToList
    For Each GroupName In Groups
      Dim GroupItem As New ComboBoxItem()
      With GroupItem
        .Content = GroupName
      End With
      GroupBox.Items.Add(GroupItem)
      If SelectedGroupName IsNot Nothing AndAlso GroupName.Equals(SelectedGroupName) Then
        GroupBox.SelectedItem = GroupItem
      End If
    Next

    If GroupBox.SelectedIndex = -1 AndAlso GroupBox.Items.Count > 0 Then
      GroupBox.SelectedIndex = GroupBox.Items.Count - 1
    End If
  End Sub

  Private Sub RevisionWindow_Loaded(sender As Object, e As System.Windows.RoutedEventArgs) Handles Me.Loaded
    RevisionBox.Focus()
  End Sub

  Private Sub AddButton_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles AddButton.Click
    If RevisionBox.Text.Trim.Length = 0 Then
      MessageBox.Show("You must specified one or more revisions.", Title, MessageBoxButton.OK, MessageBoxImage.Exclamation)

    ElseIf GroupBox.Text.Trim.Length = 0 Then
      MessageBox.Show("You must specified the group of revision(s).", Title, MessageBoxButton.OK, MessageBoxImage.Exclamation)
      GroupBox.IsDropDownOpen = True

    Else
      _Saved = True
      Close()
    End If
  End Sub
End Class
