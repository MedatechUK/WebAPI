Imports System.IO
Imports System.Net
Imports System.Reflection
Imports Newtonsoft.Json
Imports MedatechUK.Web

Namespace oData


#Region "Attributes"

    <System.AttributeUsage(System.AttributeTargets.Class)>
    Public Class oRowClass
        Inherits System.Attribute
        Public formType As Type = Nothing
        Public serialType As Type = Nothing

        Private _FormName As String = String.Empty
        Public ReadOnly Property FormName As String
            Get
                Return _FormName
            End Get
        End Property

        Private _SerialName As String = String.Empty
        Public ReadOnly Property SerialName As String
            Get
                Return _SerialName
            End Get
        End Property

        Sub New(ByVal FormName As String, ByVal SerialName As String)

            _FormName = FormName
            _SerialName = SerialName

        End Sub

        Sub Load(ByRef Sender As Assembly)

            For Each ay In Sender.ExportedTypes
                If String.Compare(ay.Name, FormName) = 0 Then
                    formType = ay

                End If
                If String.Compare(ay.Name, SerialName) = 0 Then
                    serialType = ay

                End If

                If Not formType Is Nothing And Not serialType Is Nothing Then
                    Exit For

                End If

            Next

        End Sub

    End Class

    <System.AttributeUsage(System.AttributeTargets.Property)>
    Public Class oDataColumn
        Inherits System.Attribute
        Public title As String
        Public length As Integer
        Public mandatory As Boolean
        Public key As Boolean
        Public [ReadOnly] As Boolean

        Sub New(ByVal columnTitle As String)
            title = columnTitle
            length = 0
            mandatory = False
            key = False
            [ReadOnly] = False
        End Sub

    End Class

