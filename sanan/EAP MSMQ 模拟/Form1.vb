Imports System.Messaging
Imports System.Xml
Imports System.IO
Imports System.Collections
Public Class Form1
    Dim OUISendMSMQ As MessageQueue
    Dim WithEvents OUIReceiveMSMQ As MessageQueue
    Dim DMSSendMSMQ As MessageQueue
    Dim WithEvents DMSReceiveMSMQ As MessageQueue
    Dim RMSSendMSMQ As MessageQueue
    Dim WithEvents RMSReceiveMSMQ As MessageQueue
    Dim APCSendMSMQ As MessageQueue
    Dim WithEvents APCReceiveMSMQ As MessageQueue
    Dim MESSendMSMQ As MessageQueue
    Dim WithEvents MESReceiveMSMQ As MessageQueue
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        OUISendMSMQ.Send(TextBox1.Text)
    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        For Each file In Directory.EnumerateFiles("Log")
            Dim sr As New StreamReader(file)
            Dim w = False
            Dim xml = ""
            While Not sr.EndOfStream
                Dim line = sr.ReadLine()
                If line.Contains("<message>") Then
                    w = True
                    xml = "<?xml version=""1.0"" encoding=""utf-16""?>"
                End If
                If w Then
                    Dim ww = True
                    If line.Contains("<tid>") Then
                        xml += "<tid></tid>"
                        ww = False
                    End If
                    If line.Contains("<datetime>") Then
                        xml += "<datetime></datetime>"
                        ww = False
                    End If
                    If line.Contains("<eqp_id>") Then
                        xml += "<eqp_id></eqp_id>"
                        ww = False
                    End If
                    If ww Then
                        xml += line
                    End If
                End If
                If line.Contains("</message>") Then
                    w = False
                    Dim xDoc As New XmlDocument
                    xDoc.LoadXml(xml)
                    Dim xMsgID = xDoc.Item("message").Item("msg_id").InnerText
                    If xMsgID.EndsWith("_R") Then
                        If Not xmlsr.ContainsKey(xMsgID) Then
                            xmlsr.Add(xMsgID, xml)
                        End If
                    Else
                        If Not xmls.ContainsKey(xMsgID) Then
                            xmls.Add(xMsgID, xml)
                            BackgroundWorker1.ReportProgress(1, xMsgID)
                        End If
                    End If
                End If
            End While
            sr.Close()
        Next
    End Sub

    Private Sub BackgroundWorker1_ProgressChanged(sender As Object, e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged
        ListBox1.Items.Add(e.UserState.ToString())
        Text = DateTime.Now.ToString
    End Sub

    Private Sub BackgroundWorker1_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted

    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        BackgroundWorker1.RunWorkerAsync()
        OUISendMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\oui.to.c201")
        OUIReceiveMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\c201.to.oui")
        DMSSendMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\dms.to.c201")
        DMSReceiveMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\c201.to.dms")
        RMSSendMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\rms.to.c201")
        RMSReceiveMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\c201.to.rms")
        APCSendMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\apc.to.c201")
        APCReceiveMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\c201.to.apc")
        MESSendMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\mes.to.c201")
        MESReceiveMSMQ = New MessageQueue("FormatName:DIRECT=OS:100367IT-WWK\Private$\c201.to.mes")
        OUIReceiveMSMQ.BeginReceive()
        DMSReceiveMSMQ.BeginReceive()
        RMSReceiveMSMQ.BeginReceive()
        APCReceiveMSMQ.BeginReceive()
        MESReceiveMSMQ.BeginReceive()
    End Sub

    Dim xTID As String
    Dim xEQPID As String
    Dim xmls As New Dictionary(Of String, String)()
    Dim xmlsr As New Dictionary(Of String, String)()
    Private Sub DoMSMQ(send As MessageQueue, receive As MessageQueue, s As String)
        Dim xDoc As New XmlDocument
        Dim xMsgID As String
        xDoc.LoadXml(s)
        xMsgID = xDoc.Item("message").Item("msg_id").InnerText
        xTID = xDoc.Item("message").Item("tid").InnerText
        xEQPID = xDoc.Item("message").Item("eqp_id").InnerText
        If xmlsr.ContainsKey(xMsgID + "_R") Then
            Dim xml = xmlsr(xMsgID + "_R")
            xml = xml.Replace("<tid></tid>", "<tid>" + xTID + "</tid>")
            xml = xml.Replace("<datetime></datetime>", "<datetime>" + DateTime.Now.ToString + "</datetime>")
            xml = xml.Replace("<eqp_id></eqp_id>", "<eqp_id>" + xEQPID + "</eqp_id>")
            send.Send(xml, “back”)
            s += vbNewLine
            s += vbNewLine
            s += xml
        End If
        Me.Invoke(New shows(AddressOf show), s)
        receive.BeginReceive()
    End Sub

    Private Sub OUIReceiveMSMQ_ReceiveCompleted(ByVal sender As Object, ByVal e As System.Messaging.ReceiveCompletedEventArgs) Handles OUIReceiveMSMQ.ReceiveCompleted
        Dim targetTypes(0) As Type
        targetTypes(0) = GetType(String)
        e.Message().Formatter = New XmlMessageFormatter(targetTypes)
        DoMSMQ(OUISendMSMQ, OUIReceiveMSMQ, e.Message().Body.ToString)
    End Sub

    Private Sub RMSReceiveMSMQ_ReceiveCompleted(ByVal sender As Object, ByVal e As System.Messaging.ReceiveCompletedEventArgs) Handles RMSReceiveMSMQ.ReceiveCompleted
        Dim targetTypes(0) As Type
        targetTypes(0) = GetType(String)
        e.Message().Formatter = New XmlMessageFormatter(targetTypes)
        DoMSMQ(RMSSendMSMQ, RMSReceiveMSMQ, e.Message().Body.ToString)
    End Sub

    Private Sub DMSReceiveMSMQ_ReceiveCompleted(ByVal sender As Object, ByVal e As System.Messaging.ReceiveCompletedEventArgs) Handles DMSReceiveMSMQ.ReceiveCompleted
        Dim targetTypes(0) As Type
        targetTypes(0) = GetType(String)
        e.Message().Formatter = New XmlMessageFormatter(targetTypes)
        DoMSMQ(DMSSendMSMQ, DMSReceiveMSMQ, e.Message().Body.ToString)
    End Sub

    Private Sub APCReceiveMSMQ_ReceiveCompleted(ByVal sender As Object, ByVal e As System.Messaging.ReceiveCompletedEventArgs) Handles APCReceiveMSMQ.ReceiveCompleted
        Dim targetTypes(0) As Type
        targetTypes(0) = GetType(String)
        e.Message().Formatter = New XmlMessageFormatter(targetTypes)
        DoMSMQ(APCSendMSMQ, APCReceiveMSMQ, e.Message().Body.ToString)
    End Sub

    Private Sub MESReceiveMSMQ_ReceiveCompleted(ByVal sender As Object, ByVal e As System.Messaging.ReceiveCompletedEventArgs) Handles MESReceiveMSMQ.ReceiveCompleted
        Dim targetTypes(0) As Type
        targetTypes(0) = GetType(String)
        e.Message().Formatter = New XmlMessageFormatter(targetTypes)
        DoMSMQ(MESSendMSMQ, MESReceiveMSMQ, e.Message().Body.ToString)
    End Sub

    Public Delegate Sub shows(ByVal s As String)

    Public Sub show(s As String)
        TextBox1.Text = s
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged
        TextBox1.Text = xmls(ListBox1.SelectedItem.ToString())
        If xmlsr.ContainsKey(ListBox1.SelectedItem.ToString() + "_R") Then TextBox2.Text = xmlsr(ListBox1.SelectedItem.ToString() + "_R")
        TextBox1.Text = TextBox1.Text.Replace("<tid></tid>", "<tid>" + xTID + "</tid>")
        TextBox1.Text = TextBox1.Text.Replace("<datetime></datetime>", "<datetime>" + DateTime.Now.ToString + "</datetime>")
        TextBox1.Text = TextBox1.Text.Replace("<eqp_id></eqp_id>", "<eqp_id>" + xEQPID + "</eqp_id>")
        TextBox2.Text = TextBox2.Text.Replace("<tid></tid>", "<tid>" + xTID + "</tid>")
        TextBox2.Text = TextBox2.Text.Replace("<datetime></datetime>", "<datetime>" + DateTime.Now.ToString + "</datetime>")
        TextBox2.Text = TextBox2.Text.Replace("<eqp_id></eqp_id>", "<eqp_id>" + xEQPID + "</eqp_id>")
    End Sub
End Class
