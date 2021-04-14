Imports System.Data.SqlClient
Imports System.Text
Imports System.Web

Friend Class sqlXML
    Inherits Dictionary(Of String, String)
    Implements IDisposable

    Private _ObjectID As Integer
    Private _ObjectName As String
    Private _e As EndPoint

    Sub New(ByRef e As EndPoint, ByRef cn As SqlConnection)

        _e = e

        Using command As New SqlCommand(
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
                "And (LOWER(SO.name)=LOWER('{1}')) " &
                "And P.is_output=1 ",
                e.requestEnv,
                e.requestEndpoint
                ), cn
            )
            Using rs As SqlDataReader = command.ExecuteReader
                If rs.HasRows Then
                    While rs.Read
                        _ObjectName = rs("ObjectName")
                        _ObjectID = rs("ObjectID")

                    End While
                Else
                    Throw New NotImplementedException

                End If

            End Using

        End Using

        Using command As New SqlCommand(
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
                e.requestEnv,
                _ObjectID
                ), cn
            )
            Using rs As SqlDataReader = command.ExecuteReader
                If rs.HasRows Then
                    While rs.Read
                        Me.Add(rs("ParameterName").substring(1), rs("ParameterDataType"))
                    End While

                End If

            End Using

        End Using

    End Sub

    Public ReadOnly Property Command(cn As SqlConnection) As SqlCommand
        Get
            Dim sqlString As New StringBuilder
            For Each p In Me.Keys
                If HttpContext.Current.Request(p) Is Nothing Then
                    Throw New Exception(
                        String.Format(
                            "The '{0}' parameter is mandatory.",
                            p
                        )
                    )
                End If
            Next

            sqlString.AppendFormat("use [{0}]; SELECT {1}(", _e.requestEnv, _ObjectName)
            For Each p In Me.Keys
                Select Case Me(p).ToLower
                    Case "char", "varchar", "text", "nchar", "nvarchar", "ntext"
                        sqlString.AppendFormat("'{0}'", HttpContext.Current.Request(p))

                    Case Else
                        sqlString.Append(HttpContext.Current.Request(p))

                End Select

                If Not String.Compare(Me.Last.Key, p) = 0 Then
                    sqlString.Append(", ")
                End If

            Next
            sqlString.Append(") ")

            Return New SqlCommand(sqlString.ToString, cn)

        End Get
    End Property

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
