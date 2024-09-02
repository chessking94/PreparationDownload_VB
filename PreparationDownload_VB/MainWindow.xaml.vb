'TODO: Support for variants
'TODO: Additional arguments for ECO, minimum number of moves, etc. Might require pgn-extract loops
'TODO: Casing; pgn-extract apparently needs exact upper/lower case for parsing usernames. Look into returning proper casing from API call if successful

Class MainWindow
    Private Property IsValidated As Boolean = True
    Private Property ValidationFailReasons As String = ""

    'TODO: VS doesn't like these "non-constant fields should not be visible". Can I rework this to make it more robust? Maybe move _clsParameters here?
    Public Shared FirstName As String
    Public Shared LastName As String
    Public Shared Username As String
    Public Shared Site As String
    Public Shared TimeControl As String
    Public Shared Color As String
    Public Shared StartDate As Date
    Public Shared EndDate As Date
    Public Shared ReplaceUsername As Boolean = True
    Public Shared WriteLog As Boolean = False

    Public Shared ErrorList As String  'TODO: Convert this to a proper list

    'Friend Sub UseArguments(pi_args As String())
    '    Dim args As Dictionary(Of String, String) = Utilities_NetCore.ParseCommandLineArguments(pi_args)

    '    'TODO: set variable defaults
    '    'FirstName = "Ethan"
    '    'LastName = "Hunt"
    '    'Username = ""
    '    'Site = "All"
    '    'TimeControl = "All"
    '    'Color = "Both"
    '    'StartDate = New Date(2024, 6, 1)
    '    'EndDate = New Date(2024, 7, 1)
    '    'Replace username with real name = True
    '    'Write log record = True

    '    For Each kvp As KeyValuePair(Of String, String) In args
    '        MessageBox.Show($"Key: {kvp.Key}, Value: {kvp.Value}")
    '        Select Case kvp.Key.ToLower
    '            Case "-f", "--first"
    '                FirstName = kvp.Value.Trim
    '            Case "-l", "--last"
    '                LastName = kvp.Value.Trim
    '            Case "-u", "--user"
    '                Username = kvp.Value.Trim
    '            Case "-s", "--site"
    '                Site = kvp.Value.Trim
    '            Case "-t", "--time"
    '                TimeControl = kvp.Value.Trim
    '            Case "-c", "--color"
    '                Color = kvp.Value.Trim
    '            Case "--startdate"
    '                'TODO: how can I parse/validate the strings and turn them into proper dates?
    '            Case "--enddate"

    '            Case "--log"
    '                If kvp.Value.Trim = "1" Then WriteLog = True  'return true if 1, false for anything else
    '            Case Else
    '                Throw New Exception($"Invalid positional argument '{kvp.Key}'")
    '        End Select
    '    Next

    '    'TODO: validate variable values

    '    'TODO: run the process
    'End Sub

    Private Sub WindowLoaded() Handles Me.Loaded
        Dim list_Sites As New List(Of String) From {"Chess.com", "Lichess", "All"}
        sel_Site.ItemsSource = list_Sites

        Dim list_timeControls As New List(Of String) From {"Bullet", "Blitz", "Rapid", "Classical", "Correspondence", "All"}
        sel_TimeControl.ItemsSource = list_timeControls

        Dim list_Colors As New List(Of String) From {"White", "Black", "Both"}
        sel_Color.ItemsSource = list_Colors
    End Sub

    Private Sub FirstLastChanged() Handles inp_FirstName.SelectionChanged, inp_LastName.SelectionChanged
        If inp_FirstName.Text <> "" OrElse inp_LastName.Text <> "" Then
            inp_Username.IsEnabled = False
        Else
            inp_Username.IsEnabled = True
        End If
    End Sub

    Private Sub UsernameChanged() Handles inp_Username.SelectionChanged
        If inp_Username.Text <> "" Then
            inp_FirstName.IsEnabled = False
            inp_LastName.IsEnabled = False
        Else
            inp_FirstName.IsEnabled = True
            inp_LastName.IsEnabled = True
        End If
    End Sub

    Private Sub ReplaceUsername_Checked(sender As Object, e As RoutedEventArgs) Handles chk_ReplaceUsername.Checked
        If chk_ReplaceUsername.IsChecked Then
            ReplaceUsername = True
        Else
            ReplaceUsername = False
        End If
    End Sub

    Private Sub WriteLog_Checked(sender As Object, e As RoutedEventArgs) Handles chk_WriteLog.Checked
        If chk_WriteLog.IsChecked Then
            WriteLog = True
        Else
            WriteLog = False
        End If
    End Sub

    Private Sub Run() Handles cmd_Run.Click
        pgnExtractExists()
        If ErrorList <> "" Then
            MessageBox.Show(ErrorList, "Process Failed - Missing Dependency", MessageBoxButton.OK, MessageBoxImage.Stop)
        Else
            Enabler(False)

            Try
#If DEBUG Then
                FirstName = "Ethan"
                LastName = "Hunt"
                Username = ""
                Site = "All"
                TimeControl = "All"
                Color = "Both"
                StartDate = New Date(2024, 6, 1)
                EndDate = New Date(2024, 7, 1)
#Else
                'validate inputs
                ValidateName()
                ValidateSite()
                ValidateTimeControl()
                ValidateColor()
                ValidateDates()
