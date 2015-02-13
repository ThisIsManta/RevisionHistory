Partial Public Class MainWindow

  Private Const SearchHint As String = "Search..."
  Private Const PreferencesFileName As String = "Preferences.xml"
  Private Const WebLocation As String = "https://thehub.thomsonreuters.com/docs/DOC-506989"
  Private WorkingFileLocation As String = "Log.xml"
  Private WorkingDocument As XDocument
  Private UnmergedDocument As New XDocument(New XElement("Revisions"))
  Private SelectedRevisions As New SortedList(Of Integer, RevisionItem)
  Private MaximumRevisionNumber As Integer = 1000
  Private Worker As System.Threading.Thread
  Private UnmergedWorker As System.Threading.Thread
  Private _IsReady As Boolean = False
  Private AliasMap As New Dictionary(Of String, Dictionary(Of String, String))
  Private HomeAuthorBoxCache As SortedSet(Of String) = Nothing
  Friend Shared PreferencesDocument As XDocument
  Friend Shared SummerCyan As New SolidColorBrush(Color.FromRgb(4, 174, 218))

  Public ReadOnly Property IsReady As Boolean
    Get
      Return _IsReady
    End Get
  End Property

  Private Sub MainWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
    'abort working thread
    If Worker IsNot Nothing AndAlso Worker.IsAlive Then
      Worker.Abort()
    End If
    If UnmergedWorker IsNot Nothing AndAlso UnmergedWorker.IsAlive Then
      UnmergedWorker.Abort()
    End If

    'Save last values from preferences
    Try
      Dim LastValues = PreferencesDocument.Element("Preferences").Element("LastValues")
      If LastValues IsNot Nothing Then
        'Save working file location
        Dim CurrentDirectory As String = (New System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly.Location)).Directory.FullName
        If Not String.IsNullOrEmpty(WorkingFileLocation) AndAlso WorkingFileLocation.StartsWith(CurrentDirectory) Then
          WorkingFileLocation = WorkingFileLocation.Substring(CurrentDirectory.Length + 1)
        End If
        If LastValues.Element("WorkingFileLocation") Is Nothing Then
          LastValues.Add(New XElement("WorkingFileLocation", WorkingFileLocation))
        Else
          LastValues.Element("WorkingFileLocation").Value = WorkingFileLocation
        End If

        'Save text box contents in every tab
        Dim Boxes As New List(Of Control)
        FindControlByName(RibbonContainer, Nothing, "Box", Boxes)
        If Boxes.Any Then
          For Each Box In Boxes
            If LastValues.Element(Box.Name) Is Nothing Then
              If TypeOf Box Is TextBox Then
                LastValues.Add(New XElement(Box.Name, DirectCast(Box, TextBox).Text))

              ElseIf TypeOf Box Is Primitives.ToggleButton Then
                LastValues.Add(New XElement(Box.Name, DirectCast(Box, Primitives.ToggleButton).IsChecked.ToString))
              End If

            Else
              If TypeOf Box Is TextBox Then
                LastValues.Element(Box.Name).Value = DirectCast(Box, TextBox).Text

              ElseIf TypeOf Box Is Primitives.ToggleButton Then
                LastValues.Element(Box.Name).Value = DirectCast(Box, Primitives.ToggleButton).IsChecked.ToString
              End If
            End If
          Next
        End If

        If String.IsNullOrEmpty(RevisionWindow.SelectedGroupName) Then
          If LastValues.Element("SelectedGroupBox") IsNot Nothing Then
            LastValues.Element("SelectedGroupBox").Remove()
          End If
        Else
          If LastValues.Element("SelectedGroupBox") Is Nothing Then
            LastValues.Add(New XElement("SelectedGroupBox", RevisionWindow.SelectedGroupName))
          Else
            LastValues.Element("SelectedGroupBox").Value = RevisionWindow.SelectedGroupName
          End If
        End If

        If LastValues.Element(InnerContentSpace.Name) IsNot Nothing Then
          LastValues.Element(InnerContentSpace.Name).Remove()
        End If
        LastValues.Add(New XElement(InnerContentSpace.Name, ContentSpace.ColumnDefinitions.First.Width.Value))
      End If

      'write to file
      PreferencesDocument.Save(PreferencesFileName, SaveOptions.None)

    Catch ex As Exception
      MessageBox.Show("Could not write " & PreferencesFileName & ".", Title, MessageBoxButton.OK, MessageBoxImage.Error)
    End Try

    ReleaseLockOnWorkingDocument()
  End Sub

  Private Sub MainWindow_Initialized(sender As Object, e As System.EventArgs) Handles Me.Initialized
    BindTabEvents()
    BindRibbonButtonEvents(RibbonContainer)

    'SearchBox_LostFocus(Nothing, Nothing)

    'Load last text box contents from preferences
    Try
      PreferencesDocument = XDocument.Load(PreferencesFileName)
      Dim LastValues = PreferencesDocument.Element("Preferences").Element("LastValues")
      If LastValues IsNot Nothing Then
        Dim Boxes As New List(Of Control)
        FindControlByName(RibbonContainer, Nothing, "Box", Boxes)
        If Boxes.Any Then
          For Each Box In Boxes
            If LastValues.Element(Box.Name) IsNot Nothing Then
              If TypeOf Box Is TextBox Then
                DirectCast(Box, TextBox).Text = LastValues.Element(Box.Name).Value

              ElseIf TypeOf Box Is Primitives.ToggleButton Then
                DirectCast(Box, Primitives.ToggleButton).IsChecked = Boolean.Parse(LastValues.Element(Box.Name).Value)
              End If
            End If
          Next
        End If

        If LastValues.Element("SelectedGroupBox") IsNot Nothing Then
          RevisionWindow.SelectedGroupName = LastValues.Element("SelectedGroupBox").Value
        End If

        For Each T In New FrameworkElement() {UnmergedToggleButton, MergedToggleButton}
          If LastValues.Element(T.Name) IsNot Nothing AndAlso Boolean.Parse(LastValues.Element(T.Name).Value) = False Then
            MergedToggleButton_MouseUp(T, Nothing)
          End If
        Next

        If LastValues.Element(InnerContentSpace.Name) IsNot Nothing Then
          ContentSpace.ColumnDefinitions.First.Width = New GridLength(Double.Parse(LastValues.Element(InnerContentSpace.Name).Value))
        End If
      End If

    Catch ex As Exception
      MessageBox.Show("Could not read " & PreferencesFileName & ".", Title, MessageBoxButton.OK, MessageBoxImage.Error)
    End Try

    If String.IsNullOrEmpty(ViewMaxRevisionBox.Text) Then
      ViewMaxRevisionBox.Text = MaximumRevisionNumber
    Else
      MaximumRevisionNumber = Integer.Parse(ViewMaxRevisionBox.Text)
    End If

    'store last working file location
    If PreferencesDocument IsNot Nothing Then
      WorkingFileLocation = PreferencesDocument.Element("Preferences").Element("LastValues").Element("WorkingFileLocation").Value
    End If

    'Dim Colorization = XDocument.Load("Preferences.xml").Element("Preferences").Element("Colorization").Elements("Color")
    'For Each C In Colorization
    '  If C.Attribute("name") IsNot Nothing AndAlso C.Attribute("rgb") IsNot Nothing Then
    '    Dim Name As String = C.Attribute("name").Value
    '    Dim RGB As String() = C.Attribute("rgb").Value.Split(",").Select(Function(Current As String) Current.Trim).ToArray
    '    If Not String.IsNullOrEmpty(Name) AndAlso RGB.Length = 3 AndAlso Not RevisionItem.ColorMasks.ContainsKey(Name.ToLower) Then
    '      RevisionItem.ColorMasks.Add(Name.ToLower, Color.FromArgb(85, Byte.Parse(RGB(0)), Byte.Parse(RGB(1)), Byte.Parse(RGB(2))))
    '    End If
    '  End If
    'Next

    Dim AliasContainer = PreferencesDocument.Element("Preferences").Element("Alias").Elements
    For Each A In AliasContainer
      Dim InnerAliasMap As Dictionary(Of String, String)
      If AliasMap.ContainsKey(A.Name.LocalName) Then
        InnerAliasMap = AliasMap(A.Name.LocalName)
      Else
        InnerAliasMap = New Dictionary(Of String, String)(A.Elements.Count)
        AliasMap.Add(A.Name.LocalName, InnerAliasMap)
      End If

      If A.Attribute("preferred") IsNot Nothing Then
        Dim PreferredValue As String = A.Attribute("preferred").Value
        Dim Matches = A.Elements("Match")
        For Each M In Matches
          Dim Key As String = M.Value.ToLower.Trim
          If Key.Length > 0 AndAlso Not InnerAliasMap.ContainsKey(Key) Then
            InnerAliasMap.Add(Key, PreferredValue)
          End If
        Next
      End If
    Next
  End Sub

  Private Sub MainWindow_Loaded(sender As Object, e As System.Windows.RoutedEventArgs) Handles Me.Loaded
    'load last working file (if any)
    Try
      WorkingDocument = XDocument.Load(WorkingFileLocation)

    Catch ex As Exception
      WorkingDocument = New XDocument(New XElement("Revisions"))
    End Try

    If WorkingDocument.Element("Revisions").Attribute("isLocked") IsNot Nothing AndAlso Boolean.Parse(WorkingDocument.Element("Revisions").Attribute("isLocked").Value) Then
      MessageBox.Show(String.Format("Could not open file {0} because it is locked by someone that may using it.", WorkingFileLocation), Title, MessageBoxButton.OK, MessageBoxImage.Exclamation)
      End

    Else
      If SettingsLockFileBox.IsChecked Then
        If WorkingDocument.Element("Revisions").Attribute("isLocked") Is Nothing Then
          WorkingDocument.Element("Revisions").Add(New XAttribute("isLocked", Boolean.TrueString))
        Else
          WorkingDocument.Element("Revisions").Attribute("isLocked").Value = Boolean.TrueString
        End If
        WorkingDocument.Save(WorkingFileLocation)
      End If

      AsyncGetSVNAndRenderContentSpace()
    End If

    HomeAuthorBoxCache = New SortedSet(Of String)(WorkingDocument.Element("Revisions").Elements("Revision").Select(Function(Current As XElement) Current.Attribute("pAuthor").Value).Distinct, New CaseInsensitiveStringComparer)
    HomeAuthorBox.ItemsSource = HomeAuthorBoxCache.AsEnumerable
  End Sub

  'Private Sub SearchBox_GotFocus(sender As Object, e As System.Windows.RoutedEventArgs) Handles SearchBox.GotFocus
  '  If SearchBox.Text.Equals(SearchHint) Then
  '    SearchBox.Clear()
  '    SearchBox.Foreground = Brushes.Black
  '    SearchBox.FontStyle = FontStyles.Normal
  '  End If
  'End Sub

  'Private Sub SearchBox_KeyUp(sender As Object, e As System.Windows.Input.KeyEventArgs) Handles SearchBox.KeyUp
  '  Select Case e.Key
  '    Case Key.Enter

  '      Exit Select

  '    Case Key.Escape
  '      SearchBox.Clear()
  '      Exit Select

  '  End Select
  'End Sub

  'Private Sub SearchBox_LostFocus(sender As Object, e As System.Windows.RoutedEventArgs) Handles SearchBox.LostFocus
  '  If SearchBox.Text.Length = 0 Then
  '    SearchBox.Text = SearchHint
  '    SearchBox.Foreground = Brushes.Gray
  '    SearchBox.FontStyle = FontStyles.Italic
  '  End If
  'End Sub

  Private Sub HomeAddButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles HomeAddButton.MouseUp
    Dim R As New RevisionWindow
    R.ShowDialog()
    If R.Saved Then
      Using Reader As New System.IO.StringReader(R.RevisionBox.Text)
        Try
          Dim Line As String = Nothing
          Do
            Dim RI As RevisionItem = AddOrReplaceRevision(Reader, Line, R.GroupBox.Text)
            If RI IsNot Nothing Then
              MergedRevisionSpace.ScrollToVerticalOffset(RI.TranslatePoint(New Point(0, 0), MergedRevisionContainer).Y)
            End If
          Loop Until Line Is Nothing

        Catch ex As Exception
          MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
      End Using
    End If
  End Sub

  Private Sub HomeDeleteButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles HomeDeleteButton.MouseUp
    Dim Revisions As IList(Of KeyValuePair(Of Integer, RevisionItem)) = SelectedRevisions.ToList
    For Each R In Revisions
      Try
        DeleteRevision(R.Key, R.Value.RevisionNumber)

      Catch ex As Exception
        MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error)
      End Try
    Next
  End Sub

  Private Sub HomeFilterButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles HomeFilterButton.MouseUp
    If Worker IsNot Nothing AndAlso Worker.IsAlive Then
      Worker.Abort()
    End If

    Try
      Worker = New System.Threading.Thread(AddressOf RenderContentSpace)
      Worker.Priority = System.Threading.ThreadPriority.AboveNormal
      Worker.Start({HomeRevisionBox.Text, HomeDateBox.Text, HomeAuthorBox.Text, HomeMessageBox.Text, HomeGroupBox.Text, HomeFileBox.Text})

    Catch ex As Exception
      MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Exclamation)
    End Try
  End Sub

  Private Sub HomeClearButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles HomeClearButton.MouseUp
    'Clear all filters
    Dim Boxes As New List(Of Control)
    FindControlByName(HomeFilterContainer, Nothing, "Box", Boxes)
    If Boxes IsNot Nothing AndAlso Boxes.Any Then
      For Each Box As UIElement In Boxes
        If TypeOf Box Is TextBox Then
          DirectCast(Box, TextBox).Clear()
        ElseIf TypeOf Box Is ComboBox Then
          DirectCast(Box, ComboBox).SelectedIndex = -1
          DirectCast(Box, ComboBox).Text = String.Empty
        End If
      Next
    End If

    AsyncRenderContentSpace()
  End Sub

  Private Sub HomeSelectButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles HomeSelectButton.MouseUp
    If SelectedRevisions.Count = 0 Then
      SelectAllRevision()

    Else
      For Each R In SelectedRevisions
        R.Value.IsSelected = False
      Next
      SelectedRevisions.Clear()
    End If

    RenderContentText(Nothing, Nothing)
  End Sub

  Private Sub HomeFilterBox_KeyUp(sender As Object, e As System.Windows.Input.KeyEventArgs) Handles HomeRevisionBox.KeyUp, HomeDateBox.KeyUp, HomeAuthorBox.KeyUp, HomeMessageBox.KeyUp, HomeGroupBox.KeyUp, HomeFileBox.KeyUp
    If e.Key = Key.Enter Then
      HomeFilterButton_MouseUp(Nothing, Nothing)

    ElseIf e.Key = Key.Escape Then
      DirectCast(sender, TextBox).Clear()
    End If
  End Sub

  Private Sub HomeOpenButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles HomeOpenButton.MouseUp
    ReleaseLockOnWorkingDocument()

    Dim FileDialog As New Microsoft.Win32.OpenFileDialog
    With FileDialog
      .Title = "Choose a log document"
      .Filter = "XML Documents (*.xml)|*.xml"
      .CheckFileExists = True
    End With

    If FileDialog.ShowDialog Then
      WorkingFileLocation = FileDialog.FileName
      AsyncRenderContentSpace()
    End If
  End Sub

  Private Sub HomeCopyButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles HomeCopyButton.MouseUp
    Dim PreferSetting As Boolean = ViewSimplePathBox.IsChecked
    If PreferSetting Then
      ViewSimplePathBox.IsChecked = False
    End If

    Dim Range As New TextRange(RevisionBox.Document.ContentStart, RevisionBox.Document.ContentEnd)
    If Range.Text.Length > 0 Then
      Clipboard.SetText(Range.Text)
      StatusText.Text = "COPIED"
    End If

    If PreferSetting Then
      ViewSimplePathBox.IsChecked = True
    End If
  End Sub

  Private Sub ViewSimplePathBox_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles ViewSimplePathBox.Checked, ViewSimplePathBox.Unchecked, ViewExtendedLogBox.Checked, ViewExtendedLogBox.Unchecked
    RenderContentText(Nothing, Nothing)
  End Sub

  Private Sub ViewMaxRevisionBox_TextChanged(sender As Object, e As System.Windows.Controls.TextChangedEventArgs) Handles ViewMaxRevisionBox.TextChanged
    Dim Temp As Integer
    If ViewMaxRevisionBox.Text.Trim.Length = 0 OrElse Not Integer.TryParse(ViewMaxRevisionBox.Text, Temp) OrElse Temp <= 0 Then
      MessageBox.Show("Invalid maximum number of revision.", Title, MessageBoxButton.OK, MessageBoxImage.Exclamation)
      Temp = 1000
    End If
    MaximumRevisionNumber = Temp
    ViewMaxRevisionBox.Text = Temp
  End Sub

  Private Sub ViewAuthorColorBox_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles ViewAuthorColorBox.Checked, ViewAuthorColorBox.Unchecked
    RevisionItem.Colorize = ViewAuthorColorBox.IsChecked
    If IsLoaded Then
      AsyncRenderContentSpace()
    End If
  End Sub

  Private Sub SettingsEditXmlButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles SettingsEditXmlButton.MouseUp
    Dim PreferencesFileLocation As String = System.IO.Path.Combine(New System.IO.FileInfo(Reflection.Assembly.GetExecutingAssembly.Location).Directory.FullName, PreferencesFileName)
    Try
      Process.Start("notepad++", PreferencesFileLocation)
    Catch ex As Exception
      Process.Start("notepad", PreferencesFileLocation)
    End Try
  End Sub

  Private Sub MoreInfoLink_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles MoreInfoLink.MouseUp
    Try
      Process.Start("chrome", WebLocation)
    Catch ex As Exception
      Process.Start("iexplore", WebLocation)
    End Try
  End Sub

  Private Sub ViewSortByAddedBox_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles ViewSortByAddedBox.Checked, ViewSortByNumberBox.Checked
    If IsLoaded Then
      AsyncRenderContentSpace()
    End If
  End Sub

  Private Sub MergedToggleButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles MergedToggleButton.MouseUp, UnmergedToggleButton.MouseUp
    Dim Button As Image = sender
    Dim Content As UIElement = FindName(Button.Name.Remove(Button.Name.IndexOf("Toggle")) & "RevisionSpace")
    Dim Lock As Image = FindName(Button.Name.Remove(Button.Name.IndexOf("Toggle")) & "LockIcon")
    Dim Space As Grid = Button.Parent
    If Space.Children.Contains(Content) Then
      Space.Children.Remove(Content)
      Space.RowDefinitions(Grid.GetRow(Content)).Height = New GridLength(1, GridUnitType.Auto)
      Button.Source = New BitmapImage(New Uri("Images/eye_close.png", UriKind.Relative))
      If Lock IsNot Nothing Then
        Space.Children.Remove(Lock)
      End If

    Else
      Space.Children.Add(Content)
      Space.RowDefinitions(Grid.GetRow(Content)).Height = New GridLength(1, GridUnitType.Star)
      Button.Source = New BitmapImage(New Uri("Images/eye.png", UriKind.Relative))
      If Lock IsNot Nothing Then
        Space.Children.Add(Lock)
      End If
    End If

    'save preferences
    If PreferencesDocument.Element("Preferences").Element("LastValues").Element(Button.Name) IsNot Nothing Then
      PreferencesDocument.Element("Preferences").Element("LastValues").Element(Button.Name).Remove()
    End If
    PreferencesDocument.Element("Preferences").Element("LastValues").Add(New XElement(Button.Name, Space.Children.Contains(Content).ToString))
  End Sub

  Private Sub HomeGetSVNButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles HomeGetSVNButton.MouseUp
    Dim Settings As New LogWindow
    Settings.ShowDialog()
    If Settings.DialogResult.HasValue AndAlso Settings.DialogResult.Value = True Then
      AsyncGetSVNAndRenderContentSpace()
    End If
  End Sub

  Private Sub HomeMergeButton_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles HomeMergeButton.MouseUp
    Dim ToBeMerged As IEnumerable(Of Integer) = SelectedRevisions.Values.Where(Function(Current As RevisionItem) Object.ReferenceEquals(Current.Parent, UnmergedRevisionContainer)).Select(Function(Current As RevisionItem) Current.RevisionNumber)

    Dim Merge As New MergeWindow
    Merge.RevisionList.Text = String.Join(",", ToBeMerged)
    Merge.ShowDialog()

    If Merge.DialogResult.HasValue AndAlso Merge.DialogResult.Value = True Then
      For Each RN In ToBeMerged
        Dim RevisionNumber As String = RN.ToString
        Dim FocusedRevision = UnmergedDocument.Element("Revisions").Elements().First(Function(Current As XElement) Current.Attribute("pRevision").Value.Equals(RevisionNumber))
        FocusedRevision.Remove()
        If Not String.IsNullOrWhiteSpace(Merge.GroupBox.Text) Then
          FocusedRevision.Add(New XAttribute("xGroup", Merge.GroupBox.Text))
        End If
        WorkingDocument.Element("Revisions").Add(FocusedRevision)
      Next

      Dim MaxRevisionNumber As Integer = ToBeMerged.Max

      'Save last revision number
      Dim LastRevision As Integer
      If WorkingDocument.Element("Revisions").Attribute("lastRevision") Is Nothing Then
        WorkingDocument.Element("Revisions").Add(New XAttribute("lastRevision", MaxRevisionNumber))
      ElseIf Integer.TryParse(WorkingDocument.Element("Revisions").Attribute("lastRevision").Value, LastRevision) AndAlso MaxRevisionNumber > LastRevision Then
        WorkingDocument.Element("Revisions").Attribute("lastRevision").Value = MaxRevisionNumber
      End If

      'Save change
      WriteWorkingDocumentToFile()

      AsyncRenderContentSpace()
    End If
  End Sub
End Class
