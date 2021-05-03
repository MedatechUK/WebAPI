Imports System.Data.SqlClient
Imports System.IO
Imports System.Text
Imports System.Web
Imports System.Web.Configuration
Imports System.Xml

Public MustInherit Class xmlfeed
    Inherits MedatechUK.Logging.Logable
    Implements IHttpHandler

    Public MustOverride Sub response(ByRef cn As SqlConnection, ByRef w As XmlTextWriter)

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        Me.logHandler = AddressOf hLog
        Try
            StartLog()

            With context.Response
                .Clear()
                .ContentType = "text/xml"
                .ContentEncoding = Encoding.UTF8

                Using objX As New XmlTextWriter(context.Response.OutputStream, Nothing)
                    With objX
                        .WriteStartDocument()
                        Using cn As New SqlConnection(Me.PriorityDBConnection)
                            cn.Open()
                            Me.response(cn, objX)

                        End Using

                        .WriteEndDocument()

                    End With

                End Using

            End With

        Catch exp As InvalidExpressionException 'Bad Environent
            context.Response.StatusCode = 501
            Logging.Log("Invalid Priority company.")

        Catch exp As Exception
            context.Response.StatusCode = 500
            Logging.Log(exp)

        Finally
            With New DirectoryInfo(Path.Combine(LogDir.FullName, Now.ToString("yyyy-MM")))
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

    Private ReadOnly Property PriorityDBConnection As String
        Get
            Try
                Return WebConfigurationManager.ConnectionStrings("priority").ConnectionString

            Catch
                Throw New Exception("The service is not configured.")

            End Try

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

#Region "log"

    Public ReadOnly Property LogDir As DirectoryInfo
        Get
            Return New DirectoryInfo(Path.Combine(HttpContext.Current.Server.MapPath("/api/"), "log"))
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
                    String.Format("{0}{1}", New FileInfo(.Request.PhysicalPath).Name, str.ToString),
                    UserHost
                )

            End With

        Catch ex As Exception
            Throw New InvalidExpressionException 'Bad environment

        End Try

    End Sub

#End Region

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

End Class
