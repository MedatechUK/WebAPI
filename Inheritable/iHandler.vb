Imports System.IO
Imports System.Text
Imports System.Web
Imports System.Xml
Imports Newtonsoft.Json

Namespace Web

    Public MustInherit Class iHandler : Inherits EndPoint

#Region "Metadata"

        Public Sub SetMeta(ByRef Metadata As xmlHandlerProps)
            With Metadata
                Name = .EndPoint
                Hidden = .Hidden
            End With

        End Sub

#End Region

#Region "Overridable stubs"

        Public Overridable Sub XmlStylesheet(ByRef Schemas As Schema.XmlSchemaSet)

        End Sub

        Public Overridable Sub XMLHandler(ByRef w As XmlTextWriter, ByRef Request As XmlDocument)
            Throw New NotImplementedException()
        End Sub

        Public Overridable Sub StreamHandler(ByRef w As XmlTextWriter, ByRef Request As StreamReader)
            Throw New NotImplementedException()
        End Sub

        Public Overridable Sub jsonHandler(ByRef w As JsonWriter, ByRef json As String)
            Throw New NotImplementedException()
        End Sub

#End Region

#Region "Process Request"

        Dim _thisRequest As New XmlDocument
        Public Overrides Sub ProcessRequest(ByVal context As HttpContext)

            Dim x As String
            Dim reader As StreamReader = Nothing

            With context
                Try
                    reader = New StreamReader(.Request.InputStream)
                    With reader
                        x = reader.ReadToEnd
                        While Not (String.Compare(x.Substring(0, 1), "<") = 0 Or String.Compare(x.Substring(0, 1), "{") = 0)
                            x = x.Substring(1)
                        End While
                        If Not x.Length > 0 Then
                            Throw New Exception("Invalid data.")
                        End If
                        .BaseStream.Position = 0
                    End With

                    With .Response
                        Select Case apiLang
                            Case eLang.json
                                .ContentType = "text/json"
                                setXml(String.Format("<json><![CDATA[{0}]]></json>", x))

                                Using strm As New StreamWriter(.OutputStream)
                                    Using objx As New JsonTextWriter(strm)
                                        With objx
                                            log.LogData.Append("Passing to JSON handler.").AppendLine()
                                            jsonHandler(objx, x)

                                        End With
                                    End Using

                                End Using

                            Case Else
                                .ContentType = "text/xml"
                                setXml(x)

                                ' Add XSDs
                                Dim ret As New XmlReaderSettings()
                                XmlStylesheet(ret.Schemas)
                                If ret.Schemas.Count > 0 Then
                                    ret.ValidationType = ValidationType.Schema
                                    log.LogData.AppendFormat("{0} XSD(s) loaded.", _thisRequest.Schemas.Schemas.Count + 1).AppendLine()
                                    reader.BaseStream.Position = 0

                                    ' And load
                                    _thisRequest.Load(XmlReader.Create(reader, ret))

                                Else
                                    _thisRequest.LoadXml(x)

                                End If

                                Using objX As New XmlTextWriter(.OutputStream, Nothing)
                                    With objX
                                        .WriteStartDocument()

                                        Try
                                            reader.BaseStream.Position = 0
                                            log.LogData.Append("Passing to stream handler.").AppendLine()
                                            StreamHandler(objX, reader)

                                        Catch ex As NotImplementedException
                                            Try
                                                log.LogData.Append("Passing to XML Document handler.").AppendLine()
                                                XMLHandler(objX, _thisRequest)

                                            Catch ex2 As NotImplementedException
                                                Throw New NotImplementedException

                                            End Try
                                        End Try

                                        .WriteEndDocument()

                                    End With

                                End Using

                        End Select

                    End With

                Catch ex As Exception
                    Throw (ex)

                End Try

            End With

        End Sub

#End Region

    End Class

End Namespace