Imports System.Data.SqlClient
Imports System.Web
Imports System.Web.Configuration
Imports System.Xml
Imports System.ComponentModel.Composition
Imports System.ComponentModel.Composition.Hosting
Imports System.IO
Imports System.Text
Imports Newtonsoft.Json

Namespace Web

    Public MustInherit Class EndPoint : Implements IDisposable : Implements IHttpHandler

        Public Enum eLang
            xml
            json
        End Enum

#Region "Implements IHttpHandler"

        Public MustOverride Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest

        Private ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
            Get
                Return False
            End Get
        End Property

#End Region

#Region "Imported MEF Enumerables"

        Private ReadOnly Property BinCatalog As Primitives.ComposablePartCatalog
            Get
                Return New DirectoryCatalog(
                Path.Combine(
                    HttpContext.Current.Request.PhysicalApplicationPath,
                    "bin"
                )
            )
            End Get
        End Property

        <ImportMany()>
        Private Property _handlers As IEnumerable(Of Lazy(Of xmlHandler, xmlHandlerProps))
        Public ReadOnly Property handlers As IEnumerable(Of Lazy(Of xmlHandler, xmlHandlerProps))
            Get
                Return HttpContext.Current.Items("handlers")
            End Get
        End Property

        <ImportMany()>
        Private Property _feeds As IEnumerable(Of Lazy(Of xmlFeed, xmlFeedProps))
        Public ReadOnly Property feeds As IEnumerable(Of Lazy(Of xmlFeed, xmlFeedProps))
            Get
                Return HttpContext.Current.Items("feeds")
            End Get
        End Property

        Public Sub New()

            With HttpContext.Current.Items
                If .Item("mefLoad") Is Nothing Then

                    Dim catalog = New AggregateCatalog()
                    catalog.Catalogs.Add(BinCatalog)

                    Dim container As New CompositionContainer(catalog)
                    container.ComposeParts(Me)

                    .Add("mefLoad", True)
                    .Add("feeds", _feeds)
                    .Add("handlers", _handlers)

                End If

            End With

        End Sub

#End Region

#Region "Public Properties"

        Public ReadOnly Property BubbleID As String
            Get
                Return HttpContext.Current.Items("bubbleid")
            End Get
        End Property

        Private _EndPoint As String
        Public Property Name As String
            Get
                Return _EndPoint
            End Get
            Set(value As String)
                _EndPoint = value
            End Set
        End Property

        Private _Hidden As Boolean = False
        Public Property Hidden As Boolean
            Get
                Return _Hidden
            End Get
            Set(value As Boolean)
                _Hidden = value
            End Set
        End Property

        Public Property apiLang As eLang
            Get
                Return HttpContext.Current.Items("apilang")
            End Get
            Set(value As eLang)
                HttpContext.Current.Items("apilang") = value
            End Set
        End Property

        Public ReadOnly Property requestEnv As String
            Get
                Try
                    If Not HttpContext.Current.Request("environment") Is Nothing Then
                        Return HttpContext.Current.Request("environment")
                    Else
                        Return ""
                    End If
                Catch ex As Exception
                    Return ""
                End Try
            End Get
        End Property

        Public ReadOnly Property requestEndpoint As String
            Get
                Return HttpContext.Current.Request("endpoint")
            End Get
        End Property

        Public Property log As oMsgLog
            Get
                Return TryCast(HttpContext.Current.Items("log"), oMsgLog)
            End Get
            Set(value As oMsgLog)
                HttpContext.Current.Items("log") = value
            End Set
        End Property

        Public ReadOnly Property httpMethod
            Get
                Return HttpContext.Current.Request.HttpMethod.ToLower
            End Get
        End Property

        Public ReadOnly Property UserHost
            Get
                Return HttpContext.Current.Request.UserHostAddress
            End Get
        End Property

        Public ReadOnly Property xmlSP As Integer
            Get
                Return HttpContext.Current.Items("xmlSP")
            End Get
        End Property

        Public ReadOnly Property FileType As String
            Get
                If HttpContext.Current.Request("filetype") Is Nothing Then
                    Return ""
                Else
                    Return HttpContext.Current.Request("filetype").ToLower
                End If
            End Get
        End Property

        Public ReadOnly Property feedDir As DirectoryInfo
            Get
                Return New DirectoryInfo(
                Path.Combine(
                    HttpContext.Current.Request.PhysicalApplicationPath,
                    "feeds"
                )
            )
            End Get
        End Property

        Public ReadOnly Property sqlFile As FileInfo
            Get
                Return New FileInfo(
                 Path.Combine(
                    Path.Combine(
                        HttpContext.Current.Request.PhysicalApplicationPath,
                        "feeds"
                    ),
                    String.Format("{0}.sql", requestEndpoint)
                )
            )
            End Get
        End Property

