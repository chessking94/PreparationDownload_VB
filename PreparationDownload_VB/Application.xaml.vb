Class Application
    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException can be handled in this file.

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        Dim mainWindow As New MainWindow()
        mainWindow.Show()

        'If e.Args.Length > 0 Then
        '    'arguments, run without interactivity
        '    mainWindow.UseArguments(e.Args)

        '    'TODO: need to terminate the program at the end, doesn't seem to naturally
        'Else
        '    'no arguments, open the interactive main window
        '    mainWindow.Show()
        'End If
    End Sub
End Class
