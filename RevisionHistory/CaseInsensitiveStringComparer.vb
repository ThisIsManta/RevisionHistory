Public Class CaseInsensitiveStringComparer
  Implements IComparer(Of String)

  Public Function Compare(Current As String, Another As String) As Integer Implements System.Collections.Generic.IComparer(Of String).Compare
    Return String.Compare(Current.ToLower, Another.ToLower)
  End Function
End Class
