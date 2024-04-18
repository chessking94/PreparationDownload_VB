Class MainWindow
    Private Property IsValidated As Boolean = True
    Private Property ValidationFailReasons As String = ""
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

    Public Shared ErrorList As String

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

    Private Sub ReplaceUsername_Checked(sender As Object, e As RoutedEventArgs)
        If chk_ReplaceUsername.IsChecked Then
            ReplaceUsername = True
        Else
            ReplaceUsername = False
        End If
    End Sub

    Private Sub WriteLog_Checked(sender As Object, e As RoutedEventArgs)
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
            Try
                Enabler(False)

#If DEBUG Then
                FirstName = "Ethan"
                LastName = "Hunt"
                Username = ""
                Site = "All"
                TimeControl = "All"
                Color = "Both"
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

                RunProcess()

                Try
                    Process.Start("explorer.exe", clsBase.rootDir)
                Catch ex As Exception
                    ErrorList = AppendText(ErrorList, $"Unable to open directory {clsBase.rootDir}, file(s) can be found at that location")
                End Try

                If ErrorList <> "" Then
                    MessageBox.Show(ErrorList, "Process Complete - Errors", MessageBoxButton.OK, MessageBoxImage.Warning)
                End If

                Enabler(True)

            Catch ex As Exception
                MessageBox.Show(ex.Message, "Process Failed", MessageBoxButton.OK, MessageBoxImage.Error)
                Enabler(True)

            End Try
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
