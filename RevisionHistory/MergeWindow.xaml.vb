Partial Public Class MergeWindow

  Public Sub New()
    MyBase.New()

    Me.InitializeComponent()
  End Sub

  Private Sub MergeWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
    If GroupBox.SelectedIndex = -1 Then
      RevisionWindow.SelectedGroupName = Nothing
    Else
      RevisionWindow.SelectedGroupName = DirectCast(GroupBox.SelectedItem, ComboBoxItem).Content.ToString
    End If
  End Sub

  Private Sub MergeWindow_Initialized(sender As Object, e As System.EventArgs) Handles Me.Initialized
    Dim Groups = XDocument.Load("Log.xml").Element("Revisions").Elements("Revision").Where(Function(Current As XElement) Current.Attribute("xGroup") IsNot Nothing).Select(Function(Current As XElement) Current.Attribute("xGroup").Value).Distinct.ToList
    For Each GroupName In Groups
      Dim GroupItem As New ComboBoxItem()
      With GroupItem
        .Content = GroupName
      End With
      GroupBox.Items.Add(GroupItem)
      If RevisionWindow.SelectedGroupName IsNot Nothing AndAlso GroupName.Equals(RevisionWindow.SelectedGroupName) Then
        GroupBox.SelectedItem = GroupItem
      End If
    Next

    If GroupBox.SelectedIndex = -1 AndAlso GroupBox.Items.Count > 0 Then
      GroupBox.SelectedIndex = GroupBox.Items.Count - 1
    End If
  End Sub

  Private Sub MergeButton_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles MergeButton.Click
    Clipboard.SetText(RevisionList.Text)

    DialogResult = True
  End Sub

  Private Sub CancelButton_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles CancelButton.Click
    DialogResult = False
  End Sub

End Class
