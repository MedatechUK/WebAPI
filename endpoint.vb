Imports System.Data.SqlClient
Imports System.IO
Imports System.Text
Imports System.Web
Imports System.Web.Configuration
Imports System.Xml
Imports Newtonsoft.Json

Public MustInherit Class EndPoint
    Inherits MedatechUK.Logging.Logable
    Implements IHttpHandler

#Region "Implements IHttpHandler"

    Private ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property

    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest

        Me.logHandler = AddressOf hLog
        Try
            StartLog()
            Select Case httpMethod
                Case "get"
                    Me.GET(context)

                Case "post"
                    Me.POST(context)

            End Select

        Catch exp As InvalidExpressionException 'Bad Environent
            context.Response.StatusCode = 501
            Logging.Log("Invalid Priority company.")

        Catch exp As Exception
            context.Response.StatusCode = 500
            Logging.Log(exp)

        Finally
            With New DirectoryInfo(Path.Combine(logdir.FullName, Now.ToString("yyyy-MM")))
                If Not .Exists Then .Create()
                Using sw As New StreamWriter(
                    Path.Combine(
                        .FullName,
                            String.Format(
                                "{0}.txt",
                                Now.ToString("yyMMdd")
                            )
                        ), True
                    )
                    sw.Write(translog.ToString)

                End Using
            End With

        End Try

    End Sub

    Sub [GET](ByRef context As HttpContext)
        With context.Response
            Try
                Using cn As New SqlConnection(Me.PriorityDBConnection)
                    cn.Open()
                    Using r As New sqlXML(Me, cn)

                        Select Case _RequestLang
                            Case eLang.xml
                                .ContentType = "text/xml"
                                Using objX As New XmlTextWriter(.OutputStream, Nothing)
                                    With objX
                                        .WriteStartDocument()
                                        .WriteNode(r.Command(cn).ExecuteXmlReader(), True)
                                        .WriteEndDocument()

                                    End With
                                End Using

                            Case eLang.json
                                .ContentType = "text/json"
                                Dim doc As New XmlDocument
                                doc.Load(r.Command(cn).ExecuteXmlReader())
                                .Write(JsonConvert.SerializeXmlNode(doc))

                        End Select
                        .StatusCode = 200

                    End Using
                End Using

            Catch ex As NotImplementedException
                Logging.Log("SQL Feed not found [{0}].", requestEndpoint)
                .StatusCode = 404

            Catch ex As Exception
                Throw ex

            End Try

        End With
    End Sub

    Sub [POST](ByRef context As HttpContext)
        With context.Response
            Using EX As New MedatechUK.Deserialiser.AppExtension(AddressOf hLog)
                If EX.LexByAssemblyName(requestEndpoint) Is Nothing Then
                    Logging.Log("Endpoint Lexor not found [{0}].", requestEndpoint)
                    .StatusCode = 404

                Else
                    With EX.LexByAssemblyName(requestEndpoint)
                        .Deserialise(New IO.StreamReader(context.Request.InputStream), requestEnv)

                    End With
                    .StatusCode = 201

                End If

            End Using

        End With

    End Sub

#End Region

