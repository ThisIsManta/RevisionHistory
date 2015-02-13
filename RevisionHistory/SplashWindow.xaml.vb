Imports System.Windows.Controls
Imports System.Windows.Data
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Navigation

Namespace Presentation.Windows
  Public Class SplashWindow

    Private WithEvents Waiter As New System.Timers.Timer(1000)
    Private Main As MainWindow

    Public Shadows Property Title As String
      Get
        Return MyBase.Title
      End Get
      Set(Value As String)
        MyBase.Title = Value
        Header.Text = Value
      End Set
    End Property

    Public Shadows Property Background As Brush
      Get
        Return RectShape.Fill
      End Get
      Set(Value As Brush)
        UpperTriShape.Fill = Value
        RectShape.Fill = Value
        LowerTriShape.Fill = Value
      End Set
    End Property

    Public Sub New()
      MyBase.New()

      Me.InitializeComponent()
    End Sub

    Private Sub SplashWindow_Loaded(sender As Object, e As System.Windows.RoutedEventArgs) Handles Me.Loaded
      Main = New MainWindow
      Main.Visibility = System.Windows.Visibility.Visible
      Waiter.Start()
    End Sub

    Private Sub Waiter_Elapsed(sender As Object, e As System.Timers.ElapsedEventArgs) Handles Waiter.Elapsed
      Me.Dispatcher.Invoke(Sub()
                             If Main.IsReady Then
                               Waiter.Stop()
                               Me.Close()
                             End If
                           End Sub)
    End Sub
  End Class
End Namespace