#End Region

    ''' <summary>
    ''' Defines a Priorty Form Row for oData.
    ''' </summary>
    Public MustInherit Class oRow : Inherits Dictionary(Of String, Object)

#Region "Local Variables"

        Private _Parent As oForm = Nothing
        Private changes As New Dictionary(Of String, oDataColumn)

        Private _formType As Type
        Private _serialType As Type

#End Region

#Region "Ctor"

        ''' <summary>    
        ''' Row Constructor method.
        ''' </summary>
        ''' <param name="Parent">oForm</param>
        Public Sub New(Parent As oForm)

            _Parent = Parent
            With Me.GetType

                Dim r As oRowClass = .GetCustomAttribute(Of oRowClass)
                If r Is Nothing Then
                    Throw New Exception(
                        String.Format(
                            "Missing oRowClass Attribute on row class {0}",
                            .Name
                        )
                    )

                End If

                r.Load(_Parent.Sender)
                If r.formType Is Nothing Then
                    Throw New Exception(
                        String.Format(
                            "Form definition {0} was not found",
                            r.FormName
                        )
                    )

                ElseIf r.serialType Is Nothing Then
                    Throw New Exception(
                        String.Format(
                            "Serial definition {0} was not found",
                            r.SerialName
                        )
                    )

                Else
                    _formType = r.formType
                    _serialType = r.serialType

                End If

                ' Load Sub forms
                Dim sf As oFormClass = _formType.GetCustomAttribute(Of oFormClass)
                If Not sf Is Nothing Then
                    For Each k As String In sf.subForms
                        Dim f As Boolean = False
                        For Each ay In _Parent.Sender.ExportedTypes
                            If String.Compare(ay.Name, k) = 0 Then
                                SubForms.Add(
                                    k,
                                    Activator.CreateInstance(
                                        ay,
                                        _Parent.Sender,
                                        Me
                                    )
                                )

                                f = True
                                Exit For

                            End If

                        Next

                        If Not f Then
                            Throw New Exception(
                                String.Format(
                                    "Subform definition {0} was not found",
                                    r.SerialName
                                )
                            )

                        End If

                    Next

                End If

                ' Load Properties
                For Each I In .GetProperties()
                    Dim col As oDataColumn = I.GetCustomAttribute(Of oDataColumn)
                    If Not col Is Nothing Then
                        Select Case I.PropertyType.Name.ToLower
                            Case "DateTime".ToLower
                                Add(I.Name, Convert.ChangeType(New DateTime(0), I.PropertyType))

                            Case "Decimal".ToLower, "Int32".ToLower
                                Add(I.Name, Convert.ChangeType(0, I.PropertyType))

                            Case Else
                                Add(I.Name, Convert.ChangeType(Nothing, I.PropertyType))

                        End Select

                    End If

                Next

            End With

        End Sub

#End Region

#Region "Get / Set Row columns"

        ''' <summary>
        ''' Set the value of the property.
        ''' </summary>
        ''' <param name="Value">The new value for the property.</param>
        ''' <param name="memberName">Optional member name obtained through reflection.</param>
        Public Sub setProperty(Value As Object, <System.Runtime.CompilerServices.CallerMemberName> Optional memberName As String = Nothing)
            If Not memberName Is Nothing Then
                With Me.GetType().GetProperty(memberName)
                    Dim X = .GetCustomAttribute(Of oDataColumn)
                    Try
                        If .GetType.ToString = "String" Then
                            If Convert.ChangeType(Value, .PropertyType, Nothing).length > X.length Then
                                Throw New Exception(
                            String.Format(
                                "must be <{0} chars",
                                X.length + 1.ToString
                            )
                        )

                            End If

                        End If

                        Me(memberName) = Convert.ChangeType(Value, .PropertyType, Nothing)
                        If Not changes.Keys.Contains(memberName) Then
                            changes.Add(memberName, X)

                        End If

                    Catch ex As Exception
                        Throw New Exception(
                        String.Format(
                            "Column [{0}], ""{1}"": {2}",
                            memberName,
                            X.title,
                            ex.Message
                        )
                    )

                    End Try

                End With

            End If

        End Sub

        ''' <summary>
        ''' Get the value of the property.
        ''' </summary>
        ''' <param name="memberName"></param>
        ''' <returns>Optional member name obtained through reflection.</returns>
        Public Function getProperty(<System.Runtime.CompilerServices.CallerMemberName> Optional memberName As String = Nothing) As Object
            If Not memberName Is Nothing Then
                Return Me(memberName)

            Else
                Return Nothing

            End If

        End Function

#End Region

#Region "Properties"

        ''' <summary>
        ''' Returns the parent form of this row.
        ''' </summary>
        ''' <returns>oForm</returns>
        Public ReadOnly Property Parent As oForm
            Get
                Return _Parent
            End Get
        End Property

        ''' <summary>
        ''' A Dictionary containing sub forms of this row.
        ''' </summary>
        Public SubForms As New Dictionary(Of String, oForm)

        ''' <summary>
        ''' Returns the form Type that the row belogs to.
        ''' </summary>
        ''' <returns>Type</returns>
        Public ReadOnly Property FormType As Type
            Get
                Return _formType
            End Get
        End Property

        ''' <summary>
        ''' Returns the key for this row.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Key As String
            Get
                Dim k As New List(Of String)
                For Each I In Me.GetType().GetProperties()
                    Dim col As oDataColumn = I.GetCustomAttribute(Of oDataColumn)
                    If Not col Is Nothing Then
                        If col.key Then
                            Select Case I.PropertyType.Name.ToLower
                                Case "DateTime".ToLower
                                    k.Add(String.Format("{0}={1}", I.Name, Uri.EscapeDataString(CDate(Me(I.Name)).ToUniversalTime().ToString("s") + "Z")))

                                Case "Decimal".ToLower, "Int32".ToLower
                                    k.Add(String.Format("{0}={1}", I.Name, Me(I.Name)))

                                Case Else
                                    k.Add(String.Format("{0}='{1}'", I.Name, Uri.EscapeDataString(Me(I.Name))))

                            End Select

                        End If

                    End If

                Next

                Dim ret As New Text.StringBuilder
                ret.Append("(")
                For Each i As String In k
                    ret.Append(i)
                    If Not String.Compare(i, k.Last) = 0 Then
                        ret.Append(",")
                    End If
                Next
                ret.Append(")")
                Return ret.ToString

            End Get
        End Property

        ''' <summary>
        ''' Return the uri path for this row, 
        ''' including recusive parentage.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property Path As String
            Get
                Dim str As String = String.Format(
                "/{0}{1}",
                _formType.Name,
                subForm(Me)
            )

                Dim o As oRow = Me.Parent.Parent
                While Not o Is Nothing
                    str = String.Format(
                    "/{0}{1}{2}{3}",
                    o.FormType.Name,
                    subForm(o),
                    o.Key,
                    str
                )
                    o = o.Parent.Parent

                End While

                Return str

            End Get

        End Property

        Private Function subForm(o As oRow) As String
            If o.Parent.Parent Is Nothing Then
                Return ""
            Else
                Return "_SUBFORM"
            End If
        End Function

        ''' <summary>
        ''' Contains the last exception from posting this row.
        ''' </summary>
        ''' <returns>oException</returns>
        Public Property Exception As Exception


        ''' <summary>
        ''' Get / Set the log for this transaction from HttpContext.
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

#End Region

#Region "Post"

        ''' <summary>
        ''' Returns a MemoryStream containing oData formatted data for this row.
        ''' </summary>
        ''' <returns>MemoryStream</returns>
        Private ReadOnly Property RequestStream As MemoryStream
            Get
                Using t As New StringWriter()
                    Using objx As New JTextWriter(t)
                        With objx
                            .WriteStartObject()
                            For Each ch As String In changes.Keys
                                If Not changes(ch).ReadOnly Then
                                    .WriteAttributeObject(ch, getProperty(ch))

                                End If
                            Next
                            .WriteEndObject()
                        End With

                        Log.LogData.AppendFormat("{0}", t.ToString).AppendLine()

                        Dim myEncoder As New System.Text.ASCIIEncoding
                        Return New MemoryStream(myEncoder.GetBytes(t.ToString))

                    End Using

                End Using

            End Get
        End Property

        ''' <summary>
        ''' Send the content of the requeststream to the oData server.
        ''' Returns a boolean value indicating POST success.
        ''' </summary>
        ''' <returns>Boolean</returns>
        Public Function Post() As Boolean

            System.Net.ServicePointManager.ServerCertificateValidationCallback =
          Function(se As Object,
          cert As System.Security.Cryptography.X509Certificates.X509Certificate,
          chain As System.Security.Cryptography.X509Certificates.X509Chain,
          sslerror As System.Net.Security.SslPolicyErrors) True

            Dim e As Object
            Using client As New oClient(Me.Path, "POST")
                e = client.GetResponse(RequestStream)

            End Using

            If TypeOf (e) Is oException Then
                With TryCast(e, oException)
                    If .StatusCode = HttpStatusCode.Conflict Then
                        log.LogData.AppendFormat(
                            "[{0}] {1}. PATCHing instead.",
                            CInt(.StatusCode),
                            .StatusCode.ToString
                        ).AppendLine()

                        Using client As New oClient(Me.Path, "PATCH")
                            e = client.GetResponse(RequestStream)
                        End Using

                    End If
                End With
            End If

            Select Case TypeOf (e) Is Exception
                Case False
                    Dim o As Object
                    Using reader As New StreamReader(
                        TryCast(e, Net.WebResponse).GetResponseStream
                    )
                        o = JsonConvert.DeserializeObject(
                        reader.ReadToEnd(),
                        _serialType
                    )

                    End Using

                    For Each i In o.GetType.GetProperties
                        If Not i.GetValue(o) Is Nothing Then
                            If Keys.Contains(i.Name) Then
                                Me(i.Name) = i.GetValue(o)
                            End If

                        End If

                    Next

                    changes.Clear()
                    Exception = Nothing

                    Return True

                Case Else
                    Exception = e
                    log.LogData.AppendFormat(Exception.Message).AppendLine()

                    Return False

            End Select

        End Function

        ''' <summary>
        ''' Send the content of the requeststream to the oData server.
        ''' Returns a boolean value indicating PATCH success.
        ''' </summary>
        ''' <returns>Boolean</returns>
        Public Function Patch() As Boolean

            System.Net.ServicePointManager.ServerCertificateValidationCallback =
          Function(se As Object,
          cert As System.Security.Cryptography.X509Certificates.X509Certificate,
          chain As System.Security.Cryptography.X509Certificates.X509Chain,
          sslerror As System.Net.Security.SslPolicyErrors) True

            Dim e As Object
            Using client As New oClient(
                String.Format(
                    "{0}{1}",
                    Me.Path,
                    Me.Key
                ),
            "PATCH")

                e = client.GetResponse(RequestStream)

            End Using

            Select Case TypeOf (e) Is Exception
                Case False
                    Dim o As Object
                    Using reader As New StreamReader(
                        TryCast(e, Net.WebResponse).GetResponseStream
                    )
                        o = JsonConvert.DeserializeObject(
                        reader.ReadToEnd(),
                        _serialType
                    )

                    End Using

                    For Each i In o.GetType.GetProperties
                        If Not i.GetValue(o) Is Nothing Then
                            If Keys.Contains(i.Name) Then
                                Me(i.Name) = i.GetValue(o)
                            End If

                        End If

                    Next

                    changes.Clear()
                    Exception = Nothing

                    Return True

                Case Else
                    Exception = e
                    log.LogData.AppendFormat(Exception.Message).AppendLine()

                    Return False

            End Select

        End Function


        ''' <summary>
        ''' Send the content of the requeststream to the oData server.
        ''' Returns a boolean value indicating PATCH success.
        ''' </summary>
        ''' <returns>Boolean</returns>
        Public Function [Get]() As Boolean

            System.Net.ServicePointManager.ServerCertificateValidationCallback =
          Function(se As Object,
          cert As System.Security.Cryptography.X509Certificates.X509Certificate,
          chain As System.Security.Cryptography.X509Certificates.X509Chain,
          sslerror As System.Net.Security.SslPolicyErrors) True

            Dim e As Object
            Using client As New oClient(
                String.Format(
                    "{0}{1}",
                    Me.Path,
                    Me.Key
                ),
            "GET")

                e = client.GetResponse()

            End Using

            Select Case TypeOf (e) Is Exception
                Case False
                    Dim o As Object
                    Using reader As New StreamReader(
                        TryCast(e, Net.WebResponse).GetResponseStream
                    )
                        o = JsonConvert.DeserializeObject(
                        reader.ReadToEnd(),
                        _serialType
                    )

                    End Using

                    For Each i In o.GetType.GetProperties
                        If Not i.GetValue(o) Is Nothing Then
                            If Keys.Contains(i.Name) Then
                                Me(i.Name) = i.GetValue(o)
                            End If

                        End If

                    Next

                    changes.Clear()
                    Exception = Nothing

                    Return True

                Case Else
                    Exception = e
                    log.LogData.AppendFormat(Exception.Message).AppendLine()

                    Return False

            End Select

        End Function

#End Region

    End Class

End Namespace