#End If

                If Not IsValidated Then
                    Throw New Exception("Validation failed:" & vbCrLf & vbCrLf & ValidationFailReasons)
                End If

                'TODO: Add a progress bar - initial attempt failed, can't seem to figure out how to make the TextBlock/TextBox object accessible outside of this class

                'tb_Status.Text = "Processing request..."
                RunProcess()
                'tb_Status.Text = "Process complete"

                If ErrorList = "" Then
                    Try
                        Process.Start("explorer.exe", clsBase.rootDir)
                    Catch ex As Exception
                        ErrorList = AppendText(ErrorList, $"Unable to open directory {clsBase.rootDir}, file(s) can be found at that location")
                    End Try
                End If

                If ErrorList <> "" Then
                    MessageBox.Show(ErrorList, "Process Complete - Errors", MessageBoxButton.OK, MessageBoxImage.Warning)
                End If

                ResetDefaults()

            Catch ex As Exception
                MessageBox.Show(ex.Message, "Process Failed", MessageBoxButton.OK, MessageBoxImage.Error)

            End Try

            Enabler(True)
        End If
    End Sub

    Private Sub pgnExtractExists()
        Dim executableName As String = "pgn-extract"
        Dim processStartInfo As New ProcessStartInfo()
        With processStartInfo
            .FileName = "cmd.exe"
            .Arguments = $"/C {executableName} -h"
            .CreateNoWindow = True
            .RedirectStandardError = True
            .UseShellExecute = False
        End With

        Dim process As New Process() With {.StartInfo = processStartInfo}
        process.Start()

        Dim sOutput As String
        Using oStreamReader As IO.StreamReader = process.StandardError
            sOutput = oStreamReader.ReadToEnd()
        End Using
        process.WaitForExit()

        If sOutput.Contains("is not recognized") Then
            ErrorList = $"{executableName} not found, unable to continue"
        End If
    End Sub

    Private Sub Enabler(ByVal pi_IsEnabled As Boolean)
        'toggle the inputs between enabled or not
        cmd_Run.IsEnabled = pi_IsEnabled

        inp_FirstName.IsEnabled = pi_IsEnabled
        inp_LastName.IsEnabled = pi_IsEnabled
        inp_Username.IsEnabled = pi_IsEnabled
        sel_Site.IsEnabled = pi_IsEnabled
        sel_TimeControl.IsEnabled = pi_IsEnabled
        sel_Color.IsEnabled = pi_IsEnabled
        inp_StartDate.IsEnabled = pi_IsEnabled
        inp_EndDate.IsEnabled = pi_IsEnabled
        chk_ReplaceUsername.IsEnabled = pi_IsEnabled
        chk_WriteLog.IsEnabled = pi_IsEnabled
    End Sub

    Private Sub ResetDefaults()
        inp_FirstName.Text = ""
        inp_LastName.Text = ""
        inp_Username.Text = ""
        sel_Site.SelectedValue = Nothing
        sel_TimeControl.SelectedValue = Nothing
        sel_Color.SelectedValue = Nothing
        inp_StartDate.SelectedDate = Nothing
        inp_EndDate.SelectedDate = Nothing
        chk_ReplaceUsername.IsChecked = True
        chk_WriteLog.IsChecked = False
    End Sub

#Region "Validation"
    Private Sub ValidateName()
        If inp_Username.Text = "" Then
            If inp_FirstName.Text = "" And inp_LastName.Text = "" Then
                IsValidated = False
                ValidationFailReasons = AppendText(ValidationFailReasons, "Missing Name or Username")
            End If

            If IsValidated AndAlso inp_FirstName.Text = "" Then
                IsValidated = False
                ValidationFailReasons = AppendText(ValidationFailReasons, "Missing First Name")
            End If

            If IsValidated AndAlso inp_LastName.Text = "" Then
                IsValidated = False
                ValidationFailReasons = AppendText(ValidationFailReasons, "Missing Last Name")
            End If
        End If

        If IsValidated Then
            FirstName = inp_FirstName.Text
            LastName = inp_LastName.Text
            Username = inp_Username.Text
        End If
    End Sub

    Private Sub ValidateSite()
        If sel_Site.SelectedValue Is Nothing Then
            IsValidated = False
            ValidationFailReasons = AppendText(ValidationFailReasons, "Missing Site")
        End If

        If IsValidated Then
            Site = sel_Site.SelectedValue.Content
        End If
    End Sub

    Private Sub ValidateTimeControl()
        If sel_TimeControl.SelectedValue Is Nothing Then
            IsValidated = False
            ValidationFailReasons = AppendText(ValidationFailReasons, "Missing Time Control")
        End If

        If IsValidated Then
            TimeControl = sel_TimeControl.SelectedValue.Content
        End If
    End Sub

    Private Sub ValidateColor()
        If sel_Color.SelectedValue Is Nothing Then
            IsValidated = False
            ValidationFailReasons = AppendText(ValidationFailReasons, "Missing Color")
        End If

        If IsValidated Then
            Color = sel_Color.SelectedValue.Content
        End If
    End Sub

    Private Sub ValidateDates()
        If inp_StartDate.SelectedDate IsNot Nothing AndAlso inp_EndDate.SelectedDate IsNot Nothing Then
            If inp_StartDate.SelectedDate > inp_EndDate.SelectedDate Then
                IsValidated = False
                ValidationFailReasons = AppendText(ValidationFailReasons, "Start Date after End Date")
            End If
        End If

        If IsValidated Then
            If inp_StartDate.SelectedDate IsNot Nothing Then
                StartDate = inp_StartDate.SelectedDate
            End If

            If inp_EndDate.SelectedDate IsNot Nothing Then
                EndDate = inp_EndDate.SelectedDate
            End If
        End If
    End Sub
#End Region
End Class