#End Region

#Region "Methods"

        Public ReadOnly Property Match() As Boolean
            Get
                Select Case HttpContext.Current.Request.HttpMethod.ToLower
                    Case "get"
                        'Check File system objects
                        If Name = "fso" Then
                            Return sqlFile.Exists
                        End If

                        ' Check database objects
                        With HttpContext.Current.Items.Item("xmlSPList")
                            For Each sp As String In .Keys()
                                If String.Compare(
                                sp,
                                String.Format("{0}.{1}", Name, requestEndpoint),
                                True
                            ) = 0 Then
                                    HttpContext.Current.Items("xmlSP") = .Item(sp)
                                    Return True

                                End If

                            Next
                        End With

                End Select

                Return _
                String.Compare(
                    Name,
                    requestEndpoint,
                    True
                ) = 0

            End Get

        End Property

        Public ReadOnly Property ValidEnv As Boolean
            Get
                Return Environments.Contains(HttpContext.Current.Request("environment"))
            End Get
        End Property

#End Region

#Region "DB Connection"

        Private ReadOnly Property PriorityDBConnection
            Get
                Try
                    Return WebConfigurationManager.ConnectionStrings("priority").ConnectionString

                Catch
                    Throw New Exception("The service is not configured.")

                End Try

            End Get
        End Property

        Public ReadOnly Property dbConnection As SqlConnection
            Get
                ' Reconnect on disconnection
                With TryCast(HttpContext.Current.Items("cn"), SqlConnection)
                    Select Case TryCast(HttpContext.Current.Items("cn"), SqlConnection).State
                        Case ConnectionState.Broken, ConnectionState.Closed
                            .Open()

                    End Select
                End With

                Return TryCast(HttpContext.Current.Items("cn"), SqlConnection)

            End Get

        End Property

#End Region

#Region "Execute SQL"

#Region "Stringbuilder support"

        Public Function ExecuteReader(ByRef sqlString As Text.StringBuilder) As SqlClient.SqlDataReader
            Return ExecuteReader(sqlString.ToString)
        End Function

        Public Function ExecuteScalar(ByRef sqlString As Text.StringBuilder) As String
            Return ExecuteScalar(sqlString.ToString)
        End Function

        Public Function ExecuteXmlReader(ByRef sqlString As Text.StringBuilder) As XmlReader
            Return ExecuteXmlReader(sqlString.ToString)
        End Function

        Public Function ExecuteNonQuery(ByRef sqlString As Text.StringBuilder) As Integer
            Return ExecuteNonQuery(sqlString.ToString)
        End Function

#End Region

#Region "SQL Commands"

        Friend Function LogStartSQL(Method As String, ByRef Statement As String) As Date
            log.LogData.AppendFormat("Executing {0}:", Method.ToUpper).AppendLine.Append(Statement).AppendLine().AppendLine()
            Return Now
        End Function

        Friend Sub LogEndSQL(starttime As Date)
            log.LogData.AppendFormat(
            "Completed in {0} seconds.",
            (Now - starttime).ToString.Replace("00:", "")
        ).AppendLine()
        End Sub

        Public Function ExecuteReader(ByRef sqlString As String) As SqlClient.SqlDataReader
            Dim ret As SqlDataReader
            Dim command As New SqlCommand(sqlString.ToString, dbConnection)
            Dim start As Date = LogStartSQL("Reader", sqlString)
            ret = command.ExecuteReader
            LogEndSQL(start)
            Return ret
        End Function

        Public Function ExecuteScalar(ByRef sqlString As String) As String
            Dim ret As String
            Dim command As New SqlCommand(sqlString, dbConnection)
            Dim start As Date = LogStartSQL("Scalar", sqlString)
            Try
                ret = command.ExecuteScalar.ToString
            Catch
                ret = ""
            End Try
            LogEndSQL(start)
            Return ret
        End Function

        Public Function ExecuteXmlReader(ByRef sqlString As String) As XmlReader
            Dim ret As XmlReader
            Dim command As New SqlCommand(sqlString, dbConnection)
            Dim start As Date = LogStartSQL("XmlReader", sqlString)
            ret = command.ExecuteXmlReader
            LogEndSQL(start)
            Return ret
        End Function

        Public Function ExecuteNonQuery(ByRef sqlString As String) As Integer
            Dim ret As Integer
            Dim command As New SqlCommand(sqlString, dbConnection)
            Dim start As Date = LogStartSQL("NonQuery", sqlString)
            ret = command.ExecuteNonQuery
            LogEndSQL(start)
            Return ret
        End Function

