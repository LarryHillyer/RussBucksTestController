Imports System
Imports System.ComponentModel
Imports System.Math
Imports System.Data
Imports System.Linq
Imports System.Xml.Linq
Imports System.Globalization

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.IO
Imports System.IO.Path

Imports System.Threading
Imports System.Threading.Tasks

Imports WpfApplication1.JoinPools.Models
Imports WpfApplication1.LosersPool.Models


Public Class NewJobWindow
    Inherits Window
    Private Sports As New Dictionary(Of String, String)

    Public Property CronJobName As String

    Public Sub New(jobName As String)
        InitializeComponent()

        Me.CronJobName = jobName


    End Sub


End Class
