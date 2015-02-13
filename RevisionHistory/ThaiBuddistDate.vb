Imports System.Globalization

Public NotInheritable Class ThaiBuddhistDate

	' rename to EnglishMonths
	Public Shared ReadOnly EnglishFullDays() As String = [Enum].GetNames(GetType(DayOfWeek))
	Public Shared ReadOnly EnglishFullMonths() As String = {"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"}
	Public Shared ReadOnly ThaiFullDays() As String = {"อาทิตย์", "จันทร์", "อังคาร", "พุธ", "พฤหัสบดี", "ศุกร์", "เสาร์"}
	Public Shared ReadOnly ThaiShortDays() As String = {"อา.", "จ.", "อ.", "พ.", "พฤ.", "ศ.", "ส."}
	Public Shared ReadOnly ThaiFullMonths() As String = {"มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"}
	Public Shared ReadOnly ThaiShortMonths() As String = {"ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."}

	Private Shared ReadOnly Separators() As Char = {"-", "/", "\", ","}
	Public Shared ReadOnly CurrentCalendar As New ThaiBuddhistCalendar

	Private _GregorianDate As Date

	Public Property GregorianDate As Date
		Get
			Return _GregorianDate
		End Get
		Set(Value As Date)
			_GregorianDate = Value
		End Set
	End Property

	Public ReadOnly Property Year As Integer
		Get
			Return CurrentCalendar.GetYear(_GregorianDate)
		End Get
	End Property

	Public Sub New()
		_GregorianDate = Date.Today
	End Sub

	Private Sub New(d As Date)
		_GregorianDate = d
	End Sub

	Public Shared Function Parse(d As Date) As ThaiBuddhistDate
		Return New ThaiBuddhistDate(d)
	End Function

	Public Shared Function TryParse(ByVal Text As String, ByRef d As ThaiBuddhistDate) As Boolean
		Dim Tokens() As String = Text.Split(Separators)
		If Tokens.Length < 2 Or Tokens.Length > 3 Then
      Tokens = Text.Split
		End If

    Dim DayPart As Integer
    Dim MonthPart As Integer = Date.Today.Month
    Dim YearPart As Integer = Date.Today.Year
    If Tokens.Length >= 2 And Tokens.Length <= 3 AndAlso
     Integer.TryParse(Tokens(0).Trim, DayPart) AndAlso DayPart >= 1 AndAlso
     TryParseMonth(Tokens(1).Trim, MonthPart) AndAlso
     (Tokens.Length < 3 OrElse Integer.TryParse(Tokens(2).Trim, YearPart)) Then

      'expand 2 digits year part to 4 digits or more in AD
      If YearPart >= 0 And YearPart < 100 Then
        Dim Baseline As Integer = Math.Pow(10, Integer.Parse(Tokens(2).Trim.Length))
        If Math.Abs(YearPart - (Date.Today.Year Mod Baseline)) < 20 Then 'output is in AD
          YearPart += (Date.Today.Year \ Baseline) * Baseline
        Else 'output is in BE
          YearPart += ((Date.Today.Year + 543) \ Baseline) * Baseline
        End If
      End If

      'convert from BE to AD
      If (YearPart - Date.Today.Year) > 400 Then
        YearPart -= 543
      End If

      'check the last day in month
      If DayPart > Date.DaysInMonth(YearPart, MonthPart) Then
        DayPart = Date.DaysInMonth(YearPart, MonthPart)
      End If

      d = New ThaiBuddhistDate(New Date(YearPart, MonthPart, DayPart))
      Return True

    ElseIf Tokens.Length = 1 Then 'single part
      Dim UnknownPart As Integer
      If Integer.TryParse(Tokens(0).Trim, UnknownPart) Then
        If UnknownPart <= Date.DaysInMonth(YearPart, MonthPart) Then 'specific day in present month
          d = New ThaiBuddhistDate(New Date(YearPart, MonthPart, UnknownPart))
          Return True

        ElseIf Math.Abs(DayPart - YearPart) < 270 Then 'present day in specific year  (AD)
          d = New ThaiBuddhistDate(New Date(UnknownPart, MonthPart, Date.Today.Day))
          Return True

        ElseIf Math.Abs(DayPart - YearPart - 543) < 270 Then 'present day in specific year (BE)
          d = New ThaiBuddhistDate(New Date(UnknownPart - 543, MonthPart, Date.Today.Day))
          Return True
        End If

      ElseIf TryParseMonth(Tokens(0).Trim, MonthPart) Then 'present day in specific month
        d = New ThaiBuddhistDate(New Date(YearPart, MonthPart, Date.Today.Day))
        Return True
      End If
    End If

    Return False
	End Function

	Private Shared Function TryParseMonth(ByVal Month As String, ByRef OneBasedIndex As Integer) As Integer
		Month = Month.ToUpper

		Dim Temp As Integer
		If Integer.TryParse(Month, Temp) AndAlso Temp >= 1 And Temp <= 12 Then
			OneBasedIndex = Temp
			Return True
		End If

		For i As Integer = 0 To 11
			If EnglishFullMonths(i).ToUpper.StartsWith(Month) Or ThaiFullMonths(i).StartsWith(Month) Or ThaiShortMonths(i).StartsWith(Month) Then
				OneBasedIndex = i + 1
				Return True
			End If
		Next

		If Month.Length = 3 Then
			For i As Integer = 0 To 11
				If EnglishFullMonths(i).Substring(0, 3).ToUpper.Equals(Month) Then
					OneBasedIndex = i + 1
					Return True
				End If
			Next
		End If

		Return False
	End Function

	Public Overloads Function ToString(ByVal Format As String, ByVal MonthSpecifier As MonthFormat, ByVal YearSpecifier As YearFormat) As String
		Dim MonthPart As String = String.Empty
		If MonthSpecifier = MonthFormat.Digit Then
			MonthPart = _GregorianDate.Month
		ElseIf MonthSpecifier = MonthFormat.ShortThai Then
			MonthPart = ThaiShortMonths(CurrentCalendar.GetMonth(_GregorianDate) - 1)
		ElseIf MonthSpecifier = MonthFormat.FullThai Then
			MonthPart = ThaiFullMonths(CurrentCalendar.GetMonth(_GregorianDate) - 1)
		ElseIf MonthSpecifier = MonthFormat.ShortEnglish Then
			MonthPart = EnglishFullMonths(CurrentCalendar.GetMonth(_GregorianDate) - 1).Substring(0, 3).ToUpper
		ElseIf MonthSpecifier = MonthFormat.FullEnglish Then
			MonthPart = EnglishFullMonths(CurrentCalendar.GetMonth(_GregorianDate) - 1)
		End If

		Dim YearPart As String = String.Empty
		If YearSpecifier = YearFormat.BuddhistEra Then
			YearPart = CurrentCalendar.GetYear(_GregorianDate)
		ElseIf YearSpecifier = YearFormat.AnnoDomini Then
			YearPart = _GregorianDate.Year
		End If

		Return String.Format(Format, CurrentCalendar.GetDayOfMonth(_GregorianDate), MonthPart, YearPart)
	End Function

	Public Overloads Function ToString(ByVal MonthSpecifier As MonthFormat, ByVal YearSpecifier As YearFormat) As String
		Dim Format As String = "{0} {1} {2}"
		If MonthSpecifier = MonthFormat.Digit Then
			Format = "{0}/{1}/{2}"
		End If

		Return ToString(Format, MonthSpecifier, YearSpecifier)
	End Function

	Public Overloads Function ToString(ByVal Format As String) As String
		Return String.Format(Format, CurrentCalendar.GetDayOfMonth(_GregorianDate), ThaiFullMonths(CurrentCalendar.GetMonth(_GregorianDate) - 1), CurrentCalendar.GetYear(_GregorianDate))
	End Function

	Public Overrides Function ToString() As String
		Return String.Format("{0}/{1}/{2}", CurrentCalendar.GetDayOfMonth(_GregorianDate), CurrentCalendar.GetMonth(_GregorianDate), CurrentCalendar.GetYear(_GregorianDate))
	End Function

End Class

Public Enum MonthFormat As Byte
  Digit
  ShortEnglish
  FullEnglish
  ShortThai
  FullThai
End Enum

Public Enum YearFormat As Byte
  AnnoDomini
  BuddhistEra
End Enum