#End Region

#Region "Priority Functions"

        ''' <summary>
        ''' A List of valid Priority Companies
        ''' </summary>
        ''' <returns>List(Of String)</returns>
        Public Function Environments() As List(Of String)
            Dim ret As New List(Of String)
            Select Case HttpContext.Current.Request.HttpMethod.ToLower
                Case "get", "patch", "view"
                    ret.Add("system")

            End Select

            Dim command As New SqlCommand(
            "use system; " &
            "select DNAME from ENVIRONMENT where DNAME <> ''",
            dbConnection
        )
            Dim rs As SqlDataReader = command.ExecuteReader
            While rs.Read
                ret.Add(rs("DNAME"))
            End While
            rs.Close()
            Return ret
        End Function

        Public Function spXML(Optional env As String = "") As Dictionary(Of String, Integer)
            Dim ret As New Dictionary(Of String, Integer)
            If env.Length = 0 Then env = requestEnv
            If env.Length > 0 Then

                Dim command As New SqlCommand(
            String.Format(
                "use {0}; " &
                "select SO.OBJECT_ID as [ObjectID], " &
                "SCHEMA_NAME(SCHEMA_ID) + '.' + SO.name AS [ObjectName] " &
                "From sys.objects AS SO " &
                    "INNER JOIN sys.parameters AS P " &
                    "On SO.OBJECT_ID = P.OBJECT_ID " &
                "WHERE 0=0 " &
                "And SO.TYPE IN ('FN') " &
                "And (TYPE_NAME(P.user_type_id)='xml') " &
                "And P.is_output=1",
                env
            ),
            dbConnection
        )
                Dim rs As SqlDataReader = command.ExecuteReader
                While rs.Read
                    ret.Add(rs("ObjectName"), rs("ObjectID"))
                End While
                rs.Close()

            End If

            Return ret

        End Function

        Public Function spParams(Optional env As String = "", Optional ID As Integer = 0) As Dictionary(Of String, String)
            Dim ret As New Dictionary(Of String, String)
            If env.Length = 0 Then env = requestEnv
            If ID = 0 Then ID = xmlSP
            Dim command As New SqlCommand(
            String.Format(
                "use {0}; " &
                "SELECT	" &
                "	P.name AS [ParameterName],	" &
                "	TYPE_NAME(P.user_type_id) AS [ParameterDataType] " &
                "FROM sys.objects AS SO	" &
                "	INNER JOIN sys.parameters AS P 	" &
                "	ON SO.OBJECT_ID = P.OBJECT_ID	" &
                "WHERE 0=0	" &
                "	And SO.OBJECT_ID = {1}	" &
                "	And P.is_output=0" &
                "order by parameter_id",
                env,
                ID
            ),
            dbConnection
        )
            Dim rs As SqlDataReader = command.ExecuteReader
            While rs.Read
                ret.Add(rs("ParameterName").substring(1), rs("ParameterDataType"))

            End While
            rs.Close()
            Return ret
        End Function

#End Region

