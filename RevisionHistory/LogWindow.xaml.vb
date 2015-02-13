Partial Public Class LogWindow

  Public Sub New()
    MyBase.New()

    Me.InitializeComponent()
  End Sub

  Private Sub LogWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
    Dim LogPreference As XElement = MainWindow.PreferencesDocument.Element("Preferences").Element("LastValues").Element("LogWindow")
    If LogPreference IsNot Nothing Then
      LogPreference.RemoveAll()

      For Each R In PathBox.ItemsSource.Cast(Of ComboBoxItem).Select(Function(Current As ComboBoxItem) New XElement("Path", Current.Content.ToString))
        LogPreference.Add(R)
      Next
      LogPreference.Add(New XAttribute("path", PathBox.Text))
      LogPreference.Add(New XAttribute("limit", LimitBox.Text))
    End If
  End Sub

  Private Sub LogWindow_Initialized(sender As Object, e As System.EventArgs) Handles Me.Initialized
    Dim LogPreference As XElement = MainWindow.PreferencesDocument.Element("Preferences").Element("LastValues").Element("LogWindow")
    If LogPreference Is Nothing Then
      LogPreference = New XElement("LogWindow")
      MainWindow.PreferencesDocument.Element("Preferences").Element("LastValues").Add(LogPreference)
    Else
      PathBox.ItemsSource = LogPreference.Elements("Path").Select(Function(Current As XElement) New ComboBoxItem With {.Content = Current.Value})
      If LogPreference.Attribute("path") IsNot Nothing Then
        PathBox.Text = LogPreference.Attribute("path").Value
      End If

      If LogPreference.Attribute("limit") IsNot Nothing Then
        LimitBox.Text = LogPreference.Attribute("limit").Value
      End If
    End If
  End Sub

  Private Sub BeginButton_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles BeginButton.Click
    PathBox.Text = PathBox.Text.Trim.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
    If PathBox.Text.Length = 0 Then
      MessageBox.Show("Invalid local path", Me.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation)
      Exit Sub
    End If

    If Not PathBox.ItemsSource.Cast(Of ComboBoxItem).Select(Function(Current As ComboBoxItem) Current.Content.ToString).Contains(PathBox.Text) Then
      Dim Holder As New List(Of ComboBoxItem)(PathBox.ItemsSource.Cast(Of ComboBoxItem))
      Holder.Add(New ComboBoxItem() With {.Content = PathBox.Text})
      PathBox.ItemsSource = Nothing
      PathBox.ItemsSource = Holder.AsEnumerable
    End If

    DialogResult = True
  End Sub

  Private Sub CancelButton_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles CancelButton.Click
    DialogResult = False
  End Sub

  Private Sub LimitBox_LostFocus(sender As Object, e As System.Windows.RoutedEventArgs) Handles LimitBox.LostFocus
    Dim Temp As Integer
    If Not (Integer.TryParse(LimitBox.Text.Trim, Temp) AndAlso Temp > 0 AndAlso Temp <= 1000) Then
      Temp = 200
    End If
    LimitBox.Text = Temp
  End Sub
End Class
