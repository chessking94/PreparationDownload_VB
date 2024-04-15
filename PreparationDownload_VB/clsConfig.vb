Imports Microsoft.Extensions.Configuration
Imports System.IO
Imports System.Reflection

Public Class clsConfig
    Private Shared _configuration As IConfiguration

    Private Shared Sub buildConfig()
        Dim appSettingsPath As String = Path.Combine(clsBase.projectDir, "appsettings.json")

        Dim builder = New ConfigurationBuilder().AddJsonFile(appSettingsPath)
        _configuration = builder.Build()
    End Sub

    Public Shared Function getConfig(key As String) As String
        buildConfig()
        Return _configuration(key)
    End Function
End Class