#Region "Logging"

        Public Sub newSQLLog()
            With HttpContext.Current
                .Items.Add("bubbleid", System.Guid.NewGuid.ToString)
                .Items.Add("xmlSP", 0)
                .Items.Add("log",
                    New oMsgLog(
                        .Request.Url.Host.ToString,
                        EvtLogSource.WEB,
                        EvtLogVerbosity.Verbose,
                        LogEntryType.Information
                    )
                )

                ' Connect database
                .Items.Add("cn", New SqlConnection)
                With TryCast(HttpContext.Current.Items("cn"), SqlConnection)
                    .ConnectionString = PriorityDBConnection
                    .Open()
                End With

                ' Build list of SQL XML functions
                .Items.Add("xmlSPList", spXML)

                ' Set API language
                .Items.Add("apilang", eLang.xml)
                Select Case FileType
                    Case "json"
                        apiLang = eLang.json

                End Select
                Select Case .Request.ContentType.ToLower
                    Case "text/json"
                        apiLang = eLang.json

                End Select

                .Response.AddHeader("BubbleID", BubbleID)
                Dim command As New SqlCommand(
                    String.Format(
                        My.Resources.log,
                        BubbleID,
                        requestEnv,
                        .Request.HttpMethod,
                        .Request("endpoint")
                    ), dbConnection
                )
                command.ExecuteNonQuery()

            End With

            log.LogData.AppendFormat(
                "Received {0} /{1}/{2}.{3} from {4}.",
                httpMethod.ToUpper,
                requestEnv,
                requestEndpoint,
                FileType,
                UserHost
            ).AppendLine()

        End Sub

        Public Sub setLog()
            Dim logstr As String = log.LogData.ToString _
                .Replace(Chr(10), "") _
                .Replace(vbTab, "   ") _
                .Replace("'", "'+char(39)+'")
            Dim command As New SqlCommand(
                String.Format(
                    My.Resources.setLog,
                    BubbleID,
                    logstr,
                    CInt(log.EntryType)
                ), dbConnection)
            command.ExecuteNonQuery()
        End Sub

        Public Sub setXml(x As String)
            Dim command As New SqlCommand(
            String.Format(
                My.Resources.setXML,
                BubbleID,
                x.Replace("'", "'+char(39)+'")
            ), dbConnection)
            command.ExecuteNonQuery()
        End Sub

        ''' <summary>
        ''' Set a key value of the saved XML.
        ''' Duplicates keys throw an error
        ''' unless optional Overwrite is True.
        ''' </summary>
        ''' <param name="x">The key value from the foreign system</param>
        ''' <param name="Overwrite">Associate this log with saved key</param>
        Public Sub setKey(x As String, Optional Overwrite As Boolean = False)
            Try
                Dim command As New SqlCommand(
                String.Format(
                    My.Resources.setfKey,
                    BubbleID,
                    x
                ), dbConnection)
                command.ExecuteNonQuery()

            Catch ex As SqlException
                If ex.Number = 2601 Then
                    If Not Overwrite Then _
                        Throw New Exception(
                            String.Format(
                                "Duplicate foreign key: '{0}'",
                                x
                            )
                        )

                    Dim command As New SqlCommand(
                        String.Format(
                            My.Resources.setfKeyReplace,
                            BubbleID,
                            x
                        ), dbConnection)
                    command.ExecuteNonQuery()

                Else
                    Throw (ex)
                End If

            Catch ex As Exception
                Throw (ex)

            End Try

        End Sub

        Public Sub ToStream(ByRef ex As Exception)
            With HttpContext.Current.Response
                .Clear()
                .ContentEncoding = Encoding.UTF8

                Select Case apiLang
                    Case eLang.json
                        .ContentType = "text/json"
                        Using strm As New StreamWriter(.OutputStream)
                            Using objx As New JTextWriter(strm)
                                With objx
                                    .WriteStartObject()
                                    .WritePropertyName("response")
                                    .WriteStartArray()
                                    .WriteStartObject()
                                    .WriteAttributeString("status", "500")
                                    .WriteAttributeString("bubbleid", BubbleID)
                                    .WriteAttributeString("message", ex.Message)
                                    .WriteAttributeString("stacktr", ex.StackTrace)
                                    .WriteEndObject()
                                    .WriteEndArray()
                                End With
                            End Using
                        End Using

                    Case Else
                        .ContentType = "text/xml"
                        Using objX As New XmlTextWriter(.OutputStream, Nothing)
                            With objX
                                .WriteStartDocument()
                                .WriteStartElement("response")
                                .WriteAttributeString("status", CStr(500))
                                .WriteAttributeString("bubbleid", BubbleID)
                                .WriteAttributeString("message", ex.Message)
                                .WriteAttributeString("stacktr", ex.StackTrace)
                                .WriteEndElement() 'End Settings 
                                .WriteEndDocument()
                                .Flush()
                                .Close()
                            End With
                        End Using

                End Select
            End With
        End Sub

#End Region

#End Region

#Region "IDisposable Support"

        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    With HttpContext.Current
                        .Items.Clear()

                    End With
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            ' TODO: uncomment the following line if Finalize() is overridden above.
            ' GC.SuppressFinalize(Me)
        End Sub

#End Region

    End Class

End Namespace
