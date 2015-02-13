Imports System.IO

Partial Public Class MainWindow

  Private Sub BindTabEvents()
    For Each o As Panel In TabContainer.Children
      AddHandler o.MouseEnter, AddressOf Tab_MouseEnter
      AddHandler o.MouseLeave, AddressOf Tab_MouseLeave
      AddHandler o.MouseUp, AddressOf Tab_MouseUp
    Next
  End Sub

  Private Sub Tab_MouseEnter(sender As Object, e As System.Windows.Input.MouseEventArgs)
    Dim CurrentMenu As Panel = sender
    If CurrentMenu.IsEnabled Then
      DirectCast(CurrentMenu.Children(0), Rectangle).Visibility = Windows.Visibility.Visible
    End If
  End Sub

  Private Sub Tab_MouseLeave(sender As Object, e As System.Windows.Input.MouseEventArgs)
    Dim CurrentMenu As Panel = sender
    If DirectCast(CurrentMenu.Children(1), TextBlock).Background Is Nothing Then
      DirectCast(CurrentMenu.Children(0), Rectangle).Visibility = Windows.Visibility.Hidden
    End If
  End Sub

  Private Sub Tab_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
    Dim CurrentTab As Panel = sender
    Dim CurrentIndex As Integer = TabContainer.Children.IndexOf(CurrentTab)
    Dim LastIndex As Integer = Math.Min(TabContainer.Children.Count, RibbonContainer.Children.Count) - 1
    For i As Integer = 0 To LastIndex
      Dim RelevantTab As Panel = TabContainer.Children(i)
      Dim RelevantRibbon As Panel = RibbonContainer.Children(i)
      If i = CurrentIndex Then
        DirectCast(RelevantTab.Children(0), Rectangle).Visibility = Windows.Visibility.Visible
        With DirectCast(RelevantTab.Children(1), TextBlock)
          .Background = Brushes.White
          .Foreground = Brushes.RoyalBlue
        End With
        RelevantRibbon.Visibility = Windows.Visibility.Visible

      Else
        DirectCast(RelevantTab.Children(0), Rectangle).Visibility = Windows.Visibility.Hidden
        With DirectCast(RelevantTab.Children(1), TextBlock)
          .Background = Nothing
          .Foreground = Brushes.DimGray
        End With
        RelevantRibbon.Visibility = Windows.Visibility.Hidden
      End If
    Next
  End Sub

  Private Sub BindRibbonButtonEvents(ButtonContainer As Panel)
    For Each Child In ButtonContainer.Children
      If TypeOf Child Is Panel Then
        Dim Item As Panel = Child
        If Item.Name.EndsWith("Button") Then
          AddHandler Item.MouseEnter, AddressOf RibbonButton_MouseEnter
          AddHandler Item.MouseLeave, AddressOf RibbonButton_MouseLeave

        ElseIf String.IsNullOrEmpty(Item.Name) OrElse Item.Name.EndsWith("Container") Then
          BindRibbonButtonEvents(Item)
        End If
      End If
    Next
  End Sub

  Private Sub RibbonButton_MouseEnter(sender As Object, e As System.Windows.Input.MouseEventArgs)
    Dim CurrentButton As Panel = sender
    If CurrentButton.IsEnabled Then
      CurrentButton.Background = Brushes.Gainsboro
    End If
  End Sub

  Private Sub RibbonButton_MouseLeave(sender As Object, e As System.Windows.Input.MouseEventArgs)
    Dim CurrentButton As Panel = sender
    CurrentButton.Background = Brushes.Transparent
  End Sub

  Private Sub AsyncRenderContentSpace()
    StatusText.Text = "LOADING..."

    If Worker IsNot Nothing AndAlso Worker.IsAlive Then
      Worker.Abort()
    End If
    Worker = New System.Threading.Thread(AddressOf RenderContentSpace)
    Worker.Priority = System.Threading.ThreadPriority.AboveNormal
    Worker.Start({HomeRevisionBox.Text, HomeDateBox.Text, HomeAuthorBox.Text, HomeMessageBox.Text, HomeGroupBox.Text, HomeFileBox.Text})
  End Sub

  Private Sub RenderContentText(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
    If sender IsNot Nothing AndAlso TypeOf sender Is RevisionItem Then
      Dim RI As RevisionItem = sender
      RI.IsSelected = Not RI.IsSelected
      If RI.IsSelected Then
        SelectedRevisions.Add(RI.Order, RI)
      Else
        SelectedRevisions.Remove(RI.Order)
      End If
    End If

    'enable/disable merge button
    HomeMergeButton.IsEnabled = SelectedRevisions.Values.Any(Function(Current As RevisionItem) Object.ReferenceEquals(Current.Parent, UnmergedRevisionContainer))
    If HomeMergeButton.IsEnabled Then
      DirectCast(HomeMergeButton.Children(0), Image).Source = New BitmapImage(New Uri("Images/tick.png", UriKind.Relative))
    Else
      DirectCast(HomeMergeButton.Children(0), Image).Source = New BitmapImage(New Uri("Images/tick_dis.png", UriKind.Relative))
    End If

    RevisionBox.Document = New FlowDocument
    For Each R In SelectedRevisions
      DirectCast(RevisionBox.Document, FlowDocument).Blocks.Add(GetRevisionContent(R.Value.RevisionNumber))
      Dim Breaker As New Paragraph
      Breaker.Background = Brushes.LightGray
      DirectCast(RevisionBox.Document, FlowDocument).Blocks.Add(Breaker)
    Next
    If SelectedRevisions.Any Then
      DirectCast(RevisionBox.Document, FlowDocument).Blocks.Remove(DirectCast(RevisionBox.Document, FlowDocument).Blocks.Last)
    End If

    If SelectedRevisions.Count <= 1 Then
      StatusText.Text = String.Format("{0} REVISION IS SELECTED", SelectedRevisions.Count)
    Else
      StatusText.Text = String.Format("{0} REVISIONS ARE SELECTED", SelectedRevisions.Count)
    End If
  End Sub

  Private Sub SelectAllRevision()
    SelectedRevisions.Clear()
    For Each RI As RevisionItem In MergedRevisionContainer.Children
      If RI.IsEnabled Then
        SelectedRevisions.Add(RI.RevisionNumber, RI)
        RI.IsSelected = True
      End If
    Next
  End Sub

  Private Function AddOrReplaceRevision(Reader As StringReader, ByRef Line As String, GroupName As String) As RevisionItem
    Dim Map As New Dictionary(Of String, String)
    Dim Message As String = String.Empty
    Dim FileFlag As Boolean = False
    Dim Files As New List(Of String)

    Do
      Line = Reader.ReadLine
      If Line Is Nothing Then
        Exit Do

      ElseIf FileFlag Then
        If Line.Trim.Length = 0 Then
          Exit Do
        Else
          Files.Add(Line)
        End If

      ElseIf Line.Contains(": ") Then
        Dim Header As String = Line.Remove(Line.IndexOf(":"))
        Dim Body As String = Line.Substring(Line.IndexOf(":") + 2)
        Map.Add(Header, Body)

      ElseIf Line.Equals("Message:") Then
        Line = Reader.ReadLine
        Dim Buffer As New System.Text.StringBuilder(Line)
        Do
          Line = Reader.ReadLine
          If Line Is Nothing OrElse Line.Equals("----") Then
            FileFlag = True
            Exit Do
          Else
            Buffer.AppendLine.Append(Line)
          End If
        Loop
        Message = Buffer.ToString
      End If
    Loop

    If Map.Count = 0 Then
      Return Nothing

    ElseIf Map.ContainsKey("Revision") Then
      Dim Number As String = Map.Item("Revision")
      WorkingDocument = XDocument.Load(WorkingFileLocation)
      Dim Revision = WorkingDocument.Element("Revisions").Elements("Revision").FirstOrDefault(Function(Current As XElement) As Boolean
                                                                                                Return Number.Equals(Current.Attribute("pRevision").Value)
                                                                                              End Function)
      Dim AddNewRevisionItem As Boolean = Revision Is Nothing
      If Revision IsNot Nothing Then 'Remove an existing revision log
        If MessageBox.Show(String.Format("Revision: {0} is already exists. Would you like to update it?", Number), Title, MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.No Then
          Return Nothing
        End If
        Revision.Remove()
      End If

      'Add new revision log
      Revision = New XElement("Revision")
      For Each A In Map
        Revision.Add(New XAttribute("p" & A.Key, A.Value))
      Next
      'Revision.Add(New XAttribute("xDate", Date.Parse(Map.Item("Date"))))
      Revision.Add(New XElement("Message", New XCData(Message)))
      If Not String.IsNullOrEmpty(GroupName) Then
        Revision.Add(New XAttribute("xGroup", GroupName))
      End If
      For Each F In Files
        Revision.Add(New XElement(F.Remove(F.IndexOf(" : ")), F.Substring(F.IndexOf(" : ") + 3)))
      Next
      WorkingDocument.Element("Revisions").Add(Revision)

      'Save last revision number
      Dim LastRevision As Integer
      If WorkingDocument.Element("Revisions").Attribute("lastRevision") Is Nothing Then
        WorkingDocument.Element("Revisions").Add(New XAttribute("lastRevision", Number))
      ElseIf Integer.TryParse(WorkingDocument.Element("Revisions").Attribute("lastRevision").Value, LastRevision) AndAlso Integer.Parse(Number) > LastRevision Then
        WorkingDocument.Element("Revisions").Attribute("lastRevision").Value = Number
      End If

      'Save change
      WriteWorkingDocumentToFile()

      'Add revision item into the container
      If AddNewRevisionItem Then
        Dim AuthorCode As Integer? = Nothing
        Dim RI As New RevisionItem(Integer.Parse(Number), MergedRevisionContainer.Children.Count, Revision.Attribute("pAuthor"))
        AddHandler RI.MouseUp, AddressOf RenderContentText
        MergedRevisionContainer.Children.Add(RI)
        'RI.RenderColor()
        Return RI

      Else
        Return Nothing
      End If

    Else
      Throw New Exception("Could not find revision number.")
    End If
  End Function

  Private Function GetRevisionContent(RevisionNumber As String) As Block
    Dim Revision As XElement
    Revision = WorkingDocument.Element("Revisions").Elements("Revision").FirstOrDefault(Function(Current As XElement) As Boolean
                                                                                          Return RevisionNumber.Equals(Current.Attribute("pRevision").Value)
                                                                                        End Function)
    If Revision Is Nothing Then
      Revision = UnmergedDocument.Element("Revisions").Elements("Revision").FirstOrDefault(Function(Current As XElement) As Boolean
                                                                                             Return RevisionNumber.Equals(Current.Attribute("pRevision").Value)
                                                                                           End Function)
    End If

    If Revision Is Nothing Then
      Return New Paragraph

    Else 'Plain original text view
      Dim Content As New Section
      Dim HeaderLines As New Paragraph
      HeaderLines.Margin = New Thickness(0)
      For Each A In Revision.Attributes
        If A.Name.LocalName.Chars(0) = "p" OrElse (ViewExtendedLogBox.IsChecked AndAlso A.Name.LocalName.Chars(0) = "x") Then
          Dim Header As New Run(A.Name.LocalName.Substring(1) & ": ")
          Header.Foreground = Brushes.SlateGray
          Dim Body As Run
          Dim InnerAliasMap As Dictionary(Of String, String) = Nothing
          If AliasMap.TryGetValue(A.Name.LocalName, InnerAliasMap) AndAlso InnerAliasMap.ContainsKey(A.Value.ToLower) Then
            Body = New Run(InnerAliasMap(A.Value.ToLower))
          Else
            Body = New Run(A.Value)
          End If
          Body.Foreground = Brushes.MediumBlue
          HeaderLines.Inlines.AddRange({Header, Body, New LineBreak})
        End If
      Next

      Dim MessageHeader As New Run("Message:")
      MessageHeader.Foreground = Brushes.SlateGray
      HeaderLines.Inlines.Add(MessageHeader)
      Dim MessageLines As New Paragraph
      MessageLines.Margin = New Thickness(30, 0, 0, 0)
      If Revision.Element("Message") IsNot Nothing Then
        Dim Body As New Run(Revision.Element("Message").Value)
        Body.Foreground = Brushes.MediumBlue
        MessageLines.Inlines.Add(Body)
      End If

      Dim Breaker As New Paragraph
      Breaker.Margin = New Thickness(0)
      Dim HorizontalLine As New Run("----")
      HorizontalLine.Foreground = Brushes.SlateGray
      Breaker.Inlines.Add(HorizontalLine)

      Content.Blocks.AddRange({HeaderLines, MessageLines, Breaker})

      Dim Files = Revision.Elements.ToList
      If Files.Any AndAlso Files.First.Name.LocalName.Equals("Message") Then
        Files.RemoveAt(0)
      End If
      If ViewSimplePathBox.IsChecked Then
        Dim FileLine As New Paragraph
        FileLine.Margin = New Thickness(0)
        AddHandler FileLine.MouseRightButtonDown, AddressOf ToggleFilePath
        Dim SymbolFont As New FontFamily("Segoe UI Symbol")
        For Each F In Files
          Dim Header As New Run()
          Header.FontFamily = SymbolFont
          Dim FileDirectory As New Run()
          FileDirectory.ToolTip = F.Value.Remove(F.Value.LastIndexOf("/") + 1)
          Dim FileName As New Run(F.Value.Substring(F.Value.LastIndexOf("/") + 1))
          FileName.FontWeight = FontWeights.Bold
          Dim ObjectIndicator As Char = "" 'Folder
          If FileName.Text.Contains(".") Then 'File
            ObjectIndicator = ""
          End If
          Select Case F.Name.LocalName
            Case "Added"
              Header.Text = "" & ObjectIndicator & ": "
              Header.Foreground = Brushes.ForestGreen
              Exit Select

            Case "Modified"
              Header.Text = "" & ObjectIndicator & ": "
              Header.Foreground = Brushes.DarkOrchid
              Exit Select

            Case "Deleted"
              Header.Text = "" & ObjectIndicator & ": "
              Header.Foreground = Brushes.OrangeRed
              Exit Select

          End Select
          FileDirectory.Foreground = Header.Foreground
          FileName.Foreground = Header.Foreground
          FileLine.Inlines.AddRange({Header, FileDirectory, FileName, New LineBreak})
        Next
        If Files.Any Then
          FileLine.Inlines.Remove(FileLine.Inlines.Last)
        End If
        Content.Blocks.Add(FileLine)

      Else
        Dim FileLines As New Paragraph
        FileLines.Margin = New Thickness(0)
        For Each F In Files
          Dim FileName As New Run(String.Format("{0} : {1}", F.Name.LocalName, F.Value))
          Select Case F.Name.LocalName
            Case "Modified"
              FileName.Foreground = Brushes.DarkOrchid
              Exit Select

            Case "Added"
              FileName.Foreground = Brushes.Green
              Exit Select

            Case "Deleted"
              FileName.Foreground = Brushes.OrangeRed
              Exit Select

          End Select
          FileLines.Inlines.AddRange({FileName, New LineBreak})
        Next
        If Files.Any Then
          FileLines.Inlines.Remove(FileLines.Inlines.Last)
        End If
        Content.Blocks.Add(FileLines)
      End If

      Return Content
    End If
  End Function

  Private Sub DeleteRevision(Order As Integer, RevisionNumber As String)
    RevisionBox.Document = New FlowDocument

    WorkingDocument = XDocument.Load(WorkingFileLocation)
    Dim Revision = WorkingDocument.Element("Revisions").Elements("Revision").FirstOrDefault(Function(Current As XElement) As Boolean
                                                                                              Return RevisionNumber.Equals(Current.Attribute("pRevision").Value)
                                                                                            End Function)
    If Revision Is Nothing Then
      Throw New Exception("Failed to remove revision: " & RevisionNumber & ".")

    Else
      Revision.Remove()
      WriteWorkingDocumentToFile()
      For i As Integer = MergedRevisionContainer.Children.Count - 1 To 0 Step -1
        With DirectCast(MergedRevisionContainer.Children(i), RevisionItem)
          If RevisionNumber.Equals(.RevisionNumber.ToString) Then
            SelectedRevisions.Remove(Order)
            MergedRevisionContainer.Children.RemoveAt(i)
            Exit For
          End If
        End With
      Next
    End If
  End Sub

  Private Sub WriteWorkingDocumentToFile()
    WorkingDocument.Save(WorkingFileLocation, SaveOptions.None)
  End Sub

  Private Sub RenderContentSpace(Filters() As String)
    'clear revision container and set title
    Me.Dispatcher.Invoke(Sub()
                           If String.IsNullOrEmpty(WorkingFileLocation) Then
                             Title = "Revision History"
                           Else
                             Title = System.IO.Path.GetFileNameWithoutExtension(WorkingFileLocation) & " - Revision History"
                           End If
                           UnmergedRevisionContainer.Children.Clear()
                           MergedRevisionContainer.Children.Clear()
                           WorkingDocument = XDocument.Load(WorkingFileLocation)

                           If Not String.IsNullOrWhiteSpace(HomeAuthorBox.Text) AndAlso Not HomeAuthorBox.ItemsSource.Cast(Of String).Contains(HomeAuthorBox.Text) Then
                             HomeAuthorBoxCache.Add(HomeAuthorBox.Text)
                             HomeAuthorBox.ItemsSource = Nothing
                             HomeAuthorBox.ItemsSource = HomeAuthorBoxCache.AsEnumerable
                           End If
                         End Sub)

    'get filtered revisions from file
    'unmerged
    Dim Index As Integer = 0
    Dim UnmergedRevisions = FilterRevision(UnmergedDocument, Filters(0), Filters(1), Filters(2), Filters(3), Filters(4), Filters(5)).ToList()
    If UnmergedRevisions IsNot Nothing AndAlso UnmergedRevisions.Any Then
      For i As Integer = 0 To UnmergedRevisions.Count - 1
        Dim Current As XElement = UnmergedRevisions(i)
        Me.Dispatcher.Invoke(Sub()
                               Dim RI As New RevisionItem(Integer.Parse(Current.Attribute("pRevision").Value), Index, Current.Attribute("pAuthor"))
                               AddHandler RI.MouseUp, AddressOf RenderContentText
                               UnmergedRevisionContainer.Children.Add(RI)
                             End Sub)
        Index += 1
      Next
    End If

    'merged
    Dim MergedRevisions = FilterRevision(WorkingDocument, Filters(0), Filters(1), Filters(2), Filters(3), Filters(4), Filters(5)).ToList()
    If MergedRevisions IsNot Nothing AndAlso MergedRevisions.Any Then
      Dim FirstIndex As Integer = Math.Max(0, MergedRevisions.Count - MaximumRevisionNumber)
      Dim LastIndex As Integer = MergedRevisions.Count - 1
      For i As Integer = FirstIndex To LastIndex
        Dim Current As XElement = MergedRevisions(i)
        Me.Dispatcher.Invoke(Sub()
                               Dim RI As New RevisionItem(Integer.Parse(Current.Attribute("pRevision").Value), Index, Current.Attribute("pAuthor"))
                               AddHandler RI.MouseUp, AddressOf RenderContentText
                               MergedRevisionContainer.Children.Add(RI)
                             End Sub)
        Index += 1
      Next
    End If

    Me.Dispatcher.Invoke(Sub()
                           Dim RNs As New HashSet(Of Integer)(SelectedRevisions.Values.Select(Function(Current As RevisionItem) Current.RevisionNumber))
                           SelectedRevisions.Clear()

                           'select available revisions
                           For Each RI As RevisionItem In UnmergedRevisionContainer.Children
                             'RI.RenderColor()
                             If RNs.Remove(RI.RevisionNumber) Then
                               RI.IsSelected = True
                               SelectedRevisions.Add(RI.Order, RI)
                             End If
                           Next
                           For Each RI As RevisionItem In MergedRevisionContainer.Children
                             'RI.RenderColor()
                             If RNs.Remove(RI.RevisionNumber) Then
                               RI.IsSelected = True
                               SelectedRevisions.Add(RI.Order, RI)
                             End If
                           Next

                           RenderContentText(Nothing, Nothing)
                           UnmergedRevisionSpace.ScrollToEnd()
                           MergedRevisionSpace.ScrollToEnd()
                           _IsReady = True
                         End Sub)
  End Sub

  Private Function FilterRevision(TargetDocument As XDocument, Revision As String, DateAndRange As String, Author As String, Message As String, Group As String, File As String) As IEnumerable(Of XElement)
    Dim Revisions As IEnumerable(Of XElement) = TargetDocument.Element("Revisions").Elements("Revision")

    'Revision number
    If Not String.IsNullOrEmpty(Revision) AndAlso Revision.Trim.Length > 0 Then
      Dim Filters As New List(Of String)(Revision.Trim.Split(",; ".ToCharArray))
      For i As Integer = Filters.Count - 1 To 0 Step -1
        If Filters(i).Length = 0 Then
          Filters.RemoveAt(i)
        End If
      Next
      Revisions = Revisions.Where(Function(Current As XElement) As Boolean
                                    Dim CurrentNumber As String = Current.Attribute("pRevision").Value
                                    If Filters.Remove(CurrentNumber) Then
                                      Return True
                                    Else
                                      For Each F In Filters
                                        If F.Length > 0 AndAlso (CurrentNumber.StartsWith(F) OrElse CurrentNumber.EndsWith(F)) Then
                                          Return True
                                        ElseIf F.Contains("-") Then
                                          Dim Temp As Integer() = {Integer.Parse(F.Remove(F.IndexOf("-"))), Integer.Parse(F.Substring(F.IndexOf("-") + 1))}
                                          Dim LowerBound As Integer = Math.Min(Temp(0), Temp(1))
                                          Dim UpperBound As Integer = Math.Max(Temp(0), Temp(1))
                                          Dim Value As Integer = Integer.Parse(CurrentNumber)
                                          If Value >= LowerBound AndAlso Value <= UpperBound Then
                                            Return True
                                          End If
                                        End If
                                      Next
                                      Return False
                                    End If
                                  End Function).ToList
    End If

    'Revision date
    If Not String.IsNullOrEmpty(DateAndRange) AndAlso DateAndRange.Trim.Length > 0 Then
      DateAndRange = DateAndRange.Trim.ToLower
      Dim LowerBound As Date
      Dim UpperBound As Date
      If DateAndRange.Equals("today") Then
        DateAndRange = Date.Today.ToShortDateString
      End If
      Dim Filters As New List(Of String)(DateAndRange.Split("-".ToCharArray))
      For i As Integer = Filters.Count - 1 To 0 Step -1
        If Filters(i).Length = 0 Then
          Filters.RemoveAt(i)
        Else
          Filters(i) = Filters(i).Trim
        End If
      Next
      Dim LowerBoundParser As ThaiBuddhistDate = Nothing
      Dim UpperBoundParser As ThaiBuddhistDate = Nothing
      If Filters.Count <= 2 AndAlso ThaiBuddhistDate.TryParse(Filters.First, LowerBoundParser) Then
        LowerBound = LowerBoundParser.GregorianDate
      Else
        Throw New Exception("Invalid date filter. Only one date or date range separated by dash sign are acceptable.")
      End If
      If Filters.Count = 2 AndAlso ThaiBuddhistDate.TryParse(Filters.Last, UpperBoundParser) Then
        UpperBound = UpperBoundParser.GregorianDate.AddDays(1)
      Else
        UpperBound = LowerBound.AddDays(1)
      End If
      Revisions = Revisions.Where(Function(Current As XElement) As Boolean
                                    If Current.Attribute("pDate") IsNot Nothing Then
                                      Dim SimplifiedDate As Date = Date.Parse(Current.Attribute("pDate").Value)
                                      If SimplifiedDate >= LowerBound AndAlso SimplifiedDate < UpperBound Then
                                        Return True
                                      End If
                                    End If
                                    Return False
                                  End Function).ToList
    End If

    'Revision author
    If Not String.IsNullOrEmpty(Author) AndAlso Author.Trim.Length > 0 Then
      Dim Filters As New List(Of String)(Author.Trim.ToLower.Split(",; ".ToCharArray, StringSplitOptions.RemoveEmptyEntries))

      'add or replace alias name
      Dim InnerAliasMap As Dictionary(Of String, String) = Nothing
      If AliasMap.TryGetValue("pAuthor", InnerAliasMap) Then
        For i As Integer = Filters.Count - 1 To 0 Step -1
          Dim Target As String = Nothing
          If InnerAliasMap.ContainsKey(Filters(i)) Then
            Target = InnerAliasMap.Item(Filters(i))
          Else
            Target = Filters(i)
          End If
          Dim Matches As IEnumerable(Of String) = InnerAliasMap.Where(Function(Current) Current.Value.Equals(Target, StringComparison.OrdinalIgnoreCase)).Select(Function(Current) Current.Key)
          If Matches.Any Then
            Filters.RemoveAt(i)
            For Each M In Matches
              Filters.Insert(i, M)
            Next
          End If
        Next
      End If

      Revisions = Revisions.Where(Function(Current As XElement) As Boolean
                                    If Current.Attribute("pAuthor") IsNot Nothing Then
                                      Dim CurrentAuthor As String = Current.Attribute("pAuthor").Value.ToLower
                                      For Each F In Filters
                                        If CurrentAuthor.StartsWith(F) Then
                                          Return True
                                        End If
                                      Next
                                    End If
                                    Return False
                                  End Function).ToList
    End If

    'Revision message
    If Not String.IsNullOrEmpty(Message) AndAlso Message.Trim.Length > 0 Then
      Dim Filters As New List(Of String)(Message.Trim.ToLower.Split(",;".ToCharArray))
      For i As Integer = Filters.Count - 1 To 0 Step -1
        If Filters(i).Trim.Length = 0 Then
          Filters.RemoveAt(i)
        End If
      Next
      Revisions = Revisions.Where(Function(Current As XElement) As Boolean
                                    If Current.Element("Message") IsNot Nothing Then
                                      Dim CurrentMessage As String = Current.Element("Message").Value.ToLower
                                      For Each F In Filters
                                        If CurrentMessage.Contains(F) Then
                                          Return True
                                        End If
                                      Next
                                    End If
                                    Return False
                                  End Function).ToList
    End If

    'Revision group
    If Not String.IsNullOrEmpty(Group) AndAlso Group.Trim.Length > 0 Then
      Dim Filters As New List(Of String)(Group.Trim.ToLower.Split(",;".ToCharArray))

      'add or replace alias name
      Dim InnerAliasMap As Dictionary(Of String, String) = Nothing
      If AliasMap.TryGetValue("xGroup", InnerAliasMap) Then
        For i As Integer = Filters.Count - 1 To 0 Step -1
          Dim Target As String = Nothing
          If InnerAliasMap.ContainsKey(Filters(i)) Then
            Target = InnerAliasMap.Item(Filters(i))
          Else
            Target = Filters(i)
          End If
          Dim Matches As IEnumerable(Of String) = InnerAliasMap.Where(Function(Current) Current.Value.Equals(Target, StringComparison.OrdinalIgnoreCase)).Select(Function(Current) Current.Key)
          If Matches.Any Then
            Filters.RemoveAt(i)
            For Each M In Matches
              Filters.Insert(i, M)
            Next
          End If
        Next
      End If

      Revisions = Revisions.Where(Function(Current As XElement) As Boolean
                                    If Current.Attribute("xGroup") IsNot Nothing Then
                                      Dim CurrentGroup As String = Current.Attribute("xGroup").Value.ToLower
                                      For Each F In Filters
                                        If CurrentGroup.Contains(F) Then
                                          Return True
                                        End If
                                      Next
                                      Return False
                                    Else
                                      Return True
                                    End If
                                  End Function).ToList
    End If

    'Files
    If Not String.IsNullOrEmpty(File) AndAlso File.Trim.Length > 0 Then
      Dim Filters As New List(Of String)(File.Trim.ToLower.Split(",; ".ToCharArray))
      For i As Integer = Filters.Count - 1 To 0 Step -1
        If Filters(i).Length = 0 Then
          Filters.RemoveAt(i)
        End If
      Next
      Revisions = Revisions.Where(Function(Current As XElement) As Boolean
                                    For Each F In Filters
                                      If F.Contains(":") Then
                                        Dim Prefix As String = F.Remove(F.IndexOf(":"))
                                        Prefix = Char.ToUpper(Prefix.Chars(0)) & Prefix.Substring(1).ToLower
                                        Dim Suffix As String = F.Substring(Prefix.Length + 1)
                                        Dim FX = Current.Elements(Prefix).FirstOrDefault(Function(CurrentFile As XElement) As Boolean
                                                                                           Return CurrentFile.Value.ToLower.Contains(Suffix)
                                                                                         End Function)
                                        Return FX IsNot Nothing
                                      Else
                                        Dim Children = Current.Elements.Where(Function(CurrentFile As XElement) As Boolean
                                                                                Return Not CurrentFile.Name.LocalName.Equals("Message")
                                                                              End Function)
                                        If Children.Any Then
                                          For Each FX In Children
                                            If FX.Value.ToLower.Contains(F) Then
                                              Return True
                                            End If
                                          Next
                                        End If
                                      End If
                                    Next
                                    Return False
                                  End Function).ToList
    End If

    Dim IsSortedByRevisionNumber As Boolean = False
    Me.Dispatcher.Invoke(Sub()
                           IsSortedByRevisionNumber = ViewSortByNumberBox.IsChecked
                         End Sub)
    If IsSortedByRevisionNumber Then
      Revisions = Revisions.OrderBy(Function(Current As XElement) Current.Attribute("pRevision").Value)
    End If

    Return Revisions
  End Function

  Private Sub FindControlByName(Container As Panel, Prefix As String, Suffix As String, ByRef MatchedResult As IList(Of Control))
    If Container IsNot Nothing Then
      For Each Child In Container.Children
        If TypeOf Child Is Panel Then
          FindControlByName(Child, Prefix, Suffix, MatchedResult)

        ElseIf TypeOf Child Is ScrollViewer AndAlso TypeOf DirectCast(Child, ScrollViewer).Content Is Panel Then
          FindControlByName(DirectCast(Child, ScrollViewer).Content, Prefix, Suffix, MatchedResult)

        ElseIf TypeOf Child Is FrameworkElement Then
          Dim Name As String = DirectCast(Child, FrameworkElement).Name
          If Not String.IsNullOrEmpty(Name) AndAlso
            (String.IsNullOrEmpty(Prefix) OrElse (Not String.IsNullOrEmpty(Prefix) AndAlso Name.StartsWith(Prefix))) AndAlso
            (String.IsNullOrEmpty(Suffix) OrElse (Not String.IsNullOrEmpty(Suffix) AndAlso Name.EndsWith(Suffix))) Then
            MatchedResult.Add(Child)
          End If
        End If
      Next
    End If
  End Sub

  'Private Sub ViewAllRevisionBox_Checked(sender As Object, e As System.Windows.RoutedEventArgs)
  '  AsyncRenderContentSpace()
  'End Sub

  Private Sub RevealFullFileName(sender As Object, e As MouseEventArgs)
    If TypeOf sender Is Paragraph Then
      Dim FileLines = DirectCast(sender, Paragraph).Inlines.ToArray
      For i As Integer = 1 To FileLines.Length - 1 Step +4
        Dim FileDirectory As Run = FileLines(i)
        FileDirectory.Text = FileDirectory.ToolTip.ToString
      Next
    End If
  End Sub

  Private Sub HideFullFileName(sender As Object, e As MouseEventArgs)
    If TypeOf sender Is Paragraph Then
      Dim FileLines = DirectCast(sender, Paragraph).Inlines.ToArray
      For i As Integer = 1 To FileLines.Length - 1 Step +4
        Dim FileDirectory As Run = FileLines(i)
        FileDirectory.Text = String.Empty
      Next
    End If
  End Sub

  Private Sub ToggleFilePath(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
    If TypeOf sender Is Paragraph Then
      Dim FileLines = DirectCast(sender, Paragraph).Inlines.ToArray
      For i As Integer = 1 To FileLines.Length - 1 Step +4
        Dim FileDirectory As Run = FileLines(i)
        If FileDirectory.Text.Length = 0 Then
          FileDirectory.Text = FileDirectory.ToolTip.ToString
        Else
          FileDirectory.Text = String.Empty
        End If
      Next
    End If
  End Sub

  Private Sub ReleaseLockOnWorkingDocument()
    Try
      If Not String.IsNullOrEmpty(WorkingFileLocation) AndAlso WorkingDocument IsNot Nothing Then
        WorkingDocument.Element("Revisions").Attribute("isLocked").Value = Boolean.FalseString
        WorkingDocument.Save(WorkingFileLocation)
      End If
    Catch ex As Exception
    End Try
  End Sub

  Private Sub AsyncGetSVNAndRenderContentSpace()
    StatusText.Text = "GETTING SVN LOG..."
    UnmergedLockIcon.Visibility = Windows.Visibility.Visible
    UnmergedRevisionSpace.IsEnabled = False
    HomeGetSVNButton.IsEnabled = False

    If UnmergedWorker IsNot Nothing AndAlso UnmergedWorker.IsAlive Then
      UnmergedWorker.Abort()
    End If

    Dim IsReadyToGo As Boolean = True
    Dim LogPreference As XElement = PreferencesDocument.Element("Preferences").Element("LastValues").Element("LogWindow")
    If LogPreference Is Nothing OrElse LogPreference.Attribute("limit") Is Nothing OrElse LogPreference.Attribute("path") Is Nothing Then
      'below code is similar to HomeGetSVNButton_MouseUp()
      Dim Settings As New LogWindow
      Settings.ShowDialog()
      IsReadyToGo = Settings.DialogResult.HasValue AndAlso Settings.DialogResult.Value = True
    End If

    If IsReadyToGo Then
      UnmergedWorker = New System.Threading.Thread(
        New System.Threading.ParameterizedThreadStart(
          Sub(Parameter As Object)
            Dim UnmergedDocument = GetUnmergedDocument(Integer.Parse(LogPreference.Attribute("limit").Value),
                                                       LogPreference.Attribute("path").Value,
                                                       New HashSet(Of Integer)(WorkingDocument.Element("Revisions").Elements("Revision").Select(Function(Current As XElement) Integer.Parse(Current.Attribute("pRevision").Value))))

            Me.Dispatcher.Invoke(Sub()
                                   Me.UnmergedDocument = UnmergedDocument
                                   StatusText.Text = "READY"
                                   UnmergedLockIcon.Visibility = Windows.Visibility.Hidden
                                   UnmergedRevisionSpace.IsEnabled = True
                                   HomeGetSVNButton.IsEnabled = True

                                   AsyncRenderContentSpace()
                                 End Sub)
          End Sub)
        )
      UnmergedWorker.Priority = System.Threading.ThreadPriority.AboveNormal
      UnmergedWorker.Start()
    End If
  End Sub

  Private Function GetUnmergedDocument(LimitNumberOfRevision As Integer, LogPath As String, MergedRevisionNumbers As HashSet(Of Integer)) As XDocument
    Dim Holder As New XDocument(New XElement("Revisions"))

    Dim Processor As New Process()
    With Processor
      With .StartInfo
        .FileName = "svn"
        .Arguments = "log -v --limit " & LimitNumberOfRevision & " """ & LogPath & """"
        .UseShellExecute = False
        .CreateNoWindow = True
        .RedirectStandardOutput = True
      End With
      .Start()
      '.WaitForExit()

      Dim Applicable As Boolean = False
      Dim Buffer As New System.Text.StringBuilder
      Do Until .StandardOutput.EndOfStream
        Dim Line As String = .StandardOutput.ReadLine
        If Line IsNot Nothing Then
          If Line.Equals("------------------------------------------------------------------------") Then
            Applicable = True
            Dim Reader As New StringReader(Buffer.ToString)
            If Buffer.Length = 0 Then
              Continue Do
            Else
              Buffer.Clear()
            End If

            Dim R As New XElement("Revision")
            With R
              Dim Parts As String() = Reader.ReadLine.Split("|").Select(Function(Current) Current.Trim).ToArray
              Dim RN As Integer
              If Not Parts(0).StartsWith("r") OrElse Not Integer.TryParse(Parts(0).Substring(1), RN) OrElse MergedRevisionNumbers.Contains(RN) Then
                Continue Do
              End If
              .Add(New XAttribute("pRevision", RN))
              .Add(New XAttribute("pAuthor", Parts(1)))
              If Parts(2).Contains("(") Then
                Parts(2) = Parts(2).Substring(0, Parts(2).IndexOf("("))
              End If
              .Add(New XAttribute("pDate", Parts(2)))
              Do
                Line = Reader.ReadLine
                If Line.Length = 0 Then
                  Exit Do
                Else
                  If Line.Contains(" (from ") AndAlso Line.EndsWith(")") Then
                    Line = Line.Substring(0, Line.IndexOf(" (from "))
                  End If
                  If Line.StartsWith("   A") Then
                    .Add(New XElement("Added", Line.Substring(5)))
                  ElseIf Line.StartsWith("   M") Then
                    .Add(New XElement("Modified", Line.Substring(5)))
                  ElseIf Line.StartsWith("   D") Then
                    .Add(New XElement("Deleted", Line.Substring(5)))
                  End If
                End If
              Loop
              .AddFirst(New XElement("Message", New XCData(Reader.ReadToEnd.Trim)))
            End With
            Holder.Element("Revisions").Add(R)

          ElseIf Applicable = False Then
            Me.Dispatcher.Invoke(Sub()
                                   MessageBox.Show("Cannot get SVN log.", Title, MessageBoxButton.OK, MessageBoxImage.Exclamation)
                                 End Sub)
            Exit Do

          Else
            Buffer.AppendLine(Line)
          End If
        End If
      Loop
    End With

    Return Holder
  End Function

End Class
