Imports System.Windows.Data
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Media.Imaging
Imports System.Windows.Navigation
Imports System.Windows.Shapes

Partial Public Class RevisionItem

  'Private Shared GrayMask As Brush = New SolidColorBrush(Color.FromArgb(127, 192, 192, 192))
  'Public Shared ColorMasks As New Dictionary(Of String, Color)
  Public Shared Property Colorize As Boolean = False

  Private _IsSelected As Boolean = False

  Public Property Author As String

  Public ReadOnly Property RevisionNumber As Integer
    Get
      Return Integer.Parse(RevisionFirstText.Text & RevisionLastText.Text)
    End Get
  End Property

  Public Property Order As Integer = Integer.MaxValue

  Public Property IsSelected As Boolean
    Get
      Return _IsSelected
    End Get
    Set(Value As Boolean)
      _IsSelected = Value

      If Colorize Then
        Dim TrueColor As Color = DirectCast(Background, SolidColorBrush).Color
        If _IsSelected Then
          Background = MainWindow.SummerCyan
          Foreground = Brushes.White

        Else
          Background = Brushes.Transparent
          If IsEnabled Then
            Foreground = Brushes.Black
          Else
            IsEnabled = MyBase.IsEnabled
          End If
        End If

      Else
        If _IsSelected Then
          Background = MainWindow.SummerCyan
          Foreground = Brushes.White

        Else
          Background = Brushes.Transparent
          Foreground = Brushes.Black
        End If
      End If
    End Set
  End Property

  Public Shadows Property IsEnabled As Boolean
    Get
      Return MyBase.IsEnabled
    End Get
    Set(Value As Boolean)
      MyBase.IsEnabled = Value
      If Value Then
        IsSelected = _IsSelected
      Else
        Foreground = Brushes.Silver
      End If
    End Set
  End Property

  Public Sub New(RevisionNumber As Integer, Order As Integer, Author As XAttribute)
    MyBase.New()

    Me.InitializeComponent()

    If RevisionNumber >= 10000 Then
      RevisionFirstText.Text = RevisionNumber.ToString.Substring(0, RevisionNumber.ToString.Length - 4)
      RevisionLastText.Text = RevisionNumber.ToString.Remove(0, RevisionNumber.ToString.Length - 4)
    Else
      RevisionFirstText.Text = RevisionNumber
    End If
    Me.Order = Order
    If Author IsNot Nothing Then
      Me.Author = Author.Value.ToLower
    End If
  End Sub

  Private Sub RevisionItem_MouseEnter(sender As Object, e As System.Windows.Input.MouseEventArgs) Handles Me.MouseEnter
    If IsEnabled Then
      BorderBrush = Brushes.DarkGray
    End If
  End Sub

  Private Sub RevisionItem_MouseLeave(sender As Object, e As System.Windows.Input.MouseEventArgs) Handles Me.MouseLeave
    BorderBrush = Nothing
  End Sub

  'Public Sub RenderColor()
  '  If Not String.IsNullOrEmpty(Author) AndAlso ColorMasks.ContainsKey(Author) AndAlso Colorize Then
  '    Background = New SolidColorBrush(ColorMasks.Item(Author))
  '  Else
  '    Background = New SolidColorBrush(Color.FromArgb(0, 255, 255, 255)) '0,0,205
  '  End If
  '  'IsSelected = _IsSelected
  'End Sub

  'Public Sub UpdateMask(Container As Panel, CurrentIndex As Integer)
  '  If CurrentIndex > 0 Then
  '    Dim PreviousItem As RevisionItem = Container.Children.Item(CurrentIndex - 1)
  '    Dim CurrentItem As RevisionItem = Container.Children.Item(CurrentIndex)
  '    If PreviousItem.AuthorCode = CurrentItem.AuthorCode Then
  '      CurrentItem.RootSpace.Background = PreviousItem.RootSpace.Background
  '    ElseIf Object.ReferenceEquals(PreviousItem.RootSpace.Background, GrayMask) Then
  '      CurrentItem.RootSpace.Background = Brushes.Transparent
  '    Else
  '      CurrentItem.RootSpace.Background = GrayMask
  '    End If
  '  End If
  'End Sub
End Class
