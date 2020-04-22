Imports System.IO
Imports System.Net
Imports System.Web.Configuration
Imports System.Xml
Imports System.Xml.Serialization
Imports MedatechUK.Web

Namespace oData

    ''' <summary>
    ''' An oData client
    ''' </summary>
    Public Class oClient : Implements IDisposable

        ''' <summary>
        ''' Get / Set the log for this transaction.
        ''' </summary>
        ''' <returns>oMsgLog</returns>
        Public Property log As oMsgLog
            Get
                Return TryCast(HttpContext.Current.Items("log"), oMsgLog)
            End Get
            Set(value As oMsgLog)
                HttpContext.Current.Items("log") = value
            End Set
        End Property

        Private _Request As Net.HttpWebRequest


        ''' <summary>
        ''' oClient constructor method.
        ''' </summary>
        ''' <param name="Path">String</param>
        ''' <param name="Method">String</param>
        Sub New(Path As String, Optional Method As String = "POST")

            Dim uri As New UriBuilder
            Try
                With uri
                    .Scheme = Split(WebConfigurationManager.AppSettings("oDataHost"), "://")(0)
                    .Host = Split(WebConfigurationManager.AppSettings("oDataHost"), "://")(1)
                    .Path = String.Format(
                            "/odata/Priority/{0}/{1}{2}",
                            WebConfigurationManager.AppSettings("tabulaini"),
                            HttpContext.Current.Request("environment"),
                            Path
                        )

                    log.LogData.AppendFormat("{0} ROOT{1}", Method.ToUpper, Path).AppendLine()

                    _Request = CType(Net.HttpWebRequest.Create(uri.Uri), Net.HttpWebRequest)
                    With _Request
                        .Method = Method
                        .Proxy = Nothing
                        .UserAgent = "Medatech .net oData Client"
                        .ContentType = "application/json"
                        .Credentials = New NetworkCredential(
                                WebConfigurationManager.AppSettings("ouser"),
                                WebConfigurationManager.AppSettings("opass")
                            )

                    End With

                End With

            Catch ex As Exception
                Throw New Exception("Invalid oData configuration.")

            End Try

        End Sub

        ''' <summary>
        ''' Returns a WebResponse or an exception in 
        ''' response to the Requested MemoryStream.
        ''' </summary>
        ''' <param name="Request"></param>
        ''' <returns>Object</returns>
        Public Function GetResponse(Optional ByRef Request As MemoryStream = Nothing) As Object

            Dim e As Object
            Dim buffer(1024) As Byte
            Dim bytesRead As Integer

            System.Net.ServicePointManager.ServerCertificateValidationCallback =
          Function(se As Object,
          cert As System.Security.Cryptography.X509Certificates.X509Certificate,
          chain As System.Security.Cryptography.X509Certificates.X509Chain,
          sslerror As System.Net.Security.SslPolicyErrors) True

            With _Request
                Try
                    If Not Request Is Nothing Then
                        .ContentLength = Request.Length
                        Using requestStream As Stream = .GetRequestStream()
                            With requestStream
                                While True
                                    bytesRead = Request.Read(buffer, 0, buffer.Length)
                                    If bytesRead = 0 Then
                                        Exit While

                                    End If
                                    .Write(buffer, 0, bytesRead)

                                End While

                            End With

                        End Using

                    End If

                    e = .GetResponse()

                Catch ex As WebException
                    With ex
                        If TryCast(.Response, HttpWebResponse) Is Nothing Then
                            e = New Exception(ex.Status.ToString)

                        Else
                            Dim str As String
                            With .Response
                                Using reader As New StreamReader(.GetResponseStream)
                                    str = reader.ReadToEnd()
                                End Using
                            End With

                            Try
                                Dim lder As New XmlDocument
                                lder.LoadXml(str)
                                With TryCast(.Response, HttpWebResponse)
                                    e = New Exception(
                                        String.Format("{1}",
                                            CInt(.StatusCode).ToString,
                                            lder.SelectSingleNode(
                                                "FORM/InterfaceErrors/text"
                                            ).InnerText
                                        )
                                    )

                                End With

                            Catch xmlex As Exception
                                If Len(str) > 0 Then
                                    If _Request.Method.ToUpper = "GET" And TryCast(.Response, HttpWebResponse).StatusCode = HttpStatusCode.InternalServerError Then
                                        With TryCast(.Response, HttpWebResponse)
                                            e = New oException(
                                                HttpStatusCode.NotFound,
                                                String.Format("[{0}] {1}",
                                                    CInt(HttpStatusCode.NotFound).ToString,
                                                    HttpStatusCode.NotFound.ToString
                                                )
                                            )
                                        End With

                                    Else

                                        Select Case TryCast(.Response, HttpWebResponse).StatusCode
                                            Case HttpStatusCode.NotFound
                                                With TryCast(.Response, HttpWebResponse)
                                                    e = New oException(
                                                    .StatusCode,
                                                    String.Format("[{0}] {1}",
                                                        CInt(.StatusCode).ToString,
                                                        .StatusDescription
                                                    )
                                                )
                                                End With

                                            Case Else
                                                With TryCast(.Response, HttpWebResponse)
                                                    e = New oException(
                                                    .StatusCode,
                                                    String.Format("[{0}] {1}",
                                                        CInt(.StatusCode).ToString,
                                                        .StatusDescription
                                                    )
                                                )
                                                End With

                                        End Select

                                    End If

                                Else
                                    With TryCast(.Response, HttpWebResponse)
                                        e = New oException(
                                        .StatusCode,
                                        String.Format("[{0}] {1}",
                                            CInt(.StatusCode).ToString,
                                            .StatusDescription
                                        )
                                    )
                                    End With

                                End If

                            End Try

                        End If

                    End With


                Catch ex As Exception
                    e = ex

                End Try

            End With

            Return e

        End Function

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
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