#Region "log"

    Public ReadOnly Property LogDir As DirectoryInfo
        Get
            Return New DirectoryInfo(Path.Combine(HttpContext.Current.Server.MapPath("/"), "log"))
        End Get
    End Property

    Private translog As New StringBuilder
    Private Sub hLog(Sender As Object, e As MedatechUK.Logging.LogArgs)
        If Not Sender Is Nothing Then
            translog.AppendFormat("{0}> {1} {2}", Format(Now, "HH:mm:ss"), Sender.ToString, e.Message).AppendLine()

        Else
            translog.AppendFormat("{0}> {1}", Format(Now, "HH:mm:ss"), e.Message).AppendLine()

        End If

    End Sub

    Sub StartLog()
        Try
            With HttpContext.Current
                Select Case httpMethod
                    Case "get"
                        Dim rv As New Dictionary(Of String, String)
                        Dim str As New StringBuilder

                        For Each k As String In .Request.QueryString
                            If Not (String.Compare(k, "environment", True) = 0 Or String.Compare(k, "endpoint", True) = 0) Then
                                rv.Add(k, .Request.QueryString(k))
                            End If
                        Next

                        If rv.Count > 0 Then str.Append("?")
                        For Each r As String In rv.Keys
                            str.AppendFormat("{0}={1}", r, rv(r))
                            If Not String.Compare(rv.Keys.Last, r, True) = 0 Then
                                str.Append("&")

                            End If
                        Next

                        MedatechUK.Logging.Log(
                        "Received {0} /{1}/{2} from {3}.",
                        httpMethod.ToUpper,
                        .Request("environment"),
                        String.Format("{0}{1}", .Request("endpoint"), str.ToString),
                        UserHost
                    )

                    Case "post"
                        .Items("bubbleid") = System.Guid.NewGuid.ToString
                        MedatechUK.Logging.Log(
                            "Received {0} /{1}/{2} from {3}.",
                            httpMethod.ToUpper,
                            .Request("environment"),
                            .Request("endpoint"),
                            UserHost
                        )
                        Dim bytes
                        With .Request.InputStream
                            bytes = New Byte(.Length - 1) {}
                            .Read(bytes, 0, bytes.Length - 1)
                            .Position = 0
                        End With

                        With New DirectoryInfo(Path.Combine(LogDir.FullName, Now.ToString("yyyy-MM")))
                            With New DirectoryInfo(Path.Combine(.FullName, Now.ToString("yyMMdd")))
                                If Not .Exists Then .Create()
                                Dim save As New FileInfo(
                                    Path.Combine(
                                        .FullName,
                                            String.Format(
                                                "{0}.{1}",
                                                HttpContext.Current.Items("bubbleid"),
                                                RequestLang.ToString
                                            )
                                        )
                                )
                                MedatechUK.Logging.Log("Saving POSTed data to {0}.", save.FullName)
                                Using sw As New StreamWriter(save.FullName, True)
                                    sw.Write(Encoding.ASCII.GetString(bytes))

                                End Using
                            End With
                        End With

                End Select

            End With

        Catch ex As Exception
            Throw New InvalidExpressionException 'Bad environment

        End Try

    End Sub

#End Region

#Region "Properties"

    Public ReadOnly Property UserHost
        Get
            Return HttpContext.Current.Request.UserHostAddress
        End Get
    End Property

    Public ReadOnly Property httpMethod
        Get
            Return HttpContext.Current.Request.HttpMethod.ToLower
        End Get
    End Property

    Public ReadOnly Property requestEnv As String
        Get
            Static ret As String = Nothing
            If ret Is Nothing Then
                If HttpContext.Current.Request("environment") Is Nothing Then _
                    Throw New InvalidExpressionException

                Using cn As New SqlConnection(Me.PriorityDBConnection)
                    cn.Open()
                    Using command As New SqlCommand(
                        "use system; " &
                        "select DNAME from ENVIRONMENT where DNAME <> ''", cn
                    )
                        Using rs As SqlDataReader = command.ExecuteReader
                            While rs.Read
                                If String.Compare(rs("DNAME"), HttpContext.Current.Request("environment"), True) = 0 Then
                                    ret = rs("DNAME")
                                    Exit While

                                End If

                            End While
                        End Using
                    End Using
                End Using

                If ret Is Nothing Then Throw New InvalidExpressionException

            End If

            Return ret

        End Get

    End Property

    Public ReadOnly Property requestEndpoint As String
        Get
            Static ret As String = Nothing
            If ret Is Nothing Then
                Select Case HttpContext.Current.Request.HttpMethod.ToLower
                    Case "get"
                        Select Case Split(HttpContext.Current.Request("endpoint"), ".")(1).ToLower
                            Case "xml"
                                _RequestLang = eLang.xml
                                ret = Split(HttpContext.Current.Request("endpoint"), ".")(0)

                            Case "json"
                                _RequestLang = eLang.json
                                ret = Split(HttpContext.Current.Request("endpoint"), ".")(0)

                            Case Else
                                Throw New NotSupportedException

                        End Select

                    Case Else
                        ret = HttpContext.Current.Request("endpoint")

                End Select
            End If
            Return ret
        End Get
    End Property

#Region "Request Language"

    Public Enum eLang
        xml
        json

    End Enum

    Private _RequestLang As eLang = eLang.xml
    Public ReadOnly Property RequestLang As eLang
        Get
            Return _RequestLang
        End Get
    End Property

#End Region

    Public ReadOnly Property PriorityDBConnection As String
        Get
            Try
                Return WebConfigurationManager.ConnectionStrings("priority").ConnectionString

            Catch
                Throw New Exception("The service is not configured.")

            End Try

        End Get
    End Property

#End Region

End Class
