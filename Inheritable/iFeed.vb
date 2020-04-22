Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Xml
Imports Newtonsoft.Json

Namespace Web

    Public MustInherit Class iFeed : Inherits EndPoint

#Region "Metadata"

        Public Sub SetMeta(ByRef Metadata As xmlFeedProps)
            With Metadata
                Name = .EndPoint
                Hidden = .Hidden
            End With
        End Sub

#End Region

#Region "Overridable stubs"

        ''' <summary>
        ''' The sql query that will be executed.
        ''' Parameters are caputured from the GET request.
        ''' </summary>
        ''' <returns></returns>
        Public Overridable Function Query() As String
            Throw New NotSupportedException

        End Function


        ''' <summary>
        ''' Any SQL CREATEs for functions used by the query.
        ''' PATCHing the MEF feed executes the CREATE in the selected environment.
        ''' </summary>
        ''' <returns></returns>
        Public Overridable Function InstallQuery() As String
            Throw New NotSupportedException

        End Function

        Public Overridable Sub ProcessReq(ByVal context As HttpContext)
            Throw New NotSupportedException

        End Sub

#End Region

#Region "Process Request"

        Public Function Params() As Dictionary(Of String, String)
            Dim ret As New Dictionary(Of String, String)
            Dim declaration As New Regex("(?<=declare.*@)[A-Z0-9]*", RegexOptions.IgnoreCase)
            For Each m As Match In declaration.Matches(Me.Query)
                Dim tp As New Regex(String.Format("(?<=declare.@{0}.)[a-zA-Z0-9]*", m.Value), RegexOptions.IgnoreCase)
                ret.Add(m.Value, tp.Matches(Me.Query)(0).Value.ToLower)
            Next

            Return ret
        End Function

        Public Overrides Sub ProcessRequest(ByVal context As HttpContext)

            With context.Response

                Try
                    ProcessReq(context)

                Catch exep As NotSupportedException

                    Select Case apiLang
                        Case eLang.json
                            .ContentType = "text/json"
                            Dim doc As New XmlDocument
                            doc.Load(
                            ExecuteXmlReader(
                                String.Format(
                                    "use {0}; {1}",
                                    requestEnv,
                                    Statement
                                )
                            )
                        )
                            .Write((JsonConvert.SerializeXmlNode(doc)))


                        Case Else
                            .ContentType = "text/xml"
                            Using objX As New XmlTextWriter(.OutputStream, Nothing)
                                With objX
                                    .WriteStartDocument()
                                    .WriteNode(
                                        ExecuteXmlReader(
                                            String.Format(
                                                "use {0}; {1}",
                                                requestEnv,
                                                Statement
                                            )
                                        ),
                                        True
                                    )
                                    .WriteEndDocument()
                                End With
                            End Using

                    End Select

                Catch exep As Exception
                    Throw (exep)

                End Try

            End With

        End Sub

        Private Function Statement()

            If xmlSP = 0 Then

                Dim sqlString As String = Query()
                Dim GETRequest As New GetParams()

                ' --- 20/04/2013 - si
                ' --- Impliment SQL parameters from the request string.

                ' --- 09/03/2017 - si
                ' --- Added support for non-character data types in request.

                ' Check the .sql for mandatory fields
                Dim Mandatory As Regex = New Regex(
            "declare.*@.*--.*mandatory",
            RegexOptions.IgnoreCase
        )
                ' Iterate through the mandatory fields
                For Each m As Match In Mandatory.Matches(sqlString)
                    Dim expectedVar As String =
                Trim(m.Value.Substring(m.Value.IndexOf("@") + 1).Split(" ")(0))

                    ' If mandatory parameter is missing then throw an error
                    If Not GETRequest.Keys.Contains(expectedVar) Then
                        Throw New Exception(
                    String.Format(
                        "The {0} parameter is mandatory.",
                        expectedVar
                    )
                )
                    ElseIf GETRequest(expectedVar).Length = 0 Then
                        Throw New Exception(
                    String.Format(
                        "The {0} parameter is mandatory.",
                        expectedVar
                    )
                )
                    End If
                Next

                ' Iterate through the parameters in the request string
                For Each k As String In GETRequest.Keys
                    Dim declaration As Regex = New Regex(
                String.Format(
                    "declare.*@{0}.*",
                    k
                ),
                RegexOptions.IgnoreCase
            )
                    ' Check the .SQL for matching DECLARE statements
                    If declaration.IsMatch(sqlString) Then

                        ' Get the parameter name from the DECLARE statement
                        Dim MatchVal As String = declaration.Match(sqlString).Value
                        Dim VarName As String = Trim(
                    MatchVal.Substring(MatchVal.IndexOf("@")).Split(" ")(0)
                )

                        ' Get the type of the variable
                        Dim vType As Regex = New Regex(
                    "[A-Za-z]+"
                )
                        Dim VarType As String = vType.Matches(Split(MatchVal, VarName)(1))(0).Value.ToLower

                        ' Find the SET statement
                        Dim SetStatement As Regex = New Regex(
                    String.Format(
                        "set.*@{0} .*=.*",
                        k
                    ),
                    RegexOptions.IgnoreCase
                )

                        ' Find the IN statement
                        Dim inStatement As Regex = New Regex(
                    String.Format(
                        "in.*\(.*@{0}.*?\)",
                        k
                    ),
                    RegexOptions.IgnoreCase
                )

                        ' Find the Count statement
                        Dim CountStatement As Regex = New Regex(
                    String.Format(
                        "set.*@{0}count.*=.*[0-9]+",
                        k
                    ),
                    RegexOptions.IgnoreCase
                )

                        If SetStatement.Match(sqlString).Success Then
                            ' Update the SET statement for this parameter
                            sqlString = SetStatement.Replace(
                        sqlString,
                        String.Format(
                            "set {0} = {1}",
                            VarName,
                            Apostrophe(GETRequest(k), VarType)
                        )
                    )

                        ElseIf inStatement.Match(sqlString).Success Then
                            sqlString = inStatement.Replace(
                        sqlString,
                        String.Format(
                            "in ({0})",
                            Apostrophe(GETRequest(k), VarType)
                        )
                    )
                            sqlString = CountStatement.Replace(
                        sqlString,
                        String.Format(
                            "set @{0}count = {1}",
                            k,
                            UBound(GETRequest(k).Split(",")) + 1
                        )
                    )

                        End If
                    End If
                Next
                Return sqlString

            Else

                Dim sqlString As New StringBuilder
                Dim param As Dictionary(Of String, String) = spParams()
                For Each p In param.Keys
                    If HttpContext.Current.Request(p) Is Nothing Then
                        Throw New Exception(
                        String.Format(
                            "The '{0}' parameter is mandatory.",
                            p
                        )
                    )
                    End If
                Next

                sqlString.AppendFormat("SELECT {0}.{1}(", Name, requestEndpoint)
                For Each p In param.Keys
                    Select Case param(p).ToLower
                        Case "char", "varchar", "text", "nchar", "nvarchar", "ntext"
                            sqlString.AppendFormat("'{0}'", HttpContext.Current.Request(p))

                        Case Else
                            sqlString.Append(HttpContext.Current.Request(p))

                    End Select

                    If Not String.Compare(param.Last.Key, p) = 0 Then
                        sqlString.Append(", ")
                    End If
                Next
                sqlString.Append(") ")

                Return sqlString.ToString

            End If

        End Function

        Public Sub Install(ByVal context As HttpContext)
            Dim q As String
            With log.LogData
                .AppendFormat("Installing SQL from feed {0}.", Name).AppendLine()
                Try
                    q = InstallQuery()

                Catch ex As NotSupportedException
                    .AppendFormat("Install is not supported by this feed.").AppendLine()
                    Exit Sub

                Catch ex As Exception
                    Throw ex

                End Try

                For Each statement As String In Split(q, vbCrLf & "go" & vbCrLf,, CompareMethod.Text)
                    .AppendFormat("use {0}; {1}", requestEnv, statement).AppendLine()
                    ExecuteNonQuery(String.Format("use {0}; {1}", requestEnv, statement))
                Next

            End With

        End Sub

#End Region

#Region "Private Functions"

        Function Apostrophe(Value As String, vType As String)
            Dim sb As New StringBuilder
            Dim first As Boolean = True
            For Each str As String In Split(Value, ",")
                Select Case vType
                    Case "char", "varchar", "text", "nchar", "nvarchar", "ntext"
                        If Not first Then sb.Append(",")
                        sb.Append(String.Format("'{0}'", str))

                    Case Else
                        If Not first Then sb.Append(",")
                        sb.Append(String.Format("{0}", str))

                End Select
                first = False
            Next

            Return sb.ToString

        End Function

#End Region

    End Class

End Namespace
